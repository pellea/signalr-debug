using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace SignalRServer
{
    //[HubName("sysmanHub")]
    public class SystemManagerHub : Hub
    {
        #region Methods : Overrides of Hub

        public override Task OnConnectedAsync()
        {
            var dummy = SystemManager.Instance;

            Console.WriteLine($"Client '{this.Context.ConnectionId}' connected.");

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception ex)
        {
            var connectionId = this.Context.ConnectionId;

            if (ex == null)
            {
                Console.WriteLine($"Client '{connectionId}' explicitly closed the connection.");
            }
            else
            {
                Console.WriteLine($"Client '{connectionId}' timed out.");
            }

            var oldConnectionsCount = SystemManager.Instance.ConnectionsCount;

            SystemManager.Instance.CleanConnection(connectionId);
            Console.WriteLine($"Cleaning connections ... (old:'{oldConnectionsCount}'; new:'{SystemManager.Instance.ConnectionsCount}')");

            return Task.CompletedTask;
        }

        #endregion

        #region Methods : Connect & Disconnect

        public async Task Join(string clientName, string clientDescription)
        {
            if (string.IsNullOrWhiteSpace(clientName))
            {
                throw new ArgumentNullException(clientName);
            }

            clientName = clientName.ToLowerInvariant();

            var connectionId = this.Context.ConnectionId;

            var alreadyJoined = true;
            if (SystemManager.Instance.AddConnection(clientName, clientDescription, connectionId))
            {
                alreadyJoined = false;
            }
            else
            {
                string oldConnectionId = SystemManager.Instance.Connections[clientName].ConnectionId;
                SystemManager.Instance.ReplaceConnection(clientName, connectionId);
            }

            Console.WriteLine($"Client '{connectionId}'/'{clientName}' joined. (alreadyJoined: '{alreadyJoined}').");

            await this.Clients.Client(connectionId).InvokeAsync("HasJoined", alreadyJoined);
            
            await this.Clients.All.InvokeAsync("test");
            Console.WriteLine(">>>>>>>>>>>>>>> Invoke 'test'");

            await this.Clients.All.InvokeAsync("testNumber", 99);
            Console.WriteLine(">>>>>>>>>>>>>>> Invoke 'testNumber'");
        }

        public async Task Leave(string clientName)
        {
            if(string.IsNullOrWhiteSpace(clientName))
            {
                return;
            }

            clientName = clientName.ToLowerInvariant();

            var connectionId = this.Context.ConnectionId;

            if (SystemManager.Instance.RemoveConnection(clientName))
            {
                Console.WriteLine($"Client '{connectionId}'/'{clientName}' left.");
            }

            await this.Clients.Client(connectionId).InvokeAsync("HasLeft", true);
        }

        #endregion

        #region Methods : General service methods
        
        public async Task<bool> SendMessage(string clientName, string messageType, string messageId)
        {
            if (string.IsNullOrWhiteSpace(clientName))
            {
                return false;
            }

            clientName = clientName.ToLowerInvariant();
            
            SystemManager.Instance.Connections.TryGetValue(clientName, out ClientConnectionInfo relatedClient);
            if (relatedClient == null)
            {
                return false;
            }

            await this.Clients.Client(relatedClient.ConnectionId).InvokeAsync("SendCommand", messageType, messageId);

            return true;
        }

        public bool IsConnected(string clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName))
            {
                return false;
            }

            clientName = clientName.ToLowerInvariant();
            
            SystemManager.Instance.Connections.TryGetValue(clientName, out ClientConnectionInfo relatedClient);
            if (relatedClient != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void GetConnectedClients()
        {
            var connectionId = this.Context.ConnectionId;

            List<ClientConnectionInfo> connectedClients = SystemManager.Instance.Connections.Select(e => e.Value).ToList();
            this.Clients.Client(connectionId).InvokeAsync("ReceiveGetConnectedClientsCallback", connectedClients);
        }

        #endregion
    }
}