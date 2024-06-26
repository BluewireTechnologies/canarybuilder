using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;

namespace Bluewire.RepositoryLinter;

public class Constants
{
    /// <summary>
    /// Repositories which need linting, and rules for selecting branches.
    /// </summary>
    public static readonly ImmutableArray<SubjectRepository> Repositories = new []
    {
        new SubjectRepository("epro")
        {
            GetBranchRules = x =>
            {
                if (new Ref("origin/master").Equals(x)) return BranchRules.All;
                if (!RefHelper.IsInHierarchy("origin", x)) return BranchRules.None;
                var name = RefHelper.Unqualify("origin", x);
                if (!StructuredBranch.TryParse(name, out var branch)) return BranchRules.None;
                if (branch.Namespace != "release" &&
                    branch.Namespace != "backport" &&
                    branch.Namespace != "candidate")
                {
                    return BranchRules.None;
                }

                switch (branch.Name)
                {
                    // Specific versions:
                    case "22.01":
                        return new BranchRules { CheckPreReleasePackages = true };
                }

                if (!Version.TryParse(branch.Name, out var version)) return BranchRules.None;
                if (version >= new Version(24, 01)) return new BranchRules { CheckPreReleasePackages = true, CheckTargetFrameworks = true };
                if (version >= new Version(23, 01)) return new BranchRules { CheckPreReleasePackages = true };
                return BranchRules.None;
            },
        },
        new SubjectRepository("epro-dictation-app"),
        new SubjectRepository("epro-filedrop"),
        new SubjectRepository("epro-masterfiles"),
        new SubjectRepository("epro-metaparser"),
        new SubjectRepository("epro-ocr"),

        new SubjectRepository("server-monitor"),
        new SubjectRepository("speech-tools"),
        new SubjectRepository("third-party-integrations"),

        new SubjectRepository("Bluewire.Common"),
        new SubjectRepository("Bluewire.Common.Console"),
        new SubjectRepository("Bluewire.Dictation"),
        new SubjectRepository("Bluewire.Epro.Scripting"),
        new SubjectRepository("Bluewire.Indexing"),
        new SubjectRepository("Bluewire.Licensing"),
        new SubjectRepository("Bluewire.Metrics"),
        new SubjectRepository("Bluewire.NHibernate.History"),
        new SubjectRepository("Bluewire.Reporting"),
        new SubjectRepository("Bluewire.Snomed"),
        new SubjectRepository("Bluewire.Speech"),
        new SubjectRepository("Bluewire.Webhooks"),
        new SubjectRepository("canarybuilder"),
    }.ToImmutableArray();

    public static readonly ImmutableHashSet<string> BlessedTargetFrameworks = new []
    {
        "net48",
        "netstandard1.0",
        "netstandard1.3",
        "netstandard1.6",
        "netstandard2.0",
        "netstandard2.1",
        "net6",
        "net6.0",
        "net6.0-windows10.0.18362.0",
    }.ToImmutableHashSet();

    public static readonly ImmutableDictionary<string, Version> MinimumPackageVersions = new Dictionary<string, Version>
    {
        ["NUnit.ConsoleRunner"] = new Version(3, 16, 2),
        ["log4net"] = new Version(2, 0, 15),
        ["JetBrains.dotCover.CommandLineTools"] = new Version(2023, 2, 3),

        // Our packages.
        // These don't need to be up-to-the-minute. Subminor version bumps resulting from
        // dependency updates or build changes which don't change observed behaviour can
        // be ignored. Consumer should use assembly binding redirects if necessary.
        ["Bluewire.Common"] = new Version(15, 0, 0),
        ["Bluewire.Common.Buffers"] = new Version(2, 0, 0),
        ["Bluewire.Common.Certificates"] = new Version(2, 0, 0),
        ["Bluewire.Common.Communication"] = new Version(13, 2, 0),
        ["Bluewire.Common.Data"] = new Version(1, 0, 0),
        ["Bluewire.Common.Resource"] = new Version(13, 1, 0),
        ["Bluewire.Content"] = new Version(2, 1, 0),
        ["Bluewire.Content.Html"] = new Version(4, 0, 0),
        ["Bluewire.Contracts.Testing"] = new Version(1, 0, 0),
        ["Bluewire.Diff"] = new Version(1, 0, 0),
        ["Bluewire.Logging.Log4Net"] = new Version(2, 1, 0),
        ["Bluewire.Common.Console"] = new Version(12, 0, 0),
        ["Bluewire.Common.Console.Client"] = new Version(10, 0, 0),
        ["Bluewire.Common.Console.Formatting"] = new Version(2, 0, 0),
        ["Bluewire.Common.Console.NUnit3"] = new Version(10, 0, 0),
        ["Bluewire.Indexing.Query"] = new Version(24, 0, 0),
        ["Bluewire.Indexing.Storage"] = new Version(24, 2, 2),
        ["Bluewire.ClinicalCodingResources"] = new Version(22, 0),
        ["Schema.SnomedRF2"] = new Version(3, 1, 0),
        ["Bluewire.Audio.Formats"] = new Version(2, 0, 0),
        ["Bluewire.Audio.Ogg"] = new Version(2, 0, 0),
        ["Bluewire.Audio.Transcoding"] = new Version(2, 0, 0),
        ["Bluewire.Dictation.Storage"] = new Version(1, 2, 0),
        ["Bluewire.Speech.SoundTouch"] = new Version(2, 0, 0),
        ["Bluewire.Metrics.Json"] = new Version(3, 0, 0),
        ["Bluewire.Metrics.TimeSeries"] = new Version(2, 0, 0),
        ["Bluewire.MetricsAdapter"] = new Version(4, 0, 0),
        ["Metrics.IISApplicationCounters"] = new Version(2, 0, 0),
        ["Bluewire.Text"] = new Version(4, 5, 0),
        ["Bluewire.Bayes"] = new Version(1, 0, 0),
        ["Bluewire.NHibernate.Audit"] = new Version(11, 0, 0),
        ["Bluewire.IntervalTree"] = new Version(4, 0, 0),
    }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    public static readonly ImmutableDictionary<string, Version> MaximumPackageVersions = new Dictionary<string, Version>
    {
        // Primarily for the Epro repository: build agents do not have the latest C# compiler.
        ["Microsoft.CodeAnalysis.CSharp"] = new Version(4, 7, 0),
        ["Microsoft.CodeAnalysis.CSharp.Workspaces"] = new Version(4, 7, 0),
        ["Microsoft.CodeAnalysis.CSharp.Workspaces.MSBuild"] = new Version(4, 7, 0),
    }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

}
