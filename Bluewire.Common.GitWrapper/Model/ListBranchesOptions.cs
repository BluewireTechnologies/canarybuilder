namespace Bluewire.Common.GitWrapper.Model
{
    public struct ListBranchesOptions
    {
        public bool Remote { get; set; }
        public Ref UnmergedWith { get; set; }
        public Ref MergedWith { get; set; }
        public Ref Contains { get; set; }
    }
}
