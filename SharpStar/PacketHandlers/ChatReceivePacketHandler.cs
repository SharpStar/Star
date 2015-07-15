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
using SharpStar.PlayerCommands;
using StarLib;
using StarLib.Commands.PlayerEvent;
using StarLib.Extensions;
using StarLib.Packets;
using StarLib.Packets.Starbound;
using StarLib.Server;

namespace SharpStar.PacketHandlers
{
	public class ChatReceivePacketHandler : PacketHandler<ChatReceivePacket>
	{
		public override Task HandleAsync(ChatReceivePacket packet, StarConnection connection)
		{
			if (packet.Message.StartsWith("/"))
			{
				string command = packet.Message.Substring(1);

				if (!Program.PlayerCommandManager.PassCommand(command, new PlayerCommandContext(connection.Proxy.Player)))
					if (!StarMain.Instance.PassPlayerEventCommand(command, connection.Proxy.Player))
						connection.Proxy.SendChatMessage("server", "No such command!");

				packet.Ignore = true;
			}

            return Task.FromResult(false);
		}

		public override Task HandleSentAsync(ChatReceivePacket packet, StarConnection connection)
		{
            return Task.FromResult(false);
		}
	}
}
