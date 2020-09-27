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

        public override IPromiseWrapper<VoidReturn> Close(Exception error, bool timeout)
        {
            // todo later call application layer.
            return null;
        }

        public override IPromiseWrapper<VoidReturn> ProcessReceive(ProtocolDatagram message)
        {
            return HandlePostNetworkClosing(message);
        }

        public override IPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message)
        {
            var sendConfirmationPromise = SessionHandler.EndpointHandler.HandleSend(SessionHandler.ConnectedEndpoint, message);
            FulfilmentCallback<object, AbstractPromise<VoidReturn>> sendConfirmationHandler = _ =>
                SessionHandler.RunSessionStateHandlerCallback(_ => HandlePostNetworkClosing(message));
            SessionHandler._currentState = AbstractSessionHandler.SessionStateClosing;
            return sendConfirmationPromise.WrapThenCompose(sendConfirmationHandler);
        }

        private IPromiseWrapper<VoidReturn> HandlePostNetworkClosing(ProtocolDatagram message)
        {
            SessionHandler._isClosed = true;
            SessionHandler.EndpointHandler.RemoveSessionHandler(SessionHandler.ConnectedEndpoint, SessionHandler.SessionId);
            // todo later call application layer.
            return null;
        }
    }
}
