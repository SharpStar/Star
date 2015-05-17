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
using System.Text;
using System.Threading.Tasks;
using StarLib;
using StarLib.Logging;
using StarLib.Packets;
using StarLib.Packets.Starbound;
using StarLib.Server;

namespace SharpStar.PacketHandlers
{
	public class ConnectSuccessHandler : PacketHandler<ConnectSuccessPacket>
	{
		public override Task HandleAsync(ConnectSuccessPacket packet, StarConnection connection)
		{
			if (!connection.Proxy.Player.AuthSuccess)
				packet.Ignore = true;

			if (connection.Proxy.Player.Account != null)
				StarLog.DefaultLogger.Info("Player {0} ({1}) has joined", connection.Proxy.Player.Name, connection.Proxy.Player.Account.Username);
			else
				StarLog.DefaultLogger.Info("Player {0} has joined", connection.Proxy.Player.Name);

            return Task.FromResult(false);
		}

		public override Task HandleSentAsync(ConnectSuccessPacket packet, StarConnection connection)
		{
            return Task.FromResult(false);
		}
	}
}
