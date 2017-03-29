using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bluewire.Tools.Runner.Shared
{
    public struct Build
    {
        /// <summary>
        /// Commit hash
        /// </summary>
        public Ref Commit { get; set; }

        /// <summary>
        /// Semantic version representing the location of the Commit in the git tree
        /// </summary>
        public SemanticVersion SemanticVersion { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", Commit.ToString(), SemanticVersion.ToString());
        }
    }
}
