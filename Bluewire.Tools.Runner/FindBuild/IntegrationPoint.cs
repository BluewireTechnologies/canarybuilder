using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;

namespace Bluewire.Tools.Runner.FindBuild
{
    public struct IntegrationQueryResult
    {
        /// <summary>
        /// Ref which was the subject of the query.
        /// </summary>
        public Ref Subject { get; set; }
        /// <summary>
        /// Branch into which Subject was integrated.
        /// </summary>
        public StructuredBranch TargetBranch { get; set; }
        /// <summary>
        /// Commit hash which integrated Subject into TargetBranch.
        /// </summary>
        public Ref IntegrationPoint { get; set; }
    }
}
