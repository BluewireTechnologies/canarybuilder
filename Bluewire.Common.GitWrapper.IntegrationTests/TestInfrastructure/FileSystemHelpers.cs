using System.IO;

namespace Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure
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
