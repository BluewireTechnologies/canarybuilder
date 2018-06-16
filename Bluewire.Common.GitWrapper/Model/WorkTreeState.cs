namespace Bluewire.Common.GitWrapper.Model
{
    public enum WorkTreeState
    {
        // Internal use only.
        Unknown = 0,

        Unmodified,
        Modified,
        Deleted,
        UpdatedButUnmerged,
        Untracked,
        Ignored
    }
}
