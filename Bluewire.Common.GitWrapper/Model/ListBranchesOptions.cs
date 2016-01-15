namespace Bluewire.Common.GitWrapper.Model
{
    public struct ListBranchesOptions
    {
        public bool Remote { get; set; }
        public Ref UnmergedWith { get; set; }
    }
}
