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
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zlib;
using Nito.AsyncEx;
using StarLib.DataTypes;
using StarLib.Extensions;
using StarLib.Logging;
using StarLib.Networking;
using StarLib.Packets;
using StarLib.Packets.Serialization;
using Timer = System.Timers.Timer;

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

        public TcpClient ConnectionClient { get; protected set; }

        public ConcurrentDictionary<byte, List<IPacketHandler>> PacketHandlers
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

        private readonly AsyncCollection<Packet> _packetQueue;

        private readonly ConcurrentDictionary<byte, List<IPacketHandler>> _packetHandlers;

        private readonly CancellationTokenSource _cts;

        private NetworkStream _networkStream;

        #endregion

        protected StarConnection(Type[] packetTypes)
        {
            _packetQueue = new AsyncCollection<Packet>();
            _packetHandlers = new ConcurrentDictionary<byte, List<IPacketHandler>>();
            _cts = new CancellationTokenSource();
            _connected = 0;

            PacketReader = new PacketReader(packetTypes);
        }

        /// <summary>
        /// Registers a set of packet handlers to be used by new connections
        /// </summary>
        /// <param name="handlers">The handlers to register</param>
        public void RegisterPacketHandlers(IEnumerable<IPacketHandler> handlers)
        {
            lock (_packetHandlers)
            {
                foreach (IPacketHandler handler in handlers)
                {
                    if (!PacketHandlers.ContainsKey(handler.PacketId))
                        PacketHandlers.AddOrUpdate(handler.PacketId, new List<IPacketHandler>(new[] { handler }), (id, p) => p);
                    else
                        PacketHandlers[handler.PacketId].Add(handler);
                }
            }
        }

        /// <summary>
        /// Removes a packet handler
        /// </summary>
        /// <param name="handler"></param>
        public bool UnregisterPacketHandler(IPacketHandler handler)
        {
            lock (_packetHandlers)
            {
                if (PacketHandlers.ContainsKey(handler.PacketId))
                    return PacketHandlers[handler.PacketId].Remove(handler);
            }

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

            Interlocked.CompareExchange(ref _connected, 1, 0);

            _networkStream = ConnectionClient.GetStream();

            ConnectionClient.NoDelay = true;

            Task recvTask = Task.Run(() => ProcessReceive(), _cts.Token);
            Task sendTask = Task.Run(() => FlushPackets(), _cts.Token);

            return Task.FromResult(true);
        }

        protected void Close()
        {
            CloseAsync().Wait();
        }

        protected virtual async Task CloseAsync()
        {
            if (!Connected)
                return;

            Interlocked.CompareExchange(ref _connected, 0, 1);

            _cts.Cancel();

            try
            {
                _packetQueue.CompleteAdding();

                if (ConnectionClient != null)
                {
                    await Task.Factory.FromAsync(ConnectionClient.Client.BeginDisconnect(false, null, null), ConnectionClient.Client.EndDisconnect);

                    Stopwatch sw = Stopwatch.StartNew();

                    byte[] tmpBuf = new byte[1024];

                    while (await _networkStream.ReadAsync(tmpBuf, 0, tmpBuf.Length) > 0)
                    {
                        if (sw.Elapsed > TimeSpan.FromSeconds(1))
                            break;
                    }

                    sw.Stop();
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
            }
        }

        protected virtual async Task ProcessReceive()
        {
            byte[] buffer = new byte[8192];

            while (true)
            {
                if (_cts.IsCancellationRequested)
                    break;

                byte[] data;
                try
                {
                    int len = await _networkStream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);

                    if (len == 0)
                    {
                        Close();

                        break;
                    }

                    data = new byte[len];
                    Buffer.BlockCopy(buffer, 0, data, 0, len);
                }
                catch
                {
                    Close();

                    break;
                }

                if (_cts.IsCancellationRequested)
                    break;

                foreach (Packet packet in PacketReader.Read(data, 0))
                {
                    if (_cts.IsCancellationRequested)
                        break;

                    try
                    {
                        if (packet == null)
                        {
                            StarLog.DefaultLogger.Warn("Encountered null packet!");
                            continue;
                        }

                        packet.Direction = Direction;

                        List<IPacketHandler> pHandlers = null;
                        if (PacketHandlers.ContainsKey(packet.PacketId))
                        {
                            pHandlers = PacketHandlers[packet.PacketId];

                            foreach (IPacketHandler beforeHandler in pHandlers.Where(p => p.Type.IsInstanceOfType(packet)))
                            {
                                beforeHandler.HandleBefore(packet, this);
                            }
                        }

                        EventHandler<PacketEventArgs> packetArgs = PacketReceived;
                        if (packetArgs != null)
                            packetArgs(this, new PacketEventArgs(Proxy, packet));

                        await OtherConnection.SendPacketAsync(packet);

                        if (pHandlers != null)
                        {
                            foreach (IPacketHandler sentHandler in pHandlers.Where(p => p.Type.IsInstanceOfType(packet)))
                            {
                                sentHandler.HandleAfter(packet, this);
                            }
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
        public virtual async Task SendPacketAsync(Packet sendingPacket)
        {
            if (!Connected)
                return;

            try
            {
                await _packetQueue.AddAsync(sendingPacket);
            }
            catch (InvalidOperationException)
            {
                Close();
            }

            //if (!Connected)
            //{
            //    if (!_packetQueue.IsAddingCompleted)
            //        _packetQueue.TryAdd(sendingPacket);
            //}
            //else
            //{
            //    if (_packetQueue.Count > 0)
            //        FlushPackets();
            //    else
            //        FlushPacket(sendingPacket);
            //}
        }

        /// <summary>
        /// Flush all packets currently queued to the client asynchronously
        /// </summary>
        public virtual async Task FlushPackets()
        {
            while (await _packetQueue.OutputAvailableAsync(_cts.Token))
            {
                if (_cts.IsCancellationRequested)
                    break;

                Packet packet;

                try
                {
                    packet = await _packetQueue.TakeAsync(_cts.Token);
                }
                catch
                {
                    break;
                }

                if (_cts.IsCancellationRequested)
                    break;

                await FlushPacket(packet);
            }
        }

        protected virtual async Task FlushPacket(Packet packet)
        {
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

                bool compressed = buffer.Length >= 8192;

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

                if (Connected)
                {
                    await _networkStream.WriteAsync(finalBuffer, 0, finalBuffer.Length, _cts.Token);

                    EventHandler<PacketEventArgs> packetSent = PacketSent;
                    if (packetSent != null)
                        packetSent(this, new PacketEventArgs(Proxy, packet));
                }
            }
            catch
            {
                Close();
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
            }
        }

        ~StarConnection()
        {
            Dispose(false);
        }

    }
}
