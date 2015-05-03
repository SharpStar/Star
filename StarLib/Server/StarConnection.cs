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

        public abstract void Start();

        public abstract void Stop();
        #endregion

        #region Public Properties
        public bool IsDisposed { get; private set; }

        public StarConnection OtherConnection { get; set; }

        public StarProxy Proxy { get; internal set; }

        public Socket ConnectionSocket { get; protected set; }

        public Dictionary<byte, List<IPacketHandler>> PacketHandlers
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

        private SocketAsyncEventArgs _readArgs;

        private readonly BlockingCollection<Packet> _packetQueue;

        private readonly Dictionary<byte, List<IPacketHandler>> _packetHandlers;


        //private readonly CancellationTokenSource _flushToken;
        #endregion

        protected StarConnection(Type[] packetTypes)
        {
            _packetQueue = new BlockingCollection<Packet>(new ConcurrentQueue<Packet>());
            _packetHandlers = new Dictionary<byte, List<IPacketHandler>>();
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
                        PacketHandlers.Add(handler.PacketId, new List<IPacketHandler>(new[] { handler }));
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
        public void StartReceive()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("this");

            if (Connected)
                throw new Exception("Already connected and receiving!");

            Interlocked.CompareExchange(ref _connected, 1, 0);

            ConnectionSocket.NoDelay = true;

            _readArgs = new SocketAsyncEventArgs();

            byte[] buffer = new byte[16000];
            _readArgs.SetBuffer(buffer, 0, buffer.Length);
            _readArgs.RemoteEndPoint = ConnectionSocket.RemoteEndPoint;

            _readArgs.Completed += IO_Completed;

            bool willRaiseEvent = ConnectionSocket.ReceiveAsync(_readArgs);

            if (!willRaiseEvent)
            {
                ProcessReceive(_readArgs);
            }
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (IsDisposed || !Connected)
            {
                e.Completed -= IO_Completed;
                e.Dispose();

                return;
            }

            try
            {
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        ProcessReceive(e);
                        break;
                    case SocketAsyncOperation.Send:
                        e.Completed -= IO_Completed;
                        e.SetBuffer(null, 0, 0);
                        e.Dispose();

                        break;
                    default:
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }
            catch (Exception ex)
            {
                ex.LogError();

                Close();
            }
        }

        protected virtual void Close()
        {
            if (!Connected)
                return;

            Interlocked.CompareExchange(ref _connected, 0, 1);

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                _packetQueue.CompleteAdding();

                if (ConnectionSocket != null)
                {
                    ConnectionSocket.Shutdown(SocketShutdown.Send);

                    byte[] tmpBuf = new byte[1024];
                    while (ConnectionSocket.Poll(100000, SelectMode.SelectRead)) //100 ms timeout
                    {
                        if (ConnectionSocket.Receive(tmpBuf) <= 0 || sw.Elapsed > TimeSpan.FromSeconds(5))
                            break;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                sw.Stop();

                if (ConnectionSocket != null)
                {
                    ConnectionSocket.Close();
                    ConnectionSocket = null;
                }

                EventHandler disconnected = Disconnected;
                if (disconnected != null)
                    disconnected(this, EventArgs.Empty);

                if (_readArgs != null)
                    _readArgs.Dispose();
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            OnRecieved(e);
        }

        protected virtual void OnRecieved(SocketAsyncEventArgs e)
        {
            if (IsDisposed || !Connected)
                return;

            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                byte[] cpyBuff = new byte[e.BytesTransferred];
                Buffer.BlockCopy(e.Buffer, e.Offset, cpyBuff, 0, cpyBuff.Length);

                foreach (Packet packet in PacketReader.Read(cpyBuff, 0))
                {
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

                            foreach (IPacketHandler beforeHandler in pHandlers)
                            {
                                beforeHandler.HandleBefore(packet, this);
                            }
                        }

                        EventHandler<PacketEventArgs> packetArgs = PacketReceived;
                        if (packetArgs != null)
                            packetArgs(this, new PacketEventArgs(Proxy, packet));

                        OtherConnection.SendPacket(packet);

                        if (pHandlers != null)
                        {
                            foreach (IPacketHandler sentHandler in pHandlers)
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

                try
                {
                    bool willRaiseEvent = ConnectionSocket.ReceiveAsync(e);

                    if (!willRaiseEvent)
                    {
                        ProcessReceive(e);
                    }
                }
                catch
                {
                    Close();
                }
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Sends a packet to this connection
        /// </summary>
        /// <param name="sendingPacket">The packet to send</param>
        public virtual void SendPacket(Packet sendingPacket)
        {
            if (!Connected)
            {
                if (!_packetQueue.IsAddingCompleted)
                    _packetQueue.Add(sendingPacket);
            }
            else
            {
                if (_packetQueue.Count > 0)
                    FlushPackets();
                else
                    FlushPacket(sendingPacket);
            }
        }

        /// <summary>
        /// Flush all packets currently queued to the client
        /// </summary>
        public void FlushPackets()
        {
            foreach (Packet packet in _packetQueue.GetConsumingEnumerable())
            {
                FlushPacket(packet);
            }
        }

        protected virtual void FlushPacket(Packet packet)
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

                bool compressed = buffer.Length >= 128000;

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

                SocketAsyncEventArgs writeArgs = new SocketAsyncEventArgs();
                writeArgs.RemoteEndPoint = ConnectionSocket.RemoteEndPoint;
                writeArgs.SetBuffer(finalBuffer, 0, finalBuffer.Length);
                writeArgs.Completed += IO_Completed;

                Packet tmpPacket = packet;
                EventHandler<SocketAsyncEventArgs> handler = null;
                handler = (s, e) =>
                {
                    EventHandler<PacketEventArgs> packetSent = PacketSent;
                    if (packetSent != null)
                        packetSent(this, new PacketEventArgs(Proxy, tmpPacket));

                    if (handler != null)
                        writeArgs.Completed -= handler;
                };

                writeArgs.Completed += handler;

                if (Connected)
                {
                    if (!ConnectionSocket.SendAsync(writeArgs))
                        IO_Completed(this, writeArgs);
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

            _readArgs = null;
        }

        ~StarConnection()
        {
            Dispose(false);
        }

    }
}
