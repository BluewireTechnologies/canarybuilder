using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluewire.Common.Git.Model
{
    public class GitStatusEntry
    {
        public string Path { get; set; }
        public string NewPath { get; set; }
        public IndexState IndexState { get; set; }
        public WorkTreeState WorkTreeState { get; set; }

        private sealed class GitStatusEntryEqualityComparer : IEqualityComparer<GitStatusEntry>
        {
            public bool Equals(GitStatusEntry x, GitStatusEntry y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.Path, y.Path) && string.Equals(x.NewPath, y.NewPath) && x.IndexState == y.IndexState && x.WorkTreeState == y.WorkTreeState;
            }

            public int GetHashCode(GitStatusEntry obj)
            {
                unchecked
                {
                    var hashCode = (obj.Path != null ? obj.Path.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (obj.NewPath != null ? obj.NewPath.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (int) obj.IndexState;
                    hashCode = (hashCode*397) ^ (int) obj.WorkTreeState;
                    return hashCode;
                }
            }
        }

        public static IEqualityComparer<GitStatusEntry> EqualityComparer { get; } = new GitStatusEntryEqualityComparer();
    }
}
