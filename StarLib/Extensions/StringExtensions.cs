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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StarLib.Extensions
{
	public static class StringExtensions
	{
		private static readonly Regex StripColorRegex = new Regex("\\^[#a-zA-Z0-9]+;", RegexOptions.Compiled);

		public static IEnumerable<string> SeparateString(this string str, int max)
		{
			if (str.Length > max)
			{
				int ctr = 0;

				string line = str;
				while (ctr < str.Length)
				{
					int min = Math.Min(line.Length, max);

					string nextLine = line.Substring(0, min);

					line = line.Substring(min);

					yield return nextLine;

					ctr += min;
				}
			}
			else
			{
				yield return str;
			}
		}

		public static string StripColors(this string text)
		{
			return StripColorRegex.Replace(text, string.Empty);
		}
	}
}
