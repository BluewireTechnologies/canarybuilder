using System;
using System.IO;
using Bluewire.Build.DeploymentReadmeParser;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.RepositoryLinter.ReadmeValidator
{
    public class GitDocumentSource : IDocumentSource
    {
        private readonly IGitFilesystemContext workingCopyOrRepo;
        private readonly GitSession session;
        private readonly Ref commit;

        public Uri Root { get; } = new Uri("git://repository.root/");

        public GitDocumentSource(IGitFilesystemContext workingCopyOrRepo, GitSession session, Ref commit)
        {
            this.workingCopyOrRepo = workingCopyOrRepo;
            this.session = session;
            this.commit = commit;
        }

        public TextReader Open(Uri uri)
        {
            var relativePath = GetRelativePath(uri);
            var ms = new MemoryStream();
            session.ReadFile(workingCopyOrRepo, commit, relativePath, ms).GetAwaiter().GetResult();
            ms.Position = 0;
            return new StreamReader(ms);
        }

        public bool Exists(Uri uri)
        {
            var relativePath = GetRelativePath(uri);
            return session.FileExists(workingCopyOrRepo, commit, relativePath).GetAwaiter().GetResult();
        }

        private string GetRelativePath(Uri uri)
        {
            var relativePath = Root.MakeRelativeUri(uri);
            return relativePath.OriginalString;
        }

        public Uri CreateUri(string relativePath)
        {
            return new Uri(Root, new Uri(relativePath, UriKind.Relative));
        }
    }
}
