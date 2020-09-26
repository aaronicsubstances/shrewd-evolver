using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace PortableIPC.Abstractions
{
    public abstract class AbstractSessionHandler
    {
        // run these 4 serially
        public abstract AbstractPromise ProcessReceive(ProtocolDatagram message);
        public abstract AbstractPromise ProcessSend(ProtocolDatagram message);
        protected internal abstract AbstractPromise HandleIdleTimeout();
        public abstract AbstractPromise Close();

        protected internal static int SessionStateIndeterminate = 0;
        protected internal static int SessionStateReceiving = 1;
        protected internal static int SessionStateSending = 2;
        protected internal static int SessionStateClosing = 3;

        // Refers to properties which remain constant throught session handler lifetime.
        public IPEndPoint ConnectedEndpoint { get; set; }

        public long IdleTimeoutMillis { get; set; }
        public long AckTimeoutMillis
        {
            get
            {
                return EndpointHandler.EndpointConfig.AckTimeoutMillis;
            }
        }
        protected internal IEndpointHandler EndpointHandler { get; }
        protected internal string SessionId { get; }

        // Refers to mutable state of session handler.
        protected internal bool _isClosed = false;
        protected internal short _expectedSequenceNumber = 0;
        protected internal int _currentState;

        // runs code under sync lock.
        protected internal abstract object RunSerially(AbstractExecutableCode code, object arg);

        
        // run these 2 serially

        protected internal abstract AbstractPromiseWrapper RunSessionStateHandlerCallback(AbstractExecutableCode code);
        protected internal abstract AbstractPromiseWrapper HandleAckTimeout(ProtocolDatagram message);


        // don't run serially since it is always called by session state handlers in sync lock
        protected internal abstract AbstractPromise ProcessDiscardedMessage(ProtocolDatagram message); 

        protected internal abstract void SetIdleTimeout(); // called by send/receive session state handlers.
        protected internal abstract void ClearIdleTimeout(); // clear for both send and receive.

        protected internal AbstractPromise GenerateAbstractPromise(AbstractPromise.ExecutionCode code)
        {
            return EndpointHandler.GenerateAbstractPromise(code);
        }
        protected internal AbstractPromise GenerateAlreadySuccessfulAbstractPromise(object value)
        {
            return EndpointHandler.GenerateAlreadySuccessfulAbstractPromise(value);
        }
        protected internal AbstractPromise GenerateAlreadyFailedAbstractPromise(Exception reason)
        {
            return EndpointHandler.GenerateAlreadyFailedAbstractPromise(reason);
        }
    }
}
