using PortableIPC.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace PortableIPC.Core
{
    /// <summary>
    /// This session state handler processes receipt of PDUs which need acknowledgments.
    /// </summary>
    public class ReceivingSessionStateHandler: ISessionStateHandler
    {
        private readonly DefaultSessionHandler _sessionHandler;
        private readonly AbstractPromiseApi _promiseApi;
        private bool _isOpened = false;

        public ReceivingSessionStateHandler(DefaultSessionHandler sessionHandler)
        {
            _sessionHandler = sessionHandler;
            _promiseApi = sessionHandler.EndpointHandler.PromiseApi;
        }

        public void Dispose(Exception error, bool timeout)
        { }

        public AbstractPromiseWrapper<VoidReturn> ProcessReceive(ProtocolDatagram message, bool reset)
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

            _sessionHandler._currentState = DefaultSessionHandler.SessionStateReceiving;
            _sessionHandler.ResetIdleTimeout();

            if (message.OpCode == ProtocolDatagram.OpCodeOpen)
            {
                // Set connection parameters.
                _sessionHandler.SetSessionParametersOnOpen(message);
            }
            var ack = new ProtocolDatagram
            {
                OpCode = ProtocolDatagram.OpCodeAck,
                SequenceNumber = message.SequenceNumber,
                SessionId = message.SessionId
            };
            var ackConfirmationPromise = _sessionHandler.EndpointHandler.HandleSend(_sessionHandler.ConnectedEndpoint, ack);
            FulfilmentCallback<VoidReturn, AbstractPromise<VoidReturn>> ackConfirmationHandler = _ =>
                _sessionHandler.RunSessionStateHandlerCallback( _ =>  HandleAckSendConfirmation(message));
            return ackConfirmationPromise.WrapThenCompose(ackConfirmationHandler);
        }

        private AbstractPromiseWrapper<VoidReturn> HandleAckSendConfirmation(ProtocolDatagram message)
        {
            _sessionHandler._expectedSequenceNumber++;
            _sessionHandler._currentState = DefaultSessionHandler.SessionStateIndeterminate;
            _sessionHandler.ResetIdleTimeout();

            // Pass message onto application layer.
            FulfilmentCallback<object, AbstractPromise<VoidReturn>> transferHandler = _ =>
                {
                    if (message.OpCode == ProtocolDatagram.OpCodeOpen)
                    {
                        return _sessionHandler.OnOpenReceived(message);
                    }
                    else
                    {
                        return _sessionHandler.OnDataReceived(message);
                    }
                };
            return _promiseApi.Resolve((object)null).WrapThenCompose(transferHandler);
        }

        public AbstractPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message, bool reset)
        {
            // never handle sending at all, so return null.
            return null;
        }
    }
}
