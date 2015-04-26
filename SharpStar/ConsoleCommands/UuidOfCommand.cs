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
using StarLib.Logging;
using StarLib.Starbound;

namespace SharpStar.ConsoleCommands
{
	public class UuidOfCommand : ConsoleCommand
	{
		public UuidOfCommand() : base(StarMain.Instance.CurrentLocalization["UuidOfCommandName"] ?? "uuidof")
		{
			Parts["{0}"] = p =>
			{
				string playerName = p.Arguments[0];

				var players = StarMain.Instance.Server.Proxies.Where(x => x.Player != null &&
					x.Player.NameWithoutColor.Equals(playerName, StringComparison.CurrentCultureIgnoreCase)).Select(x => x.Player).ToList();

				if (!players.Any())
				{
					StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["UuidOfCommandPlayerNotOnlineFormat"],
						playerName);

					return;
				}

				foreach (Player player in players)
				{
					StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["UuidOfCommandResponseFormat"], player.Name, player.Uuid);
				}
			};
		}

		public override string Description
		{
			get
			{
				return StarMain.Instance.CurrentLocalization["UuidOfCommandDesc"];
			}
		}

		public override string GetHelp(string[] arguments)
		{
			return StarMain.Instance.CurrentLocalization["UuidOfCommandHelp"];
		}
	}
}
