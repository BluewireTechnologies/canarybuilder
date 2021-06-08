using System;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Stash.Remote;

namespace Bluewire.Stash.Tool
{
    class CommandServicesHelper
    {
        public async Task<GitSession?> PrepareGitSession(VerboseLogger logger)
        {
            var invocationLogger = logger.GetConsoleInvocationLogger(VerbosityLevels.ShowGitInvocations);
            var git = await new GitFinder(invocationLogger).FromEnvironment();
            if (git == null)
            {
                logger.WriteLine(VerbosityLevels.ShowWarnings, "Unable to find git.exe. Some features may be unavailable.");
                return null;
            }
            return new GitSession(git, invocationLogger);
        }

        public async Task<IGitFilesystemContext?> TryFindGitFilesystemContext(GitSession? gitSession, ArgumentValue<string?> hintPath, VerboseLogger logger)
        {
            if (hintPath.Value == null) return null;

            var wasExplicitlyRequested = hintPath.Source == ArgumentSource.Argument;
            if (gitSession == null)
            {
                logger.WriteLine(wasExplicitlyRequested ? VerbosityLevels.Default : VerbosityLevels.ShowWarnings, "Git is not available. No repository or working copy information can be used.");
                return null;
            }

            var workingCopyOrRepo = await gitSession.FindWorkingCopyContaining(hintPath.Value);
            if (workingCopyOrRepo == null)
            {
                logger.WriteLine(wasExplicitlyRequested ? VerbosityLevels.Default : VerbosityLevels.ShowWarnings, $"No repository or working copy could be found: {hintPath.Value}");
                return null;
            }

            return workingCopyOrRepo;
        }

        public async Task<VersionMarker> GetVersionMarkerFromGitWorkingCopy(GitSession? gitSession, IGitFilesystemContext? gitFilesystemContext)
        {
            if (gitSession == null || gitFilesystemContext == null)
            {
                throw new ApplicationException("No version information was specified, and no Git working copy was available to infer it.");
            }

            var headRef = await gitSession.ResolveRef(gitFilesystemContext, Ref.Head);
            if (headRef == null)
            {
                throw new ApplicationException($"Unable to infer a version from the Git repository at {gitFilesystemContext.GitWorkingDirectory}");
            }
            return new VersionMarker(headRef.ToString());
        }

        public async Task<ICommitTopology?> GetCommitTopology(GitSession? gitSession, IGitFilesystemContext? gitFilesystemContext)
        {
            if (gitSession != null && gitFilesystemContext != null)
            {
                return new GitCommitTopology(gitSession, gitFilesystemContext);
            }
            return null;
        }

        public async Task<IRemoteStashRepository> GetRemoteStashRepository(IAuthentication authentication, Uri? uri, string? remoteStashName, CancellationToken token)
        {
            if (uri == null) throw new ApplicationException("No remote stash root URI specified.");
            if (remoteStashName == null) throw new ApplicationException("No remote stash name specified.");
            var authenticationProvider = await authentication.Create();
            var authResult = await authenticationProvider.Authenticate(token);
            return new HttpRemoteStashRepository(uri, remoteStashName, authResult);
        }
    }
}
