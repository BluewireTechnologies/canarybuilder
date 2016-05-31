using System;
using System.IO;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Common.GitWrapper
{
    /// <summary>
    /// Location of a Git repository.
    /// </summary>
    public class GitRepository : IGitFilesystemContext
    {
        public string Location { get; }

        public GitRepository(string gitDirPath)
        {
            this.Location = gitDirPath;
            if (!Directory.Exists(Location)) throw new DirectoryNotFoundException($"Repository not found: {gitDirPath}");
        }
        
        public string Path(string relativePath)
        {
            if (relativePath == null) throw new ArgumentNullException(nameof(relativePath));
            return System.IO.Path.Combine(Location, relativePath);
        }

        public Uri GetLocationUri()
        {
            return new UriBuilder { Scheme = "file", Path = Location }.Uri;
        }

        public static GitRepository Find(string path)
        {
            var directory = new DirectoryInfo(path);
            if (directory.Name.EndsWith(".git"))
            {
                return new GitRepository(path);
            }
            return new GitWorkingCopy(path).GetDefaultRepository();
        }

        IConsoleProcess IGitFilesystemContext.Invoke(CommandLine cmd)
        {
            // We could just as easily use '--git-dir <location>' here, I think, but I'm not sure of all the implications.
            return cmd.RunFrom(Location);
        }
    }
}
