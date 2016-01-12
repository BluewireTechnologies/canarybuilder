using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure
{
    public struct AssemblyTemporaryWorkspace
    {
        public string AssemblyName { get; }
        public string BasePath { get; }
    }

    static class TemporaryDirectoryForTest
    {
        // I think we should not see more than one assembly per appdomain, but would not like to rely upon it.
        private static readonly ConcurrentDictionary<Assembly, string> temporaryDirectoriesForAssemblies = new ConcurrentDictionary<Assembly, string>();


        private static string GetTemporaryDirectoryPathForAssembly(Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;
            var pid = Process.GetCurrentProcess().Id;

            var assemblyDirectory = $"{PathSegmentSanitiser.Instance.Sanitise(assemblyName)}-{pid}";

            return Path.Combine(Path.GetTempPath(), "NUnit3", assemblyDirectory);
        }

        private static string GetShortenedTestName(Assembly assembly, string testFullName)
        {
            var assemblyName = assembly.GetName().Name;
            Debug.Assert(testFullName != assemblyName); // Should be impossible.
            return testFullName.StartsWith(assemblyName) ? testFullName.Substring(assemblyName.Length + 1) : testFullName;
        }

        private static string LookupTemporaryDirectoryForAssembly(Assembly assembly)
        {
            return temporaryDirectoriesForAssemblies.GetOrAdd(assembly, GetTemporaryDirectoryPathForAssembly);
        }

        public static void CleanTemporaryDirectoryForAssembly(Assembly assembly)
        {
            var location = GetTemporaryDirectoryPathForAssembly(assembly);
            if (!Directory.Exists(location)) return;
            try
            {
                Directory.Delete(location, false);
            }
            catch { }
        }

        private const string Bluewire_TemporaryDirectoryKey = "bluewire.temporary_directory";
        
        private static Test GetActualTestFromContext(TestContext context)
        {
            var type = context.Test.GetType();
            Debug.Assert(type == typeof(TestContext.TestAdapter));
            var field = type.GetField("_test", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Assert(field != null);
            return (Test)field.GetValue(context.Test);
        }

        public static string Allocate(TestContext context)
        {
            return Allocate(GetActualTestFromContext(context));
        }

        public static string Allocate(ITest test)
        {
            var path = Get(test.Properties);
            if (path != null) return path;

            var newPath = Generate(test);
            test.Properties.Set(Bluewire_TemporaryDirectoryKey, newPath);
            return newPath;
        }

        public static string Get(IPropertyBag testProperties)
        {
            var path = testProperties.Get(Bluewire_TemporaryDirectoryKey);
            return path?.ToString();
        }

        private static string Generate(ITest test)
        {
            var testDetails = (Test)test;
            var containingType = testDetails.TypeInfo.Type;
            return Path.Combine(
                LookupTemporaryDirectoryForAssembly(containingType.Assembly),
                PathSegmentSanitiser.Instance.Sanitise(GetShortenedTestName(containingType.Assembly, test.FullName)));
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