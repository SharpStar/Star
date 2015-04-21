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
using StarLib.Commands.PlayerEvent;
using StarLib.Extensions;
using StarLib.Starbound;

namespace SharpStar.PlayerCommands
{
	public class PlayerListPlayersCommand : PlayerEventCommand
	{
		public PlayerListPlayersCommand() : base(StarMain.Instance.CurrentLocalization["PlayerListPlayersCommandName"] ?? "players")
		{
			Parts[string.Empty] = p =>
			{
				SendPlayerInfos(p.Player, StarMain.Instance.Server.Proxies.Where(x => x.Player != null).Select(x => x.Player).Paged(0, 5));
			};

			Parts["{0}"] = p =>
			{
				int page;

				if (!int.TryParse(p.Arguments[0], out page))
				{
					p.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
						string.Format(StarMain.Instance.CurrentLocalization["PlayerListPlayersCommandInvalidPage"]));
				}

				SendPlayerInfos(p.Player, StarMain.Instance.Server.Proxies.Where(x => x.Player != null).Select(x => x.Player).Paged(page - 1, 5));
			};

			Parts["{0} {1}"] = p =>
			{

				int page;

				if (!int.TryParse(p.Arguments[0], out page))
				{
					p.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
						string.Format(StarMain.Instance.CurrentLocalization["PlayerListPlayersCommandInvalidPage"]));
				}

				SendPlayerInfos(p.Player, StarMain.Instance.Server.Proxies.Where(x => x.Player != null).Select(x => x.Player)
					.Where(x => x.Name.IndexOf(p.Arguments[1], StringComparison.CurrentCultureIgnoreCase) >= 0).Paged(page - 1, 5));
			};
		}

		private void SendPlayerInfos(Player commandPlr, IEnumerable<Player> players)
		{
			int ctr = 1;
			foreach (Player player in players)
			{
				if (player.Account != null)
					commandPlr.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
						string.Format("{0}. {1} ({2})", ctr, player.Name, player.Account.Username));
				else
					commandPlr.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
						string.Format("{0}. {1}", ctr, player.Name));

				ctr++;
			}
		}

		public override string Description
		{
			get
			{
				return StarMain.Instance.CurrentLocalization["PlayerListPlayersCommandDesc"];
			}
		}

		public override string GetHelp(string[] arguments)
		{
			return StarMain.Instance.CurrentLocalization["PlayerListPlayersCommandHelp"];
		}
	}
}
