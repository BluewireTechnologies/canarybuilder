using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace RefCleaner.Collectors
{
    class BranchDetailsParser : GitLineOutputParser<BranchDetails>
    {
        private readonly List<UnexpectedGitOutputFormatDetails> errors = new List<UnexpectedGitOutputFormatDetails>();

        public override IEnumerable<UnexpectedGitOutputFormatDetails> Errors => errors;
        private readonly char[] splitOnWhitespace = "\t ".ToCharArray();

        public override bool Parse(string line, out BranchDetails entry)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            var parts = line.Split(splitOnWhitespace, StringSplitOptions.RemoveEmptyEntries);

            var error = new UnexpectedGitOutputFormatDetails { Line = line };
            var datestamp = StrictISO8601.TryParseExact(parts[0], error);

            var objectName = ValidateRef(parts[1], error);
            var branchName = ValidateRef(parts[2], error);

            if (error.Explanations.Any())
            {
                errors.Add(error);
                entry = null;
                return false;
            }
            entry = new BranchDetails
            {
                Name = branchName,
                Ref = branchName,
                ResolvedRef = objectName,
                CommitDatestamp = datestamp.GetValueOrDefault()
            };
            return true;
        }

        private Ref ValidateRef(string name, UnexpectedGitOutputFormatDetails error)
        {
            try
            {
                return new Ref(name);
            }
            catch (Exception ex)
            {
                error.Explanations.Add(ex.Message);
                return null;
            }
        }
    }
}
