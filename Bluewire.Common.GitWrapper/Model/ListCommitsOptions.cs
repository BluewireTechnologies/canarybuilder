using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluewire.Common.GitWrapper.Model
{
    public struct ListCommitsOptions
    {
        public bool FirstParentOnly { get; set; }
        public bool AncestryPathOnly { get; set; }
    }
}
