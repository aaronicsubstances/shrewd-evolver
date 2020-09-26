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

        public abstract AbstractPromiseWrapper ProcessReceive(ProtocolDatagram message);
        public abstract AbstractPromiseWrapper ProcessSend(ProtocolDatagram message);

        public abstract AbstractPromiseWrapper Close(Exception error, bool timeout);
        protected internal AbstractPromiseWrapper ProcessDiscardedMessage(ProtocolDatagram message)
        {
            return new AbstractPromiseWrapper(SessionHandler.ProcessDiscardedMessage(message), null);
        }

        protected internal AbstractPromise GenerateAbstractPromise(AbstractPromise.ExecutionCode code)
        {
            return SessionHandler.EndpointHandler.GenerateAbstractPromise(code);
        }
        protected internal AbstractPromise GenerateAlreadySuccessfulAbstractPromise(object value)
        {
            return SessionHandler.EndpointHandler.GenerateAlreadySuccessfulAbstractPromise(value);
        }
        protected internal AbstractPromise GenerateAlreadyFailedAbstractPromise(Exception reason)
        {
            return SessionHandler.EndpointHandler.GenerateAlreadyFailedAbstractPromise(reason);
        }
    }
}