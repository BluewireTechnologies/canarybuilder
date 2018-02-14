using System;

namespace CanaryBuilder.Logging
{
    public interface IPlainTextLogWriter
    {
        void WriteLine(string line, ConsoleColor? textColour = null);
    }
}
