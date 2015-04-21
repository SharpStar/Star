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

namespace StarLib.Packets.Serialization
{
    public abstract class SerializableType
    {
        public Type Type { get; set; }

        public Delegate Serializer { get; set; }

        public Delegate Deserializer { get; set; }
    }

    public class SerializableType<TReader, TWriter, TOut> : SerializableType
    {

        public SerializableType(Action<TWriter, TOut> serializer, Func<TReader, TOut> deserializer)
        {
            Type = typeof(TOut);
            Serializer = serializer;
            Deserializer = deserializer;
        }

        public void Serialize(TWriter writer, TOut obj)
        {
            var s = (Action<TWriter, TOut>)Serializer;

            s(writer, obj);
        }

        public TOut Deserialize(TReader reader)
        {
            var d = (Func<TReader, TOut>)Deserializer;

            return d(reader);
        }

    }
}
