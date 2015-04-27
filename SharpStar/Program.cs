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
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using log4net.Config;
using Mono.Unix;
using Mono.Unix.Native;
using Newtonsoft.Json;
using SharpStar.Configuration;
using SharpStar.ConsoleCommands;
using SharpStar.Native;
using SharpStar.PacketHandlers;
using SharpStar.PlayerCommands;
using SharpStar.Server;
using StarLib;
using StarLib.Commands.Console;
using StarLib.Commands.PlayerEvent;
using StarLib.Configuration;
using StarLib.Extensions;
using StarLib.Localization;
using StarLib.Logging;
using StarLib.Mono;
using StarLib.Packets;
using StarLib.Server;

namespace SharpStar
{
	class Program
	{
		private static readonly StarLog Log = StarLog.DefaultLogger;

		private static readonly Version SharpStarVersion = Assembly.GetExecutingAssembly().GetName().Version;
		private static readonly Version StarVersion = typeof(StarMain).Assembly.GetName().Version;

		private static readonly Lazy<ConsoleCommand[]> _createConsoleCommands = new Lazy<ConsoleCommand[]>(CreateConsoleCommands);
		private static readonly Lazy<PlayerEventCommand[]> _createPlayerCommands = new Lazy<PlayerEventCommand[]>(CreatePlayerCommands);
		private static readonly Lazy<IPacketHandler[]> _createPacketHandlers = new Lazy<IPacketHandler[]>(CreatePacketHandlers);

		private static bool _shutdown;

		private static JsonFileConfiguration<SharpConfig> _configFile;

		public static SharpConfig Configuration
		{
			get
			{
				return _configFile.Config;
			}
		}

		public static ConsoleCommand[] ConsoleCommandsToAdd
		{
			get
			{
				return _createConsoleCommands.Value;
			}
		}

		public static IPacketHandler[] HandlersToAdd
		{
			get
			{
				return _createPacketHandlers.Value;
			}
		}

		public static PlayerEventCommand[] PlayerCommands
		{
			get
			{
				return _createPlayerCommands.Value;
			}
		}

		static void Main(string[] args)
		{
			XmlConfigurator.Configure();
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			_configFile = new JsonFileConfiguration<SharpConfig>("sharpconfig.json", new JsonSerializerSettings());
			_configFile.Load();

			Run();

			if (StarMain.Instance.ServerConfig.RunAsService)
			{
				if (MonoHelper.IsRunningOnMono)
				{
					StarLog.DefaultLogger.Info("You are currently running Mono version {0}", MonoHelper.GetMonoVersion());

					WaitForUnixExit();
				}

				ServiceBase.Run(new StarService());

				return;
			}

			Console.CancelKeyPress += Console_CancelKeyPress;
			Console.SetError(TextWriter.Null);

			if (MonoHelper.IsRunningOnMono)
			{
				StarLog.DefaultLogger.Info("You are currently running Mono version {0}", MonoHelper.GetMonoVersion());

				WaitForUnixExit();
			}
			else
			{
				NativeMethods.SetConsoleCtrlHandler(ConsoleCtrlCheck, true);
			}

			while (true)
			{
				string input = Console.ReadLine();

				if (input != null)
				{
					if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
						Shutdown();

					StarMain.Instance.ConsoleCommandManager.TryPassConsoleCommand(input);
				}
			}
		}

		public static void Run()
		{
			Log.Info("SharpStar Version {0}.{1}.{2}.{3}", SharpStarVersion.Major, SharpStarVersion.Minor, SharpStarVersion.Build, SharpStarVersion.Revision);
			Log.Info("Star Version {0}.{1}.{2}.{3}", StarVersion.Major, StarVersion.Minor, StarVersion.Build, StarVersion.Revision);

			StarClientManager scm = new StarClientManager();
			scm.StartWatchingProxies();

			SetupStar();
		}

		private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			Shutdown();
		}

		private static void SetupStar()
		{
			StarMain.Instance.CurrentLocalization = new SimpleLocalizationFile("english.l10n");
			StarMain.Instance.Init();

			StarMain.Instance.ShutdownInitiated += ShutdownInitiated;
			StarMain.Instance.ConsoleCommandManager.AddCommands(ConsoleCommandsToAdd);
			StarMain.Instance.Server.AddPacketHandlers(HandlersToAdd);

			StarMain.Instance.Start();
		}

		private static void ShutdownInitiated(object sender, EventArgs e)
		{
			Log.Info("Kicking all players...");

			foreach (StarProxy proxy in StarMain.Instance.Server.Proxies)
			{
				proxy.Kick(StarMain.Instance.CurrentLocalization["Shutdown"]);
			}
		}

		private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
		{
			Shutdown();

			return true;
		}

		private static void WaitForUnixExit()
		{
			UnixSignal[] signals =
			{
				new UnixSignal (Signum.SIGINT),
				new UnixSignal (Signum.SIGTERM)
			};

			new Thread(() =>
			{
				UnixSignal.WaitAny(signals, -1);

				Shutdown();
			}).Start();
		}

		public static void Shutdown(bool exit = true)
		{
			if (_shutdown)
				return;

			_shutdown = true;

			Log.Info("Shutting down...");

			string reason = StarMain.Instance.CurrentLocalization["Shutdown"];

			if (!string.IsNullOrEmpty(reason))
			{
				foreach (StarProxy proxy in StarMain.Instance.Server.Proxies)
				{
					proxy.Kick(reason);
				}
			}

			StarMain.Instance.Shutdown();

			if (exit)
				Environment.Exit(0);
		}

		private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			e.SetObserved();
			e.Exception.LogError();
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			((Exception)e.ExceptionObject).LogError();

			if (e.IsTerminating)
				Shutdown();
		}

		private static ConsoleCommand[] CreateConsoleCommands()
		{
			return new ConsoleCommand[]
			{
				new HelpCommand(),
				new KickAllCommand(),
				new ListPlayersCommand(),
				new ListCommand(),
				new ReloadConfigsCommand(),
				new UuidOfCommand()
			};
		}

		private static IPacketHandler[] CreatePacketHandlers()
		{
			return new IPacketHandler[]
			{
				new AllPacketsHandler(),
				new ClientConnectHandler(),
				new ServerDisconnectHandler(),
				new ChatReceivePacketHandler(),
				new HandshakeChallengeHandler(),
				new HandshakeResponseHandler(),
				new ConnectSuccessHandler(),
				new ConnectFailureHandler(),
				new PlayerWarpHandler(),
				new ClientDisconnectRequestHandler(),
				new ChatSendHandler(),
				new PlayerWarpResultHandler(),
				new CelestialRequestHandler(),
				//new GiveItemHandler()
			};
		}

		private static PlayerEventCommand[] CreatePlayerCommands()
		{
			return new PlayerEventCommand[]
			{
				new PlayerHelpCommand(),
				new PlayerListCommand(),
				new PlayerListPlayersCommand(),
				new PlayerRegisterAccountCommand(),
				new PlayerWarpToPlayerCommand()
			};
		}
	}
}
