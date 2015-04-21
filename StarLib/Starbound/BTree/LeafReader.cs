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
using System.Linq;

namespace StarLib.Starbound.BTree
{
	public class LeafReader
	{
		public SBBF03 File { get; protected set; }

		public BTreeLeaf Leaf { get; protected set; }

		public int Offset { get; protected set; }

		public List<int> Visited { get; protected set; }

		public LeafReader(SBBF03 sb, BTreeLeaf leaf)
		{
			File = sb;
			Leaf = leaf;
			Offset = 0;
			Visited = new List<int>(new[] { leaf.Index });
		}

		public virtual byte[] Read(int length)
		{

			int offset = Offset;

			if (offset + length <= Leaf.Data.Length)
			{
				Offset += length;

				return Leaf.Data.Skip(offset).Take(length).ToArray();
			}

			byte[] data;

			using (MemoryStream ms = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(ms))
				{
					byte[] buf = Leaf.Data.Skip(offset).ToArray();

					writer.Write(buf, 0, buf.Length);

					int numRead = buf.Length;

					length -= numRead;

					while (length > 0)
					{
						if (!Leaf.NextBlock.HasValue)
							break;

						int nextBlock = Leaf.NextBlock.Value;

						Visited.Add(nextBlock);

						Leaf = (BTreeLeaf)File.GetBlock(nextBlock);

						byte[] buf2 = Leaf.Data.Take(length).ToArray();

						writer.Write(buf2);

						numRead = buf2.Length;
						length -= numRead;
					}

					Offset = numRead;

					data = ms.ToArray();
				}

			}

			return data;

		}
	}
}
