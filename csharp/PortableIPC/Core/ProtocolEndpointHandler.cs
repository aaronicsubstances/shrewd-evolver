using PortableIPC.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PortableIPC.Core
{
    public class ProtocolEndpointHandler
    {
        private readonly Dictionary<IPEndPoint, Dictionary<string, DefaultSessionHandler>> _sessionHandlerMap;
        private readonly AbstractPromise<VoidReturn> _voidReturnPromise;
        private readonly object _disposeLock = new object();
        private bool _isDisposing = false;

        public ProtocolEndpointHandler(AbstractDatagramSocket networkSocket, EndpointConfig endpointConfig, AbstractPromiseApi promiseApi)
        {
            NetworkSocket = networkSocket;
            EndpointConfig = endpointConfig;
            PromiseApi = promiseApi;
            _sessionHandlerMap = new Dictionary<IPEndPoint, Dictionary<string, DefaultSessionHandler>>();
            _voidReturnPromise = PromiseApi.Resolve(VoidReturn.Instance);
        }

        public AbstractDatagramSocket NetworkSocket { get; }
        public EndpointConfig EndpointConfig { get; }

        public AbstractPromiseApi PromiseApi { get; }

        public AbstractPromise<VoidReturn> HandleSend(IPEndPoint endpoint, ProtocolDatagram message)
        {
            lock (_disposeLock)
            {
                if (_isDisposing)
                {
                    return PromiseApi.Reject(new Exception("endpoint handler is shutting down"));
                }
            }

            // send through datagram socket.
            byte[] pdu;
            try
            {
                pdu = message.ToRawDatagram();
            }
            catch (Exception ex)
            {
                return PromiseApi.Reject(ex);
            }
            return HandleException(NetworkSocket.HandleSend(endpoint, pdu, 0, pdu.Length));
        }

        public AbstractPromise<VoidReturn> Shutdown()
        {
            // swallow exceptions.
            lock (_disposeLock)
            {
                if (_isDisposing)
                {
                    return _voidReturnPromise;
                }
                _isDisposing = true;
            }

            List<IPEndPoint> endpoints;
            lock (_sessionHandlerMap)
            {
                endpoints = _sessionHandlerMap.Keys.ToList();
            }
            var retVal = _voidReturnPromise;
            foreach (var endpoint in endpoints)
            {
                retVal = _voidReturnPromise.ThenCompose(_ => HandleSendCloseAll(endpoint));
            }
            lock(_sessionHandlerMap)
            {
                _sessionHandlerMap.Clear();
            }

            return retVal;
        }

        public AbstractPromise<VoidReturn> HandleReceive(IPEndPoint endpoint, byte[] rawBytes, int offset, int length)
        {
            lock (_disposeLock)
            {
                if (_isDisposing)
                {
                    return PromiseApi.Reject(new Exception("endpoint handler is shutting down"));
                }
            }

            // process data from datagram socket.
            ProtocolDatagram dg;
            try
            {
                dg = ProtocolDatagram.Parse(rawBytes, offset, length);
            }
            catch (Exception ex)
            {
                return PromiseApi.Reject(ex);
            }
            if (dg.OpCode == ProtocolDatagram.OpCodeCloseAll)
            {
                return HandleReceiveCloseAll(endpoint);
            }
            DefaultSessionHandler sessionHandler = GetOrCreateSessionHandler(endpoint, dg.SessionId);
            if (sessionHandler != null)
            {
                return sessionHandler.ProcessReceive(dg);
            }
            else
            {
                return PromiseApi.Reject(new Exception($"Could not allocate handler for session {dg.SessionId} from {endpoint}"));
            }
        }

        public AbstractPromise<VoidReturn> HandleSendCloseAll(IPEndPoint endpoint)
        {
            ProtocolDatagram pdu = new ProtocolDatagram
            {
                OpCode = ProtocolDatagram.OpCodeCloseAll,
                SessionId = EndpointConfig.GenerateNullSessionId(),
            };
            // swallow any send exception.
            return HandleSend(endpoint, pdu).
                ThenCompose(_ => HandleReceiveCloseAll(endpoint), _ => _voidReturnPromise);
        }

        private AbstractPromise<VoidReturn> HandleReceiveCloseAll(IPEndPoint endpoint)
        {
            var sessionHandlersSubset = new List<DefaultSessionHandler>();
            lock (_sessionHandlerMap)
            {
                if (_sessionHandlerMap.ContainsKey(endpoint))
                {
                    sessionHandlersSubset = _sessionHandlerMap[endpoint].Values.ToList();
                    _sessionHandlerMap.Remove(endpoint);
                }
            }
            AbstractPromise<VoidReturn> retResult = _voidReturnPromise;
            foreach (var sessionHandler in sessionHandlersSubset)
            {
                var nextResult = SwallowException(sessionHandler.Close(null, false));
                retResult = retResult.ThenCompose(_ => nextResult);
            }
            return retResult;
        }

        private AbstractPromise<VoidReturn> HandleException(AbstractPromise<VoidReturn> promise)
        {
            return promise.Then<VoidReturn>(null, err =>
            {
                // log.
            });
        }

        private AbstractPromise<VoidReturn> SwallowException(AbstractPromise<VoidReturn> promise)
        {
            return promise.ThenCompose(null, err =>
            {
                // log.
                return _voidReturnPromise;
            });
        }

        public AbstractPromise<VoidReturn> OpenSession(IPEndPoint endpoint, DefaultSessionHandler sessionHandler,
            ProtocolDatagram message)
        {
            if (sessionHandler.SessionId == null)
            {
                return PromiseApi.Reject(new Exception("session handler has null session id"));
            }
            if (message.SessionId != sessionHandler.SessionId)
            {
                return PromiseApi.Reject(new Exception("message session id differs from that of session handler"));
            }
            if (message.OpCode != ProtocolDatagram.OpCodeOpen || message.SequenceNumber != 0)
            {
                return PromiseApi.Reject(new Exception("invalid opening message"));
            }
            lock (_sessionHandlerMap)
            {
                Dictionary<string, DefaultSessionHandler> subDict;
                if (_sessionHandlerMap.ContainsKey(endpoint))
                {
                    subDict = _sessionHandlerMap[endpoint];
                }
                else
                {
                    subDict = new Dictionary<string, DefaultSessionHandler>();
                    _sessionHandlerMap.Add(endpoint, subDict);
                }
                subDict.Add(sessionHandler.SessionId, sessionHandler);
            }
            return sessionHandler.ProcessSend(message);
        }

        internal void RemoveSessionHandler(IPEndPoint endpoint, string sessionId)
        {
            lock (_sessionHandlerMap)
            {
                if (_sessionHandlerMap.ContainsKey(endpoint))
                {
                    var subDict = _sessionHandlerMap[endpoint];
                    if (subDict.ContainsKey(sessionId))
                    {
                        subDict.Remove(sessionId);
                        if (subDict.Count == 0)
                        {
                            _sessionHandlerMap.Remove(endpoint);
                        }
                    }
                }
            }
        }

        private DefaultSessionHandler GetOrCreateSessionHandler(IPEndPoint endpoint, string sessionId)
        {
            lock (_sessionHandlerMap)
            {
                // handle case in which session handlers must always be created externally,
                // e.g. in client mode
                Dictionary<string, DefaultSessionHandler> subDict = null;
                if (_sessionHandlerMap.ContainsKey(endpoint))
                {
                    subDict = _sessionHandlerMap[endpoint];
                }
                DefaultSessionHandler sessionHandler = null;
                if (subDict != null && subDict.ContainsKey(sessionId))
                {
                    sessionHandler = subDict[sessionId];
                }
                else
                {
                    if (EndpointConfig.SessionHandlerFactory != null)
                    {
                        sessionHandler = EndpointConfig.SessionHandlerFactory.Create(endpoint, sessionId);
                    }
                    if (sessionHandler != null)
                    {
                        if (subDict == null)
                        {
                            subDict = new Dictionary<string, DefaultSessionHandler>();
                            _sessionHandlerMap.Add(endpoint, subDict);
                        }
                        subDict.Add(sessionId, sessionHandler);
                    }
                }
                return sessionHandler;
            }
        }
    }
}
