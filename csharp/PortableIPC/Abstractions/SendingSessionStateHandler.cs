using System;
using System.Collections.Generic;
using System.Text;

namespace PortableIPC.Abstractions
{
    /// <summary>
    /// This session state handler processes sending of non-ack PDUs which do not terminate a session.
    /// Also processes receipt of acknowledgments.
    /// </summary>
    public class SendingSessionStateHandler: AbstractSessionStateHandler
    {
        private AbstractPromise.ResolutionFunction _pendingResolveFunc;
        private AbstractPromise.RejectionFunction _pendingRejectFunc;
        private AbstractPromise _ackTimeoutPromise;

        public SendingSessionStateHandler(AbstractSessionHandler sessionHandler)
            : base(sessionHandler)
        { }

        public override void Init()
        {
            _pendingResolveFunc = null;
            _pendingRejectFunc = null;
            _ackTimeoutPromise = null;
        }

        public override AbstractPromiseWrapper Close(Exception error, bool timeout)
        {
            if (_pendingRejectFunc == null)
            {
                return null;
            }
            var closePromise = GenerateAlreadySuccessfulAbstractPromise(null);
            AbstractPromise.SuccessCallback closeHandler = _ =>
            {
                // pass error to application layer, outside of synchronization lock.
                _pendingRejectFunc.Invoke(error);
                return null;
            };
            return new AbstractPromiseWrapper(closePromise, closeHandler);
        }

        public override AbstractPromiseWrapper ProcessReceive(ProtocolDatagram message)
        {
            if (_pendingResolveFunc != null)
            {
                _ackTimeoutPromise.Cancel();
                SessionHandler._expectedSequenceNumber++;
                SessionHandler._currentState = AbstractSessionHandler.SessionStateIndeterminate;
                SessionHandler.SetIdleTimeout();

                // pass to application layer by calling resolve function outside of synchronization lock.
                var successPromise = GenerateAlreadySuccessfulAbstractPromise(null);
                AbstractPromise.SuccessCallback successHandler = v =>
                {
                    _pendingResolveFunc.Invoke(v);
                    return null;
                };
                return new AbstractPromiseWrapper(successPromise, successHandler);
            }
            else
            {
                return ProcessDiscardedMessage(message);
            }
        }

        public override AbstractPromiseWrapper ProcessSend(ProtocolDatagram message)
        {
            var sendConfirmationPromise = SessionHandler.EndpointHandler.HandleSend(SessionHandler.ConnectedEndpoint, message);
            AbstractPromise.SuccessCallback sendConfirmationHandler = _ =>
                SessionHandler.RunSessionStateHandlerCallback( _ => HandleSendConfirmation(message));
            SessionHandler._currentState = AbstractSessionHandler.SessionStateSending;
            return new AbstractPromiseWrapper(sendConfirmationPromise, sendConfirmationHandler);
        }

        private AbstractPromiseWrapper HandleSendConfirmation(ProtocolDatagram message)
        {
            _ackTimeoutPromise = SessionHandler.EndpointHandler.SetTimeout(SessionHandler.AckTimeoutMillis);
            AbstractPromise.SuccessCallback ackTimeoutHandler = _ =>
            {
                return SessionHandler.HandleAckTimeout(message);
            };

            var ack = new ProtocolDatagram
            {
                OpCode = ProtocolDatagram.OpCodeAck,
                SequenceNumber = message.SequenceNumber,
                SessionId = message.SessionId
            };
            var ackReceiptPromise = GenerateAbstractPromise((resolveFunc, rejectFunc) =>
            {
                _pendingResolveFunc = resolveFunc;
                _pendingRejectFunc = rejectFunc;
            });
            return new AbstractPromiseWrapper(ackReceiptPromise);
        }
    }
}
