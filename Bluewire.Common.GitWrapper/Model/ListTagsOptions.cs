namespace Bluewire.Common.GitWrapper.Model
{
    public struct ListTagsOptions
    {
        public Ref Contains { get; set; }
        public string Pattern { get; set; }
    }
}
