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

namespace StarLib.DataTypes
{
    /// <summary>
    /// A helper class for VLQs (Variable Length Quantities)
    /// </summary>
    public static class VLQ
    {
        /// <summary>
        /// Convert a VLQ to an array of bytes
        /// </summary>
        /// <param name="value">The VLQ</param>
        /// <returns>The data</returns>
        public static byte[] Create(ulong value)
        {
            var result = new Stack<byte>();

            if (value == 0)
                result.Push(0);

            while (value > 0)
            {
                byte tmp = (byte)(value & 0x7f);

                value >>= 7;

                if (result.Count > 0)
                    tmp |= 0x80;

                result.Push(tmp);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Convert a Signed VLQ to an array of bytes
        /// </summary>
        /// <param name="value">The signed VLQ</param>
        /// <returns>The data</returns>
        public static byte[] CreateSigned(long value)
        {
            long result = Math.Abs(value * 2);

            if (value < 0)
                result -= 1;

            return Create((ulong)result);
        }

		public static ulong FromFunc(Func<int, byte> read, Func<int, bool> condition, out int size)
		{
			int ctr = 0;
			ulong value = 0L;
			while (condition(ctr))
			{
				byte tmp = read(ctr);

				value = (value << 7) | (uint)(tmp & 0x7f);

				if ((tmp & 0x80) == 0)
				{
					size = ctr + 1;
					return value;
				}

				ctr++;
			}

			throw new Exception("Error parsing VLQ");
		}

		public static ulong FromBuffer(byte[] buffer, int offset, int length, out int size)
		{
			//int ctr = offset;
			//ulong value = 0L;
			//while (ctr < length)
			//{
			//	byte tmp = buffer[ctr];

			//	value = (value << 7) | (uint)(tmp & 0x7f);

			//	if ((tmp & 0x80) == 0)
			//	{
			//		size = ctr + 1;
			//		return value;
			//	}

			//	ctr++;
			//}

			//throw new Exception("Error parsing VLQ");
			ulong value = FromFunc(ctr => buffer[ctr + offset], ctr => ctr + offset < length, out size);
			size += offset;

			return value;
		}

		public static long FromBufferSigned(byte[] buffer, int offset, int length, out int size)
		{
			ulong value = FromBuffer(buffer, offset, length, out size);

			if ((value & 1) == 0x00)
				return (long)value >> 1;

			return -((long)(value >> 1) + 1);
		}
    }
}
