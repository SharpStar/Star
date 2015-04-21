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

namespace SharpStar.ConsoleCommands
{
	public class ListCommand : ConsoleCommand
	{
		public ListCommand() : base(StarMain.Instance.CurrentLocalization["ListConsoleCommandName"] ?? "list")
        {
			Parts[string.Empty] = p =>
			{
				foreach (ConsoleCommand cmd in StarMain.Instance.ConsoleCommandManager.Paged(0))
				{
					StarLog.DefaultLogger.Info("{0} - {1}", cmd.CommandName, cmd.Description);
				}

				StarLog.DefaultLogger.Info(string.Format(StarMain.Instance.CurrentLocalization["ListConsoleCommandPageFormat"], 1,
					Math.Ceiling(StarMain.Instance.ConsoleCommandManager.Count() / 6.0)));
			};

			Parts["{0}"] = p =>
			{
				int page = 1;
				if (p.Arguments.Length == 1)
				{
					if (!int.TryParse(p.Arguments[0], out page))
					{
						StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["ListConsoleCommandInvalidPage"]);

						return;
					}
				}
				
				var commands = StarMain.Instance.ConsoleCommandManager;
                foreach (ConsoleCommand cmd in commands.Paged(page - 1, 6))
				{
					StarLog.DefaultLogger.Info("{0} - {1}", cmd.CommandName, cmd.Description);
				}

				StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["ListConsoleCommandPageFormat"], page,
					Math.Ceiling(commands.Count() / 6.0));
			};
		}

		public override string Description
		{
			get
			{
				return StarMain.Instance.CurrentLocalization["ListConsoleCommandDesc"];
			}
		}
		public override string GetHelp(string[] arguments)
		{
			return StarMain.Instance.CurrentLocalization["ListConsoleCommandHelp"];
        }
	}
}
