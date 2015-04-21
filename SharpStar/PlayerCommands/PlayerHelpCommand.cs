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
using StarLib.Starbound;

namespace SharpStar.PlayerCommands
{
	public class PlayerHelpCommand : PlayerEventCommand
	{
		public override string Description
		{
			get { return StarMain.Instance.CurrentLocalization["PlayerHelpCommandDesc"]; }
		}

		public PlayerHelpCommand() : base(StarMain.Instance.CurrentLocalization["PlayerHelpCommandName"] ?? "starhelp")
		{
			Parts[string.Empty] = p =>
			{
				p.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
					GetHelp(null));
			};

			Parts["{0}"] = p =>
			{
				Player player = p.Player;

				PlayerEventCommand sharpCmd = Program.PlayerCommands.SingleOrDefault(x => x.CommandName.Equals(p.Arguments[0]));

				if (sharpCmd != null)
				{
					player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
						sharpCmd.GetHelp(p.Arguments.Skip(1).ToArray()));

					return;
				}

				foreach (IPluginManager ipm in StarMain.Instance.PluginManagers)
				{
					foreach (IPlugin plugin in ipm.GetPlugins())
					{
						PlayerEventCommand cmd = plugin.PlayerCommandManager.SingleOrDefault(x => x.CommandName.Equals(p.Arguments[0]));

						if (cmd != null)
							player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
								cmd.GetHelp(p.Arguments.Skip(1).ToArray()));
					}
				}
			};

		}

		public override string GetHelp(string[] arguments)
		{
			return StarMain.Instance.CurrentLocalization["PlayerHelpCommandHelp"];
		}
	}
}
