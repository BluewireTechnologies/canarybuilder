using System.Collections.Generic;
using System.Collections.Immutable;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.RepositoryLinter;

public class ProjectCouldBeExploredRule
{
    private readonly SubjectRepository subject;

    public ProjectCouldBeExploredRule(SubjectRepository subject)
    {
        this.subject = subject;
    }

    public IEnumerable<Failure> GetFailures(Ref branch, ImmutableArray<ProjectFile> projects)
    {
        if (!subject.GetBranchRules(branch).ReportLoadFailures) yield break;

        foreach (var project in projects)
        {
            if (project.Exception != null)
            {
                yield return new Failure
                {
                    Subject = subject,
                    Message = $"Project could not be loaded: {project.Exception.Message}",
                    Branch = branch,
                    ProjectFile = project,
                };
            }
        }
    }
}
