using System;
using Bluewire.Common.GitWrapper.Model;

namespace RefCleaner.Collectors
{
    public class BranchDetails
    {
        public string Name { get; set; }
        public Ref Ref { get; set; }
        public Ref ResolvedRef { get; set; }
        public DateTimeOffset CommitDatestamp { get; set; }
        
        public BranchDisposition Disposition { get; private set; }

        public void UpdateDisposition(BranchDisposition disposition)
        {
            // Branches already marked as 'must keep' must remain that way.
            if (Disposition == BranchDisposition.MustKeep) return;
            // Marking a branch as 'must keep' overrides any existing disposition.
            if (disposition == BranchDisposition.MustKeep)
            {
                Disposition = BranchDisposition.MustKeep;
                return;
            }
            // Setting Disposition to 'NotSet' can never override anything, and is a no-op.
            if (disposition == BranchDisposition.NotSet) return;
            // Otherwise, update our Disposition.
            Disposition = disposition;
        }
    }
}
