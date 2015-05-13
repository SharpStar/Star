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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StarLib.Configuration;
using StarLib.Extensions;
using StarLib.Logging;
using StarLib.Packets;

namespace StarLib.Server
{
    /// <summary>
    /// A Starbound server connection implementation over TCP
    /// </summary>
    public class StarServer
    {
        private long _serverRunning;

        private int _numConnected;
        private int _totalJoined;

        private Type[] _packetTypes;

        private readonly List<IPacketHandler> _packetHandlers;

        public StarProxyManager Proxies { get; private set; }

        protected TcpListener ListenSocket;

        public bool ServerRunning
        {
            get
            {
                return Interlocked.Read(ref _serverRunning) == 1;
            }
        }

        public IPEndPoint LocalEndPoint { get; private set; }

        public IPEndPoint ServerEndPoint { get; private set; }

        public List<IPacketHandler> PacketHandlers
        {
            get
            {
                lock (_packetHandlers)
                {
                    return _packetHandlers.ToList();
                }
            }
        }

        public bool AcceptingConnections { get; set; }

        protected ServerConfiguration ServerConfig { get; private set; }

        /// <summary>
        /// Constructs a <see cref="StarServer"/> instance
        /// </summary>
        /// <param name="config">The configuration for this <see cref="StarServer"/></param>
        /// <param name="connManager"></param>
        public StarServer(ServerConfiguration config, StarProxyManager connManager)
            : this(config, connManager, new Type[0])
        {
        }

        /// <summary>
        /// Constructs a <see cref="StarServer"/> instance
        /// </summary>
        /// <param name="config">The configuration for this <see cref="StarServer"/></param>
        /// <param name="proxyManager"></param>
        /// <param name="packetTypes">The packet types to be used by this server instance</param>
        public StarServer(ServerConfiguration config, StarProxyManager proxyManager, Type[] packetTypes)
            : this(config, proxyManager, packetTypes, new IPacketHandler[0])
        {
        }

        /// <summary>
        /// Constructs a <see cref="StarServer"/> instance
        /// </summary>
        /// <param name="config">The configuration for this <see cref="StarServer"/></param>
        /// <param name="proxyManager"></param>
        /// <param name="packetTypes">The packet types to be used by this server instance</param>
        /// <param name="packetHandlers">The packet handlers to be used by this server instance</param>
        public StarServer(ServerConfiguration config, StarProxyManager proxyManager, Type[] packetTypes, IEnumerable<IPacketHandler> packetHandlers)
        {
            AddPacketTypes(packetTypes);

            Proxies = proxyManager;
            ServerConfig = config;

            IPAddress bindAddress;
            IPAddress serverAddress;

            if (ServerConfig.BindAddress == "0.0.0.0")
                bindAddress = IPAddress.Any;
            else
                bindAddress = IPAddress.Parse(ServerConfig.BindAddress);

            if (ServerConfig.ServerBindAddress == "127.0.0.1")
                serverAddress = IPAddress.Loopback;
            else
                serverAddress = IPAddress.Parse(ServerConfig.ServerBindAddress);

            LocalEndPoint = new IPEndPoint(bindAddress, ServerConfig.BindPort);
            ServerEndPoint = new IPEndPoint(serverAddress, ServerConfig.ServerBindPort);

            _packetHandlers = new List<IPacketHandler>();
            _packetHandlers.AddRange(packetHandlers.Distinct());

            _serverRunning = 0;
        }

        /// <summary>
        /// Adds a series of Packet types to be used by the proxies for processing Packets
        /// </summary>
        /// <param name="packetTypes">The types of Packet types to add</param>
        public void AddPacketTypes(params Type[] packetTypes)
        {
            foreach (Type pType in packetTypes)
            {
                if (!typeof(Packet).IsAssignableFrom(pType))
                    throw new ArgumentException(string.Format("Invalid packet type {0}", pType.FullName), "packetTypes");
            }

            _packetTypes = _packetTypes == null ? packetTypes : _packetTypes.Union(packetTypes).ToArray();
        }

        /// <summary>
        /// Removes a set of Packet types<para/>
        /// After removal, clients that connect will no longer use this packet type for processing
        /// </summary>
        /// <param name="packetTypes">The packet types to remove</param>
        public void RemovePacketTypes(params Type[] packetTypes)
        {
            _packetTypes = _packetTypes.Except(packetTypes).ToArray();
        }

