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
using StarLib.Commands;
using StarLib.Commands.Console;
using StarLib.Logging;

namespace SharpStar.ConsoleCommands
{
    public sealed class HelpCommand : ConsoleCommand
    {
        private readonly StarLog _logger = StarLog.DefaultLogger;

        public HelpCommand() : base(StarMain.Instance.CurrentLocalization["HelpConsoleCommandName"] ?? "starhelp")
        {
            Parts[string.Empty] = p =>
            {
                _logger.Info(GetHelp(null));
            };

            Parts["{0}"] = p =>
            {
                CommandInfo cmd = StarMain.Instance.ConsoleCommandManager.SingleOrDefault(x => x.Name.Equals(p.Arguments[0], StringComparison.CurrentCultureIgnoreCase));

                if (cmd == null)
                {
                    _logger.Info(StarMain.Instance.CurrentLocalization["HelpConsoleCommandNotFound"]);

                    return;
                }
                
                ConsoleCommand cCmd = (ConsoleCommand)Activator.CreateInstance(cmd.Type);

                _logger.Info(cCmd.GetHelp(p.Arguments.Skip(1).ToArray()));

            };

        }

        public override string Description
        {
            get { return StarMain.Instance.CurrentLocalization["HelpConsoleCommandDesc"]; }
        }

        public override string GetHelp(string[] arguments)
        {
            return StarMain.Instance.CurrentLocalization["HelpConsoleCommandHelp"];
        }
    }
}
