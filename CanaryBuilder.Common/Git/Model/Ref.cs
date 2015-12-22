﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanaryBuilder.Common.Git.Model
{
    /// <summary>
    /// Wrapper for Git ref names or hashes.
    /// </summary>
    /// <remarks>
    /// This type is basically a trivial wrapper for a not-null, not-whitespace string.
    /// Equality is defined as 'case-sensitive, ordinal'. Note that this class knows nothing
    /// about repository structure and cannot tell if two different ref strings point to the
    /// same object.
    /// </remarks>
    public class Ref
    {
        private readonly string refName;

        public Ref(string refName)
        {
            Validate(refName);
            this.refName = refName;
        }

        public static void Validate(string refName)
        {
            if (String.IsNullOrWhiteSpace(refName)) throw new ArgumentNullException(nameof(refName), "No refname specified.");
            if (refName.Any(Char.IsWhiteSpace)) throw new ArgumentException($"'{refName}' is not a valid ref because it contains whitespace.", nameof(refName));
        }

        public override string ToString()
        {
            return refName;
        }

        protected bool Equals(Ref other)
        {
            return string.Equals(refName, other.refName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Ref) obj);
        }

        public override int GetHashCode()
        {
            return refName.GetHashCode();
        }
    }
}
