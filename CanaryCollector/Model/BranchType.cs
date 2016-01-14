namespace CanaryCollector.Model
{
    /// <summary>
    /// Priority. Lower values are more important.
    /// </summary>
    public enum BranchType
    {
        BugFix = 0,
        Feature = 1,
        TechnicalDebt = 2,
        Performance = 3
    }
}