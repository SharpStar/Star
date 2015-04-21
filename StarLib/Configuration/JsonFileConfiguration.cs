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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StarLib.Extensions;

namespace StarLib.Configuration
{
	public class JsonFileConfiguration<T> : IConfiguration where T : StarConfiguration, new()
	{

		private readonly JsonSerializerSettings _jsonSettings;

		public string FileName { get; set; }

		public T Config { get; set; }

		public JsonFileConfiguration(string file, JsonSerializerSettings settings)
		{
			FileName = file;
			_jsonSettings = settings;
		}

		public void Load()
		{
			if (!File.Exists(FileName))
			{
				Config = new T();
				Config.SetDefaults();

				Save();
			}
			else
			{
				Config = JsonConvert.DeserializeObject<T>(File.ReadAllText(FileName), _jsonSettings);
			}
		}

		public void Save()
		{
			if (Config == null)
				throw new NullReferenceException("Config cannot be null!");

			try
			{
				using (FileStream fs = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
				using (StreamWriter writer = new StreamWriter(fs))
					writer.Write(JsonConvert.SerializeObject(Config, Formatting.Indented, _jsonSettings));
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}
	}
}
