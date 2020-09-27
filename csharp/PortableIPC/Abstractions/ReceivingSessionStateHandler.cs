using System;
using System.Collections.Generic;
using System.Text;

namespace PortableIPC.Abstractions
{
    /// <summary>
    /// This session state handler processes receipt of PDUs which need acknowledgments.
    /// </summary>
    public class ReceivingSessionStateHandler: AbstractSessionStateHandler
    {
        public ReceivingSessionStateHandler(AbstractSessionHandler sessionHandler)
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
            var ack = new ProtocolDatagram
            {
                OpCode = ProtocolDatagram.OpCodeAck,
                SequenceNumber = message.SequenceNumber,
                SessionId = message.SessionId
            };
            var ackConfirmationPromise = SessionHandler.EndpointHandler.HandleSend(SessionHandler.ConnectedEndpoint, ack);
            FulfilmentCallback<object, AbstractPromise<VoidReturn>> ackConfirmationHandler = _ =>
                SessionHandler.RunSessionStateHandlerCallback( _ =>  HandleAckSendConfirmation());
            SessionHandler._currentState = AbstractSessionHandler.SessionStateReceiving;
            return ackConfirmationPromise.WrapThenCompose(ackConfirmationHandler);
        }

        private IPromiseWrapper<VoidReturn> HandleAckSendConfirmation()
        {
            SessionHandler._expectedSequenceNumber++;
            SessionHandler._currentState = AbstractSessionHandler.SessionStateIndeterminate;
            SessionHandler.SetIdleTimeout();

            //todo pass message onto application layer.
            return null;
        }

        public override IPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message)
        {
            return ProcessDiscardedMessage(message);
        }
    }
}
