using System;

namespace Bluewire.Tools.GitRepository
{
    public class BuildNumberOutOfRangeException : InvalidOperationException
    {
        public BuildNumberOutOfRangeException(string message) : base(message)
        {
        }
    }
}
