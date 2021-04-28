using System;

namespace Bluewire.Tools.Builds.Shared
{
    public class RefNotFoundException : ApplicationException
    {
        public RefNotFoundException(string commitRef) : base($"Cannot find the specified ref {commitRef}.")
        {
        }
    }
}
