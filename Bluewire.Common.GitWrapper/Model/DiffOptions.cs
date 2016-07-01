namespace Bluewire.Common.GitWrapper.Model
{
    public struct DiffOptions
    {
        public bool Cached { get; set; }
        public Ref From { get; set; }
        public Ref To { get; set; }
    }
}
