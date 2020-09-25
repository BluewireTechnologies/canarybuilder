using System.Collections.Generic;

namespace Bluewire.Tools.RepoHotspots
{
    public class StringTable
    {
        public StringTable(int capacity)
        {
            table = new Dictionary<string, string>(capacity);
        }

        private readonly Dictionary<string, string> table;

        public string Get(string str)
        {
            if (table.TryGetValue(str, out var cached)) return cached;
            table.Add(str, str);
            return str;
        }
    }
}
