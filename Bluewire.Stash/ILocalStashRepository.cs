using System.Collections.Generic;
using System.Threading;

namespace Bluewire.Stash
{
    public interface ILocalStashRepository : IStashRepository
    {
        IAsyncEnumerable<string> CleanUpTemporaryObjects(CancellationToken token);
    }
}
