using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Common.GitWrapper.Parsing
{
    public class RefNameColumnLineParser : IGitLineOutputParser<Ref>
    {
        private readonly int columnIndex;
        private readonly List<UnexpectedGitOutputFormatDetails> errors = new List<UnexpectedGitOutputFormatDetails>();

        public IEnumerable<UnexpectedGitOutputFormatDetails> Errors => errors;
        private readonly char[] splitOnWhitespace = "\t ".ToCharArray();

        public RefNameColumnLineParser(int columnIndex)
        {
            this.columnIndex = columnIndex;
        }

        public bool Parse(string line, out Ref entry)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));
            
            var parts = line.Split(splitOnWhitespace, StringSplitOptions.RemoveEmptyEntries);
                
            var error = new UnexpectedGitOutputFormatDetails { Line = line };
            entry = ValidateRef(parts.ElementAtOrDefault(columnIndex), error);
            if (error.Explanations.Any())
            {
                errors.Add(error);
                entry = null;
                return false;
            }
            return true;
        }

        private Ref ValidateRef(string name, UnexpectedGitOutputFormatDetails error)
        {
            try
            {
                return new Ref(name);
            }
            catch(Exception ex)
            {
                error.Explanations.Add(ex.Message);
                return null;
            }
        }

        public Ref ParseOrNull(string line)
        {
            Ref entry;
            if(Parse(line, out entry)) return entry;
            return null;
        }
    }
}