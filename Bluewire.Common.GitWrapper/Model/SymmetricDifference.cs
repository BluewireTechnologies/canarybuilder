using System;

namespace Bluewire.Common.GitWrapper.Model
{
    /// <summary>
    /// Represents the symmetric difference between 'left' and 'right', ie:
    /// * the commits in 'left' and 'right' which do not exist in both,
    /// * left...right,
    /// * left right --not $(git-merge-base --all left right)
    /// </summary>
    public class SymmetricDifference
    {
        private readonly Ref left;
        private readonly Ref right;

        public SymmetricDifference(Ref left, Ref right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));
            this.left = left;
            this.right = right;
        }

        public override string ToString()
        {
            return $"{left}...{right}";
        }

        public static implicit operator string(SymmetricDifference difference)
        {
            return difference?.ToString();
        }
    }
}
