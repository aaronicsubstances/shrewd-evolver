using AaronicSubstances.ShrewdEvolver.Polling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static AaronicSubstances.ShrewdEvolver.Polling.PollingUtils;

namespace AaronicSubstances.ShrewdEvolver.UnitTests
{
    public class PollingTests
    {
        private static readonly Action<string> FailureLogger = msg =>
        {
            // for better assert failure agreement with message, use Assert.True
            // rather than Assert.False
            Assert.True(false, msg);
        };

        [Fact]
        public async Task TestPollAsync()
        {
            // test expected poll count.
            int retVal = await PollAsync(a =>
            {
                return new PollCallbackRet<int>
                {
                    NextValue = a.Value + 1
                };
            }, 2000, 5000, 1, CancellationToken.None);
            Assert.Equal(4, retVal);

            // test use of stop.
            retVal = await PollAsync(a =>
            {
                return new PollCallbackRet<int>
                {
                    Stop = a.Value == 2,
                    NextValue = a.Value + 1
                };
            }, 1000, 5000, 0, CancellationToken.None);
            Assert.Equal(3, retVal);

            // test that null return equivalent to continue.
            retVal = await PollAsync(a =>
            {
                return null;
            }, 1000, 5000, 0, CancellationToken.None);
            Assert.Equal(0, retVal);

            // test exception catch
            var promise = PollAsync(a =>
            {
                if (a.LastCall)
                {
                    throw new ArgumentException();
                }
                return null;
            }, 1000, 5000, 0, CancellationToken.None);
            await Assert.ThrowsAsync<ArgumentException>(() => promise);

            // test cancellation
            var cancellationSource = new CancellationTokenSource(3000);
            promise = PollAsync(a =>
            {
                return null;
            }, 2000, 5000, 1, cancellationSource.Token);
            await Assert.ThrowsAsync<OperationCanceledException>(() => promise);
        }

        [Fact]
        public async Task TestAssertConditionFulfilmentAsync()
        {
            await AssertConditionFulfilmentAsync(TimeSpan.FromSeconds(3), () => true,
                FailureLogger);
            await Assert.ThrowsAnyAsync<Exception>(() => AssertConditionFulfilmentAsync(TimeSpan.FromSeconds(3),
                () => false, FailureLogger));
        }

        [Fact]
        public async Task TestAwaitConditionFulfilmentAsync()
        {
            var startTime = DateTime.UtcNow;
            await AwaitConditionFulfilmentAsync(TimeSpan.FromSeconds(3), _ =>
            {
                return (DateTime.UtcNow - startTime).TotalSeconds > 2;
            }, FailureLogger);
            startTime = DateTime.UtcNow;
            await Assert.ThrowsAnyAsync<Exception>(() => AwaitConditionFulfilmentAsync(TimeSpan.FromSeconds(3), _ =>
            {
                return (DateTime.UtcNow - startTime).TotalSeconds > 5;
            }, FailureLogger));
        }
    }
}
