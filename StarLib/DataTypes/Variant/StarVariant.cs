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
using StarLib.Networking;

namespace StarLib.DataTypes.Variant
{
    /// <summary>
    /// A Starbound data type that contains serveral different types of values
    /// </summary>
    public class StarVariant : IVariant, IEquatable<StarVariant>
    {

        public VariantValue Value { get; private set; }

        private StarVariant()
        {
        }

        /// <summary>
        /// Create a Variant from the specified value<para/>
        /// Accepted Values: <see cref="string"/>, <see cref="double"/>, <see cref="bool"/>, <see cref="ulong"/>, <see cref="uint"/>
        /// <see cref="ushort"/>, <see cref="byte"/>, <see cref="StarVariant"/>[], <see cref="VariantDictionary"/>
        /// </summary>
        /// <param name="value">The value this Variant will represent</param>
        public StarVariant(VariantValue value)
        {
            Value = value;
            //if (!(value == null ||
            //      value is string ||
            //      value is double ||
            //      value is bool ||
            //      value is ulong ||
            //      value is uint ||
            //      value is ushort ||
            //      value is byte ||
            //      value is StarVariant[] ||
            //      value is VariantDictionary))
            //{
            //    throw new InvalidCastException(string.Format("Variants are unable to represent {0}.", value.GetType()));
            //}

            //Value = new VariantValue(value);
        }

        /// <summary>
        /// Creates a <see cref="StarVariant"/> from the specified stream
        /// </summary>
        /// <param name="reader">The stream to read from</param>
        /// <returns>A <see cref="StarVariant"/> object</returns>
        public static StarVariant ReadFrom(StarReader reader)
        {
            var variant = new StarVariant();

            Stack<IVariant> vars = new Stack<IVariant>();
            vars.Push(variant);

            while (vars.Count > 0)
            {
                IVariant var = vars.Pop();
                StarVariant newVar = new StarVariant();

                if (var is VariantDictionary)
                {
                    VariantDictionary varDict = (VariantDictionary)var;

                    string key = reader.ReadString();

                    varDict.Add(key, newVar);
                }

                byte type = reader.ReadByte();

                switch (type)
                {
                    case 1:
                        newVar.Value = new VariantValue(null);
                        break;
                    case 2:
                        newVar.Value = new VariantValue(reader.ReadDouble());
                        break;
                    case 3:
                        newVar.Value = new VariantValue(reader.ReadBoolean());
                        break;
                    case 4:
                        newVar.Value = new VariantValue(reader.ReadVLQ());
                        break;
                    case 5:
                        newVar.Value = new VariantValue(reader.ReadString());
                        break;
                    case 6:
                        var array = new StarVariant[reader.ReadVLQ()];

                        for (int i = 0; i < array.Length; i++)
                        {
                            array[(array.Length - 1) - i] = new StarVariant();
                            vars.Push(array[(array.Length - 1) - i]);
                        }

                        newVar.Value = new VariantValue(array);

                        break;
                    case 7:
                        var dict = new VariantDictionary();
                        ulong length = reader.ReadVLQ();

                        for (ulong i = 0; i < length; i++)
                        {
                            vars.Push(dict);
                        }

                        newVar.Value = new VariantValue(dict);

                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Unknown Variant type: 0x{0:X2}", type));
                }

                if (var is StarVariant)
                {
                    StarVariant var2 = (StarVariant)var;
                    var2.Value = newVar.Value;
                }

            }

            return variant;
        }

        /// <summary>
        /// Writes this <see cref="StarVariant"/> to the stream
        /// </summary>
        /// <param name="writer">The stream to write to</param>
        public void WriteTo(StarWriter writer)
        {
            Stack<IVariant> vars = new Stack<IVariant>();
            vars.Push(this);

            while (vars.Count > 0)
            {
                IVariant var = vars.Pop();

                object val = null;

                if (var is StarVariant)
                {
                    StarVariant variant = (StarVariant)var;

                    val = variant.Value.Value;
                }

                if (var is VariantPair)
                {
                    VariantPair vp = (VariantPair)var;

                    writer.WriteStarString(vp.Key);
                    vars.Push(vp.Value);
                }
                else if (val == null)
                {
                    writer.Write((byte)1);
                }
                else if (val is double)
                {
                    writer.Write((byte)2);
                    writer.Write((double)val);
                }
                else if (val is bool)
                {
                    writer.Write((byte)3);
                    writer.Write((bool)val);
                }
                else if (val is ulong)
                {
                    writer.Write((byte)4);
                    writer.WriteVlq((ulong)val);
                }
                else if (val is string)
                {
                    writer.Write((byte)5);
                    writer.WriteStarString((string)val);
                }
                else if (val is StarVariant[])
                {
                    writer.Write((byte)6);
                    var array = (StarVariant[])val;
                    writer.WriteVlq((ulong)array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        vars.Push(array[(array.Length - 1) - i]);
                    }
                }
                else if (val is VariantDictionary)
                {
                    writer.Write((byte)7);
                    var dict = (VariantDictionary)val;
                    writer.WriteVlq((ulong)dict.Count);
                    foreach (var kvp in dict)
                    {
                        vars.Push(new VariantPair { Key = kvp.Key, Value = kvp.Value });
                    }
                }
                else
                {
                    throw new InvalidCastException(string.Format("Variants are unable to represent {0}.", val.GetType()));
                }
            }
        }

        public bool Equals(StarVariant other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StarVariant)obj);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }

        public static bool operator ==(StarVariant left, StarVariant right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StarVariant left, StarVariant right)
        {
            return !Equals(left, right);
        }

    }
}
