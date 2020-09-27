using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PortableIPC.Abstractions
{
    public interface IEndpointHandler
    {
        EndpointConfig EndpointConfig { get; }
        AbstractPromiseApi PromiseApi { get; }

        

        void AddSessionHandler(IPEndPoint endpoint, AbstractSessionHandler sessionHandler);

        void RemoveSessionHandler(IPEndPoint endpoint, string sessionId);
        AbstractSessionHandler GetOrCreateSessionHandler(IPEndPoint endpoint, string sessionId);// handle null return as client mode handler

        AbstractPromise<VoidReturn> HandleReceive(IPEndPoint endpoint, byte[] rawBytes, int offset, int length);
        AbstractPromise<VoidReturn> HandleSend(IPEndPoint endpoint, ProtocolDatagram message);
        AbstractPromise<VoidReturn> HandleCloseAll(IPEndPoint endpoint);
    }
}
