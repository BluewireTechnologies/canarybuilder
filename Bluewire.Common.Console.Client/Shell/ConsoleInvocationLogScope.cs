using System;
using System.Collections.Generic;
using System.Linq;

namespace Bluewire.Common.Console.Client.Shell
{
    public class ConsoleInvocationLogScope : IConsoleInvocationLogScope
    {
        public ConsoleInvocationLogScope(params IDisposable[] subscriptions)
        {
            this.subscriptions.AddRange(subscriptions);
        }

        protected void RecordSubscription(IDisposable subscription)
        {
            subscriptions.Add(subscription);
        }
        
        private List<IDisposable> subscriptions = new List<IDisposable>();

        public virtual void Dispose()
        {
            if (subscriptions != null)
            {
                while (subscriptions.Any())
                {
                    var s = subscriptions[0];
                    subscriptions.RemoveAt(0);
                    s.Dispose();
                }
                subscriptions = null;
            }
        }
        
        public virtual void IgnoreExitCode()
        {
        }

        public static readonly ConsoleInvocationLogScope None = new ConsoleInvocationLogScope() { subscriptions = null };
    }
}