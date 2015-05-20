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

        private static readonly Type[] _packetHandlerTypes =
        {
            typeof(ClientConnectHandler),
            typeof(ServerDisconnectHandler),
            typeof(ChatReceivePacketHandler),
            typeof(HandshakeChallengeHandler),
            typeof(HandshakeResponseHandler),
            typeof(ConnectSuccessHandler),
            typeof(ConnectFailureHandler),
            typeof(PlayerWarpHandler),
            typeof(ClientDisconnectRequestHandler),
            typeof(ChatSendHandler),
            typeof(PlayerWarpResultHandler)
        };

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

        public static Type[] HandlersToAdd
        {
            get
            {
                return _packetHandlerTypes;
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
                Run().Wait();
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
                        string[] ex = input.Split(' ');

                        if (ex[0].Equals("exit", StringComparison.OrdinalIgnoreCase))
                        {
                            TimeSpan? ts = null;
                            if (ex.Length == 2)
                            {
                                double time;

                                if (double.TryParse(ex[1], out time))
                                {
                                    ts = TimeSpan.FromMinutes(time);
                                }
                            }

                            Shutdown(false, ts);

                            if (!ts.HasValue)
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

        public static async Task Run()
        {
            Log.Info("SharpStar Version {0}.{1}.{2}.{3}", SharpStarVersion.Major, SharpStarVersion.Minor, SharpStarVersion.Build, SharpStarVersion.Revision);
            Log.Info("Star Version {0}.{1}.{2}.{3}", StarVersion.Major, StarVersion.Minor, StarVersion.Build, StarVersion.Revision);

            await SetupStar();

            StarClientManager scm = new StarClientManager();
            scm.StartWatchingProxies();
        }

        private static async void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            await Shutdown();
        }

        private static Task SetupStar()
        {
            StarMain.Instance.CurrentLocalization = new SimpleLocalizationFile("english.l10n");
            StarMain.Instance.Init();

            StarMain.Instance.ConsoleCommandManager.AddCommands(ConsoleCommandsToAdd);
            StarMain.Instance.Server.AddPacketHandlers(HandlersToAdd);

            return StarMain.Instance.Start();
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            Shutdown(false).Wait();

            return false;
        }

        private static void WaitForUnixExit()
        {
            UnixSignal[] signals =
            {
                new UnixSignal (Signum.SIGINT),
                new UnixSignal (Signum.SIGTERM)
            };

            Task.Run(async () =>
            {
                UnixSignal.WaitAny(signals, -1);

                await Shutdown();
            });
        }

        public static async Task Shutdown(bool exit = true, TimeSpan? wait = null)
        {
            if (_shutdown)
                return;

            StarMain.Instance.Server.AcceptingConnections = false;

            if (wait.HasValue)
            {
                StarLog.DefaultLogger.Info("Shutting down in {0} minutes and {1} seconds.", wait.Value.Minutes, wait.Value.Seconds);

                string message = StarMain.Instance.CurrentLocalization["ShutdownIn"];

                DateTime end = DateTime.UtcNow.Add(wait.Value.Add(TimeSpan.FromMilliseconds(100)));

                var tasks = new List<Task>();
                if (!string.IsNullOrEmpty(message))
                {
                    foreach (var proxy in StarMain.Instance.Server.Proxies)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            TimeSpan ts = TimeSpan.FromSeconds((int)(end - DateTime.UtcNow).TotalSeconds);
                            while (ts >= TimeSpan.FromSeconds(1) && proxy.Connected)
                            {
                                TimeSpan delay;
                                string timeLeft = string.Format("{0} minutes {1} seconds",
                                    ts.Minutes, ts.Seconds);
                                try
                                {
                                    await proxy.SendChatMessageAsync(StarMain.Instance.CurrentLocalization["ServerMessageName"],
                                        string.Format(StarMain.Instance.CurrentLocalization["ShutdownIn"], timeLeft), 80);
                                }
                                catch
                                {
                                    break;
                                }

                                if (ts.Hours > 0)
                                    delay = TimeSpan.FromHours((int)(ts.TotalHours / 8.0));
                                else if (ts.Minutes > 0)
                                    delay = TimeSpan.FromMinutes((int)(ts.TotalMinutes / 6.0));
                                else if (ts.Seconds > 6)
                                    delay = TimeSpan.FromSeconds((int)(ts.TotalSeconds / 2.0));
                                else if (ts.Seconds <= 6)
                                    delay = TimeSpan.FromSeconds(1);
                                else
                                    break;

                                if (delay > TimeSpan.Zero)
                                    await Task.Delay(delay);
                                else
                                    break;

                                ts = ts.Subtract(delay);
                            }
                        }));
                    }
                }

                await Task.Delay(wait.Value);
                await Task.WhenAll(tasks);
            }

            _shutdown = true;

            Log.Info("Shutting down...");

            string reason = StarMain.Instance.CurrentLocalization["Shutdown"];

            if (!string.IsNullOrEmpty(reason))
            {
                Parallel.ForEach(StarMain.Instance.Server.Proxies, async proxy =>
                {
                    await proxy.KickAsync(reason);
                });
            }

            Task killTask = null;
            if (exit)
                killTask = Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(p => Process.GetCurrentProcess().Kill());

            StarMain.Instance.Shutdown();

            Log.Info("Shutdown complete!");

            if (killTask != null)
                await killTask;
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.Exception.LogError();
            e.SetObserved();
        }

        private static async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ((Exception)e.ExceptionObject).LogError();

            if (e.IsTerminating)
            {
                StarLog.DefaultLogger.Warn("Unhandled exception, cannot recover! Shutting down...");

                await Shutdown(false);
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
