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
using StarLib.Plugins;

namespace SharpStar.PlayerCommands
{
	public class PlayerListCommand : PlayerEventCommand
	{
		public PlayerListCommand() : base(StarMain.Instance.CurrentLocalization["PlayerListCommandName"] ?? "starlist")
        {
			Parts[string.Empty] = p =>
			{
				var player = p.Player;

				var sharpCommands = Program.PlayerCommands;
				var pluginCommands = (from ipm in StarMain.Instance.PluginManagers
									  from plugin in ipm.GetPlugins()
									  from pec in plugin.PlayerCommandManager
									  select pec);


				foreach (PlayerEventCommand pec in sharpCommands.Concat(pluginCommands).Paged(0, 5))
				{
					player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
								string.Format("/{0} - {1}", pec.CommandName, pec.Description));
				}

			};

			Parts["{0}"] = p =>
			{
				int page;

				if (!int.TryParse(p.Arguments[0], out page))
				{
					p.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
                        StarMain.Instance.CurrentLocalization["PlayerListCommandInvalidPage"]);
                }

				var player = p.Player;

				var sharpCommands = Program.PlayerCommands;
				var pluginCommands = (from ipm in StarMain.Instance.PluginManagers
									  from plugin in ipm.GetPlugins()
									  from pec in plugin.PlayerCommandManager
									  select pec);


				foreach (PlayerEventCommand pec in sharpCommands.Concat(pluginCommands).Paged(page - 1, 5))
				{
					player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
								string.Format("/{0} - {1}", pec.CommandName, pec.Description));
				}

				player.Proxy.SendChatMessage("Star", "END");

			};
		}

		public override string Description
		{
			get
			{
				return StarMain.Instance.CurrentLocalization["PlayerListCommandDesc"];
			}
		}

		public override string GetHelp(string[] arguments)
		{
			return StarMain.Instance.CurrentLocalization["PlayerListCommandHelp"];
        }
	}
}
