using System;

namespace PortableIPC.Abstractions
{
    public abstract class AbstractSessionStateHandler
    {
        public AbstractSessionStateHandler(AbstractSessionHandler sessionHandler)
        {
            SessionHandler = sessionHandler;
        }

        public AbstractSessionHandler SessionHandler { get; }

        public abstract AbstractPromiseWrapper<VoidReturn> ProcessReceive(ProtocolDatagram message, bool reset);
        public abstract AbstractPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message, bool reset);

        public abstract void Dispose(Exception error, bool timeout);
    }
}