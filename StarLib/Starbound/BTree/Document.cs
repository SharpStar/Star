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
using System.Collections.Generic;
using System.IO;
using Mono;
using StarLib.DataTypes;
using StarLib.DataTypes.Variant;
using StarLib.Networking;
using StarLib.Packets.Serialization;

namespace StarLib.Starbound.BTree
{
	public class Document
	{

		public string Name { get; set; }

		public int Version { get; set; }

		public StarVariant Data { get; set; }

		public Document()
		{
		}

		public Document(string name, int version, StarVariant data)
		{
			Name = name;
			Version = version;
			Data = data;
		}

		public static Document ReadDocument(StarReader reader)
		{
			Document doc = new Document();
			doc.Name = reader.ReadString();

			reader.ReadByte();

			doc.Version = DataConverter.BigEndian.GetInt32(reader.ReadBytes(4), 0);
			doc.Data = (StarVariant)PacketSerializer.Deserialize(reader, typeof(StarVariant));

			return doc;

		}

		public static List<Document> ListFromBuffer(byte[] data)
		{
			var documents = new List<Document>();

			using (StarReader reader = new StarReader(data))
			{
				int pos;
				int len = (int)VLQ.FromBuffer(data, 0, data.Length, out pos);

				reader.BaseStream.Seek(pos, SeekOrigin.Begin);

				for (int i = 0; i < len; i++)
					documents.Add(ReadDocument(reader));

			}

			return documents;
		}

	}
}
