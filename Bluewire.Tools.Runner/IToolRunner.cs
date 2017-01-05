using System.IO;

namespace Bluewire.Tools.Runner
{
    interface IToolRunner
    {
        string Name { get; }
        void Describe(TextWriter writer);
        int RunMain(string[] args, string parentArgs);
    }
}
