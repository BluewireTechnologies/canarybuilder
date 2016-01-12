using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using CanaryBuilder.Logging;
using CanaryBuilder.Parsers;

namespace CanaryBuilder.Merge
{
    public class MergeJob
    {
        private readonly string workingCopyPath;
        private readonly string scriptPath;

        public MergeJob(string workingCopyPath, string scriptPath)
        {
            if (workingCopyPath == null) throw new ArgumentNullException(nameof(workingCopyPath));
            if (scriptPath == null) throw new ArgumentNullException(nameof(scriptPath));
            this.workingCopyPath = workingCopyPath;
            this.scriptPath = scriptPath;
        }
        
        public async Task Run(IJobLogger logger)
        {
            var git = await new GitFinder().FromEnvironment();
            
            var jobDefinition = LoadScript(scriptPath);

            var workingCopy = new GitWorkingCopy(workingCopyPath);
            workingCopy.CheckExistence();
            

            await new MergeJobRunner(git).Run(workingCopy, jobDefinition, logger);
        }

        private static MergeJobDefinition LoadScript(string scriptPath)
        {
            if (!File.Exists(scriptPath)) throw new FileNotFoundException($"Script file does not exist: {scriptPath}", scriptPath);
            using (var script = File.OpenText(scriptPath))
            {
                return new MergeJobParser().ParseAndValidate(script);
            }
        }
    }
}
