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
        public static BranchRules All => new BranchRules { CheckTargetFrameworks = true, CheckPreReleasePackages = true, CheckMinimumPackageVersions = true };
        public static BranchRules None => default;

        public bool HasAnyRules => CheckTargetFrameworks || CheckPreReleasePackages || CheckMinimumPackageVersions;

        public bool CheckTargetFrameworks { get; init; }
        public bool CheckPreReleasePackages { get; init; }
        public bool CheckMinimumPackageVersions { get; init; }
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
                var projectFiles = await session.ListPaths(workingCopyOrRepo, branch,
                    new ListPathsOptions
                    {
                        Mode = ListPathsOptions.ListPathsMode.RecursiveFilesOnly,
                        PathFilter = x => StringComparer.OrdinalIgnoreCase.Equals(Path.GetExtension(x), ".csproj"),
                    });

                var projectDetails = await Task.WhenAll(projectFiles.Select(x => ReadProjectFile(session, x)).ToArray());
                yield return new (branch, projectDetails.ToImmutableArray());
            }
        }

        private async Task<ProjectFile> ReadProjectFile(GitSession session, PathItem projectFile)
        {
            using(var ms = new MemoryStream())
            {
                await session.ReadBlob(workingCopyOrRepo, projectFile.ObjectName, ms);
                ms.Position = 0;
                return await ReadProjectFile(projectFile.Path, ms);
            }
        }

        private async Task<ProjectFile> ReadProjectFile(string path, Stream stream)
        {
            try
            {
                var xml = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
                var propertyGroups = xml.Elements("Project").Elements("PropertyGroup").ToArray();
                var targetFrameworks = new HashSet<string>();
                targetFrameworks.UnionWith(propertyGroups.Elements("TargetFrameworks").SelectMany(x => x.Value.Split(';')));
                targetFrameworks.UnionWith(propertyGroups.Elements("TargetFramework").Select(x => x.Value));

                var packageReferences = xml.Elements("Project").Elements("ItemGroup").Elements("PackageReference");
                return new ProjectFile
                {
                    Path = path,
                    TargetFrameworks = targetFrameworks.ToImmutableArray(),
                    Packages = packageReferences.Select(ReadPackageReference).ToImmutableArray(),
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

        private PackageReference ReadPackageReference(XElement element)
        {
            var name = element.Attribute("Include")?.Value;
            if (name == null) throw new ArgumentException("PackageReference element has no Include attribute.");
            var versionElement = element.Element("Version");
            if (versionElement?.Value != null)
            {
                return new PackageReference(name, versionElement.Value);
            }
            var versionAttribute = element.Attribute("Version");
            if (versionAttribute?.Value != null)
            {
                return new PackageReference(name, versionAttribute.Value);
            }
            throw new ArgumentException("PackageReference element has no Version element or attribute.");
        }
    }

    public class ProjectFile
    {
        public string Path { get; init; } = null!;
        public ImmutableArray<string> TargetFrameworks { get; init; } = ImmutableArray<string>.Empty;
        public ImmutableArray<PackageReference> Packages { get; init; } = ImmutableArray<PackageReference>.Empty;
        public Exception? Exception { get; init; }
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
}
