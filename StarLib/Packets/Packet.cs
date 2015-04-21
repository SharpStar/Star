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
using StarLib.Server;

namespace StarLib.Packets
{
    /// <summary>
    /// Represents a Starbound Packet
    /// </summary>
    public abstract class Packet
    {
        /// <summary>
        /// The packet's identifier
        /// </summary>
        public abstract byte PacketId { get; protected set; }

        public PacketType PacketType
        {
            get
            {
                return (PacketType)PacketId;
            }
        }

        /// <summary>
        /// Specifies whether this packet should be sent or not
        /// </summary>
        public bool Ignore { get; set; }

        public bool IsReceive { get; set; }

		public Direction Direction { get; set; }

        ///// <summary>
        ///// Invoked when this packet should be read
        ///// </summary>
        ///// <param name="reader">The stream to read from</param>
        //public abstract void Read(StarReader reader);

        ///// <summary>
        ///// Invoked when this packet should be written
        ///// </summary>
        ///// <param name="writer">The stream to write to</param>
        //public abstract void Write(StarWriter writer);
    }
}
