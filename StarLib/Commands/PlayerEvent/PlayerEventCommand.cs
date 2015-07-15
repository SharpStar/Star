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
using StarLib.Extensions;
using StarLib.Starbound;

namespace StarLib.Commands.PlayerEvent
{
    public class PlayerEventCommand : Command<ParsedPlayerEventCommand, PlayerCommandContext>
    {
        protected PlayerEventCommand(string commandName) : base(commandName, new PlayerEventCommandParts(commandName), null)
        {
        }

        protected PlayerEventCommand(string commandName, PlayerCommandContext context) : base(commandName, new PlayerEventCommandParts(commandName),
            context)
        {
        }

        public override bool PassCommand(string command)
        {
            string[] result;
            var parsed = TryParseCommand(command, out result);

            if (result != null)
            {
                try
                {
                    parsed.Executor(new ParsedPlayerEventCommand { Arguments = result });
                }
                catch (PermissionDeniedException)
                {
                    if (Context.Player.Proxy.Running)
                        Context.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
                            StarMain.Instance.CurrentLocalization["PlayerCommandPermissionDenied"]);
                }
            }

            return result != null;
        }

        public override string Description { get; protected set; }

        public override string GetHelp(string[] arguments)
        {
            throw new NotImplementedException();
        }
    }

    public static class PlayerEventCommandExtensions
    {
        public static void RequiresPermissions(this PlayerEventCommand command, params string[] permissions)
        {
            if (command.Context.Player.Account == null ||
                !command.Context.Player.Account.Permissions.Any(x => permissions.Any(z => z.Equals(x.Name, StringComparison.OrdinalIgnoreCase))))
            {
                throw new PermissionDeniedException();
            }
        }
    }
}
