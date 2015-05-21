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
using System.Linq.Expressions;
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

        private readonly Dictionary<Type, Func<IPacketHandler>> _packetHandlers;

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

        public List<Type> PacketHandlers
        {
            get
            {
                return _packetHandlers.Keys.ToList();
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
            : this(config, connManager, Type.EmptyTypes)
        {
        }

        /// <summary>
        /// Constructs a <see cref="StarServer"/> instance
        /// </summary>
        /// <param name="config">The configuration for this <see cref="StarServer"/></param>
        /// <param name="proxyManager"></param>
        /// <param name="packetTypes">The packet types to be used by this server instance</param>
        public StarServer(ServerConfiguration config, StarProxyManager proxyManager, Type[] packetTypes)
            : this(config, proxyManager, packetTypes, Type.EmptyTypes)
        {
        }

        /// <summary>
        /// Constructs a <see cref="StarServer"/> instance
        /// </summary>
        /// <param name="config">The configuration for this <see cref="StarServer"/></param>
        /// <param name="proxyManager"></param>
        /// <param name="packetTypes">The packet types to be used by this server instance</param>
        /// <param name="packetHandlerTypes">The type of packet handlers to be used by this server instance</param>
        public StarServer(ServerConfiguration config, StarProxyManager proxyManager, Type[] packetTypes, Type[] packetHandlerTypes)
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

            _packetHandlers = new Dictionary<Type, Func<IPacketHandler>>();
            AddPacketHandlers(packetHandlerTypes);

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
        /// <param name="handlerTypes">The type of handlers to register</param>
        public void AddPacketHandlers(IEnumerable<Type> handlerTypes)
        {
            foreach (Type handlerType in handlerTypes)
            {
                AddPacketHandler(handlerType);
            }
        }

        public void AddPacketHandler(Type handlerType)
        {
            if (!_packetHandlers.ContainsKey(handlerType))
            {
                var func = Expression.Lambda<Func<IPacketHandler>>(Expression.New(handlerType)).Compile();

                _packetHandlers.Add(handlerType, func);
            }
        }

        /// <summary>
        /// Removes a packet handler
        /// </summary>
        /// <param name="handlerType"></param>
        public void RemovePacketHandler(Type handlerType)
        {
            _packetHandlers.Remove(handlerType);
        }

        /// <summary>
        /// Start listening for connections
        /// </summary>
        public virtual void StartServer()
        {
            if (ServerRunning)
                throw new Exception("Server is already running!");

            AcceptingConnections = true;

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

            Task.Run(() => StartAccept());
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

                //TODO: Simulate connnection, return error message to player

                client.Client.Shutdown(SocketShutdown.Both);
                client.Client.Close();

                return;
            }

            Interlocked.Increment(ref _numConnected);
            Interlocked.Increment(ref _totalJoined);

            new Thread(async () =>
            {
                try
                {
                    //client.Client.ReceiveBufferSize = 2048;
                    //client.Client.SendBufferSize = 2048;
                    client.NoDelay = true;

                    StarClientConnection cl = new StarClientConnection(client, _packetTypes);
                    cl.RegisterPacketHandlers(_packetHandlers.Select(p => p.Value()));

                    StarServerConnection server = new StarServerConnection(_packetTypes);
                    server.RegisterPacketHandlers(_packetHandlers.Select(p => p.Value()));

                    var starProxy = new StarProxy(this, cl, server);
                    starProxy.ConnectionClosed += (s, args) => Interlocked.Decrement(ref _numConnected);

                    Proxies.AddProxy(starProxy.ConnectionId, starProxy);

                    Thread.CurrentThread.Name = starProxy.ConnectionId;

                    await starProxy.StartAsync();

                    StarLog.DefaultLogger.Debug("Client disconnected, exiting client thread {0}", Thread.CurrentThread.Name);
                }
                catch (Exception ex)
                {
                    ex.LogError();
                }
            }).Start();
        }
    }
}
