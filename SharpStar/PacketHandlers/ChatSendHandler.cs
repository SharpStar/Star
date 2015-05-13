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
using StarLib.Commands.PlayerEvent;
using StarLib.Logging;
using StarLib.Packets;
using StarLib.Packets.Starbound;
using StarLib.Plugins;
using StarLib.Server;

namespace SharpStar.PacketHandlers
{
	public class ChatSendHandler : PacketHandler<ChatSendPacket>
	{
		public override void Handle(ChatSendPacket packet, StarConnection connection)
		{
			if (packet.Text.StartsWith("/"))
			{
				string command = packet.Text.Substring(1);
                
				bool found = false;

				foreach (PlayerEventCommand cmd in Program.PlayerCommands)
				{
					if (cmd.PassPlayerEventCommand(command, connection.Proxy.Player))
						found = true;
				}

				//Star command found, we're done here.
				if (found)
				{
					packet.Ignore = true;

					return;
				}

				foreach (IPluginManager pm in StarMain.Instance.PluginManagers)
				{
					if (pm.PassCommand(command, connection.Proxy.Player))
						found = true;
				}

				packet.Ignore = found;
			}
		}

		public override void HandleSent(ChatSendPacket packet, StarConnection connection)
		{
		}
	}
}
