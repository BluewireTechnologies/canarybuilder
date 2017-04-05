using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;

namespace Bluewire.Tools.Builds.Shared
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
