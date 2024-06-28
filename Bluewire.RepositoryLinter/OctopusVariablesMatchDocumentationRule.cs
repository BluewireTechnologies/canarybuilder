using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Language;
using System.Xml.Linq;
using Bluewire.Build.DeploymentReadmeParser;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.RepositoryLinter.ReadmeValidator;
using NUnit.Framework;
using Octostache;
using Octostache.Templates;
using Parser = Bluewire.Build.DeploymentReadmeParser.Parser;

namespace Bluewire.RepositoryLinter;

public partial class OctopusVariablesMatchDocumentationRule
{
    private readonly SubjectRepository subject;
    private readonly IGitFilesystemContext workingCopyOrRepo;
    private readonly GitSession session;

    public OctopusVariablesMatchDocumentationRule(SubjectRepository subject, IGitFilesystemContext workingCopyOrRepo, GitSession session)
    {
        this.subject = subject;
        this.workingCopyOrRepo = workingCopyOrRepo;
        this.session = session;
    }

    public IEnumerable<Failure> GetFailures(Ref branch, ImmutableArray<ProjectFile> projects)
    {
        if (!subject.GetBranchRules(branch).CheckOctopusVariablesMatchDocumentation) yield break;

        foreach (var project in projects)
        {
            if (project.ReadMePath == null) continue;
            if (!project.Packages.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x.Name,  "OctoPack"))) continue; // Not an Octopus package.

            var validator = new Validator(subject, workingCopyOrRepo, session, branch, project);
            validator.VisitConfiguration();
            validator.VisitDeploymentScripts();
            if (validator.VisitReadMe())
            {
                validator.CorrelateVariables();
            }

