
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.RepositoryLinter;

public class PackagesAreUpToDateRule
{
    private readonly SubjectRepository subject;

    public PackagesAreUpToDateRule(SubjectRepository subject)
    {
        this.subject = subject;
    }

    public IEnumerable<Failure> GetFailures(Ref branch, ImmutableArray<ProjectFile> projects)
    {
        if (!subject.GetBranchRules(branch).CheckMinimumPackageVersions) yield break;

        foreach (var project in projects)
        {
            foreach (var package in project.Packages)
            {
                if (!Constants.MinimumPackageVersions.TryGetValue(package.Name, out var minimumVersion)) continue;

                if (TryParsePackageVersion(package, out var version) && version >= minimumVersion) continue;

                yield return new Failure
                {
                    Subject = subject,
                    Message = $"Package {package.Name} version {package.Version}; should be at least {minimumVersion}",
                    Branch = branch,
                    ProjectFile = project,
                };
            }
        }
    }

    private static bool TryParsePackageVersion(PackageReference packageReference, [MaybeNullWhen(false)] out Version version)
    {
        var tagIndex = packageReference.Version.IndexOf('-');
        var versionNumberString = tagIndex < 0 ? packageReference.Version : packageReference.Version.Substring(0, tagIndex);
        return Version.TryParse(versionNumberString, out version);
    }
}
