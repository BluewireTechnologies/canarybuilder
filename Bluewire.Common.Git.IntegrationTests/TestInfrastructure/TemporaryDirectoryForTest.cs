using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Bluewire.Common.Git.IntegrationTests.TestInfrastructure
{
    static class TemporaryDirectoryForTest
    {
        private static readonly Lazy<string> temporaryDirectoryForAssembly = new Lazy<string>(GetTemporaryDirectoryForAssembly);

        private static string GetTemporaryDirectoryForAssembly()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var pid = Process.GetCurrentProcess().Id;

            var assemblyDirectory = $"{PathSegmentSanitiser.Instance.Sanitise(assemblyName)}-{pid}";

            return Path.Combine(Path.GetTempPath(), "NUnit3", assemblyDirectory);
        }

        public static void CleanTemporaryDirectoryForAssembly()
        {
            if (!temporaryDirectoryForAssembly.IsValueCreated) return;
            try
            {
                Directory.Delete(temporaryDirectoryForAssembly.Value, false);
            }
            catch { }
        }

        private const string Bluewire_TemporaryDirectoryKey = "bluewire.temporary_directory";
        
        public static string Allocate(TestContext testContext)
        {
            var path = Get(testContext);
            if (path != null) return path;

            var newPath = Generate(testContext);
            testContext.Test.Properties.Set(Bluewire_TemporaryDirectoryKey, newPath);
            return newPath;
        }

        public static string Get(TestContext testContext)
        {
            var path = testContext.Test.Properties.Get(Bluewire_TemporaryDirectoryKey);
            return path?.ToString();
        }

        private static string Generate(TestContext testContext)
        {
            return Path.Combine(temporaryDirectoryForAssembly.Value, PathSegmentSanitiser.Instance.Sanitise(testContext.Test.FullName));
        }

        class PathSegmentSanitiser
        {
            private readonly Regex rxInvalidChars;

            public PathSegmentSanitiser()
            {
                var invalidChars = new String(Path.GetInvalidFileNameChars());
                rxInvalidChars = new Regex($"[{Regex.Escape(invalidChars)}]", RegexOptions.Compiled);
            }

            public static readonly PathSegmentSanitiser Instance = new PathSegmentSanitiser();

            public string Sanitise(string segment)
            {
                return rxInvalidChars.Replace(segment, "_");
            }
        }
    }
}