using System.Collections.Generic;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;

namespace RefCleaner
{
    public interface IRefCollector
    {
        /// <summary>
        /// Gets a set of fully-qualified refs based on some criteria.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Ref>> CollectRefs();
    }
}
