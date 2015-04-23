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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarLib.Server
{
    /// <summary>
    /// Manages all proxies
    /// </summary>
    public class StarProxyManager : IEnumerable<StarProxy>
    {

        public event EventHandler<ProxyConnectionEventArgs> ConnectionAdded;

        protected ConcurrentDictionary<string, StarProxy> Connections;

		public int Count
		{
			get
			{
				return Connections.Count;
			}
		}

        public StarProxyManager()
        {
            Connections = new ConcurrentDictionary<string, StarProxy>();
        }

        public bool AddProxy(string connId, StarProxy proxy)
        {
            if (connId == null)
                throw new ArgumentNullException("connId");

            if (proxy == null)
                throw new ArgumentNullException("proxy");

            proxy.ConnectionClosed += (s, e) =>
            {
                RemoveProxy(connId);
                proxy.Dispose();
            };

            if (!Connections.TryAdd(connId, proxy))
                return false;

            EventHandler<ProxyConnectionEventArgs> connectionAdded = ConnectionAdded;
            if (connectionAdded != null)
                connectionAdded(this, new ProxyConnectionEventArgs(proxy));

            return true;
        }

        public bool RemoveProxy(string connId)
        {
            StarProxy proxy;
            return Connections.TryRemove(connId, out proxy);
        }

        public StarProxy this[string id]
        {
            get
            {
                return Connections[id];
            }
        }

        public IEnumerator<StarProxy> GetEnumerator()
        {
            return Connections.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
