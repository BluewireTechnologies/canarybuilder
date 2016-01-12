using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;

namespace CanaryBuilder.IntegrationTests.TestInfrastructure
{
    public class RepositoryBuilder
    {
        private readonly GitSession session;
        private readonly GitWorkingCopy workingCopy;

        public RepositoryBuilder(GitSession session, GitWorkingCopy workingCopy)
        {
            this.session = session;
            this.workingCopy = workingCopy;
            workingCopy.CheckExistence();
        }
    }
}
