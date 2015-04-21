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

namespace SharpStar.Lib.Starbound
{
    public class Key : IEquatable<Key>
    {

        public byte[] TheKey { get; set; }

        public Key(byte[] key)
        {
            TheKey = key;
        }

        public override int GetHashCode()
        {

            int hashcode = 0;

            foreach (byte b in TheKey)
            {
                hashcode += b.GetHashCode();
            }

            return hashcode;

        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Key);
        }

        public bool Equals(Key other)
        {
            return !ReferenceEquals(other, null) && TheKey.SequenceEqual(other.TheKey);
        }

        public static implicit operator Key(byte[] signature)
        {
            return new Key(signature);
        }

        public static implicit operator byte[](Key key)
        {
            return key.TheKey;
        }

        public static bool operator ==(Key key1, Key key2)
        {

            if (ReferenceEquals(key1, null) || ReferenceEquals(key2, null))
                return false;

            return key1.Equals(key2);
        }

        public static bool operator !=(Key key1, Key key2)
        {
            return !(key1 == key2);
        }

    }
}
