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
    }
}