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
using StarLib.Packets.Serialization.Attributes;

namespace StarLib.Packets
{
	public class Any
	{
		public object Value { get; set; }

		[StarSerialize(0)]
		public byte Index { get; set; }

		public T GetValue<T>()
		{
			return (T)Value;
		}
	}

	public class Any<T1, T2> : Any where T1 : new() where T2 : new()
	{	
	}

	public class Any<T1, T2, T3> : Any where T1 : new() where T2 : new() where T3 : new()
	{
	}

	public class Any<T1, T2, T3, T4> : Any where T1 : new() where T2 : new() where T3 : new() where T4 : new()
	{
	}
}
