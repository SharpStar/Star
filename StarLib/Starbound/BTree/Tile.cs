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
using System.IO;
using Mono;
using StarLib.Mono;

namespace StarLib.Starbound.BTree
{
    public class Tile
    {

        public short ForegroundMaterial { get; set; }

        public byte ForegroundHueShift { get; set; }

        public byte ForegroundVariant { get; set; }

        public short ForegroundSprite { get; set; }

        public byte ForegroundSpriteHueShift { get; set; }

        public short BackgroundMaterial { get; set; }

        public byte BackgroundHueShift { get; set; }

        public byte BackgroundVariant { get; set; }

        public short BackgroundSprite { get; set; }

        public byte BackgroundSpriteHueShift { get; set; }

        public byte Liquid { get; set; }

        public ushort LiquidPressure { get; set; }

        public byte Collision { get; set; }

        public short Dungeon { get; set; }

        public byte Biome { get; set; }

        public byte Biome2 { get; set; }

        public bool Indestructible { get; set; }

        public static Tile FromStream(Stream stream)
        {
            byte[] buf = new byte[23];

            stream.Read(buf, 0, buf.Length);

            var unpacked = DataConverter.Unpack("^sbbsbsbbsbbSbsbbb", buf, 0);

            Tile tile = new Tile();
            tile.ForegroundMaterial = (short)unpacked[0];
            tile.ForegroundHueShift = (byte)unpacked[1];
            tile.ForegroundVariant = (byte)unpacked[2];
            tile.ForegroundSprite = (short)unpacked[3];
            tile.ForegroundSpriteHueShift = (byte)unpacked[4];
            tile.BackgroundMaterial = (short)unpacked[5];
            tile.BackgroundHueShift = (byte)unpacked[6];
            tile.BackgroundVariant = (byte)unpacked[7];
            tile.BackgroundSprite = (short)unpacked[8];
            tile.BackgroundSpriteHueShift = (byte)unpacked[9];
            tile.Liquid = (byte)unpacked[10];
            tile.LiquidPressure = (ushort)unpacked[11];
            tile.Collision = (byte)unpacked[12];
            tile.Dungeon = (short)unpacked[13];
            tile.Biome = (byte)unpacked[14];
            tile.Biome2 = (byte)unpacked[15];
            tile.Indestructible = Convert.ToBoolean(unpacked[16]);

            return tile;
        }

    }
}
