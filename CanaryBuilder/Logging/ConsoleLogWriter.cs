using System;

namespace CanaryBuilder.Logging
{
    public class ConsoleLogWriter : IPlainTextLogWriter, IDisposable
    {
        public void WriteLine(string line, ConsoleColor? textColour = null)
        {
            lock(this) // Serialise output
            {
                SetColour(textColour);
                Console.Out.WriteLine(line);
            }
        }

        private static void SetColour(ConsoleColor? textColour)
        {
            if (textColour == null)
            {
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = textColour.Value;
            }
        }

        public void Dispose()
        {
            Console.ResetColor();
        }
    }
}
