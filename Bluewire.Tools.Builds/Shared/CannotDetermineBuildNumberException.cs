using System;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Tools.Builds.Shared
{
    public class CannotDetermineBuildNumberException : ApplicationException
    {
        public CannotDetermineBuildNumberException(Ref start, Ref subject)
            : base($"Could not determine build number for the commit {subject} using {start} as a starting point. Is the graph properly connected?")
        {
        }
    }
}
