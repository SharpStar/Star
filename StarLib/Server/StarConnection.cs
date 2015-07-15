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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zlib;
using StarLib.DataTypes;
using StarLib.Extensions;
using StarLib.Logging;
using StarLib.Networking;
using StarLib.Packets;
using StarLib.Packets.Serialization;
using StarLib.Utils;

namespace StarLib.Server
{
    /// <summary>
    /// The base class of a Starbound connection
    /// </summary>
    public abstract class StarConnection : IDisposable
    {
        #region Abstract Methods/Properties
        public abstract Direction Direction { get; }

        public abstract Task StartAsync();

        public abstract Task StopAsync();
        #endregion

        #region Public Properties
        public bool IsDisposed { get; private set; }

        public StarConnection OtherConnection { get; set; }

        public StarProxy Proxy { get; internal set; }

        public Socket ConnectionClient { get; protected set; }


        public TaskCompletionSource<bool> Completed { get; protected set; }

        public Dictionary<Type, List<IPacketHandler>> PacketHandlers
        {
            get
            {
                return _packetHandlers;
            }
        }

        public bool Connected
        {
            get
            {
                return Interlocked.Read(ref _connected) == 1;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return ConnectionClient.RemoteEndPoint as IPEndPoint;
            }
        }

        public PacketReader PacketReader { get; protected set; }
        #endregion

        #region Events
        public event EventHandler<PacketEventArgs> PacketSending;

        public event EventHandler<PacketEventArgs> PacketSent;

        public event EventHandler<PacketEventArgs> PacketReceived;

        public event EventHandler<PacketEventArgs> AfterPacketReceived;

        public event EventHandler Disconnected;
        #endregion


        #region Private
        private long _connected;

        private readonly Dictionary<Type, List<IPacketHandler>> _packetHandlers;

        private readonly BlockingCollection<Packet> _packetQueue;

        private readonly CancellationTokenSource _cts;

        private readonly PacketSegmentProcessor _processor;

        private SemaphoreSlim _sem;

        #endregion

        protected StarConnection(Type[] packetTypes)
        {
            _packetHandlers = new Dictionary<Type, List<IPacketHandler>>();
            _packetQueue = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
            _cts = new CancellationTokenSource();
            _sem = new SemaphoreSlim(1, 1);
            _processor = new PacketSegmentProcessor();
            _connected = 0;

            PacketReader = new PacketReader(packetTypes);
            Completed = new TaskCompletionSource<bool>();
        }

        /// <summary>
        /// Registers a set of packet handlers to be used by new connections
        /// </summary>
        /// <param name="handlers">The handlers to register</param>
        public void RegisterPacketHandlers(IEnumerable<IPacketHandler> handlers)
        {
            foreach (IPacketHandler handler in handlers)
            {
                if (!PacketHandlers.ContainsKey(handler.Type))
                    PacketHandlers.Add(handler.Type, new List<IPacketHandler>(new[] { handler }));
                else
                    PacketHandlers[handler.Type].Add(handler);
            }
        }

        /// <summary>
        /// Removes a packet handler
        /// </summary>
        /// <param name="handler"></param>
        public bool UnregisterPacketHandler(IPacketHandler handler)
        {
            if (PacketHandlers.ContainsKey(handler.Type))
                return PacketHandlers[handler.Type].Remove(handler);

            return false;
        }

        /// <summary>
        /// Start receiving data from this connection
        /// </summary>
        public virtual Task StartReceiveAsync()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("this");

            if (Connected)
                throw new Exception("Already connected and receiving!");

            if (ConnectionClient == null)
                throw new NullReferenceException();

            Interlocked.CompareExchange(ref _connected, 1, 0);

            ConnectionClient.NoDelay = true;

            SocketAsyncEventArgs args = Proxy.ListeningServer.SocketPool.Get();
            args.Completed += Operation_Completed;

            if (!ConnectionClient.ReceiveAsync(args))
                Operation_Completed(this, args);

            return Task.FromResult(false);
        }

