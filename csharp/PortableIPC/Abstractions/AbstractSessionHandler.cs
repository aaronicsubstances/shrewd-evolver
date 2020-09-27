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
        public abstract AbstractPromise<VoidReturn> ProcessReceive(ProtocolDatagram message);
        public abstract AbstractPromise<VoidReturn> ProcessSend(ProtocolDatagram message);
        protected internal abstract AbstractPromise<VoidReturn> HandleIdleTimeout();
        public abstract AbstractPromise<VoidReturn> Close();

        protected internal static int SessionStateIndeterminate = 0;
        protected internal static int SessionStateReceiving = 1;
        protected internal static int SessionStateSending = 2;
        protected internal static int SessionStateClosing = 3;

        // Refers to properties which remain constant throught session handler lifetime.
        public IPEndPoint ConnectedEndpoint { get; set; }

        public long IdleTimeoutMillis { get; set; }
        protected internal IEndpointHandler EndpointHandler { get; }
        protected internal string SessionId { get; }

        // Refers to mutable state of session handler.
        protected internal bool _isClosed = false;
        protected internal short _expectedSequenceNumber = 0;
        protected internal int _currentState;

        // runs code under sync lock.
        protected internal abstract U RunSerially<T, U>(AbstractExecutableCode<T, U> code, T arg);

        
        // run these 2 serially

        protected internal abstract AbstractPromise<VoidReturn> RunSessionStateHandlerCallback(
            AbstractExecutableCode<object, IPromiseWrapper<VoidReturn>> code);
        protected internal abstract AbstractPromise<VoidReturn> HandleAckTimeout(ProtocolDatagram message);


        // don't run serially since it is always called by session state handlers in sync lock
        protected internal abstract AbstractPromise<VoidReturn> ProcessDiscardedMessage(ProtocolDatagram message); 

        protected internal abstract void SetIdleTimeout(); // called by send/receive session state handlers.
        protected internal abstract void ClearIdleTimeout(); // clear before calling both send and receive.
    }
}
