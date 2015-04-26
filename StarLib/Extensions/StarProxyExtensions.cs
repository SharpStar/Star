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
using StarLib.Packets.Starbound;
using StarLib.Server;

namespace StarLib.Extensions
{
	public static class StarProxyExtensions
	{
		public static void SendChatMessage(this StarProxy proxy, string name, string message, int maxCharsPerLine = 60)
		{
			foreach (string line in message.SeparateString(maxCharsPerLine))
			{
				foreach (string line2 in line.Split('\n'))
				{
					proxy.ClientConnection.SendPacket(new ChatReceivePacket
					{
						Name = name,
						Message = line2,
					});
				}
			}
		}

		public static void Kick(this StarProxy proxy, string reason = "")
		{
			proxy.ClientConnection.SendPacket(new ServerDisconnectPacket
			{
				Reason = reason
			});
		}
	}
}
