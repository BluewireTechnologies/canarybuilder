using System;

namespace Bluewire.Tools.GitRepository
{
    public class BuildNumberNotFoundException : InvalidOperationException
    {
        public BuildNumberNotFoundException(string message) : base(message)
        {
        }
    }
}
