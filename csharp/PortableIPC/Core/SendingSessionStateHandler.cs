using PortableIPC.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortableIPC.Core
{
    /// <summary>
    /// This session state handler processes sending of non-ack PDUs which do not terminate a session.
    /// Also processes receipt of acknowledgments.
    /// </summary>
    public class SendingSessionStateHandler: ISessionStateHandler
    {
        private readonly DefaultSessionHandler _sessionHandler;
        private readonly AbstractPromiseApi _promiseApi;

        private bool _isOpened = false;
        private Action<VoidReturn> _pendingResolveFunc;
        private Action<Exception> _pendingRejectFunc;

        public SendingSessionStateHandler(DefaultSessionHandler sessionHandler)
        {
            _sessionHandler = sessionHandler;
            _promiseApi = sessionHandler.EndpointHandler.PromiseApi;
        }

        public void Dispose(Exception error, bool timeout)
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

        public AbstractPromiseWrapper<VoidReturn> ProcessReceive(ProtocolDatagram message, bool reset)
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
                _sessionHandler.ResetIdleTimeout();
                _sessionHandler._expectedSequenceNumber++;
                _sessionHandler._currentState = DefaultSessionHandler.SessionStateIndeterminate;

                // Pass to application layer by calling resolve function outside of synchronization lock.
                FulfilmentCallback<object, AbstractPromise<VoidReturn>> transferHandler = _ =>
                {
                    _pendingResolveFunc.Invoke(null);
                    _pendingResolveFunc = null;
                    if (message.OpCode == ProtocolDatagram.OpCodeOpen)
                    {
                        return _sessionHandler.OnOpenSent(message);
                    }
                    else
                    {
                        return _sessionHandler.OnDataSent(message);
                    }
                };
                return _promiseApi.Resolve((object)null).WrapThenCompose(transferHandler);
            }
            else
            {
                return null;
            }
        }

        public AbstractPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message, bool reset)
        {
            // should only be called during resets.
            if (!reset)
            {
                return null;
            }
            if (message.OpCode == ProtocolDatagram.OpCodeOpen)
            {
                if (_isOpened)
                {
                    return null;
                }
                _isOpened = true;
            }
            else if (message.OpCode == ProtocolDatagram.OpCodeData)
            {
                if (!_isOpened)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            _sessionHandler._currentState = DefaultSessionHandler.SessionStateSending;
            _sessionHandler.ResetIdleTimeout();

            _pendingResolveFunc = null;
            _pendingRejectFunc = null;

            if (message.OpCode == ProtocolDatagram.OpCodeOpen)
            {
                // Set connection parameters.
                _sessionHandler.SetSessionParametersOnOpen(message);
            }
            var sendConfirmationPromise = _sessionHandler.EndpointHandler.HandleSend(_sessionHandler.ConnectedEndpoint, message);
            FulfilmentCallback<VoidReturn, AbstractPromise<VoidReturn>> sendConfirmationHandler = _ =>
                _sessionHandler.RunSessionStateHandlerCallback( _ => HandleSendConfirmation(message));
            return sendConfirmationPromise.WrapThenCompose(sendConfirmationHandler);
        }

        private AbstractPromiseWrapper<VoidReturn> HandleSendConfirmation(ProtocolDatagram message)
        {
            _sessionHandler.ResetAckTimeout();
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
