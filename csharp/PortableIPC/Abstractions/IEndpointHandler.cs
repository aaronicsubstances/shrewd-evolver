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
        AbstractPromise HandleReceive(byte[] rawBytes, int offset, int length);

        AbstractPromise OpenSession(IPEndPoint endpoint, AbstractSessionHandler sessionHandler);
        AbstractPromise HandleSend(IPEndPoint endpoint, ProtocolDatagram message);

        AbstractPromise GenerateAbstractPromise(AbstractPromise.ExecutionCode code);
        AbstractPromise GenerateAlreadySuccessfulAbstractPromise(object value);
        AbstractPromise GenerateAlreadyFailedAbstractPromise(Exception reason);

        AbstractPromise SetTimeout(long millis);
    }
}