        /// <summary>
        /// Registers a set of packet handlers to be used by new connections
        /// </summary>
        /// <param name="handlers">The handlers to register</param>
        public void AddPacketHandlers(IEnumerable<IPacketHandler> handlers)
        {
            var hList = handlers.ToList();

            lock (_packetHandlers)
            {
                _packetHandlers.RemoveAll(hList.Contains);
                _packetHandlers.AddRange(hList);
            }
        }

        public void AddPacketHandler(IPacketHandler handler)
        {
            AddPacketHandlers(new[] { handler });
        }

        /// <summary>
        /// Removes a packet handler
        /// </summary>
        /// <param name="handler"></param>
        public void RemovePacketHandler(IPacketHandler handler)
        {
            lock (_packetHandlers)
            {
                _packetHandlers.Remove(handler);
            }
        }

        /// <summary>
        /// Start listening for connections
        /// </summary>
        public virtual void StartServer()
        {
            if (ServerRunning)
                throw new Exception("Server is already running!");

            //ListenSocket = new Socket(LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //ListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            //ListenSocket.Bind(LocalEndPoint);
            //ListenSocket.Listen(ServerConfig.MaxConnections);

            AcceptingConnections = true;

            Task.Run(() =>
            {
                ListenSocket = new TcpListener(LocalEndPoint.Address, LocalEndPoint.Port);
                ListenSocket.Server.NoDelay = true;
                //ListenSocket.Server.ReceiveBufferSize = 2048;
                //ListenSocket.Server.SendBufferSize = 2048;

                try
                {
                    ListenSocket.Start();
                }
                catch (Exception ex)
                {
                    ex.LogError();
                }

                Interlocked.CompareExchange(ref _serverRunning, 1, 0);

                return StartAccept();
            });
        }

        /// <summary>
        /// Stop listening for connections
        /// </summary>
        public virtual void StopServer()
        {
            if (!ServerRunning)
                return;

            AcceptingConnections = false;

            Task.Run(() =>
            {
                Parallel.ForEach(Proxies, async proxy =>
                 {
                     await proxy.CloseAsync();
                 });
            }).Wait();

            Interlocked.CompareExchange(ref _serverRunning, 0, 1);

            ListenSocket.Stop();
            ListenSocket = null;
        }

        protected virtual async Task StartAccept()
        {
            try
            {
                TcpClient client = await ListenSocket.AcceptTcpClientAsync();
                await ProcessAccept(client);

                //bool willRaiseEvent = ListenSocket.AcceptAsync(acceptEventArg);
                //if (!willRaiseEvent)
                //{
                //	ProcessAccept(acceptEventArg);
                //}
            }
            catch
            {
            }

            if (ServerRunning)
                await StartAccept();
        }

        protected async Task ProcessAccept(TcpClient client)
        {
            if (ListenSocket == null || !ServerRunning)
                return;

            if (!client.Connected)
            {
                await StartAccept();

                return;
            }

            if (!AcceptingConnections)
            {
                client.Client.Shutdown(SocketShutdown.Both);
                client.Client.Close();

                return;
            }

            StarLog.DefaultLogger.Info("Connection from {0}", client.Client.RemoteEndPoint);

            if (_numConnected >= ServerConfig.MaxConnections)
            {
                StarLog.DefaultLogger.Warn("Exceeded maximum amount of users! Disconnecting {0}", client.Client.RemoteEndPoint);

                client.Client.Shutdown(SocketShutdown.Both);
                client.Client.Close();

                return;
            }

            Interlocked.Increment(ref _numConnected);
            Interlocked.Increment(ref _totalJoined);

            try
            {
                new Thread(async () =>
                {
                    //client.Client.ReceiveBufferSize = 2048;
                    //client.Client.SendBufferSize = 2048;
                    client.NoDelay = true;

                    StarClientConnection cl = new StarClientConnection(client, _packetTypes);
                    cl.RegisterPacketHandlers(_packetHandlers);

                    StarServerConnection server = new StarServerConnection(_packetTypes);
                    server.RegisterPacketHandlers(_packetHandlers);

                    StarProxy starProxy = new StarProxy(this, cl, server);
                    starProxy.ConnectionClosed += (s, args) => Interlocked.Decrement(ref _numConnected);

                    Proxies.AddProxy(starProxy.ConnectionId, starProxy);

                    await starProxy.StartAsync();
                }).Start();
            }
            catch (Exception ex)
            {
                ex.LogError();
            }
        }
    }
}
