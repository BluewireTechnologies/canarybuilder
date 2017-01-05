using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Common.GitWrapper.Parsing;

namespace RefCleaner.Collectors
{
    /// <summary>
    /// Collects tags matching a pattern, ordered in descending order, omitting the first X entries.
    /// Used for retaining eg. the last 7 datestamped tags matching a pattern.
    /// </summary>
    public class DatestampedTagCollector : IRefCollector
    {
        private readonly GitSession session;
        private readonly IGitFilesystemContext repository;
        private readonly string remoteName;
        private readonly string pattern;
        private readonly int numberToKeep;
        private readonly GitCommandHelper helper;

        public DatestampedTagCollector(GitSession session, IGitFilesystemContext repository, string remoteName, string pattern, int numberToKeep)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (String.IsNullOrWhiteSpace(pattern)) throw new ArgumentNullException(nameof(pattern));
            this.session = session;
            helper = session.CommandHelper;
            this.repository = repository;
            this.remoteName = remoteName;
            this.pattern = pattern;
            this.numberToKeep = numberToKeep;
        }

        public async Task<IEnumerable<Ref>> CollectRefs()
        {
            var tags = await GetAllMatchingTags();
            return tags
                .OrderByDescending(t => t.ToString())
                .Skip(numberToKeep)
                .Select(t => RefHelper.PutInHierarchy("tags", t))
                .ToArray();
        }

        private async Task<Ref[]> GetAllMatchingTags()
        {
            if (String.IsNullOrWhiteSpace(remoteName))
            {
                return await session.ListTags(repository, new ListTagsOptions { Pattern = pattern });
            }
            else
            {
                var parser = new RefNameColumnLineParser(1);
                var command = helper.CreateCommand("ls-remote", "--refs", "--tags", remoteName, pattern);
                var qualifiedBranches = await helper.RunCommand(repository, command, parser);
                return qualifiedBranches.Select(b => RefHelper.Unqualify("tags", b)).ToArray();
            }
        }
    }
}
