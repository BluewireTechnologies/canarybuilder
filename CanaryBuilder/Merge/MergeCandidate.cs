using System;
using Bluewire.Common.GitWrapper.Model;

namespace CanaryBuilder.Merge
{
    public class MergeCandidate
    {
        public MergeCandidate(Ref mergeRef, IWorkingCopyVerifier verifier = null)
        {
            if (mergeRef == null) throw new ArgumentNullException(nameof(mergeRef));
            Ref = mergeRef;
            Verifier = verifier;
        }

        public Ref Ref { get; set; }
        public IWorkingCopyVerifier Verifier { get; set; }
    }
}