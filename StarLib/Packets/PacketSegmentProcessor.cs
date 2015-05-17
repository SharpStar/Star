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
using Ionic.Zlib;
using StarLib.DataTypes;
using StarLib.Networking;
using StarLib.Utils;

namespace StarLib.Packets
{
    /// <summary>
    /// Processes data that may or may not make up one or more complete packets
    /// </summary>
    public class PacketSegmentProcessor
    {
        public List<byte> PacketBuffer { get; private set; }

        public byte CurrentPacketId { get; private set; }

        public byte[] CurrentPacketData { get; private set; }

        /// <summary>
        /// Creates a <see cref="PacketSegmentProcessor"/> object
        /// </summary>
        public PacketSegmentProcessor()
        {
            PacketBuffer = new List<byte>();
        }

        /// <summary>
        /// Processes the next segment of data and gives the packet id and packet data (if any)
        /// </summary>
        /// <param name="nextSegment">The next segment to be processed</param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <returns>True if more needs to be processed, false if complete</returns>
        public bool ProcessNextSegment(byte[] nextSegment, int offset, int len)
        {
            CurrentPacketData = null;

            if (nextSegment.Length > 0)
            {
                PacketBuffer.AddRange(nextSegment.Skip(offset).Take(len));
            }

            if (PacketBuffer.Count <= 1)
                return false;

            //packetId = reader.ReadByte();
            CurrentPacketId = PacketBuffer[0];

            int pos;
            long length;

            try
            {
                //length = reader.ReadSignedVLQ();
                length = VLQ.FromEnumerableSigned(PacketBuffer, 1, PacketBuffer.Count, out pos); //the length of the packet
            }
            catch //we don't have enough data yet! 
            {
                return false;
            }

            pos += 1;

            bool compressed = length < 0;

            if (compressed)
                length = -length;

            if (PacketBuffer.Count < length + pos)
                return false;

            byte[] data = PacketBuffer.Skip(pos).Take((int)length).ToArray();
            //Buffer.BlockCopy(PacketBuffer, pos, data, 0, data.Length);

            pos += data.Length;

            if (compressed) //decompress this packet if it has been compressed
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    using (MemoryStream outStream = new MemoryStream())
                    {
                        using (ZlibStream zs = new ZlibStream(ms, CompressionMode.Decompress, true))
                        {
                            zs.CopyTo(outStream);
                        }

                        data = outStream.ToArray();
                    }
                }
                //data = await ZLib.DecompressAsync(data);
                //data = ZlibStream.UncompressBuffer(data);
            }

            CurrentPacketData = data;

            //remove the data already processed
            PacketBuffer.RemoveRange(0, pos);

            //return true if there are any more packets needing to be processed
            return PacketBuffer.Count > 0;
        }
    }
}