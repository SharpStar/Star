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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
        
        [HandleProcessCorruptedStateExceptions]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            XmlConfigurator.Configure();

            _configFile = new JsonFileConfiguration<SharpConfig>("sharpconfig.json", new JsonSerializerSettings());
            _configFile.Load();

            try
            {
                Run();
            }
            catch (Exception ex)
            {
                ex.LogError();
            }

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

            while (!_shutdown)
            {
                try
                {
                    string input = Console.ReadLine();

                    if (input != null)
                    {
                        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        {
                            Shutdown(false);

                            break;
                        }

                        StarMain.Instance.ConsoleCommandManager.TryPassConsoleCommand(input);
                    }
                }
                catch
                {
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

            StarMain.Instance.ConsoleCommandManager.AddCommands(ConsoleCommandsToAdd);
            StarMain.Instance.Server.AddPacketHandlers(HandlersToAdd);

            StarMain.Instance.Start();
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            Shutdown(false);

            return false;
        }

        private static void WaitForUnixExit()
        {
            UnixSignal[] signals =
            {
                new UnixSignal (Signum.SIGINT),
                new UnixSignal (Signum.SIGTERM)
            };

            Task.Run(() =>
            {
                UnixSignal.WaitAny(signals, -1);

                Shutdown();
            });
        }

        public static void Shutdown(bool exit = true)
        {
            if (_shutdown)
                return;

            _shutdown = true;

            StarMain.Instance.Server.AcceptingConnections = false;

            Log.Info("Shutting down...");

            string reason = StarMain.Instance.CurrentLocalization["Shutdown"];

            if (!string.IsNullOrEmpty(reason))
            {
                Parallel.ForEach(StarMain.Instance.Server.Proxies, async proxy =>
                {
                    await proxy.KickAsync(reason);
                });
            }

            StarMain.Instance.Shutdown();

            if (exit)
            {
                Log.Info("Shutdown complete!");

                Process.GetCurrentProcess().Kill();
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.Exception.LogError();
            e.SetObserved();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ((Exception)e.ExceptionObject).LogError();

            if (e.IsTerminating)
            {
                StarLog.DefaultLogger.Warn("Unhandled exception, cannot recover! Shutting down...");

                Shutdown(false);
            }
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
                new UuidOfCommand(),
                new BroadcastCommand(),
                new WarpToWorldCommand(),
                new BanHammerCommand(),
                new MakeAdminCommand()
            };
        }

        private static IPacketHandler[] CreatePacketHandlers()
        {
            return new IPacketHandler[]
            {
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
                //new CelestialRequestHandler(),
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
                new PlayerWarpToPlayerCommand(),
            };
        }
    }
}
