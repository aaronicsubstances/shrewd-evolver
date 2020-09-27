using PortableIPC.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PortableIPC.CoreImpl
{
    public class DefaultSessionHandler : AbstractSessionHandler
    {
        private readonly AbstractPromiseApi _promiseApi;
        private readonly AbstractPromise<VoidReturn> _voidReturnPromise;
        private readonly Dictionary<int, AbstractSessionStateHandler> _stateHandlers;

        public DefaultSessionHandler(IEndpointHandler endpointHandler, IPEndPoint endPoint, string sessionId) :
            base(endpointHandler, endPoint, sessionId)
        {
            _promiseApi = endpointHandler.PromiseApi;
            _voidReturnPromise = _promiseApi.Resolve(VoidReturn.Instance);

            _stateHandlers = new Dictionary<int, AbstractSessionStateHandler>();
            _stateHandlers.Add(SessionStateReceiving, new ReceivingSessionStateHandler(this));
            _stateHandlers.Add(SessionStateSending, new SendingSessionStateHandler(this));
            _stateHandlers.Add(SessionStateClosing, new ClosingSessionStateHandler(this));
        }

        protected internal override U RunSerially<T, U>(Func<bool> predicate,
            Func<T, U> code, T codeArg, Func<U> elseValue)
        {
            lock (this)
            {
                if (predicate.Invoke())
                {
                    return code.Invoke(codeArg);
                }
                else
                {
                    return elseValue.Invoke();
                };
            }
        }

        protected internal override AbstractPromise<VoidReturn> RunSessionStateHandlerCallback(
            Func<object, AbstractPromiseWrapper<VoidReturn>> code, object arg = null)
        {
            AbstractPromiseWrapper<VoidReturn> promiseWrapper = RunSerially(
                () => !_isClosed && _currentState != SessionStateIndeterminate,
                code, arg, () => _voidReturnPromise.Wrap());
            if (promiseWrapper == null) return _voidReturnPromise;
            else return promiseWrapper.Unwrap();
        }

        private bool CanProcess(ProtocolDatagram message)
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

        public override AbstractPromise<VoidReturn> ProcessReceive(ProtocolDatagram message)
        {
            AbstractPromiseWrapper<VoidReturn> promiseWrapper = RunSerially(() => CanProcess(message), _ =>
            {
                // use chain of responsibility pattern if state is not set
                if (_currentState == SessionStateIndeterminate)
                {
                    AbstractPromiseWrapper<VoidReturn> stateHandlerPromiseWrapper = null;                    
                    foreach (AbstractSessionStateHandler stateHandler in _stateHandlers.Values)
                    {
                        stateHandlerPromiseWrapper= stateHandler.ProcessReceive(message, true);
                        if (stateHandlerPromiseWrapper != null)
                        {
                            break;
                        }
                    }
                    if (stateHandlerPromiseWrapper == null)
                    {
                        stateHandlerPromiseWrapper = ProcessDiscardedMessage(message, true);
                    }
                    return stateHandlerPromiseWrapper;
                }
                else
                {
                    return _stateHandlers[_currentState].ProcessReceive(message, false);
                }
            }, (object)null, () => _voidReturnPromise.Wrap());
            if (promiseWrapper == null) return _voidReturnPromise;
            else return promiseWrapper.Unwrap();
        }

        public override AbstractPromise<VoidReturn> ProcessSend(ProtocolDatagram message)
        {
            AbstractPromiseWrapper<VoidReturn> promiseWrapper = RunSerially(() => CanProcess(message), _ =>
            {
                // use chain of responsibility pattern if state is not set
                if (_currentState == SessionStateIndeterminate)
                {
                    AbstractPromiseWrapper<VoidReturn> stateHandlerPromiseWrapper = null;
                    foreach (AbstractSessionStateHandler stateHandler in _stateHandlers.Values)
                    {
                        stateHandlerPromiseWrapper = stateHandler.ProcessSend(message, true);
                        if (stateHandlerPromiseWrapper != null)
                        {
                            break;
                        }
                    }
                    if (stateHandlerPromiseWrapper == null)
                    {
                        stateHandlerPromiseWrapper = ProcessDiscardedMessage(message, false);
                    }
                    return stateHandlerPromiseWrapper;
                }
                else
                {
                    return _stateHandlers[_currentState].ProcessSend(message, false);
                }
            }, (object)null, () => _voidReturnPromise.Wrap());
            if (promiseWrapper == null) return _voidReturnPromise;
            else return promiseWrapper.Unwrap();
        }

        protected internal override void SetIdleTimeout()
        {
            if (IdleTimeoutMillis > 0)
            {
                long boundedIdleTimeout = Math.Min(Math.Max(IdleTimeoutMillis, EndpointHandler.EndpointConfig.MininumIdleTimeoutMillis),
                    EndpointHandler.EndpointConfig.MaximumIdleTimeoutMillis);
                _lastIdleTimeoutId = _promiseApi.ScheduleTimeout(_idleTimeoutSeqNr,
                    HandleIdleTimeout, boundedIdleTimeout);
            }
        }

        protected internal override void ClearIdleTimeout()
        {
            if (_lastIdleTimeoutId != null)
            {
                _promiseApi.CancelTimeout(_lastIdleTimeoutId);
                _lastIdleTimeoutId = null;
                _idleTimeoutSeqNr++;
            }
        }

        protected internal override void SetAckTimeout()
        {
            _lastAckTimeoutId = _promiseApi.ScheduleTimeout(_ackTimeoutSeqNr,
                    HandleAckTimeout, EndpointHandler.EndpointConfig.AckTimeoutMillis);
            
        }

        protected internal override void ClearAckTimeout()
        {
            if (_lastAckTimeoutId != null)
            {
                _promiseApi.CancelTimeout(_lastAckTimeoutId);
                _lastAckTimeoutId = null;
                _ackTimeoutSeqNr++;
            }
        }

        public override AbstractPromise<VoidReturn> Close(Exception error, bool timeout)
        {
            AbstractPromiseWrapper<VoidReturn> promiseWrapper = RunSerially(() => !_isClosed, 
                _ => HandleClosing(error, timeout),
                (object)null, () => _voidReturnPromise.Wrap());
            if (promiseWrapper == null) return _voidReturnPromise;
            else return promiseWrapper.Unwrap();
        }

        private void HandleIdleTimeout(int seqNr)
        {
            AbstractPromiseWrapper<VoidReturn> promiseWrapper = RunSerially(() => seqNr == _idleTimeoutSeqNr && !_isClosed,
                _ => HandleClosing(null, true),
                (object)null, () => _voidReturnPromise.Wrap());
            if (promiseWrapper != null) promiseWrapper.Unwrap();
        }

        private void HandleAckTimeout(int seqNr)
        {
            AbstractPromiseWrapper<VoidReturn> promiseWrapper = RunSerially(() => seqNr == _ackTimeoutSeqNr && !_isClosed,
                _ => HandleClosing(null, true),
                (object)null, () => _voidReturnPromise.Wrap());
            if (promiseWrapper != null) promiseWrapper.Unwrap();
        }

        protected internal override AbstractPromiseWrapper<VoidReturn> HandleClosing(Exception error, bool timeout)
        {
            _isClosed = true;
            ClearIdleTimeout();
            ClearAckTimeout();
            EndpointHandler.RemoveSessionHandler(ConnectedEndpoint, SessionId);
            // dispose state handlers
            foreach (var stateHandler in _stateHandlers.Values)
            {
                stateHandler.Dispose(error, timeout);
            }
            // Pass on to application layer, outside of sync lock
            return _promiseApi.Resolve((object)null).WrapThenCompose(_ => OnClose(error, timeout));
        }

        protected internal override AbstractPromiseWrapper<VoidReturn> ProcessDiscardedMessage(ProtocolDatagram message,
            bool received)
        {
            // silently discard. subclasses can log
            return null;
        }

        protected internal override AbstractPromise<VoidReturn> OnOpen(ProtocolDatagram message, bool received)
        {
            return _voidReturnPromise;
        }

        protected internal override AbstractPromise<VoidReturn> OnData(ProtocolDatagram message, bool received)
        {
            return _voidReturnPromise;
        }

        protected internal override AbstractPromise<VoidReturn> OnClose(Exception error, bool timeout)
        {
            return _voidReturnPromise;
        }
    }
}
