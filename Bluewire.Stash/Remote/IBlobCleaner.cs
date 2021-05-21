using System.Threading.Tasks;

namespace Bluewire.Stash.Remote
{
    public interface IBlobCleaner
    {
        Task<bool> TryCleanUp(LocalFileSystem fileSystem, string absolutePath);
    }
}
