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
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using StarLib;
using StarLib.Database.Models;
using StarLib.Logging;
using StarLib.Networking;
using StarLib.Packets;
using StarLib.Packets.Starbound;
using StarLib.Server;
using StarLib.Starbound;

namespace SharpStar.PacketHandlers
{
    public class ClientConnectHandler : PacketHandler<ClientConnectPacket>
    {
        public override async Task HandleAsync(ClientConnectPacket packet, StarConnection connection)
        {
            Player plr = connection.Proxy.Player;

            plr.Name = packet.PlayerName;
            plr.Uuid = packet.Uuid;

            string uuid = plr.Uuid.Id;
            Character ch = StarMain.Instance.Database.GetCharacterByUuid(uuid) ?? new Character();

            ch.Name = plr.Name;
            ch.Uuid = uuid;
            ch.LastIpAddress = connection.Proxy.ClientConnection.RemoteEndPoint.Address.ToString();
            ch.Account = null;

            StarMain.Instance.Database.SaveCharacter(ch);

            if (!string.IsNullOrEmpty(packet.Account) && Program.Configuration.EnableSharpAccounts)
            {
                Account account = StarMain.Instance.Database.GetAccountByUsername(packet.Account);

                if (account == null)
                    return;

                connection.Proxy.Player.Account = account;

                packet.Account = string.Empty;

                connection.Proxy.Player.AuthAttempted = true;

                await connection.Proxy.ClientConnection.SendPacketAsync(new HandshakeChallengePacket
                {
                    Salt = Encoding.UTF8.GetBytes(account.PasswordSalt)
                });
            }
            else
            {
                Ban ban = StarMain.Instance.Database.GetBanByIp(connection.Proxy.ClientConnection.RemoteEndPoint.Address.ToString());
                if (ban != null && ban.Active)
                {
                    if (DateTime.Now > ban.ExpirationTime)
                    {
                        ban.Active = false;

                        StarMain.Instance.Database.SaveBan(ban);
                        StarMain.Instance.Database.AddEvent(string.Format("The ban for uuid {0} ({1}) has been lifted", plr.Uuid.Id, plr.Name),
                            new[] { "auto" });
                    }
                    else
                    {
                        await connection.Proxy.ClientConnection.SendPacketAsync(new ConnectFailurePacket
                        {
                            Reason = string.Format(StarMain.Instance.CurrentLocalization["BanReasonMessage"].Replace("\\n", "\n"), ban.Reason,
                                ban.ExpirationTime.ToString(StarMain.Instance.CurrentLocalization["BanMessageExpirationDateFormat"]))
                        });

                        StarMain.Instance.Database.AddEvent(string.Format("Banned uuid {0} ({1}) attempted to join!", plr.Uuid.Id, plr.Name),
                            new[] { "bans" });

                        return;
                    }
                }

                connection.Proxy.Player.AuthSuccess = true;
            }
        }

        public override Task HandleSentAsync(ClientConnectPacket packet, StarConnection connection)
        {
            return Task.FromResult(false);
        }
    }
}
