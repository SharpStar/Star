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
using Microsoft.AspNet.SignalR;
using Star.WebPanel.Hubs;
using StarLib.Events.Packets;
using StarLib.Extensions;
using StarLib.Packets;
using StarLib.Packets.Starbound;

namespace Star.WebPanel.Star.PacketEvents
{
	public class PlayerChatEvent
	{
		[PacketEvent(PacketType.ChatReceived)]
		public void OnChatReceived(PacketEvent evt)
		{
			ChatReceivePacket packet = evt.Packet as ChatReceivePacket;

			Console.WriteLine("TEST! " + evt.Packet.PacketId);

			if (packet != null)
			{
				var hub = GlobalHost.ConnectionManager.GetHubContext<StarHub>();

				hub.Clients.All.chatReceived(packet.Name.StripColors(), packet.Message);
            }
		}

	}
}
