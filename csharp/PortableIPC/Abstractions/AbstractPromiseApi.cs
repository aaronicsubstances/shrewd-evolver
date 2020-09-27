using System;
using System.Collections.Generic;
using System.Text;

namespace PortableIPC.Abstractions
{
    /// <summary>
    /// Promise API design is to take common functionality of NodeJS Promises, C#.NET Core Tasks, and
    /// Java 8 CompletableFuture.
    /// 
    /// 1. Promises automatically unwrap in NodeJs. Equivalent are
    ///  - c# task.unwrap
    ///  - java 8 completablefuture.thenComposeAsync
    ///  Conclusion: don't automatically unwrap, instead be explicit about it.
    ///  
    /// 2. Task cancellation. NodeJS Promises doesn't have Cancel API, but Java and C# do
    ///  - fortunately cancellation is needed only for timeout
    ///  Conclusion: Have a Cancel API which works only for timeouts
    /// 
    /// 3. Rejection handlers in NodeJS can return values and continue like no error happened.
    ///  - not so in C#. an error in async-await keyword usage results in an exception 
    ///  Conclusion: only accept exceptions in rejection handlers, and don't allow them to return values.
    /// </summary>
    public interface AbstractPromiseApi
    {
        AbstractPromise<T> Create<T>(ExecutionCodeOfPromise<T> code);
        AbstractPromise<T> Resolve<T>(T value);
        AbstractPromise<VoidReturn> Reject(Exception reason);

        object ScheduleTimeout(int seqNr, Action<int> cb, long millis);
        void CancelTimeout(object id);
    }

    public interface AbstractPromise<out T>
    {
        AbstractPromise<U> Then<U>(FulfilmentCallback<T, U> onFulfilled, RejectionCallback onRejected = null);
        AbstractPromise<U> ThenCompose<U>(FulfilmentCallback<T, AbstractPromise<U>> onFulfilled, RejectionCallback onRejected = null);
    }

    public delegate void ExecutionCodeOfPromise<out T>(Action<T> resolve, Action<Exception> reject);

    public delegate U FulfilmentCallback<in T, out U>(T value);
    public delegate void RejectionCallback(Exception reason);

    public interface AbstractPromiseWrapper<out T>
    {
        AbstractPromise<T> Unwrap();
    }

    internal class SimplePromiseWrapper<T>: AbstractPromiseWrapper<T>
    {
        private readonly AbstractPromise<T> _finalPromise;
        public SimplePromiseWrapper(AbstractPromise<T> finalPromise)
        {
            _finalPromise = finalPromise;
        }

        public AbstractPromise<T> Unwrap()
        {
            return _finalPromise;
        }
    }

    internal class PromiseWrappedWithCallback<T, U> : AbstractPromiseWrapper<U>
    {
        private readonly AbstractPromise<T> _interimPromise;
        private readonly FulfilmentCallback<T, U> _thenCallback;
        private readonly FulfilmentCallback<T, AbstractPromise<U>> _thenComposeCallback;
        public PromiseWrappedWithCallback(AbstractPromise<T> interimPromise,
            FulfilmentCallback<T, U> thenCallback, FulfilmentCallback<T, AbstractPromise<U>> thenComposeCallback)
        {
            _interimPromise = interimPromise;
            _thenCallback = thenCallback;
            _thenComposeCallback = thenComposeCallback;
        }

        public AbstractPromise<U> Unwrap()
        {
            if (_thenCallback != null)
            {
                return _interimPromise.Then(_thenCallback);
            }
            else
            {
                return _interimPromise.ThenCompose(_thenComposeCallback);
            }
        }
    }

    public static class AbstractPromiseExtensions
    {
        public static AbstractPromiseWrapper<T> Wrap<T>(this AbstractPromise<T> finalPromise)
        {
            return new SimplePromiseWrapper<T>(finalPromise);
        }

        public static AbstractPromiseWrapper<U> WrapThen<T, U>(this AbstractPromise<T> interimPromise,
            FulfilmentCallback<T, U> successCallback)
        {
            return new PromiseWrappedWithCallback<T, U>(interimPromise, successCallback, null);
        }

        public static AbstractPromiseWrapper<U> WrapThenCompose<T, U>(this AbstractPromise<T> interimPromise,
            FulfilmentCallback<T, AbstractPromise<U>> successCallback)
        {
            return new PromiseWrappedWithCallback<T, U>(interimPromise, null, successCallback);
        }
    }
}
