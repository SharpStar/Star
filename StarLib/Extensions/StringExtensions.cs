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
        private static readonly Regex ParseTimeRegex = new Regex("(?<value>[0-9]+)(y|mo|d|h|m|s)", RegexOptions.Compiled);

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

        public static TimeSpan ToTimeSpan(this string time)
        {
            MatchCollection matches = ParseTimeRegex.Matches(time);
            TimeSpan ts = TimeSpan.Zero;

            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                int value;

                if (!int.TryParse(match.Groups["value"].Value, out value))
                    continue;

                switch (match.Groups[1].Value)
                {
                    case "y":
                        ts = ts.Add(TimeSpan.FromDays(value * 365.2425));
                        break;
                    case "mo":
                        ts = ts.Add(TimeSpan.FromDays(value * 30.436875));
                        break;
                    case "d":
                        ts = ts.Add(TimeSpan.FromDays(value));
                        break;
                    case "h":
                        ts = ts.Add(TimeSpan.FromHours(value));
                        break;
                    case "m":
                        ts = ts.Add(TimeSpan.FromMinutes(value));
                        break;
                    case "s":
                        ts = ts.Add(TimeSpan.FromSeconds(value));
                        break;
                }
            }

            return ts;
        }

        public static DateTime ToDateTime(this string time)
        {
            MatchCollection matches = ParseTimeRegex.Matches(time);

            DateTime now = DateTime.Now;

            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                int value;

                if (!int.TryParse(match.Groups["value"].Value, out value))
                    continue;

                switch (match.Groups[1].Value)
                {
                    case "y":
                        now = now.AddYears(value);
                        break;
                    case "mo":
                        now = now.AddMonths(value);
                        break;
                    case "d":
                        now = now.AddDays(value);
                        break;
                    case "h":
                        now = now.AddHours(value);
                        break;
                    case "m":
                        now = now.AddMinutes(value);
                        break;
                    case "s":
                        now = now.AddSeconds(value);
                        break;
                }
            }

            return now;
        }
    }
}
