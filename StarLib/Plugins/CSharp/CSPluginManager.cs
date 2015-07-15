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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Addins;
using Mono.Addins.Setup;
using StarLib.Commands.PlayerEvent;
using StarLib.Events.Packets;
using StarLib.Extensions;
using StarLib.Logging;
using StarLib.Packets;
using StarLib.Packets.Starbound;
using StarLib.Server;
using StarLib.Starbound;

[assembly: AddinRoot("StarLib", "1.0")]
namespace StarLib.Plugins.CSharp
{
	public class CSPluginManager : PluginManager
	{

		protected SetupService SetupService;
		protected readonly StarLog Logger = StarLog.DefaultLogger;

		private readonly ConcurrentDictionary<string, CSPlugin> _plugins;

		private readonly CSPluginErrorStatus _errorStatus = new CSPluginErrorStatus();

		private static readonly Version StarVersion = Assembly.GetEntryAssembly().GetName().Version;

		public List<CSPlugin> Plugins
		{
			get
			{
				return _plugins.Values.ToList();
			}
		}

		public CSPluginManager()
		{
			_plugins = new ConcurrentDictionary<string, CSPlugin>();
		}

		public void Init(string dir)
		{
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			if (!AddinManager.IsInitialized)
			{
				AddinManager.Initialize(".", dir);
				AddinManager.Registry.Update(_errorStatus);

				AddinManager.AddExtensionNodeHandler(typeof(CSPlugin), OnExtensionChanged);
				AddinManager.AddinLoadError += AddinManager_AddinLoadError;
			}

			SetupService = new SetupService(AddinManager.Registry);

			try
			{
				if (!SetupService.Repositories.ContainsRepository(StarMain.Instance.ServerConfig.PluginsRepoUrl))
					SetupService.Repositories.RegisterRepository(_errorStatus, StarMain.Instance.ServerConfig.PluginsRepoUrl, false);

				SetupService.Repositories.UpdateAllRepositories(_errorStatus);
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		public override void LoadPlugins(string dir)
		{
			if (StarMain.Instance.ServerConfig.AutoUpdatePlugins)
			{
				UpdatePlugins();
			}

			RegisterEvents();
			EnablePlugins();
		}

		protected virtual void AddinManager_AddinLoadError(object sender, AddinErrorEventArgs args)
		{
			Logger.Error(args.Exception.ToString());
		}

		protected virtual void RegisterEvents()
		{
			StarServer server = StarMain.Instance.Server;
			StarProxyManager manager = server.Proxies;

			manager.ConnectionAdded += Manager_ConnectionAdded;
		}

		private void Manager_ConnectionAdded(object sender, ProxyConnectionEventArgs e)
		{
			e.Proxy.ClientConnection.PacketReceived += PacketReceived;
			e.Proxy.ServerConnection.PacketReceived += PacketReceived;

			e.Proxy.ClientConnection.AfterPacketReceived += AfterPacketReceived;
			e.Proxy.ServerConnection.AfterPacketReceived += AfterPacketReceived;
		}

		private void AfterPacketReceived(object sender, PacketEventArgs e)
		{
		    PassPacketEvent(e.Packet.PacketId, new PacketEvent(e.Packet, e.Proxy), PacketEventType.AfterSent);
		}

		private void PacketReceived(object sender, PacketEventArgs e)
		{
			PassPacketEvent(e.Packet.PacketId, new PacketEvent(e.Packet, e.Proxy), PacketEventType.BeforeSent);
		}

		protected virtual void EnablePlugins()
		{
			foreach (Addin addin in AddinManager.Registry.GetAddins())
			{
				var property = addin.Properties.SingleOrDefault(p => p.Name.Equals("star", StringComparison.OrdinalIgnoreCase));

				if (property != null)
				{
					string verStr = string.Format("{0}.{1}.{2}.{3}", StarVersion.Major, StarVersion.Minor, StarVersion.Build, StarVersion.Revision);

					if (Addin.CompareVersions(verStr, property.Value) <= 0)
					{
						AddinManager.Registry.EnableAddin(addin.Id);
					}
					else
					{
						Logger.Error("Plugin {0} requires Star version {1}+. Load failed!", addin.Description.LocalId, property.Value);
					}
				}
				else
				{
					Logger.Error("Plugin {0} does not define a minimum Star version requirement. Plugin has not been enabled!", addin.Description.LocalId);
				}
			}
		}

		public virtual void InstallPlugin(string name)
		{
			Addin addin = AddinManager.Registry.GetAddins().SingleOrDefault(p => p.Description.LocalId.Equals(name, StringComparison.OrdinalIgnoreCase));

			if (addin != null)
			{
				Logger.Error("Plugin {0} is already installed!");

				return;
			}

			var addins = SetupService.Repositories.GetAvailableAddins(RepositorySearchFlags.LatestVersionsOnly).Where(p => p.Addin.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();

			if (addins.Any())
			{
				AddinRepositoryEntry avAddin = addins.First();

				var property = avAddin.Addin.Properties.SingleOrDefault(p => p.Name.Equals("star", StringComparison.OrdinalIgnoreCase));

				if (property != null)
				{
					InstallPlugin(new[] { avAddin });
				}
				else
				{
					Logger.Error("Plugin {0} does not define a minimum Star version requirement. Installation failed!", avAddin.Addin.Id);
				}
			}
			else
			{
				Logger.Error("Could not find plugin by the name \"{0}\"", name);
			}
		}

		public virtual void UpdatePlugins()
		{
			foreach (Addin addin in AddinManager.Registry.GetAddins())
			{
				Addin refreshedAddin = AddinManager.Registry.GetAddin(addin.Id);

				UpdatePlugin(refreshedAddin.Description.LocalId);
			}

			AddinManager.Registry.Update();
		}

		public virtual bool UpdatePlugin(string name)
		{
			SetupService.Repositories.UpdateAllRepositories(null);

			Addin addin = AddinManager.Registry.GetAddins().SingleOrDefault(p => p.Description.LocalId.Equals(name, StringComparison.OrdinalIgnoreCase));

			if (addin == null)
			{
				Logger.Error("The plugin {0} is not installed!", name);

				return false;
			}

			AddinRepositoryEntry[] entries = SetupService.Repositories.GetAvailableAddinUpdates(addin.LocalId, RepositorySearchFlags.LatestVersionsOnly);

			return InstallPlugin(entries);
		}

		public virtual void UninstallPlugin(string name)
		{
			Addin addin = AddinManager.Registry.GetAddins().SingleOrDefault(p => p.Description.LocalId.Equals(name, StringComparison.OrdinalIgnoreCase));

			if (addin != null)
			{
				SetupService.Uninstall(null, addin.Id);
				AddinManager.Registry.Update();
			}
			else
			{
				Logger.Error("The plugin {0} is not installed", name);
			}
		}

		public virtual void LoadPlugin(CSPlugin plugin)
		{
			var plugins = _plugins.Where(p => p.Value.Equals(plugin)).ToList();

			if (plugins.Count > 1)
			{
				Logger.Error("Error loading plugin \"{0}\"", plugin.Name);
			}
			else if (plugins.Count > 0)
			{
				AddinManager.Registry.EnableAddin(plugins[0].Key);
			}
		}

		public virtual void UnloadPlugin(CSPlugin plugin)
		{
			var plugins = _plugins.Where(p => p.Value.Equals(plugin)).ToList();

			if (plugins.Count > 1)
			{
				Logger.Error("Error unloading plugin \"{0}\"", plugin.Name);
			}
			else if (plugins.Count > 0)
			{
				AddinManager.Registry.DisableAddin(plugins.First().Key);
			}
		}

		public override void UnloadPlugins()
		{
			foreach (CSPlugin plugin in _plugins.Values)
			{
				UnloadPlugin(plugin);
			}
		}

		public override void ReloadPlugins()
		{
			UnloadPlugins();
			EnablePlugins();
		}

		public override bool PassCommand(string command, Player player)
		{
			bool foundCommand = false;

            var ctx = new PlayerCommandContext(player);
            foreach (CSPlugin plugin in _plugins.Values)
			{
				if (plugin.PlayerCommandManager.PassCommand(command, ctx))
					foundCommand = true;
			}

			return foundCommand;
		}

		public override void PassPacketEvent(byte packetId, PacketEvent evt, PacketEventType evtType)
		{
            PassPacketEventAsync(packetId, evt, evtType).Wait();
		}

        public override Task PassPacketEventAsync(byte packetId, PacketEvent evt, PacketEventType evtType)
        {
            var tasks = new List<Task>();
            foreach (CSPlugin plugin in _plugins.Values)
            {
                tasks.Add(plugin.PacketEventManager.PassEventAsync(new PacketEventKey { EventType = evtType, PacketId = packetId }, evt));
            }

            return Task.WhenAll(tasks);
        }

        public override IPlugin[] GetPlugins()
		{
			return _plugins.Values.Cast<IPlugin>().ToArray();
		}

		protected virtual void OnExtensionChanged(object sender, ExtensionNodeEventArgs args)
		{
			var plugin = args.ExtensionObject as CSPlugin;

			if (plugin != null)
			{
				string id = Addin.GetFullId(null, args.ExtensionNode.Addin.Id, args.ExtensionNode.Addin.Version);
				Addin addin = AddinManager.Registry.GetAddin(id);

				if (args.Change == ExtensionChange.Add)
				{
					var property = addin.Properties.SingleOrDefault(p => p.Name.Equals("star", StringComparison.OrdinalIgnoreCase));

					if (property != null)
					{
						string verStr = string.Format("{0}.{1}.{2}.{3}", StarVersion.Major, StarVersion.Minor, StarVersion.Build, StarVersion.Revision);

						if (Addin.CompareVersions(verStr, property.Value) > 0)
						{
							Logger.Error("Plugin {0} requires Star version {1}+. Load failed!", addin.Description.LocalId, property.Value);

							AddinManager.Registry.DisableAddin(id);
						}
					}
					else
					{
						Logger.Error("Plugin {0} does not define a minimum Star version requirement. Load failed!", addin.Description.LocalId);

						AddinManager.Registry.DisableAddin(id);
					}

					string path = Path.GetDirectoryName(args.ExtensionObject.GetType().Assembly.Location);

					ResolveEventHandler handler = (s, e) =>
					{
						AssemblyName reqName = new AssemblyName(e.Name);

						string dllFile = string.Format("{0}.dll", reqName.Name);
						string fullDllPath = Path.Combine(path, dllFile);

						return Assembly.LoadFrom(fullDllPath);
					};

					AppDomain.CurrentDomain.AssemblyResolve += handler;

					_plugins.AddOrUpdate(id, plugin, (k, p) => p);
					plugin.Load();

					Parallel.ForEach(_plugins.Values, x => x.PluginLoaded(plugin));

					Logger.Info("Loaded plugin \"{0}\" ({1})", plugin.Name, addin.Version);
				}
				else if (args.Change == ExtensionChange.Remove)
				{
					plugin.Unload();

					Parallel.ForEach(_plugins.Values, x => x.PluginUnloaded(plugin));

					CSPlugin removed;
					while (!_plugins.TryRemove(id, out removed))
					{
						Thread.Sleep(1);
					}

					Logger.Info("Unloaded plugin \"{0}\"", plugin.Name);
				}
			}
		}

		protected virtual bool InstallPlugin(AddinRepositoryEntry[] entries)
		{
			if (entries.Length > 0)
			{
				var entry = entries.First();

				var property = entry.Addin.Properties.SingleOrDefault(p => p.Name.Equals("star", StringComparison.OrdinalIgnoreCase));

				if (property != null)
				{
					string verStr = string.Format("{0}.{1}.{2}.{3}", StarVersion.Major, StarVersion.Minor, StarVersion.Build, StarVersion.Revision);

					if (Addin.CompareVersions(verStr, property.Value) <= 0)
					{
						Logger.Info("Plugin {0} is now updating to version {1}!", entry.Addin.Name, entry.Addin.Version);

						SetupService.Install(null, entries);

						AddinManager.Registry.EnableAddin(entry.Addin.Id);
						AddinManager.Registry.Update();
					}
					else
					{
						Logger.Error("Plugin {0} now requires Star version {1}+. Update failed!", entry.Addin.Name, property.Value);
					}

					return true;
				}
				else
				{
					Logger.Error("Plugin {0} does not define a minimum Star version requirement. Update failed!", entry.Addin.Name);
				}
			}

			return false;
		}
	}
}
