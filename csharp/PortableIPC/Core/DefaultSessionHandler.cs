using PortableIPC.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PortableIPC.Core
{
    public class DefaultSessionHandler
    {
        // Refers to properties which remain constant throught session handler lifetime.
        private readonly AbstractPromiseApi _promiseApi;
        private readonly AbstractPromise<VoidReturn> _voidReturnPromise;
        private readonly Dictionary<int, ISessionStateHandler> _stateHandlers;

        protected internal static int SessionStateIndeterminate = 0;
        protected internal static int SessionStateReceiving = 1;
        protected internal static int SessionStateSending = 2;
        protected internal static int SessionStateClosing = 3;

        // Refers to mutable state of session handler.
        protected internal int _currentState = SessionStateIndeterminate;
        protected internal bool _isClosed = false;
        protected internal short _expectedSequenceNumber = 0;
        protected internal object _lastIdleTimeoutId = null;
        protected internal int _idleTimeoutSeqNr = 0;
        protected internal bool _timeoutForAck = false;
        protected internal long _idleTimeoutMillis;

        public DefaultSessionHandler(ProtocolEndpointHandler endpointHandler, IPEndPoint endPoint, string sessionId)
        {
            EndpointHandler = endpointHandler;
            ConnectedEndpoint = endPoint;
            SessionId = sessionId;

            _promiseApi = endpointHandler.PromiseApi;
            _voidReturnPromise = _promiseApi.Resolve(VoidReturn.Instance);

            _stateHandlers = new Dictionary<int, ISessionStateHandler>();
            _stateHandlers.Add(SessionStateReceiving, new ReceivingSessionStateHandler(this));
            _stateHandlers.Add(SessionStateSending, new SendingSessionStateHandler(this));
            _stateHandlers.Add(SessionStateClosing, new ClosingSessionStateHandler(this));
        }

        public ProtocolEndpointHandler EndpointHandler { get; }
        public IPEndPoint ConnectedEndpoint { get; }
        public string SessionId { get; }

        protected internal void SetSessionParametersOnOpen(ProtocolDatagram message)
        {
            if (message.IdleTimeoutMillis == null)
            {
                _idleTimeoutMillis = EndpointHandler.EndpointConfig.IdleTimeoutMillis;
            }
            else
            {
                _idleTimeoutMillis = Math.Min(Math.Max(message.IdleTimeoutMillis.Value, 
                    EndpointHandler.EndpointConfig.MininumIdleTimeoutMillis),
                    EndpointHandler.EndpointConfig.MaximumIdleTimeoutMillis);
            }
        }

        protected internal AbstractPromise<VoidReturn> RunSessionStateHandlerCallback(
            Func<object, AbstractPromiseWrapper<VoidReturn>> code, object arg = null)
        {
            AbstractPromiseWrapper<VoidReturn> promiseWrapper = null;
            lock (this)
            {
                if (!_isClosed && _currentState != SessionStateIndeterminate)
                {
                    promiseWrapper = code.Invoke(arg);
                }
            }
            return UnwrapVoidPromiseWrapper(promiseWrapper);
        }

        private AbstractPromise<VoidReturn> UnwrapVoidPromiseWrapper(AbstractPromiseWrapper<VoidReturn> promiseWrapper)
        {
            if (promiseWrapper == null) return _voidReturnPromise;
            else return promiseWrapper.Unwrap();
        }

        private bool CanProcessMessage(ProtocolDatagram message)
        {
            if (_isClosed)
            {
                return false;
            }
            if (message.SequenceNumber != _expectedSequenceNumber)
            {
                return false;
            }
            return true;
        }

        public virtual AbstractPromise<VoidReturn> ProcessReceive(ProtocolDatagram message)
        {
            AbstractPromiseWrapper<VoidReturn> promiseWrapper = null;
            lock (this)
            {
                // Ensure timeout in progress even if message cannot be processed or will
                // be discarded. By so doing idleness can be detected reliably.
                SetTimeout(false, false);
                if (CanProcessMessage(message))
                {
                    // use chain of responsibility pattern if state is not set
                    if (_currentState == SessionStateIndeterminate)
                    {
                        foreach (ISessionStateHandler stateHandler in _stateHandlers.Values)
                        {
                            promiseWrapper = stateHandler.ProcessReceive(message, true);
                            if (promiseWrapper != null)
                            {
                                break;
                            }
                        }
                        if (promiseWrapper == null)
                        {
                            promiseWrapper = ProcessDiscardedMessage(message, true);
                        }
                    }
                    else
                    {
                        promiseWrapper = _stateHandlers[_currentState].ProcessReceive(message, false);
                    }
                }
            }
            return UnwrapVoidPromiseWrapper(promiseWrapper);
        }

        public virtual AbstractPromise<VoidReturn> ProcessSend(ProtocolDatagram message)
        {
            AbstractPromiseWrapper<VoidReturn> promiseWrapper = null;
            lock (this)
            {
                // Ensure timeout in progress even if message cannot be processed or will
                // be discarded. By so doing idleness can be detected reliably.
                SetTimeout(false, false);
                if (CanProcessMessage(message))
                {
                    // use chain of responsibility pattern if state is not set
                    if (_currentState == SessionStateIndeterminate)
                    {
                        foreach (ISessionStateHandler stateHandler in _stateHandlers.Values)
                        {
                            promiseWrapper = stateHandler.ProcessSend(message, true);
                            if (promiseWrapper != null)
                            {
                                break;
                            }
                        }
                        if (promiseWrapper == null)
                        {
                            promiseWrapper = ProcessDiscardedMessage(message, false);
                        }
                    }
                    else
                    {
                        promiseWrapper = _stateHandlers[_currentState].ProcessSend(message, false);
                    }
                }
            }
            return UnwrapVoidPromiseWrapper(promiseWrapper);
        }

        protected internal void ResetIdleTimeout()
        {
            SetTimeout(true, false);
        }

        protected internal void ResetAckTimeout()
        {
            SetTimeout(true, true);
        }

        private void SetTimeout(bool reset, bool forAck)
        {
            if (reset)
            {
                CancelTimeout();
            }
            else if (_lastIdleTimeoutId != null)
            {
                return;
            }
            if (forAck)
            {
                _lastIdleTimeoutId = _promiseApi.ScheduleTimeout(_idleTimeoutSeqNr,
                    HandleTimeout, EndpointHandler.EndpointConfig.AckTimeoutMillis);
                _timeoutForAck = true;
            }
            else if (_idleTimeoutMillis > 0)
            {
                _lastIdleTimeoutId = _promiseApi.ScheduleTimeout(_idleTimeoutSeqNr,
                    HandleTimeout, _idleTimeoutMillis);
                _timeoutForAck = false;
            }
        }

        private void CancelTimeout()
        {
            if (_lastIdleTimeoutId != null)
            {
                _promiseApi.CancelTimeout(_lastIdleTimeoutId);
                _lastIdleTimeoutId = null;
                _idleTimeoutSeqNr++;
            }
        }

        public virtual AbstractPromise<VoidReturn> Close(Exception error, bool timeout)
        {
            AbstractPromiseWrapper<VoidReturn> promiseWrapper = null;
            lock (this)
            {
                if (!_isClosed)
                {
                    promiseWrapper = HandleClosing(error, timeout);
                }
            }
            return UnwrapVoidPromiseWrapper(promiseWrapper);
        }

        protected virtual void HandleTimeout(int seqNr)
        {
            AbstractPromiseWrapper<VoidReturn> promiseWrapper = null;
            lock (this)
            {
                if (seqNr == _idleTimeoutSeqNr)
                {
                    _lastIdleTimeoutId = null;
                    _idleTimeoutSeqNr++;
                    if (!_isClosed)
                    {
                        promiseWrapper = HandleClosing(null, true);
                    }
                }
            }
            UnwrapVoidPromiseWrapper(promiseWrapper);
        }

        protected internal virtual AbstractPromiseWrapper<VoidReturn> HandleClosing(Exception error, bool timeout)
        {
            _isClosed = true;
            CancelTimeout();
            EndpointHandler.RemoveSessionHandler(ConnectedEndpoint, SessionId);
            // dispose state handlers
            foreach (var stateHandler in _stateHandlers.Values)
            {
                stateHandler.Dispose(error, timeout);
            }
            // Pass on to application layer, outside of sync lock
            return _promiseApi.Resolve((object)null).WrapThenCompose(_ => OnClose(error, timeout));
        }

        protected internal virtual AbstractPromiseWrapper<VoidReturn> ProcessDiscardedMessage(ProtocolDatagram message,
            bool received)
        {
            // silently discard. subclasses can log
            return null;
        }

        protected internal virtual AbstractPromise<VoidReturn> OnOpenReceived(ProtocolDatagram message)
        {
            return _voidReturnPromise;
        }

        protected internal virtual AbstractPromise<VoidReturn> OnDataReceived(ProtocolDatagram message)
        {
            return _voidReturnPromise;
        }

        protected internal virtual AbstractPromise<VoidReturn> OnOpenSent(ProtocolDatagram message)
        {
            return _voidReturnPromise;
        }

        protected internal virtual AbstractPromise<VoidReturn> OnDataSent(ProtocolDatagram message)
        {
            return _voidReturnPromise;
        }

        protected internal virtual AbstractPromise<VoidReturn> OnClose(Exception error, bool timeout)
        {
            return _voidReturnPromise;
        }
    }
}
