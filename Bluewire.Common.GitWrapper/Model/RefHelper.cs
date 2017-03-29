using System;
using System.Collections.Generic;
using System.Linq;

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

        public static Ref GetRemoteRef(Ref @ref, string RemoteHierarchy = "origin")
        {
            if (IsInTagsHierarchy(@ref)) return @ref;
            if (IsInHierarchy(RemoteHierarchy, @ref)) return @ref;
            return new Ref($"{RemoteHierarchy}/{@ref}");
        }

        /// <summary>
        /// If the ref is in the specified hierarchy, remove the 'refs/X' part.
        /// If the ref is not in the specified hierarchy, throws an ArgumentException because
        /// without qualification a ref will be left ambiguous.
        /// </summary>
        public static Ref Unqualify(string hierarchyName, Ref @ref)
        {
            if(!IsInHierarchy(hierarchyName, @ref)) throw new ArgumentException($"Ref '{@ref}' is not in the '{hierarchyName}' hierarchy.");
            return StripHierarchyPrefixes(@ref, "refs", hierarchyName);
        }

        /// <summary>
        /// For a ref 'b/c/d/e', stripping the prefix a,b,c should result in 'd/e'.
        /// For a ref 'a/b/c', stripping the prefix b,c should result in 'a/b/c'.
        /// For a ref 'a/b', stripping the prefix a,b should cause an exception.
        /// </summary>
        /// <returns></returns>
        private static Ref StripHierarchyPrefixes(Ref @ref, params string[] prefixes)
        {
            var parts = @ref.ToString().Split('/');
            var i = 0;
            foreach(var prefix in prefixes)
            {
                if(parts[i] == prefix)
                {
                    i++;
                }
                else if(i > 0)
                {
                    return @ref;
                }
            }
            if(parts.Length <= i) throw new ArgumentException($"Ref '{@ref}' is described entirely by the specified prefixes: {String.Join("/", prefixes)}");
            return new Ref(String.Join("/", parts.Skip(i)));
        }
    }
}
