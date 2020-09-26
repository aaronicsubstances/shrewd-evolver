using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PortableIPC.Abstractions
{
    public interface ISessionHandler
    {
        static int SessionStateReceiving = 1;
        static int SessionStateSending = 2;
        static int SessionStateClosing = 3;

        // Refers to properties which remain constant throught session handler lifetime.
        IEndpointHandler EndpointHandler { get; }
        string SessionId { get; }
        IPEndPoint ConnectedEndpoint { get; }
        long IdleTimeoutMillis { get; }
        Dictionary<int, ISessionStateHandler> SessionStateHandlers { get; }

        // Refers to mutable state of session handler.
        bool IsClosed { get; set; }
        int ExpectedSequenceNumber { get; set; }
        object LastTimeoutId { get; set; }
        int LastAsyncWaitId { get; set; }
        int? CurrentState { get; set; }

        // handle OPEN specially in receive and send
        void ProcessReceive(ProtocolDatagram message);
        void ProcessSend(ProtocolDatagram message, object cb);

        void SetNextTimeout(long millis, object cb);
        void CancelLastTimeout();
        void IncrementAsyncWaitId();
        void RunTransaction(object cb);
    }
}
