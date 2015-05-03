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
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using StarLib.Extensions;
using StarLib.Logging;
using StarLib.Networking;
using StarLib.Packets.Serialization;

namespace StarLib.Packets
{
    /// <summary>
    /// <see cref="PacketReader"/> is used to read packets from a buffer
    /// </summary>
    public class PacketReader
    {

        private readonly PacketSegmentProcessor _processor;

        private readonly Dictionary<byte, Type> _packets;

        private static readonly Type GenericPacketType = typeof(GenericPacket);

        private static readonly byte[] EmptyBuffer = new byte[0];

        /// <summary>
        /// Creates a packet reader instance with no packet types registered
        /// </summary>
        public PacketReader()
        {
            _processor = new PacketSegmentProcessor();
            _packets = new Dictionary<byte, Type>();
        }

        /// <summary>
        /// Creates a packet reader instance with the specified packet types
        /// </summary>
        /// <param name="packetTypes"></param>
        public PacketReader(Type[] packetTypes)
            : this()
        {
            InitPacketTypes(packetTypes);
        }

        private void InitPacketTypes(Type[] packetTypes)
        {
            foreach (Type type in packetTypes)
            {
                RegisterPacketType(type);
            }
        }

        /// <summary>
        /// Registers a type of packet that can be read from
        /// </summary>
        /// <param name="type">The type of packet</param>
        public void RegisterPacketType(Type type)
        {
            if (type == GenericPacketType)
                throw new ArgumentException("Invalid packet type!", "type");

            if (!typeof(Packet).IsAssignableFrom(type))
                throw new ArgumentException("Type must be a subclass of the Packet class!", "type");

            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new ArgumentException(string.Format("Packet type {0} has no default constructor!", type.FullName), "type");

            Packet tempPacket = (Packet)Activator.CreateInstance(type);

            _packets.Add(tempPacket.PacketId, type);
        }

        /// <summary>
        /// Unregisters a packet type from this reader
        /// </summary>
        /// <param name="id">The id of the packet</param>
        /// <returns>True if remove was successful, false if not</returns>
        public bool UnregisterPacketType(byte id)
        {
            return _packets.Remove(id);
        }

        /// <summary>
        /// Read all packets that is contained within this buffer
        /// </summary>
        /// <param name="buffer">The buffer to read from</param>
        /// <param name="offset">The position to start at</param>
        /// <returns>One or more packets that have been decoded from the buffer</returns>
        public IEnumerable<Packet> Read(byte[] buffer, int offset)
        {
            byte packetId;
            byte[] data;
            _processor.ProcessNextSegment(buffer, offset, out packetId, out data); //first segment

            if (data == null)
                yield break;

            while (true)
            {
                yield return Decode(packetId, data);

                if (!_processor.ProcessNextSegment(EmptyBuffer, 0, out packetId, out data))
                {
                    if (data != null)
                        yield return Decode(packetId, data);

                    yield break;
                }
            }
        }

        /// <summary>
        /// Decodes (i.e. reads) a packet
        /// </summary>
        /// <param name="packetId">The id of the packet</param>
        /// <param name="data">The data that will be fed into the packet to be read</param>
        /// <returns>The decoded packet</returns>
        private Packet Decode(byte packetId, byte[] data)
        {
            Packet packet;

            try
            {
                if (_packets.ContainsKey(packetId))
                {
                    using (StarReader reader = new StarReader(data))
                    {
                        packet = PacketSerializer.Deserialize(reader, _packets[packetId]) as Packet;

                        if (reader.DataLeft != 0)
                        {
                            if (packet != null)
                                StarLog.DefaultLogger.Warn("Packet {0} is incomplete ({1} bytes left)!", packet.GetType().FullName, reader.DataLeft);
                            else
                                StarLog.DefaultLogger.Warn("Packet {0} is incomplete ({1} bytes left)!", packetId, reader.DataLeft);
                        }

                        if (packet == null)
                        {
                            StarLog.DefaultLogger.Warn("Error deserializing packet {0} ({1})", (PacketType)packetId, packetId);

                            packet = new GenericPacket(packetId) { Data = data };
                        }
                    }
                }
                else
                {
                    packet = new GenericPacket(packetId) { Data = data };
                }

                packet.IsReceive = true;
            }
            catch (Exception ex)
            {
                StarLog.DefaultLogger.Error("Packet {0} caused an error!", _packets[packetId].FullName);

                ex.LogError();

                return null;
            }

            return packet;
        }

    }
}
