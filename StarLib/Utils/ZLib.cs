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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarLib.Utils
{
    public class ZLib
    {
        private static readonly byte[] ZHeader = { 0x78, 0x9C };

        public static byte[] Compress(byte[] buffer)
        {
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    outStream.Write(ZHeader, 0, ZHeader.Length);

                    using (DeflateStream ds = new DeflateStream(outStream, CompressionLevel.Fastest, true))
                    {
                        ms.CopyTo(ds);
                    }

                    byte[] checksum = GetChecksum(buffer);

                    outStream.Write(checksum, 0, checksum.Length);

                    return outStream.ToArray();
                }
            }
        }

        public static async Task<byte[]> CompressAsync(byte[] buffer)
        {
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    outStream.Write(ZHeader, 0, ZHeader.Length);

                    using (DeflateStream ds = new DeflateStream(outStream, CompressionLevel.Fastest, true))
                    {
                        await ms.CopyToAsync(ds);
                    }

                    byte[] checksum = GetChecksum(buffer);

                    outStream.Write(checksum, 0, checksum.Length);

                    return outStream.ToArray();
                }
            }
        }

        public static byte[] Decompress(byte[] buffer)
        {
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                ms.SetLength(ms.Length - 4); //trim off the checksum
                ms.Seek(2, SeekOrigin.Begin); //skip zlib header

                using (MemoryStream outStream = new MemoryStream())
                {
                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress, true))
                    {
                        ds.CopyTo(outStream);
                    }

                    return outStream.ToArray();
                }
            }
        }

        public static async Task<byte[]> DecompressAsync(byte[] buffer)
        {
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                ms.SetLength(ms.Length - 4); //trim off the checksum
                ms.Seek(2, SeekOrigin.Begin); //skip zlib header

                using (MemoryStream outStream = new MemoryStream())
                {
                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress, true))
                    {
                        await ds.CopyToAsync(outStream);
                    }

                    return outStream.ToArray();
                }
            }
        }

        private static byte[] GetChecksum(byte[] buffer)
        {
            uint adler32 = Adler.Adler32(1, buffer, 0, buffer.Length);

            byte[] checksum = new byte[4];
            checksum[0] = (byte)((adler32 & 0xFF000000) >> 24);
            checksum[1] = (byte)((adler32 & 0x00FF0000) >> 16);
            checksum[2] = (byte)((adler32 & 0x0000FF00) >> 8);
            checksum[3] = (byte)(adler32 & 0x000000FF);

            return checksum;
        }
    }
}
