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
using System.Linq;

namespace StarLib.DataTypes.Variant
{
    /// <summary>
    /// A class representing a Variant's value
    /// </summary>
    public class VariantValue : IEquatable<VariantValue>
    {
        /// <summary>
        /// The value of the Variant
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Constructs a <see cref="VariantValue"/> object with the specified value
        /// </summary>
        /// <param name="value">The value</param>
        public VariantValue(object value)
        {
            Value = value;
        }

        public static implicit operator string(VariantValue value)
        {
            return value.Value as string;
        }

        public static implicit operator double(VariantValue value)
        {
            return Convert.ToDouble(value.Value);
        }

        public static implicit operator bool(VariantValue value)
        {
            return Convert.ToBoolean(value.Value);
        }

        public static implicit operator ulong(VariantValue value)
        {
            return Convert.ToUInt64(value.Value);
        }

        public static implicit operator uint(VariantValue value)
        {
            return Convert.ToUInt32(value.Value);
        }

        public static implicit operator ushort(VariantValue value)
        {
            return Convert.ToUInt16(value.Value);
        }

        public static implicit operator byte(VariantValue value)
        {
            return Convert.ToByte(value.Value);
        }

        public static implicit operator StarVariant[](VariantValue value)
        {
            return value.Value as StarVariant[];
        }

        public static implicit operator VariantDictionary(VariantValue value)
        {
            return value.Value as VariantDictionary;
        }

        public bool Equals(VariantValue other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (Value is StarVariant[] && other.Value is StarVariant[])
            {
                StarVariant[] thisVar = (StarVariant[])Value;
                StarVariant[] otherVar = (StarVariant[])Value;

                return thisVar.SequenceEqual(otherVar);
            }
            else if (Value is VariantDictionary && other.Value is VariantDictionary)
            {
                VariantDictionary thisDict = (VariantDictionary)Value;
                VariantDictionary otherDict = (VariantDictionary)Value;

                return thisDict.Count == otherDict.Count && !thisDict.Except(otherDict).Any();
            }

            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VariantValue)obj);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }

        public static bool operator ==(VariantValue left, VariantValue right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(VariantValue left, VariantValue right)
        {
            return !Equals(left, right);
        }
    }
}
