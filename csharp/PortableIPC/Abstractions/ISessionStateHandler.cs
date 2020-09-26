namespace PortableIPC.Abstractions
{
    public interface ISessionStateHandler
    {
        void Init();
        ISessionHandler SessionHandler { get; }
        void ProcessReceive(ProtocolDatagram message);
        void ProcessSend(ProtocolDatagram message, object cb);
    }
}