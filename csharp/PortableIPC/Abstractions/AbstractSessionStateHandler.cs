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

        public abstract void Init();

        public abstract IPromiseWrapper<VoidReturn> ProcessReceive(ProtocolDatagram message);
        public abstract IPromiseWrapper<VoidReturn> ProcessSend(ProtocolDatagram message);

        public abstract IPromiseWrapper<VoidReturn> Close(Exception error, bool timeout);
        protected internal IPromiseWrapper<VoidReturn> ProcessDiscardedMessage(ProtocolDatagram message)
        {
            return SessionHandler.ProcessDiscardedMessage(message).Wrap();
        }
    }
}