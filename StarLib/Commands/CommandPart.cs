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
using System.Text.RegularExpressions;

namespace StarLib.Commands
{
	public class CommandPart
	{
		private static readonly Regex ParseRegex = new Regex(@"((\A(?<cmd>[^\s]*){1}))|(\s?((""(?<quote>([^""]*))"")|((?<text>[^""\s]+))))", RegexOptions.Compiled);
		private static readonly Regex FormatRegex = new Regex(@"((\{(?<index>([0-9]{1,}))\})|((?<text>[^""\s]+)))", RegexOptions.Compiled);

		public string CommandName { get; protected set; }

		public string Format { get; private set; }

		private readonly List<CommandTextBase> _sections;

		public CommandPart(string cmdName, string format)
		{
			if (cmdName == null)
				throw new ArgumentNullException("cmdName");

			if (format == null)
				throw new ArgumentNullException("format");

			CommandName = cmdName;
			Format = format;

			_sections = new List<CommandTextBase>();

			Init(format);
		}

		protected void Init(string format)
		{
			MatchCollection mc = FormatRegex.Matches(format);

			foreach (Match match in mc.Cast<Match>().OrderBy(p => p.Index))
			{
				Group idxGrp = match.Groups["index"];
				Group txtGrp = match.Groups["text"];

				if (idxGrp.Success)
				{
					int idx;

					if (!int.TryParse(idxGrp.Value, out idx))
						throw new FormatException("Expected integer!");

					_sections.Add(new CommandPlaceHolder { Index = idx });
				}
				else if (txtGrp.Success)
				{
					_sections.Add(new CommandStaticText { Text = txtGrp.Value });
				}
			}
		}

		public virtual CommandError TryParse(string text, out string[] result)
		{
			if (text == null)
				throw new ArgumentNullException("text");

			result = null;

			string[] ex = text.Split(' ');

			if (ex.Length < 1)
				return CommandError.LengthMismatch;

			if (!ex[0].Equals(CommandName, StringComparison.CurrentCultureIgnoreCase))
				return CommandError.NameMismatch;

			var dict = new Dictionary<int, string>();

			MatchCollection mc = ParseRegex.Matches(text);

			int ctr = 0;
			foreach (Match match in mc.Cast<Match>().OrderBy(p => p.Index))
			{
				Group quoteGrp = match.Groups["quote"];
				Group txtGrp = match.Groups["text"];

				string val = null;

				if (quoteGrp.Success)
					val = quoteGrp.Value;
				else if (txtGrp.Success)
					val = txtGrp.Value;

				if (val != null)
				{
					if (ctr >= _sections.Count)
						return CommandError.LengthMismatch;

					if (_sections[ctr] is CommandPlaceHolder)
					{
						CommandPlaceHolder ph = (CommandPlaceHolder)_sections[ctr];

						dict.Add(ph.Index, val);
					}
					else if (_sections[ctr] is CommandStaticText)
					{
						CommandStaticText st = (CommandStaticText)_sections[ctr];

						if (!val.Equals(st.Text, StringComparison.OrdinalIgnoreCase))
							return CommandError.StaticTextMismatch;
					}

					ctr++;
				}
			}
			
			if (_sections.Count >= ex.Length)
				return CommandError.LengthMismatch;

			result = dict.OrderBy(p => p.Key).Select(p => p.Value).ToArray();

			return CommandError.Success;
		}

	}
}
