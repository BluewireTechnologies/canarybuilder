using System;

namespace Bluewire.Common.GitWrapper.Model
{
    public static class RefHelper
    {
        public static bool IsInTagsHierarchy(Ref @ref)
        {
            if (@ref.ToString().StartsWith("refs/tags/")) return true;
            if (@ref.ToString().StartsWith("tags/")) return true;
            return false;
        }

        public static bool IsInHeadsHierarchy(Ref @ref)
        {
            if (@ref.ToString().StartsWith("refs/heads/")) return true;
            if (@ref.ToString().StartsWith("heads/")) return true;
            return false;
        }

        public static bool IsInHierarchy(string hierarchyName, Ref @ref)
        {
            var name = @ref.ToString();
            if (name.StartsWith($"refs/{hierarchyName}/")) return true;
            if (name.StartsWith($"{hierarchyName}/")) return true;
            return false;
        }

        public static Ref PutInHierarchy(string hierarchyName, Ref @ref)
        {
            if (IsInHierarchy(hierarchyName, @ref)) return @ref;
            if (@ref.ToString().StartsWith("refs/")) throw new ArgumentException($"Ref '{@ref}' is already qualified. Cannot prefix with 'refs/{hierarchyName}/'.");
            return new Ref($"refs/{hierarchyName}/{@ref}");
        }
    }
}
