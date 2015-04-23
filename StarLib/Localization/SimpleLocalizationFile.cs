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

namespace StarLib.Localization
{
	public class SimpleLocalizationFile : LocalizationFile
	{
		private readonly Dictionary<string, string> _localization;

		public SimpleLocalizationFile(string fileName) : base(fileName)
		{
			_localization = new Dictionary<string, string>();
		}

		public override void Load()
		{
			base.Load();

			foreach (string line in Contents.Split('\n'))
			{
				string line2 = line.Trim();

				if (string.IsNullOrEmpty(line2) || line2.StartsWith("#"))
					continue;
				
				int idx = line2.IndexOf("=", StringComparison.CurrentCulture);

				if (idx == -1)
					throw new FormatException("Invalid format!");

				string key = line2.Substring(0, idx);
				string val = line2.Substring(idx + 1);

				_localization.Add(key, val);
			}
		}

		public override void Set(string key, string value)
		{
			_localization.Add(key, value);
		}

		public override bool Remove(string key)
		{
			return _localization.Remove(key);
		}

		public override string Get(string key)
		{
			string value;

			if (!_localization.TryGetValue(key, out value))
				return null;

			return value;
		}

		public override void Save()
		{
			StringBuilder sb = new StringBuilder();

			foreach (var kvp in _localization)
			{
				sb.AppendFormat("{0}={1}{2}", kvp.Key, kvp.Value, Environment.NewLine);
			}
		}
	}
}
