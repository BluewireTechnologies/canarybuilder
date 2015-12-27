namespace Bluewire.Common.Git.Model
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