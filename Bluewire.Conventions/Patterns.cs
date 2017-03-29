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

        // Assumes year.sprint.build-semtag format where sprint is zero padded and never greater than 99, eg. 17.05.1234-rc
        public static readonly Regex SemanticVersionStructure = new Regex(@"
^
(?<major>\d{2})\.
(?<minor>\d{2})\.
(?<build>\d+)
(- (?<semtag>[A-Za-z]+))?
$
", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

        // Assumes YouTrack ticket identifier format, eg. E-23456
        public static readonly Regex TicketIdentifier = new Regex(@"\b[A-Z]{1,3}-\d{1,5}\b", RegexOptions.IgnoreCase);

        // Assumes YouTrack ticket identifier format, eg. E-23456
        public static readonly Regex TicketIdentifierOnly = new Regex(@"^[A-Z]{1,3}-\d{1,5}$", RegexOptions.IgnoreCase);
    }
}
