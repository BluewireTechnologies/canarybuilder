using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.RepositoryLinter;

public class NoPreReleasePackagesRule
{
    private readonly SubjectRepository subject;

    public NoPreReleasePackagesRule(SubjectRepository subject)
    {
        this.subject = subject;
    }

    public IEnumerable<Failure> GetFailures(Ref branch, ImmutableArray<ProjectFile> projects)
    {
        if (!subject.GetBranchRules(branch).CheckPreReleasePackages) yield break;

        foreach (var project in projects)
        {
            foreach (var package in project.Packages)
            {
                if (IsPreRelease(package))
                {
                    yield return new Failure
                    {
                        Subject = subject,
                        Message = $"Package {package.Name} version {package.Version}",
                        Branch = branch,
                        ProjectFile = project,
                    };
                }
            }
        }
    }

    private static bool IsPreRelease(PackageReference packageReference)
    {
        var tagIndex = packageReference.Version.IndexOf('-');
        if (tagIndex < 0) return false;
        var tag = packageReference.Version.Substring(tagIndex + 1);
        if (tag == "release") return false;
        return true;
    }
}
