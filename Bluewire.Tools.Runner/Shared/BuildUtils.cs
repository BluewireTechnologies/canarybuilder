using System;
using System.Collections.Generic;
using System.Linq;

namespace Bluewire.Tools.Runner.Shared
{
    public class BuildUtils
    {
        public static Build[] DeduplicateAndPrioritiseResult(Build[] builds)
        {
            if (builds.Length <= 0) throw new ArgumentException("No build results supplied.");

            var selectedBuilds = new List<Build>();

            foreach (var commit in builds.GroupBy(b => b.Commit))
            {
                var releaseBuild = commit.Where(build => build.SemanticVersion.SemanticTag == "release").FirstOrDefault();
                if (releaseBuild.SemanticVersion != null)
                {
                    selectedBuilds.Add(releaseBuild);
                    continue;
                }
                var rcBuild = commit.Where(build => build.SemanticVersion.SemanticTag == "rc").FirstOrDefault();
                if (rcBuild.SemanticVersion != null)
                {
                    selectedBuilds.Add(rcBuild);
                    continue;
                }
                var betaBuild = commit.Where(build => build.SemanticVersion.SemanticTag == "beta").FirstOrDefault();
                if (betaBuild.SemanticVersion != null)
                {
                    selectedBuilds.Add(betaBuild);
                    continue;
                }
            }

            return selectedBuilds.ToArray();
        }
    }
}
