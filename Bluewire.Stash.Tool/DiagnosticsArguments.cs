namespace Bluewire.Stash.Tool
{
    public class DiagnosticsArguments
    {
        public DiagnosticsArguments(AppEnvironment appEnvironment)
        {
            AppEnvironment = appEnvironment;
        }

        public AppEnvironment AppEnvironment { get; }
    }
}
