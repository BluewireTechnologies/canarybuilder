namespace Bluewire.Common.GitWrapper.Model
{
    public class LogEntry
    {
        public LogEntry()
        {
            Message = "";
        }

        public Ref Ref { get; set; }
        public Ref[] MergeParents { get; set; }
        public string Author { get; set; }
        public string Date { get; set; }
        public string Message { get; set; }
        public File[] Files { get; set; }

        public struct File
        {
            public string Path { get; set; }
            public IndexState? IndexState { get; set; }

            public override string ToString() => $"{Path} ({IndexState?.ToString() ?? "no state"})";
        }
    }
}
