using PortableIPC.Abstractions;
using System;

namespace PortableIPC.Core
{
    public interface ISessionStateHandler
    {
        AbstractPromiseWrapper<VoidReturn> ProcessReceive(ProtocolDatagram message, bool reset);
        AbstractPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message, bool reset);

        void Dispose(Exception error, bool timeout);
    }
}