            foreach (var failure in validator.Failures) yield return failure;
        }
    }

    class Validator
    {
        private readonly SubjectRepository subject;
        private readonly Ref branch;
        private readonly ProjectFile project;

        private readonly Parser readmeParser = new Parser();
        private readonly GitDocumentSource source;


        public Validator(SubjectRepository subject, IGitFilesystemContext workingCopyOrRepo, GitSession session, Ref branch, ProjectFile project)
        {
            this.subject = subject;
            this.branch = branch;
            this.project = project;
            source = new GitDocumentSource(workingCopyOrRepo, session, branch);
        }

        public List<Failure> Failures { get; } = new List<Failure>();

        public HashSet<Variable> ConfigurationVariables { get; } = new HashSet<Variable>();
        public HashSet<string> MaybeDeploymentVariables { get; } = new HashSet<string>();
        public HashSet<string> DocumentedVariables { get; } = new HashSet<string>();

        private void RecordFailure(string message)
        {
            Failures.Add(new Failure
            {
                Subject = subject,
                Message = message,
                Branch = branch,
                ProjectFile = project,
            });
        }

        /// <summary>
        /// Parse the README.md to find documentation of Octopus variables.
        /// </summary>
        /// <returns>False if the README.md was present but could not be parsed, or true otherwise.</returns>
        public bool VisitReadMe()
        {
            if (project.ReadMePath == null) return true;

            var deploymentSubject = new DeploymentSubject(source) { ApplicationReadmePath = source.CreateUri(project.ReadMePath).LocalPath };

            try
            {
                var model = readmeParser.Parse(deploymentSubject);
                DocumentedVariables.UnionWith(model.Variables.Keys);
                return true;
            }
            catch (Exception ex)
            {
                RecordFailure(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Find possible references to Octopus variables in configuration files and transforms.
        /// </summary>
        public void VisitConfiguration()
        {
            foreach (var path in project.ConfigurationPaths)
            {
                VisitConfigurationPath(path);
            }
        }

        private void VisitConfigurationPath(string path)
        {
            VisitOctostacheTemplate(path);
            VisitAppSettings(path);
        }

        private void VisitAppSettings(string path)
        {
            if (!TryLoadXml(path, out var xml)) return;

            var appSettingsKeys = xml.Descendants("appSettings").Elements("add").Attributes("key");
            ConfigurationVariables.UnionWith(appSettingsKeys.Select(x => new Variable(x.Value, true)));
            var connectionStringsNames = xml.Descendants("connectionStrings").Elements("add").Attributes("name");
            ConfigurationVariables.UnionWith(connectionStringsNames.Select(x => new Variable(x.Value, true)));
        }

        private bool TryLoadXml(string path, [MaybeNullWhen(false)] out XDocument xml)
        {
            try
            {
                using (var reader = source.Open(source.CreateUri(path)))
                {
                    xml = XDocument.Load(reader);
                    return true;
                }
            }
            catch
            {
                // Not XML?
                xml = null;
                return false;
            }
        }

        private void VisitOctostacheTemplate(string path)
        {
            try
            {
                using (var reader = source.Open(source.CreateUri(path)))
                {
                    var maybeTemplate = reader.ReadToEnd();
                    if (!TemplateParser.TryParseTemplate(maybeTemplate, out var template, out var error, true))
                    {
                        RecordFailure($"Failed to parse configuration file '{path}': {error}");
                        return;
                    }
                    var collector = new SymbolCollector();
                    var variables = template.Tokens.SelectMany(collector.Collect).Distinct().ToHashSet();
                    ConfigurationVariables.UnionWith(variables.Select(x => new Variable(x, false)));
                }
            }
            catch (Exception ex)
            {
                RecordFailure($"Error while parsing configuration file '{path}': {ex.Message}");
            }
        }

        /// <summary>
        /// Find possible references to Octopus variables in PowerShell scripts.
        /// </summary>
        /// <remarks>
        /// These are only considered to be definitely variables if they also appear in documentation or configuration.
        /// </remarks>
        public void VisitDeploymentScripts()
        {
            foreach (var path in project.DeploymentScriptPaths)
            {
                VisitDeploymentScriptPath(path);
            }
        }

        private void VisitDeploymentScriptPath(string path)
        {
            try
            {
                using (var reader = source.Open(source.CreateUri(path)))
                {
                    var scriptSource = reader.ReadToEnd();
                    System.Management.Automation.Language.Parser.ParseInput(scriptSource, out var tokens, out var errors);
                    if (errors.Any())
                    {
                        foreach (var error in errors)
                        {
                            RecordFailure($"{path}: {error.Message}");
                        }
                        return;
                    }
                    MaybeDeploymentVariables.UnionWith(tokens.OfType<VariableToken>().Select(x => x.Name));
                    MaybeDeploymentVariables.UnionWith(tokens.OfType<StringLiteralToken>().Select(x => x.Value));
                    MaybeDeploymentVariables.UnionWith(tokens.OfType<StringExpandableToken>().Select(x => x.Value));
                }
            }
            catch (Exception ex)
            {
                RecordFailure($"Error while parsing PowerShell file '{path}': {ex.Message}");
            }
        }

        public void CorrelateVariables()
        {
            var probablyDeploymentVariables = MaybeDeploymentVariables
                .Intersect(ConfigurationVariables.Select(x => x.Name).Union(DocumentedVariables), StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var undocumentedVariables = ConfigurationVariables
                .Where(x => !x.IsOptionalVariable)
                .Select(x => x.Name)
                .Except(DocumentedVariables, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            foreach (var x in undocumentedVariables)
            {
                RecordFailure($"Variable is undocumented: {x}");
            }

            var unusedVariables = DocumentedVariables
                .Except(ConfigurationVariables.Select(x => x.Name), StringComparer.OrdinalIgnoreCase)
                .Except(probablyDeploymentVariables, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            foreach (var x in unusedVariables)
            {
                RecordFailure($"Variable is unused: {x}");
            }

            var inconsistentlyCasedVariables = ConfigurationVariables.Select(x => x.Name).Union(DocumentedVariables).Union(probablyDeploymentVariables)
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Where(xs => xs.Count() > 1)
                .ToArray();
            foreach (var xs in inconsistentlyCasedVariables)
            {
                RecordFailure($"Variable casing is inconsistent: {string.Join(", ", xs)}");
            }
        }
    }

    struct Variable
    {
        public Variable(string name, bool isOptionalVariable)
        {
            Name = name;
            IsOptionalVariable = isOptionalVariable;
        }

        public string Name { get; }
        /// <summary>
        /// AppSetting keys and connection strings do not need to be specified and don't necessarily need documentation currently.
        /// </summary>
        public bool IsOptionalVariable { get; }
    }
}
