using PortableIPC.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortableIPC.Core
{
    /// <summary>
    /// Processes receipt or sending of PDUs which terminate a session.
    /// </summary>
    public class ClosingSessionStateHandler : ISessionStateHandler
    {
        private readonly DefaultSessionHandler _sessionHandler;

        public ClosingSessionStateHandler(DefaultSessionHandler sessionHandler)
        {
            _sessionHandler = sessionHandler;
        }

        public void Dispose(Exception error, bool timeout)
        { }

        public AbstractPromiseWrapper<VoidReturn> ProcessReceive(ProtocolDatagram message, bool reset)
        {
            return InitiateClose(message, reset, true);
        }

        public AbstractPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message, bool reset)
        {
            return InitiateClose(message, reset, false);
        }

        private AbstractPromiseWrapper<VoidReturn> InitiateClose(ProtocolDatagram message, bool reset, bool received)
        {
            // should only be called during resets.
            if (!reset)
            {
                return null;
            }
            if (message.OpCode != ProtocolDatagram.OpCodeClose && message.OpCode != ProtocolDatagram.OpCodeError)
            {
                return null;
            }

            if (!received) 
            {
                // don't wait
                _ = _sessionHandler.EndpointHandler.HandleSend(_sessionHandler.ConnectedEndpoint, message);
            }

            Exception error = null;
            if (message.OpCode == ProtocolDatagram.OpCodeError)
            {
                error = new Exception($"Session layer protocol error: {message.ErrorCode}: {message.ErrorMessage}");
            }
            return _sessionHandler.HandleClosing(error, false);
        }
    }
}
