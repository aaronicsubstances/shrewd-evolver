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

        public override void Init()
        {
        }

        public override AbstractPromiseWrapper Close(Exception error, bool timeout)
        {
            // todo later call application layer.
            return null;
        }

        public override AbstractPromiseWrapper ProcessReceive(ProtocolDatagram message)
        {
            return HandlePostNetworkClosing(message);
        }

        public override AbstractPromiseWrapper ProcessSend(ProtocolDatagram message)
        {
            var sendConfirmationPromise = SessionHandler.EndpointHandler.HandleSend(SessionHandler.ConnectedEndpoint, message);
            AbstractPromise.SuccessCallback sendConfirmationHandler = _ =>
                SessionHandler.RunSessionStateHandlerCallback(_ => HandlePostNetworkClosing(message));
            SessionHandler._currentState = AbstractSessionHandler.SessionStateClosing;
            return new AbstractPromiseWrapper(sendConfirmationPromise, sendConfirmationHandler);
        }

        private AbstractPromiseWrapper HandlePostNetworkClosing(ProtocolDatagram message)
        {
            SessionHandler._isClosed = true;
            SessionHandler.EndpointHandler.RemoveSessionHandler(SessionHandler.ConnectedEndpoint, SessionHandler.SessionId);
            // todo later call application layer.
            return null;
        }
    }
}
