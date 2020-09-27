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
        private readonly long _ackTimeoutMillis;

        private Action<VoidReturn> _pendingResolveFunc;
        private Action<Exception> _pendingRejectFunc;
        private AbstractPromise<VoidReturn> _ackTimeoutPromise;

        public SendingSessionStateHandler(AbstractSessionHandler sessionHandler)
            : base(sessionHandler)
        {
            _promiseApi = sessionHandler.EndpointHandler.PromiseApi;
            _ackTimeoutMillis = sessionHandler.EndpointHandler.EndpointConfig.AckTimeoutMillis;
        }

        public override void Init()
        {
            _pendingResolveFunc = null;
            _pendingRejectFunc = null;
            _ackTimeoutPromise = null;
        }

        public override IPromiseWrapper<VoidReturn> Close(Exception error, bool timeout)
        {
            if (_pendingRejectFunc == null)
            {
                return null;
            }
            var closePromise = _promiseApi.Resolve(0);
            FulfilmentCallback<int, VoidReturn> closeHandler = _ =>
            {
                // pass error to application layer, outside of synchronization lock.
                _pendingRejectFunc.Invoke(error);
                return null;
            };
            return closePromise.WrapThen(closeHandler);
        }

        public override IPromiseWrapper<VoidReturn> ProcessReceive(ProtocolDatagram message)
        {
            if (_pendingResolveFunc != null)
            {
                _ackTimeoutPromise.Cancel();
                SessionHandler._expectedSequenceNumber++;
                SessionHandler._currentState = AbstractSessionHandler.SessionStateIndeterminate;
                SessionHandler.SetIdleTimeout();

                // pass to application layer by calling resolve function outside of synchronization lock.
                var successPromise = _promiseApi.Resolve(0);
                FulfilmentCallback<int, VoidReturn> successHandler = _ =>
                {
                    _pendingResolveFunc.Invoke(null);
                    return null;
                };
                return successPromise.WrapThen(successHandler);
            }
            else
            {
                return ProcessDiscardedMessage(message);
            }
        }

        public override IPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message)
        {
            var sendConfirmationPromise = SessionHandler.EndpointHandler.HandleSend(SessionHandler.ConnectedEndpoint, message);
            FulfilmentCallback<object, AbstractPromise<VoidReturn>> sendConfirmationHandler = _ =>
                SessionHandler.RunSessionStateHandlerCallback( _ => HandleSendConfirmation(message));
            SessionHandler._currentState = AbstractSessionHandler.SessionStateSending;
            return sendConfirmationPromise.WrapThenCompose(sendConfirmationHandler);
        }

        private IPromiseWrapper<VoidReturn> HandleSendConfirmation(ProtocolDatagram message)
        {
            _ackTimeoutPromise = _promiseApi.SetTimeout(_ackTimeoutMillis);
            FulfilmentCallback<object, AbstractPromise<VoidReturn>> ackTimeoutHandler = _ =>
            {
                return SessionHandler.HandleAckTimeout(message);
            };

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
