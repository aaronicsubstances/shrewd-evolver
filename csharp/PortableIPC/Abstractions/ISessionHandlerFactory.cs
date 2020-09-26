using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PortableIPC.Abstractions
{
    public interface ISessionHandlerFactory
    {
        AbstractSessionHandler Create(IPEndPoint endpoint, string sessionId);
    }
}
