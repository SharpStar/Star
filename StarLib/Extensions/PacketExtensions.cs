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
using System.Reactive.Linq;
using StarLib.Packets;
using StarLib.Server;

namespace StarLib.Extensions
{
	public static class PacketExtensions
	{

		/// <summary>
		/// Sends a packet right before a different instance of a packet is about to be sent.
		/// </summary>
		/// <param name="toSendBefore">The packet to send before</param>
		/// <param name="toSendAfter">The packet to send after</param>
		/// <param name="conn">The connection where the "before" packet should be sent to</param>
		public static void RegisterSendBefore(this Packet toSendBefore, Packet toSendAfter, StarConnection conn)
		{
			var bEvt = Observable.FromEventPattern<PacketEventArgs>(p => conn.PacketSent += p,
					p => conn.PacketSent -= p).Select(p => p.EventArgs);

			StarConnection sendToConn;

			if (toSendAfter.Direction == Direction.Client)
				sendToConn = conn.Proxy.ClientConnection;
			else
				sendToConn = conn.Proxy.ServerConnection;

			var sEvt = Observable.FromEventPattern<PacketEventArgs>(p => sendToConn.PacketSending += p,
					p => sendToConn.PacketSending -= p).Select(p => p.EventArgs);

			var afterObs = sEvt.Where(p => p.Packet == toSendAfter);
			var beforeObs = bEvt.Where(p => p.Packet == toSendBefore);

			beforeObs.Take(1).Subscribe(_ =>
			{
				toSendAfter.Ignore = false;

				sendToConn.SendPacket(toSendAfter);
			});

			afterObs.Take(1).Finally(() => conn.SendPacket(toSendBefore)).Subscribe(_ =>
			{
				toSendAfter.Ignore = true;
			});

			conn.SendPacket(toSendBefore);
		}

		/// <summary>
		/// Sends a packet right before a different type of packet is about to be sent.
		/// </summary>
		/// <typeparam name="T">The type of packet to be sent "after"</typeparam>
		/// <param name="toSendBefore">The packet to be sent before</param>
		/// <param name="afterDir">The direction that type of packet is going to be sent to</param>
		/// <param name="conn">The connection where the "before" packet should be sent to</param>
		public static void RegisterSendBefore<T>(this Packet toSendBefore, Direction afterDir, StarConnection conn) where T : Packet
		{
			var bEvt = Observable.FromEventPattern<PacketEventArgs>(p => conn.PacketSent += p,
					p => conn.PacketSent -= p).Select(p => p.EventArgs);

			StarConnection sendToConn;
			if (afterDir == Direction.Client)
				sendToConn = conn.Proxy.ServerConnection;
			else
				sendToConn = conn.Proxy.ClientConnection;

			var sEvt = Observable.FromEventPattern<PacketEventArgs>(p => sendToConn.PacketSending += p,
				p => sendToConn.PacketSending -= p).Select(p => p.EventArgs);

			var afterObs = sEvt.Where(p => p.Packet.GetType() == typeof(T));
			var beforeObs = bEvt.Where(p => p.Packet == toSendBefore);
			
			afterObs.Take(1).Finally(() => conn.SendPacket(toSendBefore)).Subscribe(p =>
			{
				p.Packet.Ignore = true;

				beforeObs.Take(1).Subscribe(x =>
				{
					p.Packet.Ignore = false;

					sendToConn.SendPacket(p.Packet);
				});
			});
		}
	}
}
