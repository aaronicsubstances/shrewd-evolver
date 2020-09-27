using PortableIPC.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;

namespace PortableIPC.CoreImpl
{
    public class DefaultEndpointHandler : IEndpointHandler
    {
        private readonly Dictionary<IPEndPoint, Dictionary<string, AbstractSessionHandler>> _sessionHandlerMap;
        private readonly AbstractPromise<VoidReturn> _voidReturnPromise;

        public DefaultEndpointHandler(EndpointConfig endpointConfig, AbstractPromiseApi promiseApi)
        {
            EndpointConfig = endpointConfig;
            PromiseApi = promiseApi;
            _sessionHandlerMap = new Dictionary<IPEndPoint, Dictionary<string, AbstractSessionHandler>>();
            _voidReturnPromise = PromiseApi.Resolve(VoidReturn.Instance);
        }

        public EndpointConfig EndpointConfig { get; }

        public AbstractPromiseApi PromiseApi { get; }

        public AbstractPromise<VoidReturn> HandleSend(IPEndPoint endpoint, ProtocolDatagram message)
        {
            // send through datagram socket.
            throw new NotImplementedException();
        }

        public AbstractPromise<VoidReturn> HandleReceive(IPEndPoint endpoint, byte[] rawBytes, int offset, int length)
        {
            // process data from datagram socket.
            ProtocolDatagram dg;
            try
            {
                dg = ProtocolDatagram.Parse(rawBytes, offset, length);
            }
            catch (Exception ex)
            {
                // log later
                return _voidReturnPromise;
            }
            if (dg.OpCode == ProtocolDatagram.OpCodeCloseAll)
            {
                return HandleCloseAll(endpoint);
            }
            AbstractSessionHandler sessionHandler = GetOrCreateSessionHandler(endpoint, dg.SessionId);
            if (sessionHandler != null)
            {
                return HandleException(sessionHandler.ProcessReceive(dg));
            }
            else
            {
                // log later
                return _voidReturnPromise;
            }
        }

        public AbstractPromise<VoidReturn> HandleCloseAll(IPEndPoint endpoint)
        {
            ICollection<AbstractSessionHandler> sessionHandlersSubset = new List<AbstractSessionHandler>();
            lock (this)
            {
                if (_sessionHandlerMap.ContainsKey(endpoint))
                {
                    sessionHandlersSubset = _sessionHandlerMap[endpoint].Values;
                    _sessionHandlerMap.Remove(endpoint);
                }
            }
            Exception error = new Exception("All sessions for endpoint closed");
            foreach (var sessionHandler in sessionHandlersSubset)
            {
                // don't wait.
                _ = HandleException(sessionHandler.Close(error, false));
            }
            return _voidReturnPromise;
        }

        private AbstractPromise<VoidReturn> HandleException(AbstractPromise<VoidReturn> promise)
        {
            return promise.Then<VoidReturn>(null, err =>
            {
                // log.
            });
        }

        public void AddSessionHandler(IPEndPoint endpoint, AbstractSessionHandler sessionHandler)
        {
            lock (this)
            {
                Dictionary<string, AbstractSessionHandler> subDict;
                if (_sessionHandlerMap.ContainsKey(endpoint))
                {
                    subDict = _sessionHandlerMap[endpoint];
                }
                else
                {
                    subDict = new Dictionary<string, AbstractSessionHandler>();
                    _sessionHandlerMap.Add(endpoint, subDict);
                }
                subDict.Add(sessionHandler.SessionId, sessionHandler);
            }
        }

        public void RemoveSessionHandler(IPEndPoint endpoint, string sessionId)
        {
            lock (this)
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

        public AbstractSessionHandler GetOrCreateSessionHandler(IPEndPoint endpoint, string sessionId)
        {
            lock (this)
            {
                // handle case in which session handlers must always be created externally,
                // e.g. in client mode
                Dictionary<string, AbstractSessionHandler> subDict = null;
                if (_sessionHandlerMap.ContainsKey(endpoint))
                {
                    subDict = _sessionHandlerMap[endpoint];
                }
                AbstractSessionHandler sessionHandler = null;
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
                            subDict = new Dictionary<string, AbstractSessionHandler>();
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
