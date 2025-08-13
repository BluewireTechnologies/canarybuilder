using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bluewire.Conventions
{
    /// <remarks>
    /// Branch names need to follow a certain format in order to be machine-parseable.
    ///
    /// [remote]/[namespace]/[name]-[ticket]-[numeric]-[release]
    ///
    /// [remote] is an (optional) Git remote name.
    /// [namespace] may have multiple levels.
    /// [name] may consist of any number of hyphen-separated terms, though more than four is discouraged.
    /// [ticket] is of the form '{type}-{number}', eg. E-23456.
    /// [numeric] is optional, but if specified must either be:
    ///   * 1+ digits, or
    ///   * a yyyyMMdd datestamp, or.
    ///   * a yyyyMMdd-HHmm datestamp.
    /// [release] is optional, but if specified must be a dot-separated version number (usually xx.yy).
    ///
    /// Note that [remote] cannot be determined without extra information about the Git repository.
    /// Unless this information has been provided, it will be included as part of [namespace].
    /// </remarks>
    public struct StructuredBranch
    {
        // Place name first so it's used by default GetHashCode() implementation.
        private string name;
        private string @namespace;
        private string ticketIdentifier;
        private string numericSuffix;
        private string targetRelease;
        private string remoteName;

        public string Namespace
        {
            get { return @namespace; }
            set { @namespace = SquashEmptyToNull(value); }
        }

        public string Name
        {
            get { return name; }
            set { name = SquashEmptyToNull(value); }
        }

        public string TicketIdentifier
        {
            get { return ticketIdentifier; }
            set { ticketIdentifier = SquashEmptyToNull(value); }
        }

        public string NumericSuffix
        {
            get { return numericSuffix; }
            set { numericSuffix = SquashEmptyToNull(value); }
        }

        public string TargetRelease
        {
            get { return targetRelease; }
            set { targetRelease = SquashEmptyToNull(value); }
        }

        public string RemoteName
        {
            get { return remoteName; }
            set { remoteName = SquashEmptyToNull(value); }
        }

        public override string ToString()
        {
            var mainNameParts = new[] {
                Name,
                TicketIdentifier,
                NumericSuffix,
                TargetRelease
            }.Where(p => !String.IsNullOrWhiteSpace(p));

            var mainName = String.Join("-", mainNameParts);

            if (mainName.Length == 0) return "";
            var pathNameParts = new []
            {
                RemoteName,
                Namespace,
                mainName,
            }.Where(p => !String.IsNullOrWhiteSpace(p));
            return String.Join("/", pathNameParts);
        }

        public static void ValidateBranchName(string raw)
        {
            Exception exception;
            if (!ValidateBranchName(raw, out exception)) throw exception;
        }

        public static bool TryParse(string raw, out StructuredBranch structured)
        {
            Exception exception;
            return TryParseInternal(raw, out structured, out exception);
        }

        public static StructuredBranch Parse(string raw)
        {
            StructuredBranch structured;
            Exception exception;
            if (!TryParseInternal(raw, out structured, out exception)) throw exception;
            return structured;
        }

        private static bool ValidateBranchName(string raw, out Exception exception)
        {
            if (String.IsNullOrEmpty(raw))
            {
                exception = new ArgumentNullException(nameof(raw));
                return false;
            }
            if (raw.StartsWith("/"))
            {
                exception = new ArgumentException($"Branch name cannot start with '/': {raw}", nameof(raw));
                return false;
            }

            // Git does not require this, I think, but for sanity's sake we're not going to allow it:
            if (raw.Cast<char>().Any(char.IsWhiteSpace))
            {
                exception = new ArgumentException("Branch name must not contain whitespace.", nameof(raw));
                return false;
            }
            exception = null;
            return true;
        }

        private static bool TryParseInternal(string raw, out StructuredBranch structured, out Exception exception)
        {
            structured = new StructuredBranch();

            if (!ValidateBranchName(raw, out exception)) return false;

            var m = Patterns.BranchStructure.Match(raw);
            if (!m.Success)
            {
                exception = new ArgumentException($"Unable to parse branch name: {raw}");
                return false;
            }

            structured.Namespace = m.Groups["namespace"]?.Value;
            structured.Name = m.Groups["name"].Value;
            structured.TicketIdentifier = m.Groups["ticketIdentifier"]?.Value;
            structured.NumericSuffix = m.Groups["numericSuffix"]?.Value;
            structured.TargetRelease = m.Groups["targetRelease"]?.Value;
            return true;
        }

        public bool TryAssignRemoteName(string candidateRemoteName, out StructuredBranch withRemote)
        {
            withRemote = this;
            if (Namespace == candidateRemoteName)
            {
                withRemote.Namespace = null;
                withRemote.RemoteName = candidateRemoteName;
                return true;
            }
            var prefixLength = candidateRemoteName.Length + 1;
            if (Namespace.Length <= prefixLength) return false;
            if (Namespace[candidateRemoteName.Length] == '/' && Namespace.StartsWith(candidateRemoteName, StringComparison.Ordinal))
            {
                var localNamespace = Namespace.Substring(prefixLength);
                withRemote.Namespace = localNamespace;
                withRemote.RemoteName = candidateRemoteName;
                return true;
            }
            return false;
        }

        public bool TryAssignRemoteName(string[] remoteNames, out StructuredBranch withRemote)
        {
            foreach (var candidate in remoteNames)
            {
                if (TryAssignRemoteName(candidate, out withRemote)) return true;
            }
            withRemote = this;
            return false;
        }

        private static string SquashEmptyToNull(string str)
        {
            if (String.IsNullOrWhiteSpace(str)) return null;
            return str;
        }
    }
}
