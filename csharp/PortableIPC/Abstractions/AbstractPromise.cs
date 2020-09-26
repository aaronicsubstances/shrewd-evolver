using System;
using System.Collections.Generic;
using System.Text;

namespace PortableIPC.Abstractions
{
    public interface AbstractPromise
    {
        AbstractPromise AttachCallbacks(SuccessCallback onFulfilled, ErrorCallback onRejected = null);
        void Cancel();

        public delegate void ExecutionCode(ResolutionFunction resolve, RejectionFunction reject);
        public delegate void ResolutionFunction(object value);
        public delegate void RejectionFunction(Exception reason);

        public delegate object SuccessCallback(object value);
        public delegate void ErrorCallback(Exception reason);
    }

    public class AbstractPromiseWrapper
    {
        public AbstractPromiseWrapper(AbstractPromise interimPromise)
            : this(interimPromise, null)
        { }

        public AbstractPromiseWrapper(AbstractPromise interimPromise, AbstractPromise.SuccessCallback successCallback)
        {
            InterimPromise = interimPromise;
            FulfilmentCallback = successCallback;
        }
        public AbstractPromise InterimPromise { get; }

        public AbstractPromise.SuccessCallback FulfilmentCallback { get; }
    }
}
