using System;
using System.Collections.Generic;
using System.Text;

namespace PortableIPC.Abstractions
{
    /// <summary>
    /// Processes receipt or sending of PDUs which terminate a session.
    /// </summary>
    public class ClosingSessionStateHandler : AbstractSessionStateHandler
    {
        public ClosingSessionStateHandler(AbstractSessionHandler sessionHandler)
            : base(sessionHandler)
        { }

        public override void Dispose(Exception error, bool timeout)
        { }

        public override AbstractPromiseWrapper<VoidReturn> ProcessReceive(ProtocolDatagram message, bool reset)
        {
            return InitiateClose(message, reset, true);
        }

        public override AbstractPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message, bool reset)
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
                _ = SessionHandler.EndpointHandler.HandleSend(SessionHandler.ConnectedEndpoint, message);
            }

            Exception error = null;
            if (message.OpCode == ProtocolDatagram.OpCodeError)
            {
                error = new Exception($"Session layer protocol error: {message.ErrorCode}: {message.ErrorMessage}");
            }
            return SessionHandler.HandleClosing(error, false);
        }
    }
}
