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
    public class CelestialCoordinates : IEquatable<CelestialCoordinates>
    {

        [StarSerialize(0)]
        public int X { get; set; }

        [StarSerialize(1)]
        public int Y { get; set; }

        [StarSerialize(2)]        
        public int Z { get; set; }

        [StarSerialize(3)]
        public int System { get; set; }

        [StarSerialize(4)]
        public int Planet { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as CelestialCoordinates;

            if (other == null)
                return false;

            return Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y;
                hashCode = (hashCode * 397) ^ Z;
                hashCode = (hashCode * 397) ^ System;
                hashCode = (hashCode * 397) ^ Planet;
                return hashCode;
            }
        }

        public static bool operator ==(CelestialCoordinates left, CelestialCoordinates right)
        {
            if (ReferenceEquals(null, left))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(CelestialCoordinates left, CelestialCoordinates right)
        {
            return !(left == right);
        }

        public bool Equals(CelestialCoordinates other)
        {
            return X == other.X && Y == other.Y && Z == other.Z && System == other.System && Planet == other.Planet;
        }

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}, Z: {2}, System: {3}, Planet: {4}", X, Y, Z, System, Planet);
        }
    }
}
