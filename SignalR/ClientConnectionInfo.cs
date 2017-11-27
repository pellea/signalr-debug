using System;

namespace SignalRServer
{
    public class ClientConnectionInfo
    {
        public string Name { get; set; }
        public string ConnectionId { get; set; }
        public string Description { get; set; }
        public DateTimeOffset ConnectionDate { get; set; }
    }
}
