// tag: 20210724T0000
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.Polling
{
    public static class PollingUtils
    {
        public static Task<T> PollAsync<T>(Func<PollCallbackArg<T>, PollCallbackRet<T>> cb,
           int intervalMillis, long totalDurationMillis, T initialValue,
           CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<T>();
            StartPolling(DateTime.UtcNow, initialValue, cb,
                intervalMillis, totalDurationMillis, cancellationToken, tcs);
            return tcs.Task;
        }

        private static void StartPolling<T>(DateTime startTime, T prevValue,
            Func<PollCallbackArg<T>, PollCallbackRet<T>> cb,
            int intervalMillis, long totalDurationMillis, CancellationToken cancellationToken,
            TaskCompletionSource<T> tcs)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // NB: current implementation invokes callback at least once.
                var cbArg = new PollCallbackArg<T>
                {
                    Value = prevValue,
                    UptimeMillis = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
                };

                // for predictability in knowing which call of the callback is the last one,
                // determine upfront rather than after callback is executed.
                var onLastCall = (totalDurationMillis - cbArg.UptimeMillis) < intervalMillis;
                cbArg.LastCall = onLastCall;

                // Now execute callback.
                var cbRes = cb.Invoke(cbArg);
                cancellationToken.ThrowIfCancellationRequested();

                if (cbRes != null && cbRes.Stop)
                {
                    tcs.SetResult(cbRes.NextValue);
                }
                else
                {
                    T nextValue = default;
                    if (cbRes != null)
                    {
                        nextValue = cbRes.NextValue;
                    }

                    // check if time is up.
                    // just in case callback modified cbArg, don't depend on cbArg.LastCall.
                    if (onLastCall)
                    {
                        tcs.SetResult(nextValue);
                    }
                    else
                    {
                        _ = Task.Delay(intervalMillis).ContinueWith(_ =>
                            StartPolling(startTime, nextValue, cb, intervalMillis,
                                totalDurationMillis, cancellationToken, tcs));
                    }
                }
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }

        public static Task AssertConditionFulfilmentAsync(TimeSpan duration, Func<bool> condition,
            Action<string> failureLogger)
        {
            return PollAsync<string>(arg =>
            {
                if (!condition.Invoke())
                {
                    var failureMsg = $"Condition being asserted is false after " +
                        $"{arg.UptimeMillis} ms";
                    failureLogger?.Invoke(failureMsg);
                    throw new Exception(failureMsg);
                }
                return null;
            }, 1000, (long)duration.TotalMilliseconds, null, CancellationToken.None);
        }

        public static Task AwaitConditionFulfilmentAsync(TimeSpan duration, Func<bool, bool> condition,
            Action<string> failureLogger)
        {
            return PollAsync<string>(arg =>
            {
                if (condition.Invoke(arg.LastCall))
                {
                    return new PollCallbackRet<string>
                    {
                        Stop = true
                    };
                }
                if (arg.LastCall)
                {
                    var failureMsg = $"Condition being awaited is still false after " +
                        $"{duration.TotalMilliseconds} ms";
                    failureLogger?.Invoke(failureMsg);
                    throw new Exception(failureMsg);
                }
                return null;
            }, 1000, (long)duration.TotalMilliseconds, null, CancellationToken.None);
        }
    }
}
