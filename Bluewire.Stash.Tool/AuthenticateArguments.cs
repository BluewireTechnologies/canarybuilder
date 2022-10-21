namespace Bluewire.Stash.Tool
{
    public class AuthenticateArguments
    {
        public AuthenticateArguments(AppEnvironment appEnvironment)
        {
            AppEnvironment = appEnvironment;
        }

        public AppEnvironment AppEnvironment { get; }

        public ArgumentValue<bool> Renew { get; set; }
    }
}
