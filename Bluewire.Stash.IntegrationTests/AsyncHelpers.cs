using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bluewire.Stash.IntegrationTests
{
    internal static class AsyncHelpers
    {
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> self)
        {
            var list = new List<T>();
            await foreach (var item in self)
            {
                list.Add(item);
            }
            return list;
        }
    }
}
