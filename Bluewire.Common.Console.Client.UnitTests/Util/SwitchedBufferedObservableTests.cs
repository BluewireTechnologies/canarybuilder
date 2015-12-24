using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Util;
using NUnit.Framework;

namespace Bluewire.Common.Console.Client.UnitTests.Util
{
    [TestFixture]
    public class SwitchedBufferedObservableTests
    {
        [Test]
        public async Task ActsLikeReplaySubjectInitially()
        {
            var sut = new SwitchedBufferedObservable<int>();

            sut.OnNext(1);
            sut.OnNext(2);
            var receiver = sut.ToArray().ToTask();
            sut.OnNext(3);
            sut.OnCompleted();

            Assert.That(await receiver, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task SubscriptionAcquiredBeforeStopBuffering_ReplaysBufferedItems()
        {
            var sut = new SwitchedBufferedObservable<int>();

            sut.OnNext(1);
            var receiver = sut.ToArray().ToTask();
            sut.StopBuffering();
            sut.OnNext(2);
            sut.OnNext(3);
            sut.OnCompleted();

            Assert.That(await receiver, Does.Contain(1));
        }

        [Test]
        public async Task SubscriptionAcquiredAfterStopBuffering_PropagatesNewItems()
        {
            var sut = new SwitchedBufferedObservable<int>();

            sut.OnNext(1);
            sut.StopBuffering();
            sut.OnNext(2);
            var receiver = sut.ToArray().ToTask();
            sut.OnNext(3);
            sut.OnCompleted();
            
            Assert.That(await receiver, Does.Contain(3));
        }

        [Test]
        public async Task SubscriptionAcquiredAfterStopBuffering_DoesNotReplayItemsReceivedBeforeStopBufferingWasCalled()
        {
            var sut = new SwitchedBufferedObservable<int>();

            sut.OnNext(1);
            sut.StopBuffering();
            sut.OnNext(2);
            var receiver = sut.ToArray().ToTask();
            sut.OnNext(3);
            sut.OnCompleted();

            Assert.That(await receiver, Does.Not.Contains(2));
        }

        [Test]
        public async Task SubscriptionAcquiredAfterStopBuffering_DoesNotReplayItemsReceivedAfterStopBufferingWasCalled()
        {
            var sut = new SwitchedBufferedObservable<int>();

            sut.OnNext(1);
            sut.StopBuffering();
            sut.OnNext(2);
            var receiver = sut.ToArray().ToTask();
            sut.OnNext(3);
            sut.OnCompleted();

            Assert.That(await receiver, Does.Not.Contains(1));
        }
    }
}
