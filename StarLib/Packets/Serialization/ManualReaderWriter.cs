using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarLib.Networking;

namespace StarLib.Packets.Serialization
{
    public abstract class ManualReaderWriter
    {
        public abstract Task Read(StarReader reader);

        public abstract Task Write(StarWriter writer);
    }
}
