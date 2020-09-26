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
        ISessionHandler GetOrAddSessionHandler(IPEndPoint endpoint, string sessionId);

        void RemoveSessionHandler(IPEndPoint endpoint, string sessionId);

        void OpenSessionHandler(IPEndPoint endpoint, ISessionHandler sessionHandler);
        void HandleReceive(byte[] rawBytes, int offset, int length);
        void HandleSend(IPEndPoint endpoint, ProtocolDatagram message, object cb);
    }
}
