using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.RepositoryLinter
{
    public struct BranchRules
    {
        public static BranchRules All =>
            new BranchRules
            {
                ReportLoadFailures = true,
                CheckTargetFrameworks = true,
                CheckPreReleasePackages = true,
                CheckMinimumPackageVersions = true,
                CheckMaximumPackageVersions = true,
                CheckSemVerProjectsHaveLocalVersionPrefix = true,
                CheckOctopusVariablesMatchDocumentation = true,
            };
        public static BranchRules None => default;

        public bool HasAnyRules => ReportLoadFailures || CheckTargetFrameworks || CheckPreReleasePackages || CheckMinimumPackageVersions || CheckMaximumPackageVersions || CheckSemVerProjectsHaveLocalVersionPrefix || CheckOctopusVariablesMatchDocumentation;

        public bool ReportLoadFailures { get; init; }
        public bool CheckTargetFrameworks { get; init; }
        public bool CheckPreReleasePackages { get; init; }
        public bool CheckMinimumPackageVersions { get; init; }
        public bool CheckMaximumPackageVersions { get; init; }
        public bool CheckSemVerProjectsHaveLocalVersionPrefix { get; init; }
        public bool CheckOctopusVariablesMatchDocumentation { get; init; }
    }

    public class RepositoryExplorer
    {
        private readonly IGitFilesystemContext workingCopyOrRepo;
        private readonly SubjectRepository info;

        public RepositoryExplorer(IGitFilesystemContext workingCopyOrRepo, SubjectRepository info)
        {
            this.workingCopyOrRepo = workingCopyOrRepo;
            this.info = info;
        }

        public async IAsyncEnumerable<(Ref Branch, ImmutableArray<ProjectFile> Projects)> GetProjectFiles(GitSession session, Func<BranchRules, bool> selectRule)
        {
            await session.Fetch(workingCopyOrRepo);
            var branches = await session.ListBranches(workingCopyOrRepo, new ListBranchesOptions { Remote = true });
            var filteredBranches = branches.Where(b => selectRule(info.GetBranchRules(b))).ToArray();

            foreach (var branch in filteredBranches)
            {
                var files = await session.ListPaths(workingCopyOrRepo, branch,
                    new ListPathsOptions
                    {
                        Mode = ListPathsOptions.ListPathsMode.RecursiveFilesOnly,
                        PathFilter = x => IsProjectFile(x) || IsReadMeFile(x) || IsConfigurationFile(x) || IsDeploymentScript(x),
                    });

                var projectFiles = files.Where(x => IsProjectFile(x.Path));
                var readmeFiles = files.Where(x => IsReadMeFile(x.Path));
                var configurationFiles = files.Where(x => IsConfigurationFile(x.Path));
                var deploymentScripts = files.Where(x => IsDeploymentScript(x.Path));

                var projectDetails = await Task.WhenAll(projectFiles
                    .Select(x => ReadProjectFile(
                        session,
                        x,
                        readmeFiles.Where(y => IsInProject(x, y)),
                        configurationFiles.Where(y => IsInProject(x, y)),
                        deploymentScripts.Where(y => IsInProject(x, y))))
                    .ToArray());
                yield return new (branch, projectDetails.ToImmutableArray());
            }

            bool IsProjectFile(string path) => StringComparer.OrdinalIgnoreCase.Equals(Path.GetExtension(path), ".csproj");
            bool IsReadMeFile(string path) => StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(path), "README.md");
            bool IsConfigurationFile(string path)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetExtension(path), ".config")) return true;
                if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetExtension(path), ".xml"))
                {
                    if (path.Contains("/Config/", StringComparison.OrdinalIgnoreCase)) return true;
                    if (path.Contains(".Debug.", StringComparison.OrdinalIgnoreCase)) return true;
                    if (path.Contains(".Release.", StringComparison.OrdinalIgnoreCase)) return true;
                    if (path.Contains(".Octopus.", StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
            bool IsDeploymentScript(string path) => StringComparer.OrdinalIgnoreCase.Equals(Path.GetExtension(path), ".ps1");

            bool IsInProject(PathItem project, PathItem item)
            {
                var projectDirectory = Path.GetDirectoryName(project.Path) + '/';
                return item.Path.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase);
            }
        }

        private async Task<ProjectFile> ReadProjectFile(GitSession session, PathItem projectFile, IEnumerable<PathItem> readmeFiles, IEnumerable<PathItem> configurationFiles, IEnumerable<PathItem> deploymentScripts)
        {
            using(var ms = new MemoryStream())
            {
                await session.ReadBlob(workingCopyOrRepo, projectFile.ObjectName, ms);
                ms.Position = 0;
                return await ReadProjectFile(projectFile.Path, ms, readmeFiles, configurationFiles, deploymentScripts);
            }
        }

        private async Task<ProjectFile> ReadProjectFile(string path, Stream stream, IEnumerable<PathItem> readmeFiles, IEnumerable<PathItem> configurationFiles, IEnumerable<PathItem> deploymentScripts)
        {
            try
            {
                var xml = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
                var propertyGroups = xml.Elements("Project").Elements("PropertyGroup").ToArray();
                var targetFrameworks = new HashSet<string>();
                targetFrameworks.UnionWith(propertyGroups.Elements("TargetFrameworks").SelectMany(x => x.Value.Split(';')));
                targetFrameworks.UnionWith(propertyGroups.Elements("TargetFramework").Select(x => x.Value));

                var packageReferences = xml.Elements("Project").Elements("ItemGroup").Elements("PackageReference");
                var projectReferences = xml.Elements("Project").Elements("ItemGroup").Elements("ProjectReference");

                // Topmost readme file only:
                var readme = readmeFiles.Select(x => x.Path).Where(x => Path.GetDirectoryName(x) == Path.GetDirectoryName(path)).SingleOrDefault();
                return new ProjectFile
                {
                    Path = path,
                    TargetFrameworks = targetFrameworks.ToImmutableArray(),
                    Packages = ReadPackageReferences(packageReferences).ToImmutableArray(),
                    Properties = propertyGroups.Elements().Select(ReadProjectProperty).ToImmutableArray(),
                    LocalPropertyNames = xml.Element("Project")?.Attribute("TreatAsLocalProperty")?.Value?.Split(";")?.ToImmutableArray() ?? ImmutableArray<string>.Empty,
                    ReadMePath = readme,
                    ConfigurationPaths = configurationFiles.Select(x => x.Path).ToImmutableArray(),
                    DeploymentScriptPaths = deploymentScripts.Select(x => x.Path).ToImmutableArray(),
                    Dependencies = ReadProjectReferences(projectReferences).ToImmutableArray(),
                };
            }
            catch (Exception ex)
            {
                return new ProjectFile
                {
                    Path = path,
                    Exception = ex,
                };
            }
        }

        private IEnumerable<PackageReference> ReadPackageReferences(IEnumerable<XElement> packageReferences)
        {
            foreach (var element in packageReferences)
            {
                var name = element.Attribute("Include")?.Value;
                if (name == null)
                {
                    if (element.Attribute("Update") != null) continue;
                    throw new ArgumentException("PackageReference element has no Include or Update attribute.");
                }
                var versionElement = element.Element("Version");
                if (versionElement?.Value != null)
                {
                    yield return new PackageReference(name, versionElement.Value);
                    continue;
                }
                var versionAttribute = element.Attribute("Version");
                if (versionAttribute?.Value != null)
                {
                    yield return new PackageReference(name, versionAttribute.Value);
                    continue;
                }
                throw new ArgumentException("PackageReference element has no Version element or attribute.");
            }
        }

        private IEnumerable<string> ReadProjectReferences(IEnumerable<XElement> projectReferences)
        {
            foreach (var element in projectReferences)
            {
                var name = element.Attribute("Include")?.Value;
                if (name == null)
                {
                    throw new ArgumentException("ProjectReference element has no Include attribute.");
                }
                yield return name;
            }
        }

        private ProjectProperty ReadProjectProperty(XElement element)
        {
            return new ProjectProperty(element.Name.LocalName, element.Value, element.Attribute("Condition")?.Value);
        }
    }

    public class ProjectFile
    {
        public string Path { get; init; } = null!;
        public ImmutableArray<string> TargetFrameworks { get; init; } = ImmutableArray<string>.Empty;
        public ImmutableArray<PackageReference> Packages { get; init; } = ImmutableArray<PackageReference>.Empty;
        public ImmutableArray<ProjectProperty> Properties { get; init; } = ImmutableArray<ProjectProperty>.Empty;
        public ImmutableArray<string> LocalPropertyNames { get; init; } = ImmutableArray<string>.Empty;
        public string? ReadMePath { get; init; }
        public ImmutableArray<string> ConfigurationPaths { get; init; } = ImmutableArray<string>.Empty;
        public ImmutableArray<string> DeploymentScriptPaths { get; init; } = ImmutableArray<string>.Empty;
        public Exception? Exception { get; init; }
        public ImmutableArray<string> Dependencies { get; init; } = ImmutableArray<string>.Empty;
    }

    public struct PackageReference
    {
        public PackageReference(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }
    }

    public class ProjectProperty
    {
        public ProjectProperty(string name, string value, string? condition = null)
        {
            Name = name;
            Value = value;
            Condition = condition;
        }

        public string Name { get; }
        public string Value { get; }
        public string? Condition { get; }
    }
}
