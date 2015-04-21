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
using Newtonsoft.Json;
using StarLib.Security;

namespace StarLib.Configuration
{
    [JsonObject]
    public class ServerConfiguration : StarConfiguration
    {
		public const string DefaultPluginRepoUrl = "http://sharpstar.org/plugins";


        [JsonProperty]
        public int BindPort { get; set; }

        [JsonProperty]
        public int ServerBindPort { get; set; }

        [JsonProperty]
        public string BindAddress { get; set; }

        [JsonProperty]
        public string ServerBindAddress { get; set; }

        [JsonProperty]
        public int MaxConnections { get; set; }

        [JsonProperty]
        public int HeartbeatTimeout { get; set; }

        [JsonProperty]
        public string PluginsRepoUrl { get; set; }

		[JsonProperty]
		public bool EnableGuestLogin { get; set; }

		[JsonProperty]
		public bool AutoUpdatePlugins { get; set; }

		[JsonProperty]
		public bool EnableDebugLog { get; set; }

		[JsonProperty]
		public bool RunAsService { get; set; }

		public override void SetDefaults()
        {
            BindPort = 21025;
            ServerBindPort = 21024;
            BindAddress = "0.0.0.0";
            ServerBindAddress = "127.0.0.1";
            MaxConnections = 100;
            HeartbeatTimeout = 120; //in seconds
            PluginsRepoUrl = DefaultPluginRepoUrl;
            AutoUpdatePlugins = true;
			RunAsService = false;
        }
    }
}
