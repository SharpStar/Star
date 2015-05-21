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
using StarLib.Database.Models;
using StarLib.Packets;
using StarLib.Packets.Starbound;
using StarLib.Server;

namespace SharpStar.PacketHandlers
{
    public class HandshakeResponseHandler : PacketHandler<HandshakeResponsePacket>
    {
        public override async Task HandleAsync(HandshakeResponsePacket packet, StarConnection connection)
        {
            Account account = connection.Proxy.Player.Account;

            if (account != null)
            {
                Ban acctBan = StarMain.Instance.Database.GetBanByAccount(account.Id);

                if (acctBan != null && acctBan.Active)
                {
                    //ban has expired, set ban to inactive
                    if (DateTime.Now > acctBan.ExpirationTime)
                    {
                        acctBan.Active = false;

                        StarMain.Instance.Database.SaveBan(acctBan);
                        StarMain.Instance.Database.AddEvent(
                            string.Format("The ban for account {0} ({1}) has been lifted", account.Username, connection.Proxy.Player.Name),
                            new[] { "auto" });
                    }
                    else
                    {
                        await connection.Proxy.ClientConnection.SendPacketAsync(new ConnectFailurePacket
                        {
                            Reason = string.Format(StarMain.Instance.CurrentLocalization["BanReasonMessage"].Replace("\\n", "\n"), acctBan.Reason,
                            acctBan.ExpirationTime.ToString(StarMain.Instance.CurrentLocalization["BanMessageExpirationDateFormat"]))
                        });

                        StarMain.Instance.Database.AddEvent(
                            string.Format("Banned account {0} ({1}) attempted to join!", account.Username, connection.Proxy.Player.Name),
                            new[] { "bans" });

                        return;
                    }
                }

                byte[] acctHash = Convert.FromBase64String(account.PasswordHash);

                connection.Proxy.Player.AuthSuccess = packet.PasswordHash.SequenceEqual(acctHash);

                if (!connection.Proxy.Player.AuthSuccess)
                {
                    await connection.Proxy.ClientConnection.SendPacketAsync(new ConnectFailurePacket
                    {
                        Reason = StarMain.Instance.CurrentLocalization["WrongPasswordError"]
                    });

                    StarMain.Instance.Database.AddEvent(
                        string.Format("Player {0} ({1}) failed to login with the username {2}", connection.Proxy.Player.Name,
                        connection.Proxy.Player.Uuid.Id, connection.Proxy.Player.Account.Username), new[] { "auth" });

                    connection.Proxy.Player.Account = null;
                }
                else
                {
                    //add the character to the databse and associate it with the account
                    Character ch = StarMain.Instance.Database.GetCharacterByUuid(connection.Proxy.Player.Uuid.Id) ?? new Character();

                    ch.Account = account;

                    StarMain.Instance.Database.SaveCharacter(ch);
                }
            }
            else if (!connection.Proxy.Player.AuthAttempted)
            {
                connection.Proxy.Player.AuthSuccess = true;
            }
            else
            {
                await connection.Proxy.ClientConnection.SendPacketAsync(new ConnectFailurePacket
                {
                    Reason = StarMain.Instance.CurrentLocalization["WrongPasswordError"]
                });
            }
        }

        public override Task HandleSentAsync(HandshakeResponsePacket packet, StarConnection connection)
        {
            return Task.FromResult(false);
        }
    }
}
