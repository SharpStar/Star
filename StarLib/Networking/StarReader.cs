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
	/// A specialized BinaryReader that is able to read Starbound data types<para/>
	/// For writing, see <seealso cref="StarWriter"/>
	/// </summary>
	public class StarReader : BinaryReader
	{
		public StarReader(byte[] data) : base(new MemoryStream(data))
		{
		}

		public long DataLeft
		{
			get
			{
				return BaseStream.Length - BaseStream.Position;
			}
		}

		/// <summary>
		/// Read a VLQ-specified amount of bytes from the stream
		/// </summary>
		/// <returns>The data</returns>
		public byte[] ReadUInt8Array()
		{
			int length = (int)ReadVLQ();

			return ReadBytes(length);
		}

		//public override int ReadInt32()
		//{
		//	return (int)ReadUInt32();
		//}

		public override uint ReadUInt32()
		{
			return (uint)(
			   (ReadByte() << 24) |
			   (ReadByte() << 16) |
			   (ReadByte() << 8) |
				ReadByte());
		}

		public override long ReadInt64()
		{
			return (long)ReadUInt64();
		}

		public override ulong ReadUInt64()
		{
			return unchecked(
				((ulong)ReadByte() << 56) |
				((ulong)ReadByte() << 48) |
				((ulong)ReadByte() << 40) |
				((ulong)ReadByte() << 32) |
				((ulong)ReadByte() << 24) |
				((ulong)ReadByte() << 16) |
				((ulong)ReadByte() << 8) |
				(ulong)ReadByte());
		}

		public byte[] ReadToEnd()
		{
			return ReadBytes((int)(BaseStream.Length - BaseStream.Position));
		}

		/// <summary>
		/// Reads a string with a VLQ-specified length from the stream
		/// </summary>
		/// <returns>The string</returns>
		public override string ReadString()
		{
			return Encoding.UTF8.GetString(ReadUInt8Array());
		}

		/// <summary>
		/// Reads a VLQ from the stream
		/// </summary>
		/// <returns>A VLQ</returns>
		public ulong ReadVLQ()
		{
			int size;
			return VLQ.FromFunc(_ => ReadByte(), ctr => true, out size);
			//ulong value = 0L;
			//while (true)
			//{
			//	byte tmp = ReadByte();

			//	value = (value << 7) | (uint)(tmp & 0x7f);

			//	if ((tmp & 0x80) == 0)
			//		break;
			//}

			//return value;
		}

		/// <summary>
		/// Reads a Signed VLQ from the stream
		/// </summary>
		/// <returns>A signed VLQ</returns>
		public long ReadSignedVLQ()
		{
			ulong value = ReadVLQ();

			if ((value & 1) == 0x00)
				return (long)value >> 1;

			return -((long)(value >> 1) + 1);
		}
	}
}
