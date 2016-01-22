using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluewire.Common.GitWrapper.Model
{
    public class TagDetails
    {
        public TagDetails()
        {
            Message = "";
        }

        public string Name { get; set; }
        public Ref Ref { get; set; }
        public Ref ResolvedRef { get; set; }
        public string Message { get; set; }
    }
}
