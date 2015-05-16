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
using StarLib.Database;
using StarLib.Database.Models;
using StarLib.Extensions;
using StarLib.Logging;
using StarLib.Server;
using StarLib.Starbound;

namespace SharpStar.ConsoleCommands
{
    public class BanHammerCommand : ConsoleCommand
    {
        public BanHammerCommand() : base(StarMain.Instance.CurrentLocalization["BanHammerCommandName"] ?? "starban")
        {
            Parts["{0} {1}"] = p =>
            {
                string uuid = p.Arguments[0];
                string reason = p.Arguments[1];
                Character ch = StarMain.Instance.Database.GetCharacterByUuid(uuid);

                if (ch == null)
                {
                    StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["BanHammerCommandNoSuchCharacter"]);

                    return;
                }

                string ip = ch.LastIpAddress;

                StarProxy proxy = StarMain.Instance.Server.Proxies.SingleOrDefault(x => x.Player != null && x.Player.Uuid.Id == uuid);

                if (proxy != null)
                {
                    ip = proxy.ClientConnection.RemoteEndPoint.Address.ToString();
                    proxy.Kick(reason);
                }

                StarMain.Instance.Database.AddBan(proxy != null ? proxy.Player.Name : string.Empty, ip, null, DateTime.MaxValue, reason);

            };

            Parts["{0} {1} {2}"] = p =>
            {
                string uuid = p.Arguments[0];
                string reason = p.Arguments[1];
                string time = p.Arguments[2];
                Character ch = StarMain.Instance.Database.GetCharacterByUuid(uuid);

                if (ch == null)
                {
                    StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["BanHammerCommandNoSuchCharacter"]);

                    return;
                }

                string ip = ch.LastIpAddress;

                StarProxy proxy = StarMain.Instance.Server.Proxies.SingleOrDefault(x => x.Player != null && x.Player.Uuid.Id == uuid);
                Account account = null;

                if (proxy != null)
                {
                    proxy.Kick(reason);

                    ip = proxy.ClientConnection.RemoteEndPoint.Address.ToString();
                    account = proxy.Player.Account;
                }

                DateTime expirDate;

                if (time.Equals("forever", StringComparison.OrdinalIgnoreCase))
                    expirDate = DateTime.MaxValue;
                else
                    expirDate = time.ToDateTime();

                Ban existingBan = StarMain.Instance.Database.GetBanByIp(ip);

                if (existingBan != null)
                {
                    existingBan.Active = true;
                    existingBan.ExpirationTime = expirDate;

                    StarMain.Instance.Database.SaveBan(existingBan);

                    StarLog.DefaultLogger.Info("Ban updated");
                }
                else
                {
                    StarMain.Instance.Database.AddBan(proxy != null ? proxy.Player.Name : string.Empty,
                        ip, account != null ? account.Id : (int?)null, expirDate, reason);

                    StarLog.DefaultLogger.Info("Ban added");
                }

                string evtText;
                if (proxy != null)
                    evtText = string.Format("Console banned {0} ({1}), reason: {2}", proxy.Player.Name, uuid, reason);
                else
                    evtText = string.Format("Console banned {0}, reason: {1}", uuid, reason);

                StarMain.Instance.Database.AddEvent(evtText, new[] { "console", "bans" });
            };
        }

        public override string Description
        {
            get
            {
                return StarMain.Instance.CurrentLocalization["BanHammerCommandDesc"];
            }
        }
        public override string GetHelp(string[] arguments)
        {
            return StarMain.Instance.CurrentLocalization["BanHammerCommandHelp"];
        }
    }
}
