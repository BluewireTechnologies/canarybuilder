using System;
using System.IO;
using System.Linq;
using Bluewire.Conventions;
using McMaster.Extensions.CommandLineUtils;

namespace Bluewire.Stash.Tool
{
    public class ArgumentsProvider
    {
        private readonly IApplication application;

        public ArgumentsProvider(IApplication application)
        {
            this.application = application;
        }

        public AppEnvironment GetAppEnvironment(CommandOption<string> gitTopologyPathOption, CommandOption<string> stashRootOption, CommandOption<string> remoteStashRootOption, CommandOption<string> clientSecretOption)
        {
            return new AppEnvironment(
                GetGitTopologyPath(gitTopologyPathOption),
                GetStashRoot(stashRootOption),
                GetRemoteStashRoot(remoteStashRootOption))
            {
                Authentication = GetAuthentication(clientSecretOption)
            };
        }

        private ArgumentValue<string?> GetGitTopologyPath(CommandOption<string> gitTopologyPathOption)
        {
            var cliValue = gitTopologyPathOption.Value();
            if (cliValue != null)
            {
                if (cliValue == "") return new ArgumentValue<string?>(null, ArgumentSource.Argument);
                var resolved = ResolveRelativePath(cliValue);
                return new ArgumentValue<string?>(resolved, ArgumentSource.Argument);
            }
            return new ArgumentValue<string?>(application.GetCurrentDirectory(), ArgumentSource.Default);
        }

        private ArgumentValue<string> GetStashRoot(CommandOption<string> stashRootOption)
        {
            var cliValue = stashRootOption.Value();
            if (cliValue != null) return new ArgumentValue<string>(EnsureTrailingSlash(cliValue), ArgumentSource.Argument);
            var envValue = application.GetEnvironmentVariable("STASH_ROOT");
            if (envValue != null) return new ArgumentValue<string>(EnsureTrailingSlash(envValue), ArgumentSource.Environment);
            var defaultValue = Path.Combine(application.GetTemporaryDirectory(), ".stashes");
            return new ArgumentValue<string>(EnsureTrailingSlash(defaultValue), ArgumentSource.Default);
        }

        private ArgumentValue<Uri?> GetRemoteStashRoot(CommandOption<string> remoteStashRootOption)
        {
            var cliValue = remoteStashRootOption.Value();
            if (cliValue != null) return new ArgumentValue<Uri?>(CreateValidAbsoluteRootUri(cliValue), ArgumentSource.Argument);
            var envValue = application.GetEnvironmentVariable("REMOTE_STASH_ROOT");
            if (envValue != null) return new ArgumentValue<Uri?>(CreateValidAbsoluteRootUri(envValue), ArgumentSource.Environment);
            return new ArgumentValue<Uri?>(null, ArgumentSource.Default);
        }

        private IAuthentication GetAuthentication(CommandOption<string> clientSecretOption)
        {
            var secret = clientSecretOption.Value();
            if (secret == null) return new PublicClientAuthentication();
            return new ClientSecretAuthentication(secret);
        }

        public ArgumentValue<string> GetStashName(CommandArgument<string> stashNameArgument)
        {
            if (stashNameArgument.Value == null) throw new ArgumentException("Must specify stash name.");
            return new ArgumentValue<string>(stashNameArgument.Value, ArgumentSource.Argument);
        }

        public ArgumentValue<string> GetSourcePath(CommandArgument<string> sourcePathArgument)
        {
            if (sourcePathArgument.Value == null) throw new ArgumentException("Must specify source path.");
            var resolved = ResolveRelativePath(sourcePathArgument.Value);
            return new ArgumentValue<string>(EnsureTrailingSlash(resolved), ArgumentSource.Argument);
        }

        public ArgumentValue<string> GetDestinationPath(CommandArgument<string> destinationPathArgument)
        {
            if (destinationPathArgument.Value == null) throw new ArgumentException("Must specify destination path.");
            var resolved = ResolveRelativePath(destinationPathArgument.Value);
            return new ArgumentValue<string>(EnsureTrailingSlash(resolved), ArgumentSource.Argument);
        }

        public ArgumentValue<VersionMarker> GetRequiredVersionMarker(CommandOption<SemanticVersion?> semanticVersionOption, CommandOption<string> commitHashOption, CommandOption<VersionMarker?>? versionMarkerOption = null)
        {
            var argument = GetVersionMarker(semanticVersionOption, commitHashOption, versionMarkerOption);
            if (argument.Value == null) throw new ArgumentException("No version specified.");
            return new ArgumentValue<VersionMarker>(argument.Value.Value, argument.Source);
        }

        public ArgumentValue<VersionMarker?> GetVersionMarker(CommandOption<SemanticVersion?> semanticVersionOption, CommandOption<string> commitHashOption, CommandOption<VersionMarker?>? versionMarkerOption = null)
        {
            var semanticVersion = semanticVersionOption.ParsedValue;
            var commitHash = string.IsNullOrEmpty(commitHashOption.Value()) ? null : commitHashOption.Value();
            var versionMarker = versionMarkerOption?.ParsedValue;

            if (versionMarker != null)
            {
                if (semanticVersion != null || commitHash != null) throw new ArgumentException($"Cannot specify --{semanticVersionOption.LongName} or --{commitHashOption.LongName} if --{versionMarkerOption!.LongName} is specified.");
                return new ArgumentValue<VersionMarker?>(versionMarker, ArgumentSource.Argument);
            }

            if (semanticVersion == null)
            {
                if (commitHash == null) return new ArgumentValue<VersionMarker?>(null, ArgumentSource.Default);
                return new ArgumentValue<VersionMarker?>(new VersionMarker(commitHash), ArgumentSource.Argument);
            }
            else
            {
                if (commitHash == null) return new ArgumentValue<VersionMarker?>(new VersionMarker(semanticVersion), ArgumentSource.Argument);
                return new ArgumentValue<VersionMarker?>(new VersionMarker(semanticVersion, commitHash), ArgumentSource.Argument);
            }
        }

        public ArgumentValue<string?> GetOptionalRemoteStashName(CommandArgument<string> stashNameArgument)
        {
            if (stashNameArgument.Value == null) return new ArgumentValue<string?>(null, ArgumentSource.Default);
            return new ArgumentValue<string?>(stashNameArgument.Value, ArgumentSource.Argument);
        }

        public ArgumentValue<bool> GetFlag(CommandOption flagOption)
        {
            if (!flagOption.Values.Any()) return new ArgumentValue<bool>(false, ArgumentSource.Default);
            return new ArgumentValue<bool>(true, ArgumentSource.Argument);
        }

        public ArgumentValue<int> GetVerbosityLevel(CommandOption verbosityOption)
        {
            if (!verbosityOption.Values.Any()) return new ArgumentValue<int>(0, ArgumentSource.Default);
            return new ArgumentValue<int>(verbosityOption.Values.Count, ArgumentSource.Argument);
        }

        private string ResolveRelativePath(string path)
        {
            return Path.GetFullPath(Path.Combine(application.GetCurrentDirectory(), path));
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path ?? "";
            if (path.Intersect(Path.GetInvalidPathChars()).Any()) return path;
            if (path.Last() != Path.DirectorySeparatorChar) return path + Path.DirectorySeparatorChar;
            return path;
        }

        private static Uri? CreateValidAbsoluteRootUri(string uriString)
        {
            if (string.IsNullOrWhiteSpace(uriString)) return null;
            if (!Uri.TryCreate(uriString.TrimEnd('/') + '/', UriKind.Absolute, out var uri)) return null;
            return uri;
        }
    }
}
