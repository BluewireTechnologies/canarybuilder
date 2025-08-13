namespace Bluewire.Conventions
{
    public static class StructuredBranchExtensions
    {
        public static bool IsMaster(this StructuredBranch branch) => branch.Namespace == null && branch.Name == "master";
    }
}
