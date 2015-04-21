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
using Mono.Addins;
using StarLib.Commands;
using StarLib.Commands.PlayerEvent;
using StarLib.Events.Packets;
using StarLib.Logging;

namespace StarLib.Plugins.CSharp
{
    [TypeExtensionPoint]
    public abstract class CSPlugin : IPlugin
    {
        public abstract string Name { get; }

        public StarLog Logger { get; private set; }
		
		public PacketEventManager PacketEventManager { get; private set; }

        public PlayerEventCommandManager PlayerCommandManager { get; private set; }

        protected CSPlugin()
        {
            PlayerCommandManager = new PlayerEventCommandManager();
			PacketEventManager = new PacketEventManager();
            Init();
        }

        private void Init()
        {
            Logger = new StarLog(Name);
        }

        public virtual void Load()
        {
        }

        public virtual void Unload()
        {
        }

        public virtual void PluginLoaded(CSPlugin plugin)
        {
        }

        public virtual void PluginUnloaded(CSPlugin plugin)
        {
        }
    }
}
