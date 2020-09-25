using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace TransactionCoordinatorProtocol
{
    public class EndpointConfig
    {
        private int _sessionIdSuffixCounter = 0;
        private  readonly string _startTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        public IPEndPoint Endpoint { get; set; }
        public int AckTimeoutMillis { get;set; }
        public int MininumIdleTimeoutMillis { get; set; }
        public int MaximumIdleTimeoutMillis { get; set; }
        public object SessionHandlerFactory { get; set; }

        public string GenerateSessionId()
        {
            var v = Interlocked.Increment(ref _sessionIdSuffixCounter);
            var sessionId = (v + _startTime).PadLeft(ProtocolDatagram.SessionIdLength, '0');
            return sessionId;
        }
    }
}
