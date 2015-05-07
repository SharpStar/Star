// SharpStar. A Starbound wrapper.
// Copyright (C) 2015 Mitchell Kutchuk
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StarLib.Logging;
using StarLib.Packets;
using StarLib.Starbound;

namespace StarLib.Server
{
    /// <summary>
    /// Describes a proxy connection between the Starbound server and the client
    /// </summary>
    public class StarProxy : IDisposable
    {

        private long _running;

        public Player Player { get; set; }

        /// <summary>
        /// The time that the client connected
        /// </summary>
        public DateTime ConnectionTime { get; protected set; }

        /// <summary>
        /// Called when the connections have been closed
        /// </summary>
        public event EventHandler<ProxyConnectionEventArgs> ConnectionClosed;

        /// <summary>
        /// The id of the connection
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// The amount of time that the this proxy has been alive for
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return DateTime.Now - ConnectionTime;
            }
        }

        public bool IsDisposed { get; private set; }

        public bool Connected
        {
            get
            {
                return (ClientConnection != null && ClientConnection.Connected) && (ServerConnection != null && ServerConnection.Connected);
            }
        }

        public bool Running
        {
            get
            {
                return Interlocked.Read(ref _running) == 1;
            }
        }

        public StarServer ListeningServer { get; private set; }

        /// <summary>
        /// The proxy to the Starbound client
        /// </summary>
        public StarConnection ClientConnection { get; set; }

        /// <summary>
        /// The proxy to the Starbound server
        /// </summary>
        public StarConnection ServerConnection { get; set; }

        /// <summary>
        /// Constructs a <see cref="StarProxy"/> object
        /// </summary>
        /// <param name="server">The Star server</param>
        /// <param name="clientConn">The Starbound client connection</param>
        /// <param name="serverConn">The Starbound server connection</param>
        public StarProxy(StarServer server, StarConnection clientConn, StarConnection serverConn)
        {
            if (server == null)
                throw new ArgumentNullException("server");

            if (clientConn == null)
                throw new ArgumentNullException("clientConn");

            if (serverConn == null)
                throw new ArgumentNullException("serverConn");

            _running = 0;

            ListeningServer = server;

            ConnectionId = Guid.NewGuid().ToString();

            ClientConnection = clientConn;
            ClientConnection.OtherConnection = serverConn;
            ClientConnection.Proxy = this;
            ClientConnection.Disconnected += Disconnected;

            ServerConnection = serverConn;
            ServerConnection.OtherConnection = clientConn;
            ServerConnection.Proxy = this;
            ServerConnection.Disconnected += Disconnected;

            Player = new Player { Proxy = this };
        }

        public void Start()
        {
            StartAsync();
        }

        /// <summary>
        /// Starts the proxies
        /// </summary>
        public Task StartAsync()
        {
            if (Running)
                throw new InvalidOperationException("This proxy is already running!");

            Interlocked.CompareExchange(ref _running, 1, 0);

            ConnectionTime = DateTime.Now;
            Task serverTask = ServerConnection.StartAsync();
            Task clientTask = ClientConnection.StartAsync();

            //return Task.WhenAll(serverTask, clientTask);
            return Task.FromResult(true);
        }

        public void Close()
        {
            CloseAsync().Wait();
        }

        /// <summary>
        /// Closes the connections
        /// </summary>
        public async Task CloseAsync()
        {
            if (!IsDisposed && Running)
            {
                if (ClientConnection != null && !ClientConnection.IsDisposed && ClientConnection.Connected)
                {
                    await ClientConnection.StopAsync();
                }

                if (ServerConnection != null && !ServerConnection.IsDisposed && ServerConnection.Connected)
                {
                    await ServerConnection.StopAsync();
                }
            }
        }

        private async void Disconnected(object sender, EventArgs e)
        {
            if (Running)
            {
                Interlocked.CompareExchange(ref _running, 0, 1);

                await CloseAsync();

                if (ConnectionClosed != null)
                    ConnectionClosed(this, new ProxyConnectionEventArgs(this));
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }

            IsDisposed = true;

            ClientConnection = null;
            ServerConnection = null;
        }

        ~StarProxy()
        {
            Dispose(false);
        }
    }
}
