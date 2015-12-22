using System.Collections.Generic;
using CanaryBuilder.Common.Git.Model;

namespace CanaryBuilder.Merge
{
    public class MergeJobDefinition
    {
        public Ref Base { get; set; }

        public Ref TemporaryBranch { get; set; }
        
        public Ref FinalBranch { get; set; }
        public Ref FinalTag { get; set; }

        public IWorkingCopyVerifier Verifier { get; set; }

        public IList<MergeCandidate> Merges { get; } = new List<MergeCandidate>();
    }
}
