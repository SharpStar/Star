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
using System.Threading.Tasks;
using StarLib.Extensions;
using StarLib.Logging;

namespace StarLib.Server
{
    /// <summary>
    /// A connection to the Starbound server
    /// </summary>
    public class StarServerConnection : StarConnection
    {
        public override Direction Direction
        {
            get { return Direction.Server; }
        }

        public StarServerConnection(Type[] packetTypes)
            : base(packetTypes)
        {
        }

        public override Task StartAsync()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = Proxy.ListeningServer.ServerEndPoint;
            args.Completed += Connection_Completed;

            socket.ConnectAsync(args);

            return Completed.Task;
        }

        private async void Connection_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                ConnectionClient = e.ConnectSocket;

                await StartReceiveAsync();
            }
            else
            {
                StarLog.DefaultLogger.Warn("Could not connect to server!");

                Completed.SetResult(false);

                await Proxy.CloseAsync();
            }

            e.Dispose();
        }

        public override Task StopAsync()
        {
            return CloseAsync();
        }
    }
}
