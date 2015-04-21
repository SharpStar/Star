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

		public override void Start()
		{
			SocketAsyncEventArgs args = new SocketAsyncEventArgs();
			args.Completed += Conn_Completed;
			args.RemoteEndPoint = Proxy.ListeningServer.ServerEndPoint;

			ConnectionSocket = new Socket(StarMain.Instance.Server.ServerEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			ConnectionSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

			ConnectionSocket.ConnectAsync(args);
		}

		public override void Stop()
		{
			Close();
		}

		private void Conn_Completed(object sender, SocketAsyncEventArgs e)
		{
			OnConnectionCompleted(e);
		}

		protected virtual void OnConnectionCompleted(SocketAsyncEventArgs e)
		{
			if (e.LastOperation == SocketAsyncOperation.Connect)
			{
				if (e.SocketError != SocketError.Success)
				{
					StarLog.DefaultLogger.Error("Proxy connection to server has failed.");

					Stop();

					return;
				}

				StartReceive();
				OtherConnection.FlushPackets();

				e.Completed -= Conn_Completed;

				e.Dispose();
			}
			else
			{
				throw new Exception("This shouldn't happen!");
			}
		}
	}
}
