namespace Bluewire.Conventions
{
    public class VersionSemantics
    {
        public SemanticVersion GetStartOfMajorMinor(SemanticVersion semVer) => new SemanticVersion(semVer.Major, semVer.Major, 0, "beta");

        /// <summary>
        /// Returns true if 'subject' is likely to be in the ancestry chain of 'reference'.
        /// </summary>
        /// <remarks>
        /// This cannot be 100% accurate since it does not have knowledge of the actual topology.
        /// It implements heuristics based on our workflows.
        /// If 'lastSubjectMasterBuild' is not null, it specifies the build number of the last beta
        /// build of subject's major.minor on the master branch.
        /// </remarks>
        public bool IsAncestor(SemanticVersion reference, SemanticVersion subject, int? lastSubjectMasterBuild = null)
        {
            // If the subject is not canonical, we cannot determine whether it is an ancestor.
            if (!IsCanonicalVersion(subject)) return false;

            var referenceMajor = int.Parse(reference.Major);
            var referenceMinor = int.Parse(reference.Minor);

            var subjectMajor = int.Parse(subject.Major);
            var subjectMinor = int.Parse(subject.Minor);

            if (referenceMajor < subjectMajor) return false;

            if (referenceMajor == subjectMajor)
            {
                if (referenceMinor < subjectMinor) return false;
                if (referenceMinor == subjectMinor)
                {
                    // Same major.minor. Can compare build numbers.

                    // If reference's build number is before subject's, subject cannot possibly be in reference's ancestory.
                    if (reference.Build < subject.Build) return false;

                    // Subject's build number is before reference's.

                    // If tags are the same, they're in the same first-parent ancestory chain.
                    if (reference.SemanticTag == subject.SemanticTag) return true;

                    // If subject's build number is before the next major.minor was created, it is in the ancestory of all release and rc branches of that major.minor.
                    if (subject.Build <= lastSubjectMasterBuild)
                    {
                        switch (reference.SemanticTag)
                        {
                            case "rc":
                            case "release":
                                return true;
                        }
                    }
                    // If subject is the initial build of this version, it must be in the ancestry even though the reference tag is not canonical.
                    if (subject.Build == 0) return true;

                    // Cannot otherwise sanely compare build numbers from different tags unless we have more topology information.
                    return false;
                }
            }

            // Reference's major.minor is newer than subject's.
            // If subject is the initial build of that version, it is most definitely in the ancestory of reference's major.minor.0.
            if (subject.Build == 0) return true;
            // If subject's build number is before the next major.minor was created, it is in the ancestory of reference's major.minor.0.
            if (lastSubjectMasterBuild != null && subject.Build <= lastSubjectMasterBuild) return true;
            return false;
        }

        /// <summary>
        /// Returns true if the specified version is 'canonical', ie. its tag uniquely identifies an ancestry chain.
        /// Release, candidate and beta/trunk branches have canonical history, while alpha or canary branches do not.
        /// </summary>
        public bool IsCanonicalVersion(SemanticVersion semVer)
        {
            switch (semVer.SemanticTag)
            {
                case "beta":
                case "rc":
                case "release":
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Get the minimum number of merge commits to integrate 'subjectTag' into 'referenceTag'.
        /// </summary>
        private int? GetRelativeWeakness(string referenceTag, string subjectTag)
        {
            if (referenceTag == "beta")
            {
                if (subjectTag == "beta") return 0;
                return null;
            }
            if (referenceTag == "rc")
            {
                if (subjectTag == "beta") return 1;
                if (subjectTag == "rc") return 0;
                return null;
            }
            if (referenceTag == "release")
            {
                if (subjectTag == "beta") return 2;
                if (subjectTag == "rc") return 1;
                if (subjectTag == "release") return 0;
                return null;
            }
            return null;
        }
    }
}
