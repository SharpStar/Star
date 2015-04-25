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
using StarLib.Server;
using StarLib.Starbound;

namespace SharpStar.PlayerCommands
{
	public class PlayerWarpToPlayerCommand : PlayerEventCommand
	{
		public PlayerWarpToPlayerCommand() : base(StarMain.Instance.CurrentLocalization["PlayerWarpToPlayerCommandName"] ?? "warpto")
		{
			Parts["{0}"] = p =>
			{
				Player fromPlr = p.Player;
				var toProxies = StarMain.Instance.Server.Proxies.Where(x => x.Player != null &&
								x.Player.NameWithoutColor.Equals(p.Arguments[0], StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.ConnectionTime)
								.ToList();

				if (toProxies.Count > 1)
				{
					fromPlr.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
						StarMain.Instance.CurrentLocalization["PlayerWarpToPlayerCommandDupNameMessage"]);

					int ctr = 1;
					foreach (StarProxy proxy in toProxies.Paged(0, 4))
					{
						fromPlr.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
							string.Format(StarMain.Instance.CurrentLocalization["PlayerWarpToPlayerCommandDupNameFormat"],
							ctr, proxy.Player.Name, proxy.Player.Uuid.Id));

						ctr++;
					}
				}
				else if (toProxies.Count == 1)
				{
					fromPlr.WarpToPlayerShip(toProxies.First().Player);

					p.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
						string.Format(StarMain.Instance.CurrentLocalization["PlayerWarpToPlayerCommandSuccessMessage"], toProxies.First().Player.Name));
				}
				else
				{
					p.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
						string.Format(StarMain.Instance.CurrentLocalization["PlayerWarpToPlayerCommandNotFoundMessage"], p.Arguments[0]));
				}
			};

			Parts["{0} {1}"] = p =>
			{
				int selection;
				if (!int.TryParse(p.Arguments[1], out selection))
				{
					p.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
						StarMain.Instance.CurrentLocalization["PlayerWarpToPlayerCommandDupInvalidSel"]);

					return;
				}

				selection--;

				Player fromPlr = p.Player;
				var toProxies = StarMain.Instance.Server.Proxies.Where(x => x.Player != null &&
								x.Player.NameWithoutColor.Equals(p.Arguments[0], StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.ConnectionTime)
								.ToList();

				if (toProxies.Count < selection)
				{
					p.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
						StarMain.Instance.CurrentLocalization["PlayerWarpToPlayerCommandDupInvalidSel"]);

					return;
				}

				fromPlr.WarpToPlayerShip(toProxies[selection].Player);

				p.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
						string.Format(StarMain.Instance.CurrentLocalization["PlayerWarpToPlayerCommandSuccessMessage"], toProxies[selection].Player.Name));
			};
		}

		public override string Description
		{
			get
			{
				return StarMain.Instance.CurrentLocalization["PlayerWarpToPlayerCommandDesc"];
			}
		}

		public override string GetHelp(string[] arguments)
		{
			return StarMain.Instance.CurrentLocalization["PlayerWarpToPlayerCommandHelp"];
		}
	}
}
