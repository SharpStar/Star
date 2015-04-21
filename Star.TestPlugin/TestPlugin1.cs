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
using Mono.Addins;
using Star.TestPlugin.Commands;
using Star.TestPlugin.Events;
using StarLib;
using StarLib.Plugins.CSharp;

[assembly: Addin("TestPlugin1", Version = "1.0.0.0")]
[assembly: AddinDescription("A test plugin")]
[assembly: AddinProperty("star", "1.0")]
[assembly: AddinDependency("StarLib", "1.0")]
namespace Star.TestPlugin
{
    [Extension]
    public class TestPlugin1 : CSPlugin
    {
        public override string Name
        {
            get { return "Test Plugin"; }
        }

        public override void Load()
        {
            Logger.Info("Loaded Test Plugin!");

            PlayerCommandManager.AddCommand(new TestCommand());
			PacketEventManager.AddEventObject(new ConnResponseEventListener());

            //StarMain.Instance.PassPlayerEventCommand("test command \"testing 321\"", null);

            //StarMain.Instance.CSPluginManager.CommandManager.TryPassCommand("test command", out success);

            //Logger.Error(success.ToString());
        }

    }
}
