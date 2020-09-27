using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace PortableIPC.Abstractions
{
    public abstract class AbstractSessionHandler
    {
        public AbstractSessionHandler(IEndpointHandler endpointHandler, IPEndPoint endPoint, string sessionId)
        {
            EndpointHandler = endpointHandler;
            ConnectedEndpoint = endPoint;
            SessionId = sessionId;
        }

        // run these 4 serially
        public abstract AbstractPromise<VoidReturn> ProcessReceive(ProtocolDatagram message);
        public abstract AbstractPromise<VoidReturn> ProcessSend(ProtocolDatagram message);
        public abstract AbstractPromise<VoidReturn> Close(Exception error, bool timeout);

        protected internal static int SessionStateIndeterminate = 0;
        protected internal static int SessionStateReceiving = 1;
        protected internal static int SessionStateSending = 2;
        protected internal static int SessionStateClosing = 3;

        // Refers to properties which remain constant throught session handler lifetime.
        protected internal IEndpointHandler EndpointHandler { get; }
        public IPEndPoint ConnectedEndpoint { get; }
        protected internal string SessionId { get; }

        public long IdleTimeoutMillis { get; set; }

        // Refers to mutable state of session handler.
        protected internal int _currentState = SessionStateIndeterminate;
        protected internal bool _isClosed = false;
        protected internal short _expectedSequenceNumber = 0;
        protected internal object _lastIdleTimeoutId = null;
        protected internal object _lastAckTimeoutId = null;
        protected internal int _idleTimeoutSeqNr = 0;
        protected internal int _ackTimeoutSeqNr = 0;

        // run these two under sync lock.
        protected internal abstract U RunSerially<T, U>(Func<bool> predicate,
            Func<T, U> code, T codeArg, Func<U> elseValue);
        protected internal abstract AbstractPromise<VoidReturn> RunSessionStateHandlerCallback(
            Func<object, AbstractPromiseWrapper<VoidReturn>> code, object arg = null);

        // don't run serially since it is always called in sync lock
        protected internal abstract AbstractPromiseWrapper<VoidReturn> ProcessDiscardedMessage(ProtocolDatagram message,
            bool received);
        protected internal abstract AbstractPromiseWrapper<VoidReturn> HandleClosing(Exception error, bool timeout);

        // called by handlers.
        protected internal abstract void SetIdleTimeout();
        protected internal abstract void ClearIdleTimeout();
        protected internal abstract void SetAckTimeout();
        protected internal abstract void ClearAckTimeout();

        // link to application layer
        protected internal abstract AbstractPromise<VoidReturn> OnOpen(ProtocolDatagram message, bool received);

        protected internal abstract AbstractPromise<VoidReturn> OnData(ProtocolDatagram message, bool received);

        protected internal abstract AbstractPromise<VoidReturn> OnClose(Exception error, bool timeout);
    }
}
