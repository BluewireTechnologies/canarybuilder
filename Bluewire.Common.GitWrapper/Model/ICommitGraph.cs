using System.Collections.Generic;

namespace Bluewire.Common.GitWrapper.Model
{
    public interface ICommitGraph
    {
        bool Contains(Ref commit);
        IEnumerable<Ref> Parents(Ref commit);
        IEnumerable<Ref> Ancestors(Ref commit);
        IEnumerable<Ref> FirstParentAncestry(Ref commit, Ref stop);
    }
}
