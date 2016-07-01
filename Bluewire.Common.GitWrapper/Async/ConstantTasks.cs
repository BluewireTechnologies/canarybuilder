using System.Threading.Tasks;

namespace Bluewire.Common.GitWrapper.Async
{
    internal class ConstantTasks
    {
        public static readonly Task<bool> True = Task.FromResult(true);
        public static readonly Task<bool> False = Task.FromResult(false);
    }
}
