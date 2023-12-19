using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.RepositoryLinter;

public class TargetFrameworkVersionsAreBlessedRule
{
    private readonly SubjectRepository subject;

    public TargetFrameworkVersionsAreBlessedRule(SubjectRepository subject)
    {
        this.subject = subject;
    }

    public IEnumerable<Failure> GetFailures(Ref branch, ImmutableArray<ProjectFile> projects)
    {
        if (!subject.GetBranchRules(branch).CheckTargetFrameworks) yield break;

        foreach (var project in projects)
        {
            var wrongFrameworks = project.TargetFrameworks.Except(Constants.BlessedTargetFrameworks).ToArray();
            if (wrongFrameworks.Any())
            {
                yield return new Failure
                {
                    Subject = subject,
                    Message = $"Invalid TargetFrameworks {string.Join(", ", wrongFrameworks)}",
                    Branch = branch,
                    ProjectFile = project,
                };
            }
        }
    }
}
