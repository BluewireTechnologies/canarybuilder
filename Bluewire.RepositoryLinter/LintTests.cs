using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.RepositoryLinter
{
    [TestFixture]
    public class LintTests
    {
        private GitSession session = null!;

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
        }

        private const string LocalWorkspaceRoot = @"e:\dev";

        private IGitFilesystemContext GetWorkingCopy(SubjectRepository subject)
        {
            var path = Path.Combine(LocalWorkspaceRoot, subject.Name);
            var workingCopy = new GitWorkingCopy(path);
            workingCopy.CheckExistence();
            workingCopy.GetDefaultRepository();
            return workingCopy;
        }

        /// <summary>
        /// Checks all rules at once. Faster than doing them individually as information is fetched only once.
        /// </summary>
        [TestCaseSource(typeof(Constants), nameof(Constants.Repositories))]
        public async Task AllRules(SubjectRepository subject)
        {
            var workingCopy = GetWorkingCopy(subject);
            var explorer = new RepositoryExplorer(workingCopy, subject);

            var failureCount = 0;
            await foreach (var branchCase in explorer.GetProjectFiles(session, x => x.HasAnyRules))
            {
                var failures = new List<Failure>();
                failures.AddRange(new NoPreReleasePackagesRule(subject).GetFailures(branchCase.Branch, branchCase.Projects));
                failures.AddRange(new TargetFrameworkVersionsAreBlessedRule(subject).GetFailures(branchCase.Branch, branchCase.Projects));
                failures.AddRange(new PackagesAreUpToDateRule(subject).GetFailures(branchCase.Branch, branchCase.Projects));
                failures.AddRange(new PackagesAreSupportedByBuildAgentsRule(subject).GetFailures(branchCase.Branch, branchCase.Projects));

                if (!failures.Any()) continue;

                TestContext.WriteLine($"Branch {branchCase.Branch}: {failures.Count} failure(s)");
                foreach (var byProject in failures.GroupBy(x => x.ProjectFile))
                {
                    TestContext.WriteLine($" * Project {byProject.Key.Path}: {byProject.Count()} failure(s)");
                    foreach (var failure in byProject)
                    {
                        TestContext.WriteLine($"   * {failure.Message}");
                    }
                }
                failureCount += failures.Count;
            }
            Assert.That(failureCount, Is.Zero);
        }

        [Explicit]
        [TestCaseSource(typeof(Constants), nameof(Constants.Repositories))]
        public async Task NoPreReleasePackages(SubjectRepository subject)
        {
            var workingCopy = GetWorkingCopy(subject);
            var explorer = new RepositoryExplorer(workingCopy, subject);

            var failureCount = 0;
            await foreach (var branchCase in explorer.GetProjectFiles(session, x => x.CheckPreReleasePackages))
            {
                var failures = new NoPreReleasePackagesRule(subject).GetFailures(branchCase.Branch, branchCase.Projects).ToArray();
                ReportForSingleRule(branchCase.Branch, failures);
                failureCount += failures.Length;
            }
            Assert.That(failureCount, Is.Zero);
        }

        [Explicit]
        [TestCaseSource(typeof(Constants), nameof(Constants.Repositories))]
        public async Task TargetFrameworkVersionsAreBlessed(SubjectRepository subject)
        {
            var workingCopy = GetWorkingCopy(subject);
            var explorer = new RepositoryExplorer(workingCopy, subject);

            var failureCount = 0;
            await foreach (var branchCase in explorer.GetProjectFiles(session, x => x.CheckTargetFrameworks))
            {
                var failures = new TargetFrameworkVersionsAreBlessedRule(subject).GetFailures(branchCase.Branch, branchCase.Projects).ToArray();
                ReportForSingleRule(branchCase.Branch, failures);
                failureCount += failures.Length;
            }
            Assert.That(failureCount, Is.Zero);
        }

        [Explicit]
        [TestCaseSource(typeof(Constants), nameof(Constants.Repositories))]
        public async Task PackagesAreUpToDate(SubjectRepository subject)
        {
            var workingCopy = GetWorkingCopy(subject);
            var explorer = new RepositoryExplorer(workingCopy, subject);

            var failureCount = 0;
            await foreach (var branchCase in explorer.GetProjectFiles(session, x => x.CheckMinimumPackageVersions))
            {
                var failures = new PackagesAreUpToDateRule(subject).GetFailures(branchCase.Branch, branchCase.Projects).ToArray();
                ReportForSingleRule(branchCase.Branch, failures);
                failureCount += failures.Length;
            }
            Assert.That(failureCount, Is.Zero);
        }

        [Explicit]
        [TestCaseSource(typeof(Constants), nameof(Constants.Repositories))]
        public async Task PackagesAreSupportedByBuildAgents(SubjectRepository subject)
        {
            var workingCopy = GetWorkingCopy(subject);
            var explorer = new RepositoryExplorer(workingCopy, subject);

            var failureCount = 0;
            await foreach (var branchCase in explorer.GetProjectFiles(session, x => x.CheckMaximumPackageVersions))
            {
                var failures = new PackagesAreSupportedByBuildAgentsRule(subject).GetFailures(branchCase.Branch, branchCase.Projects).ToArray();
                ReportForSingleRule(branchCase.Branch, failures);
                failureCount += failures.Length;
            }
            Assert.That(failureCount, Is.Zero);
        }

        private static void ReportForSingleRule(Ref branch, IReadOnlyCollection<Failure> failures)
        {
            TestContext.WriteLine($"Branch {branch}: {failures.Count} failure(s)");
            foreach (var failure in failures)
            {
                TestContext.WriteLine($" * Project {failure.ProjectFile.Path}: {failure.Message}");
            }
        }
    }
}
