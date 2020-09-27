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
        private readonly AbstractPromiseApi _promiseApi;

        private Action<VoidReturn> _pendingResolveFunc;
        private Action<Exception> _pendingRejectFunc;

        public SendingSessionStateHandler(AbstractSessionHandler sessionHandler)
            : base(sessionHandler)
        {
            _promiseApi = sessionHandler.EndpointHandler.PromiseApi;
        }

        public override void Dispose(Exception error, bool timeout)
        {
            if (_pendingRejectFunc != null)
            {
                if (error == null)
                {
                    if (timeout)
                    {
                        error = new Exception("Session timed out");
                    }
                    else
                    {
                        error = new Exception("Session closed");
                    }
                }
                _pendingRejectFunc.Invoke(error);
                _pendingRejectFunc = null;
            };
        }

        public override AbstractPromiseWrapper<VoidReturn> ProcessReceive(ProtocolDatagram message, bool reset)
        {
            // should only be called after reset.
            if (reset)
            {
                return null;
            }
            if (message.OpCode != ProtocolDatagram.OpCodeAck)
            {
                return null;
            }

            if (_pendingResolveFunc != null)
            {
                SessionHandler.ClearAckTimeout();
                SessionHandler._expectedSequenceNumber++;
                SessionHandler._currentState = AbstractSessionHandler.SessionStateIndeterminate;
                SessionHandler.SetIdleTimeout();

                // Pass to application layer by calling resolve function outside of synchronization lock.
                FulfilmentCallback<object, AbstractPromise<VoidReturn>> transferHandler = _ =>
                {
                    _pendingResolveFunc.Invoke(null);
                    _pendingResolveFunc = null;
                    if (message.OpCode == ProtocolDatagram.OpCodeOpen)
                    {
                        return SessionHandler.OnOpen(message, false);
                    }
                    else
                    {
                        return SessionHandler.OnData(message, false);
                    }
                };
                return _promiseApi.Resolve((object)null).WrapThenCompose(transferHandler);
            }
            else
            {
                return null;
            }
        }

        public override AbstractPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message, bool reset)
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

            SessionHandler._currentState = AbstractSessionHandler.SessionStateSending;
            SessionHandler.ClearIdleTimeout();

            _pendingResolveFunc = null;
            _pendingRejectFunc = null;

            if (message.OpCode == ProtocolDatagram.OpCodeOpen)
            {
                // Set connection parameters.
                SessionHandler.IdleTimeoutMillis = message.IdleTimeoutMillis ?? 0L;
            }
            var sendConfirmationPromise = SessionHandler.EndpointHandler.HandleSend(SessionHandler.ConnectedEndpoint, message);
            FulfilmentCallback<VoidReturn, AbstractPromise<VoidReturn>> sendConfirmationHandler = _ =>
                SessionHandler.RunSessionStateHandlerCallback( _ => HandleSendConfirmation(message));
            return sendConfirmationPromise.WrapThenCompose(sendConfirmationHandler);
        }

        private AbstractPromiseWrapper<VoidReturn> HandleSendConfirmation(ProtocolDatagram message)
        {
            SessionHandler.SetAckTimeout();
            var ack = new ProtocolDatagram
            {
                OpCode = ProtocolDatagram.OpCodeAck,
                SequenceNumber = message.SequenceNumber,
                SessionId = message.SessionId
            };
            var ackReceiptPromise = _promiseApi.Create<VoidReturn>((resolveFunc, rejectFunc) =>
            {
                _pendingResolveFunc = resolveFunc;
                _pendingRejectFunc = rejectFunc;
            });
            return ackReceiptPromise.Wrap();
        }
    }
}
