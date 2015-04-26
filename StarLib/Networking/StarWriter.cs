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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarLib.DataTypes;
using StarLib.DataTypes.Variant;

namespace StarLib.Networking
{
	/// <summary>
	/// A specialized BinaryWriter that is able to write Starbound data types<para/>
	/// For reading, use <seealso cref="StarReader"/>
	/// </summary>
	public class StarWriter : BinaryWriter
	{
		public new MemoryStream BaseStream { get; private set; }

		public StarWriter()
			: base(new MemoryStream())
		{
			BaseStream = (MemoryStream)base.BaseStream;
		}

		public void WriteUInt8Array(byte[] data)
		{
			WriteVlq((ulong)data.Length);
			Write(data);
		}

		/// <summary>
		/// Writes a string to the stream with a VLQ length
		/// </summary>
		/// <param name="str">The string to write</param>
		public void WriteStarString(string str)
		{
			WriteUInt8Array(Encoding.UTF8.GetBytes(str));
		}

		public override void Write(ulong value)
		{
			WriteVlq(value);
		}

		public override void Write(int value)
		{
			Write((uint)value);
		}

		/// <summary>
		/// Write a VLQ to the stream
		/// </summary>
		/// <param name="vlq">The VLQ to write</param>
		public void WriteVlq(ulong vlq)
		{
			byte[] buffer = VLQ.Create(vlq);

			Write(buffer);
		}

		/// <summary>
		/// Write a Signed VLQ to the stream
		/// </summary>
		/// <param name="vlq">The VLQ to write</param>
		public void WriteSignedVLQ(long vlq)
		{
			byte[] buffer = VLQ.CreateSigned(vlq);

			Write(buffer);
		}

		public byte[] ToArray()
		{
			return BaseStream.ToArray();
		}
	}
}
