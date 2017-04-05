using System;

namespace Bluewire.Tools.Builds.FindBuild
{
    public class RepositoryStructureException : ApplicationException
    {
        public RepositoryStructureException(string message) : base(message)
        {
        }
    }
}
