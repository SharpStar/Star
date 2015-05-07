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
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using StarLib;
using StarLib.Extensions;
using StarLib.Logging;
using StarLib.Packets;
using StarLib.Packets.Starbound;
using StarLib.Server;
using StarLib.Starbound;
using SharpStar.Extensions;

namespace SharpStar.Server
{
    public class StarClientManager
    {

        protected ConcurrentDictionary<StarProxy, IDisposable> ProxyHeartbeats { get; private set; }

        private static readonly StarMain star = StarMain.Instance;

        public StarClientManager()
        {
            ProxyHeartbeats = new ConcurrentDictionary<StarProxy, IDisposable>();
        }

        public void StartWatchingProxies()
        {
            StarProxyManager connManager = star.ConnectionManager;
            connManager.ConnectionAdded += (s, e) =>
            {
                WatchProxy(e.Proxy);
            };

            foreach (StarProxy proxy in connManager)
            {
                WatchProxy(proxy);
            }
        }

        public void StopWatchingProxies()
        {
            foreach (StarProxy proxy in ProxyHeartbeats.Keys)
            {
                StopWatchingProxy(proxy);
            }

            ProxyHeartbeats.Clear();
        }

        public bool StopWatchingProxy(StarProxy proxy)
        {
            if (!ProxyHeartbeats.ContainsKey(proxy))
                return false;

            IDisposable watched;
            ProxyHeartbeats.TryRemove(proxy, out watched);
            watched.Dispose();

            return true;
        }

        protected virtual void WatchProxy(StarProxy proxy)
        {
            if (ProxyHeartbeats.ContainsKey(proxy))
                return;

            proxy.ConnectionClosed += Proxy_ConnectionClosed;

            RegisterHeartbeatCheck(proxy);
            RegisterAccountCheck(proxy);
        }

        private void Proxy_ConnectionClosed(object sender, ProxyConnectionEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Proxy.Player.Name))
                StarLog.DefaultLogger.Info("Player {0} disconnected! ({1})", e.Proxy.Player.Name, e.Proxy.ConnectionId);
            else
                StarLog.DefaultLogger.Info("Connection {0} disconnected", e.Proxy.ConnectionId);
        }

        protected void RegisterAccountCheck(StarProxy proxy)
        {
            if (!proxy.Connected || proxy.ServerConnection == null || proxy.ClientConnection == null)
                return;

            var sEvt = Observable.FromEventPattern<PacketEventArgs>(p => proxy.ServerConnection.PacketReceived += p,
                    p => proxy.ServerConnection.PacketReceived -= p).Select(p => p.EventArgs);
            var cEvt = Observable.FromEventPattern<PacketEventArgs>(p => proxy.ClientConnection.PacketReceived += p,
                    p => proxy.ClientConnection.PacketReceived -= p).Select(p => p.EventArgs);

            var handshake = cEvt.Where(p => p.Packet is HandshakeResponsePacket).Take(1);
            var success = sEvt.Where(p => p.Packet is ConnectSuccessPacket).Take(1);

            handshake.Merge(success).Take(2).CombineWithPrevious((p1, p2) => new
            {
                Proxy = p2.Proxy,
                Previous = p1,
                Current = p2
            }).Subscribe(p =>
            {
                if (p.Proxy.Player.AuthAttempted)
                {
                    if (p.Previous == null && p.Current.Packet is ConnectSuccessPacket)
                    {
                        p.Current.Packet.Ignore = true;
                    }
                    else if (p.Previous != null && p.Previous.Packet is ConnectSuccessPacket && p.Current.Packet is HandshakeResponsePacket)
                    {
                        p.Previous.Packet.Ignore = false;

                        p.Proxy.ClientConnection.SendPacket(p.Previous.Packet);
                    }
                }
            });
        }

        protected void RegisterHeartbeatCheck(StarProxy proxy)
        {
            StarConnection sc = proxy.ClientConnection;

            var packetRecv = Observable.FromEventPattern<PacketEventArgs>(p => sc.PacketReceived += p, p => sc.PacketReceived -= p);
            var heartbeat = (from p in packetRecv where p.EventArgs.Packet.PacketType == PacketType.Heartbeat select p)
                            .Timeout(TimeSpan.FromSeconds(StarMain.Instance.ServerConfig.HeartbeatTimeout));

            var checker = heartbeat.Subscribe(e => { }, e =>
            {
                if (!sc.IsDisposed && sc.Connected)
                {
                    if (!string.IsNullOrEmpty(proxy.Player.Name))
                        StarLog.DefaultLogger.Info("Did not receive a heartbeat packet from the player {0} for a while, kicking.", proxy.Player.Name);
                    else
                        StarLog.DefaultLogger.Info("Did not receive a heartbeat packet from the client {0} for a while, kicking.", proxy.ConnectionId);
                }

                try
                {
                    proxy.Close();
                }
                catch
                {
                }
            }, () => { });

            sc.Disconnected += (s, e) =>
            {
                IDisposable watched;

                ProxyHeartbeats.TryRemove(proxy, out watched);

                if (watched != null)
                    watched.Dispose();
            };

            ProxyHeartbeats.TryAdd(proxy, checker);
        }

    }
}
