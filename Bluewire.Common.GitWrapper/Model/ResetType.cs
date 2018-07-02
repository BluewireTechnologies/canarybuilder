namespace Bluewire.Common.GitWrapper.Model
{
    public enum ResetType
    {
        // Update ref only
        Soft,
        // Reset index
        Mixed,
        // Reset index and working tree
        Hard,
        // Update files which are not staged or modified
        Merge,
        // Update files which are not modified
        Keep
    }
}
