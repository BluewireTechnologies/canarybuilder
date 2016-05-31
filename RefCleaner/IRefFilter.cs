using System.Threading.Tasks;
using RefCleaner.Collectors;

namespace RefCleaner
{
    public interface IRefFilter
    {
        Task ApplyFilter(BranchDetails details);
    }
}
