using System.Collections.Generic;
using System.Linq;

namespace Bluewire.Common.GitWrapper.Parsing
{
    class GitLineOutputCollector : GitLineOutputParser<string>
    {
        public override IEnumerable<UnexpectedGitOutputFormatDetails> Errors => Enumerable.Empty<UnexpectedGitOutputFormatDetails>();
        public override bool Parse(string line, out string entry)
        {
            entry = line;
            return true;
        }
    }
}
