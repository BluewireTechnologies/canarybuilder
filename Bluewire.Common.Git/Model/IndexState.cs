namespace Bluewire.Common.Git.Model
{
    public enum IndexState
    {
        // Internal use only.
        Unknown = 0,

        Unmodified,
        Modified,
        Added,
        Deleted,
        Renamed,
        Copied,
        UpdatedButUnmerged,
        Untracked,
        Ignored
    }
}