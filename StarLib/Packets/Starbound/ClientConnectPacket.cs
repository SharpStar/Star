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
using StarLib.Misc;
using StarLib.Packets.Serialization.Attributes;

namespace StarLib.Packets.Starbound
{
	public class ClientConnectPacket : Packet
	{
		public override byte PacketId
		{
			get { return (byte)PacketType.ClientConnect; }
			protected set { throw new NotImplementedException(); }
		}
		
		[StarSerialize(0)]
		public byte[] AssetDigest { get; set; }

		[StarSerialize(1)]
		public Uuid Uuid { get; set; }

		[StarSerialize(2)]
		public string PlayerName { get; set; }

		[StarSerialize(3)]
		public string Species { get; set; }

		[StarSerialize(4)]
		public byte[] Shipworld { get; set; }

		[StarSerialize(5)]
		public int ShipLevel { get; set; }

		[StarSerialize(6)]
		public int MaxFuel { get; set; }

		[StarSerialize(7)]
		public IList<string> Capabilities { get; set; }

		[StarSerialize(8)]
		public string Account { get; set; }

		[Greedy]
		[StarSerialize(9)]
		public IList<byte> Unknown { get; set; }

	}
}
