﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
            if (IsInHierarchy(hierarchyName, @ref)) return EnsureFullyQualifed(hierarchyName, @ref);
            if (IsFullyQualifed(@ref)) throw new ArgumentException($"Ref '{@ref}' is already qualified. Cannot prefix with 'refs/{hierarchyName}/'.");
            return new Ref($"refs/{hierarchyName}/{@ref}");
        }

        public static bool IsFullyQualifed(Ref @ref)
        {
            return @ref.ToString().StartsWith("refs/");
        }

        private static Ref EnsureFullyQualifed(string hierarchyName, Ref @ref)
        {
            if (IsFullyQualifed(@ref)) return @ref;
            if (IsInHierarchy(hierarchyName, @ref)) return new Ref($"refs/{@ref}");
            throw new ArgumentException($"Ref '{@ref}' is not in the specified hierarchy: {hierarchyName}.");
        }

        public static Ref GetRemoteRef(Ref @ref, string remoteHierarchy = "origin")
        {
            if (IsInHierarchy(remoteHierarchy, @ref)) return @ref;
            return new Ref($"{remoteHierarchy}/{@ref}");
        }

        /// <summary>
        /// If the ref is in the specified hierarchy, remove the 'refs/X' part.
        /// If the ref is not in the specified hierarchy, throws an ArgumentException because
        /// without qualification a ref will be left ambiguous.
        /// </summary>
        public static Ref Unqualify(string hierarchyName, Ref @ref)
        {
            if (!IsInHierarchy(hierarchyName, @ref)) throw new ArgumentException($"Ref '{@ref}' is not in the '{hierarchyName}' hierarchy.");
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
            foreach (var prefix in prefixes)
            {
                if (parts[i] == prefix)
                {
                    i++;
                }
                else if (i > 0)
                {
                    return @ref;
                }
            }
            if (parts.Length <= i) throw new ArgumentException($"Ref '{@ref}' is described entirely by the specified prefixes: {String.Join("/", prefixes)}");
            return new Ref(String.Join("/", parts.Skip(i)));
        }

        private static readonly Regex rxSha1 = new Regex("^[a-f0-9]{40}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static bool IsSha1Hash(Ref @ref) => rxSha1.IsMatch(@ref.ToString());
    }
}
