using System;

namespace Bluewire.Common.GitWrapper.Model
{
    /// <summary>
    /// Represents the difference between 'include' and 'exclude', ie:
    /// * the commits in 'include' which do not exist in 'exclude',
    /// * exclude..include,
    /// * ^exclude include
    /// </summary>
    public class Difference : IRefRange
    {
        private readonly Ref exclude;
        private readonly Ref include;

        public Difference(Ref exclude, Ref include)
        {
            if (exclude == null) throw new ArgumentNullException(nameof(exclude));
            if (include == null) throw new ArgumentNullException(nameof(include));
            this.exclude = exclude;
            this.include = include;
        }

        public override string ToString()
        {
            return $"{exclude}..{include}";
        }

        public static implicit operator string(Difference difference)
        {
            return difference?.ToString();
        }
    }
}
