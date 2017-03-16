namespace Bluewire.Conventions
{
    public struct BranchType
    {
        private BranchType(string semanticTag, string branchFilter)
        {
            SemanticTag = semanticTag;
            BranchFilter = branchFilter;
        }

        public string SemanticTag { get; }
        public string BranchFilter { get; }

        public static BranchType None = default(BranchType);
        public static BranchType Beta = new BranchType("beta", "master");
        public static BranchType Master = new BranchType("beta", "master"); // Alias.
        public static BranchType ReleaseCandidate = new BranchType("rc", "candidate/*");
        public static BranchType Release = new BranchType("release", "release/*");
        public static BranchType Canary = new BranchType("canary", "canary/*");
    }
}
