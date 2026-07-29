using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.RepositoryLinter.ReadmeValidator;

namespace Bluewire.RepositoryLinter;

public class Log4NetLoggersUseAppenderRefRule
{
    private readonly SubjectRepository subject;
    private readonly IGitFilesystemContext workingCopyOrRepo;
    private readonly GitSession session;

    public Log4NetLoggersUseAppenderRefRule(SubjectRepository subject, IGitFilesystemContext workingCopyOrRepo, GitSession session)
    {
        this.subject = subject;
        this.workingCopyOrRepo = workingCopyOrRepo;
        this.session = session;
    }

    public IEnumerable<Failure> GetFailures(Ref branch, ImmutableArray<ProjectFile> projects)
    {
        if (!subject.GetBranchRules(branch).CheckOctopusVariablesMatchDocumentation) yield break;

        var validator = new Validator(subject, workingCopyOrRepo, session, branch);

        foreach (var project in projects)
        {
            validator.VisitConfiguration(project);
        }

        foreach (var failure in validator.Failures) yield return failure;
    }

    class Validator
    {
        private readonly SubjectRepository subject;
        private readonly Ref branch;

        private readonly GitDocumentSource source;

        public Validator(SubjectRepository subject, IGitFilesystemContext workingCopyOrRepo, GitSession session, Ref branch)
        {
            this.subject = subject;
            this.branch = branch;
            source = new GitDocumentSource(workingCopyOrRepo, session, branch);
        }

        public List<Failure> Failures { get; } = new List<Failure>();

        private void RecordFailure(ProjectFile project, string message)
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
        /// Find possible references to Octopus variables in configuration files and transforms.
        /// </summary>
        public void VisitConfiguration(ProjectFile project)
        {
            foreach (var path in project.ConfigurationPaths)
            {
                VisitConfigurationPath(project, path);
            }
        }

        private void VisitConfigurationPath(ProjectFile project, string path)
        {
            if (!TryLoadXml(path, out var xml))
            {
                if (IsProbablySupposedToBeXml(path))
                {
                    RecordFailure(project, $"{path}: Expected an XML file, but it did not parse as valid.");
                }
                return; // Not XML?

            }
            var log4Net = xml.Descendants("log4net").ToArray();

            foreach (var root in log4Net.Elements("root"))
            {
                var appender = root.Element("appender");
                if (appender != null)
                {
                    RecordFailure(project, $"{DescribeSource(path, appender)}: Root logger cannot contain <appender>. Must use <appender-ref> instead.");
                }
            }

            foreach (var logger in log4Net.Elements("logger"))
            {
                var appender = logger.Element("appender");
                if (appender != null)
                {
                    var name = logger.Attribute("name")?.Value ?? "";
                    RecordFailure(project, $"{DescribeSource(path, appender)}: Logger '{name}' cannot contain <appender>. Must use <appender-ref> instead.");
                }
            }
        }

        private string DescribeSource(string path, IXmlLineInfo? info)
        {
            if (info?.HasLineInfo() != true) return path;
            return $"{path}:{info.LineNumber}";
        }

        private bool IsProbablySupposedToBeXml(string path)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(path), "App.config")) return true;
            if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(path), "log4net.config")) return true;
            return false;
        }

        private bool TryLoadXml(string path, [MaybeNullWhen(false)] out XDocument xml)
        {
            try
            {
                using (var reader = source.Open(source.CreateUri(path)))
                {
                    xml = XDocument.Load(reader, LoadOptions.SetLineInfo);
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
    }
}
