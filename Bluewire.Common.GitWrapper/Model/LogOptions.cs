using System.Text.RegularExpressions;

namespace Bluewire.Common.GitWrapper.Model
{
    public struct LogOptions
    {
        public Regex MatchMessage { get; set; }
        public LogShowMerges ShowMerges { get; set; }
        public bool IncludeAllRefs { get; set; }
    }
}
