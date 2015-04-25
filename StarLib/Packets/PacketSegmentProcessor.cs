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
using Ionic.Zlib;
using StarLib.DataTypes;
//using StarLib.Networking;

namespace StarLib.Packets
{
	/// <summary>
	/// Processes data that may or may not make up one or more complete packets
	/// </summary>
	public class PacketSegmentProcessor
	{

		private readonly object _locker = new object();

		public byte[] PacketBuffer { get; private set; }

		/// <summary>
		/// Creates a <see cref="PacketSegmentProcessor"/> object
		/// </summary>
		public PacketSegmentProcessor()
		{
			PacketBuffer = new byte[0];
		}

		/// <summary>
		/// Processes the next segment of data and gives the packet id and packet data (if any)
		/// </summary>
		/// <param name="nextSegment">The next segment to be processed</param>
		/// <param name="offset">The position to start at</param>
		/// <param name="packetId">The id of the packet</param>
		/// <param name="packetData">The packet data</param>
		/// <returns>True if more needs to be processed, false if complete</returns>
		public bool ProcessNextSegment(byte[] nextSegment, int offset, out byte packetId, out byte[] packetData)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");

			lock (_locker)
			{
				if (nextSegment.Length > 0)
				{
					//create a new buffer that will fit the amount of data to be held
					byte[] newBuffer = new byte[PacketBuffer.Length + (nextSegment.Length - offset)];

					//copy over the data from the (soon to be) old buffer to the new one
					if (PacketBuffer.Length > 0)
						Buffer.BlockCopy(PacketBuffer, 0, newBuffer, 0, PacketBuffer.Length);

					Buffer.BlockCopy(nextSegment, offset, newBuffer, PacketBuffer.Length, nextSegment.Length - offset);

					PacketBuffer = newBuffer; //set the packet buffer to the new one
				}

				packetData = null;
				packetId = 0;

				if (PacketBuffer.Length <= 1)
					return false;

				packetId = PacketBuffer[0];

				int pos;
				long length;

				try
				{
					length = VLQ.FromBufferSigned(PacketBuffer, 1, PacketBuffer.Length, out pos); //the length of the packet
				}
				catch //we don't have enough data yet! 
				{
					return false;
				}

				bool compressed = length < 0;

				if (compressed)
					length = -length;

				if (PacketBuffer.Length < length + pos)
					return false;

				byte[] data = new byte[(int)length];
				Buffer.BlockCopy(PacketBuffer, pos, data, 0, data.Length);

				pos += data.Length;

				if (compressed)	//uncompress this packet if it has been compressed
				{
					//using (MemoryStream ms = new MemoryStream())
					//{
					//	using (MemoryStream dataMs = new MemoryStream(data))
					//	{
					//		using (ZlibStream zlib = new ZlibStream(ms, CompressionMode.Decompress, true))
					//		{
					//			dataMs.CopyTo(zlib);
					//		}
					//	}

					//	data = ms.ToArray();
					//}
					data = ZlibStream.UncompressBuffer(data);
				}

				packetData = data;

				//set the packet buffer to the remaining data for the next packet to process
				//byte[] rest = reader.ReadToEnd();
				byte[] rest = new byte[PacketBuffer.Length - pos];
				Buffer.BlockCopy(PacketBuffer, pos, rest, 0, rest.Length);

				PacketBuffer = rest;

				return rest.Length > 0;
			}
		}
	}
}
