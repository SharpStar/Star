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

namespace StarLib.Starbound
{
    public class CelestialInfo : IEquatable<CelestialInfo>
    {
        [StarSerialize(0)]
        public int PlanetOrbitalLevels { get; set; }

        [StarSerialize(1)]
        public int SatelliteOrbitalLevels { get; set; }

        [StarSerialize(2)]
        public int ChunkSize { get; set; }

        [StarSerialize(3)]
        public int XyCoordinateMin { get; set; }

        [StarSerialize(4)]
        public int XyCoordinateMax { get; set; }

        [StarSerialize(5)]
        public int ZCoordinateMin { get; set; }

        [StarSerialize(6)]
        public int ZCoordinateMax { get; set; }


        public bool Equals(CelestialInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return PlanetOrbitalLevels == other.PlanetOrbitalLevels && SatelliteOrbitalLevels == other.SatelliteOrbitalLevels
                && ChunkSize == other.ChunkSize && XyCoordinateMax == other.XyCoordinateMax && XyCoordinateMin == other.XyCoordinateMin
                && ZCoordinateMin == other.ZCoordinateMin && ZCoordinateMax == other.ZCoordinateMax;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CelestialInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PlanetOrbitalLevels;
                hashCode = (hashCode * 397) ^ SatelliteOrbitalLevels;
                hashCode = (hashCode * 397) ^ ChunkSize;
                hashCode = (hashCode * 397) ^ XyCoordinateMax;
                hashCode = (hashCode * 397) ^ XyCoordinateMin;
                hashCode = (hashCode * 397) ^ ZCoordinateMin;
                hashCode = (hashCode * 397) ^ ZCoordinateMax;
                return hashCode;
            }
        }

        public static bool operator ==(CelestialInfo left, CelestialInfo right)
        {
            if (ReferenceEquals(null, left))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(CelestialInfo left, CelestialInfo right)
        {
            return !(left == right);
        }
    }
}
