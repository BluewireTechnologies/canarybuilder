using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluewire.Common.Git.IntegrationTests.TestInfrastructure
{
    public static class FileSystemHelpers
    {
        public static void CleanDirectory(string directory)
        {
            Clean(new DirectoryInfo(directory));
        }

        private static void Clean(FileSystemInfo entry)
        {
            var directory = entry as DirectoryInfo;
            if (directory != null)
            {
                foreach (var fileSystemInfo in directory.EnumerateFileSystemInfos())
                {
                    Clean(fileSystemInfo);
                }
            }
            entry.Attributes = FileAttributes.Normal;
            entry.Delete();
        }
    }
}
