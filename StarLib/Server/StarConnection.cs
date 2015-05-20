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

        public TcpClient ConnectionClient { get; protected set; }

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
                return ConnectionClient.Client.RemoteEndPoint as IPEndPoint;
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

        private SocketAsyncEventArgs _readEventArgs;

        private TaskCompletionSource<bool> _completed;

        private readonly Dictionary<Type, List<IPacketHandler>> _packetHandlers;

        private readonly CancellationTokenSource _cts;

        #endregion

        protected StarConnection(Type[] packetTypes)
        {
            //_packetQueue = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
            _packetHandlers = new Dictionary<Type, List<IPacketHandler>>();
            _cts = new CancellationTokenSource();
            _readEventArgs = new SocketAsyncEventArgs();
            _connected = 0;

            PacketReader = new PacketReader(packetTypes);
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
        public virtual async Task StartReceiveAsync()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("this");

            if (Connected)
                throw new Exception("Already connected and receiving!");

            Interlocked.CompareExchange(ref _connected, 1, 0);

            ConnectionClient.Client.NoDelay = true;

            byte[] buffer = new byte[65536];
            _readEventArgs.Completed += Operation_Completed;
            _readEventArgs.SetBuffer(buffer, 0, buffer.Length);

            _completed = new TaskCompletionSource<bool>();

            if (!ConnectionClient.Client.ReceiveAsync(_readEventArgs))
            {
                await ProcessReceiveAsync(_readEventArgs);
            }

            await _completed.Task;
        }

        private async void Operation_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    await ProcessReceiveAsync(e);
                    break;
                case SocketAsyncOperation.Send:

                    EventHandler<PacketEventArgs> packetSent = PacketSent;
                    if (packetSent != null)
                        packetSent(this, new PacketEventArgs(Proxy, e.UserToken as Packet));

                    e.Dispose();
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
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
                if (ConnectionClient != null)
                {
                    await Task.Factory.FromAsync(ConnectionClient.Client.BeginDisconnect(false, null, null), ConnectionClient.Client.EndDisconnect);

                    if (ConnectionClient.Client.Poll(10000, SelectMode.SelectRead))
                    {
                        //Stopwatch sw = Stopwatch.StartNew();

                        //byte[] tmpBuf = new byte[1024];
                        //while (_networkStream.Read(tmpBuf, 0, tmpBuf.Length) > 0)
                        //{
                        //    if (sw.Elapsed > TimeSpan.FromMilliseconds(100))
                        //        break;
                        //}

                        //sw.Stop();
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

                if (_completed != null)
                {
                    _completed.SetResult(true);
                    _completed = null;
                }
            }
        }

        protected virtual async Task ProcessReceiveAsync(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                if (_cts.IsCancellationRequested)
                    return;

                foreach (Packet packet in PacketReader.Read(e.Buffer, e.Offset, e.BytesTransferred))
                {
                    if (_cts.IsCancellationRequested)
                        return;

                    try
                    {
                        packet.Direction = Direction;

                        Type pType = packet.GetType();
                        List<IPacketHandler> pHandlers = null;
                        if (PacketHandlers.ContainsKey(pType))
                        {
                            pHandlers = PacketHandlers[pType];

                            foreach (IPacketHandler beforeHandler in pHandlers)
                            {
                                await beforeHandler.HandleBeforeAsync(packet, this);
                            }
                        }

                        EventHandler<PacketEventArgs> packetArgs = PacketReceived;
                        if (packetArgs != null)
                            packetArgs(this, new PacketEventArgs(Proxy, packet));

                        await OtherConnection.SendPacketAsync(packet);

                        if (pHandlers != null)
                        {
                            foreach (IPacketHandler sentHandler in pHandlers)
                            {
                                await sentHandler.HandleAfterAsync(packet, this);
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

                if (ConnectionClient != null && !ConnectionClient.Client.ReceiveAsync(e))
                {
                    Operation_Completed(this, e);
                }
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
            return FlushPacket(sendingPacket);
        }

        protected virtual Task FlushPacket(Packet packet)
        {
            if (ConnectionClient != null && !ConnectionClient.Connected)
                return Task.FromResult(false);

            try
            {
                packet.Direction = Direction;

                EventHandler<PacketEventArgs> packetSending = PacketSending;
                if (packetSending != null)
                    packetSending(this, new PacketEventArgs(Proxy, packet));

                if (packet.Ignore)
                    return Task.FromResult(false);

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

                bool compressed = buffer.Length >= 8192 || packet.AlwaysCompress;

                if (compressed)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (ZlibStream zs = new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression))
                        {
                            zs.Write(buffer, 0, buffer.Length);
                        }

                        buffer = ms.ToArray();
                    }
                }

                if (ConnectionClient != null && ConnectionClient.Connected)
                {
                    int length = compressed ? -buffer.Length : buffer.Length;

                    byte[] lenBuf = VLQ.CreateSigned(length);

                    byte[] finalBuffer = new byte[1 + lenBuf.Length + buffer.Length];
                    finalBuffer[0] = packet.PacketId;
                    Buffer.BlockCopy(lenBuf, 0, finalBuffer, 1, lenBuf.Length);
                    Buffer.BlockCopy(buffer, 0, finalBuffer, 1 + lenBuf.Length, buffer.Length);

                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.RemoteEndPoint = RemoteEndPoint;
                    args.UserToken = packet;
                    args.Completed += Operation_Completed;
                    args.SetBuffer(finalBuffer, 0, finalBuffer.Length);

                    //var segments = new List<ArraySegment<byte>>
                    //{
                    //    new ArraySegment<byte>(new[] { packet.PacketId }),
                    //    new ArraySegment<byte>(lenBuf),
                    //    new ArraySegment<byte>(buffer)
                    //};

                    //args.BufferList = segments;

                    if (!ConnectionClient.Client.SendAsync(args))
                        Operation_Completed(this, args);
                }
            }
            catch (Exception ex)
            {
                ex.LogError();
                return CloseAsync();
            }

            return Task.FromResult(false);
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
                _readEventArgs.Dispose();
            }

            _readEventArgs = null;
        }

        ~StarConnection()
        {
            Dispose(false);
        }
    }
}
