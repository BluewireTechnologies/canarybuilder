using System.Linq;
using CanaryCollector.Collectors;
using CanaryCollector.Model;
using NUnit.Framework;

namespace CanaryCollector.UnitTests.Collectors
{
    [TestFixture]
    public class TicketAndBranchAssociatorTests
    {
        private TicketAndBranchAssociator sut = new TicketAndBranchAssociator();

        [Test]
        public void AssociatesTicketWithReferencingBranch()
        {
            var ticket = new IssueTicket { Identifier = "E-23456" };
            var branchName = "bugfix/package-structure-E-23456";
            
            var associations = sut.Apply(new[] { ticket }, new[] { branchName }).ToArray();

            Assert.That(associations, Is.EqualTo(new[] {
                new TicketLinkedBranch { Branch = new Branch { Name = branchName }, Ticket = ticket }
            }));
        }

        [Test]
        public void ExcludesBranchesWhichTargetARelease()
        {
            var ticket = new IssueTicket { Identifier = "E-23456" };
            var branchName = "bugfix/package-structure-E-23456-15.14";
            
            var associations = sut.Apply(new[] { ticket }, new[] { branchName }).ToArray();

            Assert.That(associations, Is.Empty);
        }

        [Test]
        public void DoesNotAssociateTicketWithBranchReferencingADifferentTicket()
        {
            var ticket = new IssueTicket { Identifier = "E-10000" };
            var branchName = "bugfix/package-structure-E-23456";
            
            var associations = sut.Apply(new[] { ticket }, new[] { branchName }).ToArray();

            Assert.That(associations, Is.Empty);
        }

        [Test]
        public void AssociatesTicketWithAllValidReferencingBranches()
        {
            var ticket = new IssueTicket { Identifier = "E-23456" };
            var firstBranch = "bugfix/package-structure-first-E-23456";
            var secondBranch = "bugfix/package-structure-second-E-23456";
            
            var associations = sut.Apply(new[] { ticket }, new[] { firstBranch, secondBranch }).ToArray();

            Assert.That(associations, Is.EquivalentTo(new[] {
                new TicketLinkedBranch { Branch = new Branch { Name = firstBranch }, Ticket = ticket },
                new TicketLinkedBranch { Branch = new Branch { Name = secondBranch }, Ticket = ticket }
            }));
        }
    }
}
