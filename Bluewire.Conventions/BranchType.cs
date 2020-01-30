namespace Bluewire.Conventions
{
    public struct BranchType
    {
        private BranchType(string semanticTag, params string[] branchFilters)
        {
            SemanticTag = semanticTag;
            BranchFilters = branchFilters;
        }

        public string SemanticTag { get; }
        public string[] BranchFilters { get; }

        public static BranchType None = default(BranchType);
        public static BranchType Beta = new BranchType("beta", "backport/*", "master");
        public static BranchType Master = new BranchType("beta", "master"); // Alias.
        public static BranchType ReleaseCandidate = new BranchType("rc", "candidate/*");
        public static BranchType Release = new BranchType("release", "release/*");
        public static BranchType Canary = new BranchType("canary", "canary/*");
    }
}
