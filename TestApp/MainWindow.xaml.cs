using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Constants
        public static readonly string ClientGuid = "A1245444-FDFA-4E52-ADA2-C8C403BBB1E4";

        // Fields
        private HubConnection _connection;

        // Properties
        public ObservableCollection<LogMessage> Messages { get; set; } = new ObservableCollection<LogMessage>();

        public List<string> ConnectionUrls { get; set; }

        public string BaseUrl { get; set; }

        public int Interval { get; set; } = 30;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            this.ConnectionUrls = new List<string>();
            this.ConnectionUrls.Add("http://localhost:5004/mgmt");

            this.BaseUrl = this.ConnectionUrls[0];
        }

        private async Task Start()
        {
            // Create proxy            
            this._connection = 
                new HubConnectionBuilder()
                    .WithConsoleLogger(LogLevel.Trace)
                    .WithUrl(this.BaseUrl)
                    .WithJsonProtocol()
                    .WithTransport(TransportType.WebSockets)
                    .Build();

            this._connection.Connected += this.OnConnected;
            this._connection.Closed += this.OnConnectionClosed;

            // Set up handler
            this._connection.On("test", () => { int i = 0; i++; i--; });
            this._connection.On<object>("testNumber", _ => { int i = 0; i++; i--; });
            this._connection.On<int>("testNumber", _ => { int i = 0; i++; i--; });

            this._connection.On<bool>("HasJoined",
                (result) =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() => { Log($"RECEIVED: Join (alreadyJoined: '{result}') - {ClientGuid}"); }));
                });

            this._connection.On<bool>("HasLeft",
                (result) =>
                {
                    this.Dispatcher.BeginInvoke(new Action(() => { this.Log($"RECEIVED: HasLeft '{result}'"); }));
                });

            this._connection.On<string, string>("SendCommand",
                async (messageType, messageId) =>
                {
                    await this.Dispatcher.BeginInvoke(new Action(() => { this.Log($"RECEIVED: SendCommand '{messageType}'"); }));
                });

            // Connect
            try
            {
                await this.Dispatcher.BeginInvoke(new Action(() => { this.Log($"Connecting to {this.BaseUrl}..."); }));
                await this._connection.StartAsync();
            }
            catch (Exception e)
            {
                this.Log($"{e.ToString()} - {e.Message} \r\n{e.StackTrace}");
            }
        }

        private async void startBtn_Click(object sender, RoutedEventArgs e)
        {
            await Start();
        }

        private async Task OnConnected()
        {
            await this.Dispatcher.BeginInvoke(new Action(() => { Log($"Connected to {this.BaseUrl}"); }));
            await this.Dispatcher.BeginInvoke(new Action(() => { Log($"Waiting 2 second before 'join' call..."); }));

            await Task.Delay(TimeSpan.FromSeconds(2));

            var command = "Join";
            await this.Dispatcher.BeginInvoke(new Action(() => { Log($"SENDING: {command}"); }));

            await this._connection.InvokeAsync("Join", ClientGuid, "TestApp desc");
        }

        private async Task OnConnectionClosed(Exception obj)
        {
            await this.Dispatcher.BeginInvoke(new Action(() => { Log($"/!\\ Connection closed, try to restart", true); }));
            await Start();
        }

        private void stopBtn_Click(object sender, RoutedEventArgs e)
        {
            this._connection.Closed -= OnConnectionClosed;
            this._connection.DisposeAsync();
            this.Dispatcher.BeginInvoke(new Action(() => { Log($"Connection disposed ('{(this._connection == null)}')"); }));
        }

        private async void sendLeaveCommand_Click(object sender, RoutedEventArgs e)
        {
            var command = "Leave";
            await this.Dispatcher.BeginInvoke(new Action(() => { Log($"SENDING: {command}"); }));

            await this._connection.InvokeAsync("Leave", ClientGuid);
        }

        #region Methods : Helpers

        private void Log(string text, bool error = false)
        {
            Messages.Insert(0, new LogMessage { Text = DateTime.Now.ToString(@"dd/MM | HH:mm:ss | ") + text, Error = error });
        } 

        #endregion
    }
}
