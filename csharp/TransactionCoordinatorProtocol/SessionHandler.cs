using System;
using System.Collections.Generic;
using System.Net;

namespace TransactionCoordinatorProtocol
{
    public interface ISessionHandler
    {
        IEndpointHandler EndpointHandler { get; }
        string SessionId { get; }
        IPEndPoint ConnectedEndpoint { get; }
        void ProcessReceive(ProtocolDatagram message); // handle OPEN specially.
        void ProcessSend(ProtocolDatagram message, object cb); // handle OPEN specially
        object SetTimeout(long millis, object cb);
        void CancelTimeout(object timeoutId);
        int GenerateAsyncWaitId();
        string ValidateMessage(ProtocolDatagram message);
    }

    public class DefaultSessionHandler
    {
        private long _idleTimeoutMillis; 
        private Dictionary<State, StateHandler> _stateHandlers;
        private State? currentState; // null means indeterminate state
        private int _lastAsyncWaitId;
        private int _expectedSequenceNumber;

        enum State
        {
            SENDING, RECEIVING, CLOSING
        }

        interface StateHandler
        {
            void ProcessReceive(ProtocolDatagram message);
            void ProcessSend(ProtocolDatagram message, object cb);
        }
    }
}
