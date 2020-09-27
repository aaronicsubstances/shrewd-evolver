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
        private readonly AbstractPromiseApi _promiseApi;

        public ReceivingSessionStateHandler(AbstractSessionHandler sessionHandler)
            : base(sessionHandler)
        {
            _promiseApi = sessionHandler.EndpointHandler.PromiseApi;
        }

        public override void Dispose(Exception error, bool timeout)
        { }

        public override AbstractPromiseWrapper<VoidReturn> ProcessReceive(ProtocolDatagram message, bool reset)
        {
            // should only be called during resets.
            if (!reset)
            {
                return null;
            }
            if (message.OpCode != ProtocolDatagram.OpCodeOpen && message.OpCode != ProtocolDatagram.OpCodeData)
            {
                return null;
            }

            SessionHandler._currentState = AbstractSessionHandler.SessionStateReceiving;
            SessionHandler.ClearIdleTimeout();

            if (message.OpCode == ProtocolDatagram.OpCodeOpen)
            {
                // Set connection parameters.
                SessionHandler.IdleTimeoutMillis = message.IdleTimeoutMillis ?? 0L;
            }
            var ack = new ProtocolDatagram
            {
                OpCode = ProtocolDatagram.OpCodeAck,
                SequenceNumber = message.SequenceNumber,
                SessionId = message.SessionId
            };
            var ackConfirmationPromise = SessionHandler.EndpointHandler.HandleSend(SessionHandler.ConnectedEndpoint, ack);
            FulfilmentCallback<VoidReturn, AbstractPromise<VoidReturn>> ackConfirmationHandler = _ =>
                SessionHandler.RunSessionStateHandlerCallback( _ =>  HandleAckSendConfirmation(message));
            return ackConfirmationPromise.WrapThenCompose(ackConfirmationHandler);
        }

        private AbstractPromiseWrapper<VoidReturn> HandleAckSendConfirmation(ProtocolDatagram message)
        {
            SessionHandler._expectedSequenceNumber++;
            SessionHandler._currentState = AbstractSessionHandler.SessionStateIndeterminate;
            SessionHandler.SetIdleTimeout();

            // Pass message onto application layer.
            FulfilmentCallback<object, AbstractPromise<VoidReturn>> transferHandler = _ =>
                {
                    if (message.OpCode == ProtocolDatagram.OpCodeOpen)
                    {
                        return SessionHandler.OnOpen(message, true);
                    }
                    else
                    {
                        return SessionHandler.OnData(message, true);
                    }
                };
            return _promiseApi.Resolve((object)null).WrapThenCompose(transferHandler);
        }

        public override AbstractPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message, bool reset)
        {
            // never handle sending at all, so return null.
            return null;
        }
    }
}
