using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Stash.Tool
{
    public class AlphaTagFormatter
    {
        public string Format(Ref hash) => $"alpha.g{hash.ToString().Substring(0, 10)}";
    }
}
