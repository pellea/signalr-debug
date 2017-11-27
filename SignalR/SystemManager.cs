using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;

namespace SignalRServer
{
    public class SystemManager
    {
        // Fields
        private static readonly SystemManager _instance = new SystemManager();

        private readonly Dictionary<string, ClientConnectionInfo> _connections = new Dictionary<string, ClientConnectionInfo>();

        public Dictionary<string, ClientConnectionInfo> Connections
        {
            get
            {
                return _connections;
            }
        }

        // Properties
        public static SystemManager Instance => _instance;

        public object ConnectionsCount => this._connections.Count;

        // Initializers
        public SystemManager()
        {
        }


        #region Methods : Connections cache

        public bool AddConnection(string name, string description, string connectionId)
        {
            if (this._connections.ContainsKey(name))
            {
                return false;
            }

            this._connections.Add(
                name, 
                new ClientConnectionInfo()
                {
                    ConnectionId = connectionId,
                    Description = description,
                    Name = name,
                    ConnectionDate = System.DateTime.Now
                });

            return true;
        }

        public void ReplaceConnection(string name, string connectionId)
        {
            this._connections[name].ConnectionId = connectionId;
        }

        public bool RemoveConnection(string name)
        {
            if (this._connections.ContainsKey(name))
            {
                this._connections.Remove(name);
                return true;
            }

            return false;
        }

        #endregion

        public void CleanConnection(string connectionId)
        {
            var elementToDelete = (from connection in _connections
                                   where connection.Value.ConnectionId == connectionId
                                   select connection.Key).ToList();

            foreach (string element in elementToDelete)
            {
                this._connections.Remove(element);
            }
        }
    }

}