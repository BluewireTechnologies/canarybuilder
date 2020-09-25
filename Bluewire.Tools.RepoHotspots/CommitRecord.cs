using System;
using System.Collections.Immutable;

namespace Bluewire.Tools.RepoHotspots
{
    public struct CommitRecord
    {
        public string Sha1 { get; set; }
        public bool IsMerge { get; set; }
        public DateTimeOffset CommitDate { get; set; }
        public DateTimeOffset AuthorDate { get; set; }
        public ImmutableArray<string> Parents { get; set; }
        public ImmutableArray<string> Tickets { get; set; }
        public ImmutableArray<string> Paths { get; set; }
    }
}
