﻿using System;
using System.IO;

namespace Bluewire.Common.GitWrapper
{
    /// <summary>
    /// Location of a Git repository.
    /// </summary>
    public class GitRepository
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

        public static GitRepository Find(string path)
        {
            var directory = new DirectoryInfo(path);
            if (directory.Name.EndsWith(".git"))
            {
                return new GitRepository(path);
            }
            return new GitWorkingCopy(path).GetDefaultRepository();
        }
    }
}