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
using StarLib.Packets.Serialization.Attributes;

namespace StarLib.Starbound.Tiles
{
    /// <summary>
    /// A class representing a 'Net Tile' - Unfinished
    /// </summary>
    public class NetTile 
    {
        [StarSerialize(0)]
        public ushort background { get; set; }

        [StarSerialize(1)]
        public byte backgroundHueShift { get; set; }

        [StarSerialize(2)]
        public byte backgroundColorVariant { get; set; }

        [StarSerialize(3)]
        public ushort backgroundMod { get; set; }

        [StarSerialize(4)]
        public byte backgroundModHueShift { get; set; }

        [StarSerialize(5)]
        public ushort foreground { get; set; }

        [StarSerialize(6)]
        public byte foregroundHueShift { get; set; }

        [StarSerialize(7)]
        public byte foregroundColorVariant { get; set; }

        [StarSerialize(8)]
        public ushort foregroundMod { get; set; }

        [StarSerialize(9)]
        public byte foregroundModHueShift { get; set; }

        [StarSerialize(10)]
        public byte collision { get; set; }

        [StarSerialize(11)]
        public byte blockBiomeIndex { get; set; }

        [StarSerialize(12)]
        public byte environmentBiomeIndex { get; set; }

        [StarSerialize(13)]
        public byte LiquidLevel { get; set; }

        [StarSerialize(14)]
        public float Gravity { get; set; }

        [StarSerialize(15)]
        public ushort DungeonID { get; set; } //uint16
    }
}
