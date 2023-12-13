using System;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.RepositoryLinter;

public class SubjectRepository
{
    public SubjectRepository(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public Func<Ref, BranchRules> GetBranchRules { get; init; } = x => new Ref("origin/master").Equals(x) ? BranchRules.All : BranchRules.None;

    public override string ToString() => Name;
}
