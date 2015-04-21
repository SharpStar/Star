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
using StarLib.Packets;

namespace StarLib.Events.Packets
{
	[AttributeUsage(AttributeTargets.Method)]
	public class PacketEventAttribute : Attribute
	{
		public byte PacketId { get; private set; }

		public PacketEventType EventType { get; set; }

		public PacketEventAttribute(byte packetId, PacketEventType eventType = PacketEventType.BeforeSent)
		{
			PacketId = packetId;
			EventType = eventType;
		}

		public PacketEventAttribute(PacketType packetType, PacketEventType eventType = PacketEventType.BeforeSent) : this((byte)packetType, eventType)
		{
		}
	}
}
