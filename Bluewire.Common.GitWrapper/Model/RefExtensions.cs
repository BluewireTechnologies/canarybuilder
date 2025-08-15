using System;

namespace Bluewire.Common.GitWrapper.Model
{
    public static class RefExtensions
    {
        public static Ref Parent(this Ref @ref)
        {
            return new Ref(@ref + "^");
        }

        public static Ref Ancestor(this Ref @ref, int distance)
        {
            if (distance < 1) throw new ArgumentException("Must be a positive value.", nameof(distance));
            return new Ref(@ref + new String('^', distance));
        }

        public static bool IsNamedRefInRemote(this Ref @ref, Remote remote)
        {
            return TryGetRemoteLocalRef(@ref, remote, out _);
        }

        /// <summary>
        /// If the ref belongs to the specified remote, strips the remote name.
        /// </summary>
        /// <returns>True if the ref belongs to the specified remote.</returns>
        public static bool TryGetRemoteLocalRef(this Ref @ref, Remote remote, out Ref localRef)
        {
            var name = @ref.ToString();
            var remotePrefix = $"{remote.Name}/";
            if (name.StartsWith(remotePrefix, StringComparison.Ordinal))
            {
                var localName = name.Substring(remotePrefix.Length);
                if (Ref.IsValidName(localName))
                {
                    localRef = new Ref(localName);
                    return true;
                }
            }
            localRef = null;
            return false;
        }
    }
}
