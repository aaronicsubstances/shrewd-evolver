using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TransactionCoordinatorProtocol
{
    public interface IEndpointHandler
    {
        EndpointConfig EndpointConfig { get; }
        ISessionHandler GetOrAddSessionHandler(IPEndPoint endpoint, string sessionId); // handle null return as client mode handler
        void RemoveSessionHandler(IPEndPoint endpoint, string sessionId);

        void OpenSessionHandler(IPEndPoint endpoint, ISessionHandler sessionHandler);
        void HandleReceive();
        void HandleSend(IPEndPoint endpoint, ProtocolDatagram message, object cb);
    }

    public class DefaultEndpointHandler
    {
        
    }
}
