using System;

namespace Bluewire.Tools.Runner.FindBuild
{
    public class RepositoryStructureException : ApplicationException
    {
        public RepositoryStructureException(string message) : base(message)
        {
        }
    }
}
