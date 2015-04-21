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
using StarLib;
using StarLib.Commands.Console;
using StarLib.Extensions;
using StarLib.Logging;
using StarLib.Server;

namespace SharpStar.ConsoleCommands
{
	public class ListPlayersCommand : ConsoleCommand
	{
		public ListPlayersCommand() : base(StarMain.Instance.CurrentLocalization["ConsoleListPlayersCommandName"] ?? "players")
		{
			Parts[string.Empty] = p =>
			{
				foreach (StarProxy proxy in StarMain.Instance.Server.Proxies.Paged(0))
				{
					if (proxy.Player.Account != null)
						StarLog.DefaultLogger.Info("{0} ({1}) - {2}", proxy.Player.Name, proxy.Player.Account.Username, proxy.Player.Uuid);
					else
						StarLog.DefaultLogger.Info("{0} - {1}", proxy.Player.Name, proxy.Player.Uuid);
				}
			};

			Parts["{0}"] = p =>
			{
				int page;
				if (!int.TryParse(p.Arguments[0], out page))
				{
					StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["ConsoleListPlayersCommandInvalidPage"]);
                }

				var proxies = StarMain.Instance.Server.Proxies.Paged(page, 5);
				foreach (StarProxy proxy in proxies)
				{
					if (proxy.Player.Account != null)
						StarLog.DefaultLogger.Info("{0} ({1}) - {2}", proxy.Player.Name, proxy.Player.Account.Username, proxy.Player.Uuid);
					else
						StarLog.DefaultLogger.Info("{0} - {1}", proxy.Player.Name, proxy.Player.Uuid);
				}

				StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["ConsoleListPlayersCommandPageFormat"]);
			};
		}

		public override string Description
		{
			get
			{
				return StarMain.Instance.CurrentLocalization["ConsoleListPlayersCommandDesc"];
			}
		}

		public override string GetHelp(string[] arguments)
		{
			return StarMain.Instance.CurrentLocalization["ConsoleListPlayersCommandHelp"];
		}
	}
}
