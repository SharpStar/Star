﻿// SharpStar. A Starbound wrapper.
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
using StarLib.Packets.Serialization.Attributes;

namespace StarLib.Misc
{
	public class Uuid
	{

		//[StarSerialize(0)]
		//[StarSerializeCondition]
		//public bool HasUuid { get; set; }

		[StarSerialize(0, Length = 16)]
		public IList<byte> Data { get; set; }

		public string Id
		{
			get
			{
				return BitConverter.ToString(Data.ToArray()).Replace("-", "").ToLower();
			}
			set
			{
				byte[] uuid = Encoding.UTF8.GetBytes(value);

				if (uuid.Length != 16)
					throw new ArgumentException("Invalid length given for Uuid!", "value");

				Data = Encoding.UTF8.GetBytes(value);
			}
		}

		public override string ToString()
		{
			return Id;
		}
	}
}
