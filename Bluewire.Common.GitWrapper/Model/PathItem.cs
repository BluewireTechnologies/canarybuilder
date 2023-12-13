namespace Bluewire.Common.GitWrapper.Model
{
    public struct PathItem
    {
        public string Path { get; set; }
        public ObjectType ObjectType { get; set; }
        public Ref ObjectName { get; set; }
    }
}
