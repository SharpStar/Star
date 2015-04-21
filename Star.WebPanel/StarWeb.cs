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
using Microsoft.Owin.Hosting;
using Mono.Addins;
using Newtonsoft.Json;
using Star.WebPanel.Config;
using Star.WebPanel.Star.PacketEvents;
using StarLib.Configuration;
using StarLib.Plugins.CSharp;

[assembly: Addin("StarWeb", Version = "1.0.0.0")]
[assembly: AddinDescription("A web panel for SharpStar")]
[assembly: AddinProperty("star", "1.0")]
[assembly: AddinDependency("StarLib", "1.0")]

[assembly: ImportAddinAssembly("System.Web.Razor.dll")]
[assembly: ImportAddinAssembly("Nancy.ViewEngines.Razor.dll")]

namespace Star.WebPanel
{
	[Extension]
	public class StarWeb : CSPlugin
	{

		public override string Name
		{
			get { return "StarWeb"; }
		}


		public static WebPanelConfig WebConfig { get; private set; }


		private static readonly List<IConfiguration> Configs = new List<IConfiguration>();

		private static IDisposable _webApp;

		public override void Load()
		{
			Init();
		}

		public override void Unload()
		{
			Shutdown();
		}

		public void Init()
		{
			LoadConfigs();
			RegisterEvents();

			if (_webApp != null)
			{
				_webApp.Dispose();
			}

			_webApp = WebApp.Start<Startup>("http://+:1337");
		}

		private void RegisterEvents()
		{
			PacketEventManager.AddEventObject(new PlayerChatEvent());
		}

		private static void LoadConfigs()
		{
			Configs.Clear();

			var webConfig = new JsonFileConfiguration<WebPanelConfig>("webpanel.json", new JsonSerializerSettings());
			webConfig.Load();

			Configs.Add(webConfig);

			WebConfig = webConfig.Config;
		}

		public static void Shutdown()
		{
			SaveConfigs();

			if (_webApp != null)
			{
				_webApp.Dispose();
				_webApp = null;
			}
		}

		private static void SaveConfigs()
		{
			foreach (IConfiguration config in Configs)
			{
				config.Save();
			}
		}
	}
}
