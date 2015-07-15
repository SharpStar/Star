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

namespace SharpStar.PlayerCommands
{
    public class PlayerRegisterAccountCommand : PlayerEventCommand
    {
        public PlayerRegisterAccountCommand() : base(StarMain.Instance.CurrentLocalization["PlayerRegisterAccountCommandName"])
        {
            Parts[string.Empty] = async p =>
            {
                await Context.Player.Proxy.SendChatMessageAsync(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
                    GetHelp(null));
            };

            Parts["{0} {1} {2}"] = p =>
            {
                string username = p.Arguments[0];
                string password = p.Arguments[1];
                string confirmPwd = p.Arguments[2];

                if (password != confirmPwd)
                {
                    Context.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
                        StarMain.Instance.CurrentLocalization["PlayerRegisterAccountCommandPasswordError"]);

                    return;
                }

                if (StarMain.Instance.Database.CreateAccount(username, password))
                {
                    Context.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
                        StarMain.Instance.CurrentLocalization["PlayerRegisterAccountCreatedMessage"]);
                }
                else
                {
                    Context.Player.Proxy.SendChatMessage(StarMain.Instance.CurrentLocalization["PlayerCommandChatName"],
                        StarMain.Instance.CurrentLocalization["PlayerRegisterAccountExistsError"]);
                }

            };
        }

        public override string Description
        {
            get
            {
                return StarMain.Instance.CurrentLocalization["PlayerRegisterAccountCommandDesc"];
            }
        }

        public override string GetHelp(string[] arguments)
        {
            return StarMain.Instance.CurrentLocalization["PlayerRegisterAccountCommandHelp"];
        }
    }
}
