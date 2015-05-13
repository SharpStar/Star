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
namespace StarLib.Utils
{
    public static class Adler
    {

        public const int Base = 65521;
        public const int Max = 5552;

        public static uint Adler32(uint adler, byte[] buf, int index, int len)
        {
            if (buf == null)
                return 1;

            uint s1 = (uint)(adler & 0xffff);
            uint s2 = (uint)((adler >> 16) & 0xffff);

            while (len > 0)
            {
                int k = len < Max ? len : Max;
                len -= k;
                while (k >= 16)
                {
                    //s1 += (buf[index++] & 0xff); s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    s1 += buf[index++]; s2 += s1;
                    k -= 16;
                }
                if (k != 0)
                {
                    do
                    {
                        s1 += buf[index++];
                        s2 += s1;
                    }
                    while (--k != 0);
                }
                s1 %= Base;
                s2 %= Base;
            }
            return (uint)((s2 << 16) | s1);
        }
    }
}
