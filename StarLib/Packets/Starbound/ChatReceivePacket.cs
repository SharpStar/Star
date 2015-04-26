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
using StarLib.Networking;
using StarLib.Packets.Serialization.Attributes;
using StarLib.Starbound;

namespace StarLib.Packets.Starbound
{
	public class ChatReceivePacket : Packet
	{
		public override byte PacketId
		{
			get { return (byte)PacketType.ChatReceived; }
			protected set { throw new NotImplementedException(); }
		}

		[StarSerialize(0)]
		public RecvMessageType MessageType { get; set; }


		[StarSerialize(1)]
		public string Channel { get; set; }


		[StarSerialize(2)]
		public int ClientId { get; set; }


		[StarSerialize(3)]
		public string Name { get; set; }


		[StarSerialize(4)]
		public string Message { get; set; }

		public ChatReceivePacket()
		{
			Channel = string.Empty;
			Name = string.Empty;
			Message = string.Empty;
		}
	}
}
