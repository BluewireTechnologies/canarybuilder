using System;

namespace Bluewire.Common.GitWrapper.Model
{
    public struct ListPathsOptions
    {
        public ListPathsMode Mode { get; set; }
        public Func<string, bool> PathFilter { get; set; }

        public enum ListPathsMode
        {
            OneLevel,
            Recursive,
            RecursiveFilesOnly,
        }
    }
}
