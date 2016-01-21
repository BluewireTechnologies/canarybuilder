using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanaryBuilder.Merge;
using NUnit.Framework;

namespace CanaryBuilder.IntegrationTests
{
    public static class XAssert
    {
        public static async Task<TException> ThrowsAsync<TException>(Func<Task> testAction) where TException : Exception
        {
            try
            {
                await testAction();
                Assert.Fail();
                throw new AssertionException($"Expected an exception of type {typeof(TException).FullName} but none was thrown.");
            }
            catch (TException ex) 
            {
                return ex;
            }
        }
    }
}
