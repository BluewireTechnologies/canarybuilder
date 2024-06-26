using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.RepositoryLinter;

public class SemVerProjectsHaveLocalVersionPrefixRule
{
    private readonly SubjectRepository subject;

    public SemVerProjectsHaveLocalVersionPrefixRule(SubjectRepository subject)
    {
        this.subject = subject;
    }

    public IEnumerable<Failure> GetFailures(Ref branch, ImmutableArray<ProjectFile> projects)
    {
        if (!subject.GetBranchRules(branch).CheckTargetFrameworks) yield break;

        foreach (var project in projects)
        {
            // Assume that if VersionPrefix is explicit then the build is set up to SemVer this project.
            if (project.Properties.Any(x => x.Name == "VersionPrefix"))
            {
                if (!project.LocalPropertyNames.Contains("VersionPrefix"))
                {
                    yield return new Failure
                    {
                        Subject = subject,
                        Message = "Project specifies VersionPrefix but does not declare it TreatAsLocalProperty.",
                        Branch = branch,
                        ProjectFile = project,
                    };
                }
            }
        }
    }
}
