using System.Text.RegularExpressions;

namespace Bluewire.Conventions
{
    public static class Patterns
    {
        // Assumes YouTrack ticket identifier format, eg. E-23456
        public static readonly Regex BranchStructure = new Regex(@"
^
((?<namespace>\w+(/\w+)*)/)?
(?<name>\w[-\.\w]*?)
(- (?<ticketIdentifier>[A-Z]{1,3}-\d{1,5}))?
(- (?<numericSuffix> \d{8}(-\d{4})? | \d+ ))?
(- (?<targetRelease> \d+(.\d+)*))?
$
", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

        // Assumes YouTrack ticket identifier format, eg. E-23456
        public static readonly Regex TicketIdentifier = new Regex(@"\b[A-Z]{1,3}-\d{1,5}\b", RegexOptions.IgnoreCase);

        // Assumes YouTrack ticket identifier format, eg. E-23456
        public static readonly Regex TicketIdentifierOnly = new Regex(@"^[A-Z]{1,3}-\d{1,5}$", RegexOptions.IgnoreCase);
    }
}