        protected virtual async void Operation_Completed(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= Operation_Completed;

            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    await ProcessAsync(e);

                    Proxy.ListeningServer.SocketPool.Add(e);
                    break;
                case SocketAsyncOperation.Send:
                    e.SetBuffer(null, 0, 0);

                    if (PacketSent != null)
                        PacketSent(this, new PacketEventArgs(Proxy, (Packet)e.UserToken));
                    break;
            }
        }

        protected virtual Task CloseAsync()
        {
            if (!Connected)
                return Task.FromResult(false);

            Interlocked.CompareExchange(ref _connected, 0, 1);

            _cts.Cancel();
            _packetQueue.CompleteAdding();

            try
            {
                if (ConnectionClient != null)
                {
                    ConnectionClient.Disconnect(false);

                    if (ConnectionClient.Poll(10000, SelectMode.SelectRead))
                    {
                    }
                }
            }
            catch
            {
            }
            finally
            {
                EventHandler disconnected = Disconnected;
                if (disconnected != null)
                    disconnected(this, EventArgs.Empty);

                if (ConnectionClient != null)
                {
                    ConnectionClient.Close();
                    ConnectionClient = null;
                }

                if (Completed != null)
                {
                    Completed.SetResult(true);
                }
            }

            return Task.FromResult(false);
        }

        protected virtual async Task ProcessAsync(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                bool exception = false;

                try
                {
                    if (_cts.IsCancellationRequested)
                        return;

                    SocketAsyncEventArgs newArgs = Proxy.ListeningServer.SocketPool.Get();
                    newArgs.Completed += Operation_Completed;

                    if (ConnectionClient != null && !ConnectionClient.ReceiveAsync(newArgs))
                        Operation_Completed(this, newArgs);

                    await _sem.WaitAsync(_cts.Token);

                    var packets = PacketReader.Read(_processor, e.Buffer, e.Offset, e.BytesTransferred);

                    foreach (Packet packet in packets)
                    {
                        if (_cts.IsCancellationRequested)
                            return;

                        if (packet == null)
                        {
                            StarLog.DefaultLogger.Warn("Encountered null packet!");

                            continue;
                        }

                        try
                        {
                            packet.Direction = Direction;

                            Type pType = packet.GetType();
                            List<IPacketHandler> pHandlers = null;
                            if (PacketHandlers.ContainsKey(pType))
                            {
                                pHandlers = PacketHandlers[pType];

                                var tasks = new List<Task>();
                                foreach (IPacketHandler beforeHandler in pHandlers)
                                {
                                    tasks.Add(beforeHandler.HandleBeforeAsync(packet, this));
                                }

                                await Task.WhenAll(tasks);
                            }

                            EventHandler<PacketEventArgs> packetArgs = PacketReceived;
                            if (packetArgs != null)
                                packetArgs(this, new PacketEventArgs(Proxy, packet));

                            OtherConnection.FlushPacket(packet);

                            if (pHandlers != null)
                            {
                                var tasks = new List<Task>();
                                foreach (IPacketHandler sentHandler in pHandlers)
                                {
                                    tasks.Add(sentHandler.HandleAfterAsync(packet, this));
                                }

                                await Task.WhenAll(tasks);
                            }

                            EventHandler<PacketEventArgs> aPacketArgs = AfterPacketReceived;
                            if (aPacketArgs != null)
                                aPacketArgs(this, new PacketEventArgs(Proxy, packet));

                        }
                        catch (Exception ex)
                        {
                            ex.LogError();
                        }
                    }

                    _sem.Release();
                }
                catch (Exception)
                {
                    exception = true;
                }

                if (exception)
                    await CloseAsync();
            }
            else
            {
                await CloseAsync();
            }
        }

        /// <summary>
        /// Sends a packet to this connection
        /// </summary>
        /// <param name="sendingPacket">The packet to send</param>
        public virtual void SendPacket(Packet sendingPacket)
        {
            SendPacketAsync(sendingPacket).Wait();
        }

        /// <summary>
        /// Sends a packet to this connection asynchronously
        /// </summary>
        /// <param name="sendingPacket">The packet to send</param>
        public virtual Task SendPacketAsync(Packet sendingPacket)
        {
            FlushPacket(sendingPacket);

            return Task.FromResult(false);
        }

        protected virtual void FlushPacket(Packet packet)
        {
            if (ConnectionClient == null || !ConnectionClient.Connected)
                return;

            try
            {
                packet.Direction = Direction;

                EventHandler<PacketEventArgs> packetSending = PacketSending;
                if (packetSending != null)
                    packetSending(this, new PacketEventArgs(Proxy, packet));

                if (packet.Ignore)
                    return;

                byte[] buffer;

                GenericPacket gp = packet as GenericPacket;

                if (gp != null)
                {
                    buffer = gp.Data;
                }
                else
                {
                    buffer = PacketSerializer.Serialize(packet);
                }

                bool compressed = buffer.Length >= 4096 || packet.AlwaysCompress;

                if (compressed)
                {
                    buffer = ZlibStream.CompressBuffer(buffer);
                }

                int length = compressed ? -buffer.Length : buffer.Length;

                byte[] lenBuf = VLQ.CreateSigned(length);

                byte[] finalBuffer = new byte[1 + lenBuf.Length + buffer.Length];
                finalBuffer[0] = packet.PacketId;
                Buffer.BlockCopy(lenBuf, 0, finalBuffer, 1, lenBuf.Length);
                Buffer.BlockCopy(buffer, 0, finalBuffer, 1 + lenBuf.Length, buffer.Length);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.UserToken = packet;
                args.Completed += Operation_Completed;
                args.SetBuffer(finalBuffer, 0, finalBuffer.Length);

                if (ConnectionClient != null && !ConnectionClient.SendAsync(args))
                    Operation_Completed(this, args);
            }
            catch
            {
                CloseAsync().Wait();
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;

            if (disposing)
            {
                _sem.Dispose();
            }

            _sem = null;
        }

        ~StarConnection()
        {
            Dispose(false);
        }
    }
}
