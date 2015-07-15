using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarLib.Packets;
using StarLib.Packets.Starbound;
using StarLib.Server;

namespace SharpStar.PacketHandlers
{
    public class CallScriptedEntityHandler : PacketHandler<CallScriptedEntityPacket>
    {
        public override Task HandleAsync(CallScriptedEntityPacket packet, StarConnection connection)
        {
            Console.WriteLine(packet.Function);

            return Task.FromResult(false);
        }

        public override Task HandleSentAsync(CallScriptedEntityPacket packet, StarConnection connection)
        {
            return Task.FromResult(false);
        }
    }
}
