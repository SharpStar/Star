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
using StarLib.Commands;
using StarLib.Commands.PlayerEvent;
using StarLib.Extensions;
using StarLib.Packets.Starbound;

namespace Star.TestPlugin.Commands
{
    public class TestCommand : PlayerEventCommand
    {
        public TestCommand()
            : base("test")
        {
            Parts["command"] = p =>
            {
				p.Player.Proxy.SendChatMessage("server", "This is a test message", 5);
            };

            //Parts["add {0}"] = (p, args) =>
            //{
            //    p.Connection.SendPacket(new ChatReceivedPacket
            //    {
            //        Name = "Server",
            //        Message = "Testing " + args[0],
            //        ClientId = 0,
            //        Channel = "",
            //    });
            //};

            Parts["command {0}"] = p =>
            {
                Console.WriteLine("COMMAND: \"" + p.Arguments[0] + "\"");
            };
        }

        public override string Description
        {
            get { return "A test"; }
        }

        public override string GetHelp(string[] arguments)
        {
            throw new NotImplementedException();
        }

        protected override void OnError(CommandPart part, CommandError error)
        {
            Console.WriteLine("ERROR! " + part.CommandName + " " + error);
        }

    }
}
