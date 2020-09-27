using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PortableIPC.Abstractions
{
    public interface IEndpointHandler
    {
        EndpointConfig EndpointConfig { get; }

        // handle null return as client mode handler
        AbstractSessionHandler GetOrAddSessionHandler(IPEndPoint endpoint, string sessionId);

        void RemoveSessionHandler(IPEndPoint endpoint, string sessionId);
        AbstractPromise<VoidReturn> HandleReceive(byte[] rawBytes, int offset, int length);

        AbstractPromise<VoidReturn> OpenSession(IPEndPoint endpoint, AbstractSessionHandler sessionHandler);
        AbstractPromise<VoidReturn> HandleSend(IPEndPoint endpoint, ProtocolDatagram message);
        AbstractPromiseApi PromiseApi { get; set; }
    }
}
