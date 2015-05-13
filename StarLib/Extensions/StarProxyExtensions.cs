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
using StarLib.Logging;
using StarLib.Packets.Starbound;
using StarLib.Server;

namespace StarLib.Extensions
{
    public static class StarProxyExtensions
    {
        public static void SendChatMessage(this StarProxy proxy, string name, string message, int maxCharsPerLine = 60)
        {
            proxy.SendChatMessageAsync(name, message, maxCharsPerLine).Wait();
        }


        public static async Task SendChatMessageAsync(this StarProxy proxy, string name, string message, int maxCharsPerLine = 60)
        {
            foreach (string line in message.SeparateString(maxCharsPerLine))
            {
                foreach (string line2 in line.Split('\n'))
                {
                    await proxy.ClientConnection.SendPacketAsync(new ChatReceivePacket
                    {
                        Name = name,
                        Message = line2,
                    });
                }
            }
        }

        public static void Kick(this StarProxy proxy, string reason = "")
        {
            proxy.KickAsync(reason).Wait();
        }

        public static async Task KickAsync(this StarProxy proxy, string reason = "")
        {
            EventHandler<PacketEventArgs> handler = (s, e) =>
            {
                if (!(e.Packet is ServerDisconnectPacket))
                {
                    e.Packet.Ignore = true;
                }
            };

            proxy.ClientConnection.PacketSending += handler;
            await proxy.ServerConnection.SendPacketAsync(new ClientDisconnectRequestPacket());
            await proxy.ClientConnection.SendPacketAsync(new ServerDisconnectPacket
            {
                Reason = reason
            });
        }
    }
}
