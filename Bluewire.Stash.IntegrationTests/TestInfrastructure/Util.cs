using System.IO;
using Moq;

namespace Bluewire.Stash.IntegrationTests.TestInfrastructure
{
    internal static class Util
    {
        public static T CallBase<T>(this T mock) where T : class
        {
            Mock.Get(mock).CallBase = true;
            return mock;
        }

        public static byte[] ToArray(this Stream stream)
        {
            var array = new byte[stream.Length];
            stream.Read(array, 0, array.Length);
            return array;
        }
    }
}
