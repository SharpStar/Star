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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StarLib.Commands.Console;
using StarLib.Commands.PlayerEvent;
using StarLib.Configuration;
using StarLib.Database;
using StarLib.Localization;
using StarLib.Logging;
using StarLib.Packets;
using StarLib.Packets.Serialization;
using StarLib.Packets.Starbound;
using StarLib.Plugins;
using StarLib.Plugins.CSharp;
using StarLib.Server;
using StarLib.Starbound;

namespace StarLib
{
    /// <summary>
    /// <see cref="StarMain"/> is the starting point for this library. All initializations are done through here<para/>
    /// Note: This class cannot be inherited and there will only ever be one instance of this class
    /// </summary>
    public sealed class StarMain
    {
        #region Singleton

        private static readonly Lazy<StarMain> _instance = new Lazy<StarMain>(() => new StarMain());

        /// <summary>
        /// The only instance of <see cref="StarMain"/>
        /// </summary>
        public static StarMain Instance
        {
            get
            {
                return _instance.Value;
            }
        }
        #endregion

        private LocalizationFile _localizationFile;

        #region Public Members
        public const string ServerConfigFile = "serverconfig.json";

        public StarProxyManager ConnectionManager { get; private set; }

        public ServerConfiguration ServerConfig { get; private set; }

        public bool Initialized { get; private set; }

        public StarServer Server { get; private set; }

        public List<Type> DefaultPacketTypes { get; private set; }

        public List<IConfiguration> Configurations { get; private set; }

        public List<IPluginManager> PluginManagers { get; private set; }

        public ConsoleCommandManager ConsoleCommandManager { get; private set; }

        public LocalizationFile CurrentLocalization
        {
            get
            {
                return _localizationFile;
            }
            set
            {
                _localizationFile = value;
                _localizationFile.Load();
            }
        }

        public StarDb Database { get; private set; }

        #endregion

        #region Private Members
        private readonly StarLog _log = StarLog.DefaultLogger;

        private readonly JsonSerializerSettings _jsonSettings;
        #endregion

        #region Events
        public event EventHandler ShutdownInitiated;
        #endregion

        private StarMain()
        {
            PluginManagers = new List<IPluginManager>();
            DefaultPacketTypes = new List<Type>();
            ConsoleCommandManager = new ConsoleCommandManager();
            ConnectionManager = new StarProxyManager();
            Configurations = new List<IConfiguration>();

            _jsonSettings = new JsonSerializerSettings();
            ReadStarConfigs();

            Initialized = false;
        }

        /// <summary>
        /// Initialize <see cref="StarMain"/> for use
        /// </summary>
        public void Init()
        {
            if (Initialized)
                throw new Exception("Star has already been initialized!");

            Initialized = true;
            InitPackets();

            Server = new StarServer(ServerConfig, ConnectionManager, DefaultPacketTypes.ToArray());
        }

        public void Start()
        {
            InitPlugins();

            _log.Info("Loading database...");
            Database = new StarDb();

            _log.Info("Loading plugins...");
            LoadPlugins();

            _log.Info("Starting server...");

            Server.StartServer();

            _log.Info("Server is now online!");
        }

        public void Shutdown()
        {
            EventHandler shutdown = ShutdownInitiated;
            if (shutdown != null)
                shutdown(this, EventArgs.Empty);

            SaveAllConfigurations();

            Server.StopServer();
            Database.Dispose();
        }

        private void InitPackets()
        {
            var tasks = new List<Task>();
            foreach (Type type in typeof(StarMain).Assembly.GetTypes().Where(p => p.Namespace == "StarLib.Packets.Starbound"))
            {
                if (!typeof(Packet).IsAssignableFrom(type))
                    continue;

                _log.Debug("Adding default packet type {0}", type.FullName);
                DefaultPacketTypes.Add(type);

                _log.Debug("Building and caching packet serializer/deserializer for type {0}", type.FullName);

                tasks.Add(Task.Run(() => PacketSerializer.BuildAndStore(type)));
            }

            Task.WhenAll(tasks).Wait();
        }

        private void InitPlugins()
        {
            CSPluginManager csManager = new CSPluginManager();
            csManager.Init("plugins");

            PluginManagers.Add(csManager);
        }

        private void LoadPlugins()
        {
            foreach (IPluginManager ipm in PluginManagers)
            {
                ipm.LoadPlugins("plugins");
            }
        }

        public bool PassPlayerEventCommand(string command, Player player)
        {
            bool fPluginCmd = false;

            foreach (IPluginManager manager in PluginManagers)
            {
                if (manager.PassCommand(command, player))
                    fPluginCmd = true;
            }

            return fPluginCmd;
        }

        private void ReadStarConfigs()
        {
            var serverConfig = new JsonFileConfiguration<ServerConfiguration>(ServerConfigFile, _jsonSettings);
            serverConfig.Load();

            ServerConfig = serverConfig.Config;

            Configurations.Add(serverConfig);
        }

        /// <summary>
        /// Save all of <see cref="StarMain"/>'s configurations to disk
        /// </summary>
        public void SaveAllConfigurations()
        {
            foreach (IConfiguration config in Configurations)
            {
                config.Save();
            }
        }

        /// <summary>
        /// Reload all of <see cref="StarMain"/>'s configurations
        /// </summary>
        public void ReloadAllConfigurations()
        {
            foreach (IConfiguration config in Configurations)
            {
                config.Load();
            }
        }

    }
}
