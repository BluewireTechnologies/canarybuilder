using System;

namespace Bluewire.Tools.GitRepository
{
    public class RepositoryStructureException : ApplicationException
    {
        public RepositoryStructureException(string message) : base(message)
        {
        }
    }
}
