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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StarLib;
using StarLib.Commands.Console;
using StarLib.Extensions;
using StarLib.Logging;
using StarLib.Server;
using StarLib.Starbound;

namespace SharpStar.ConsoleCommands
{
    public class WarpToWorldCommand : ConsoleCommand
    {
        public WarpToWorldCommand() : base(StarMain.Instance.CurrentLocalization["WarpToWorldCommandName"] ?? "worldwarp")
        {
            Parts["{0} {1} {2} {3}"] = p =>
            {
                string uuid = p.Arguments[0];
                string coordStr = p.Arguments[1];
                string spawnXStr = p.Arguments[2];
                string spawnYStr = p.Arguments[3];

                int spawnX;
                int spawnY;
                if (!int.TryParse(spawnXStr, out spawnX) || !int.TryParse(spawnYStr, out spawnY))
                {
                    StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["WarpToWorldCommandSpawnXYInvalid"]);

                    return;
                }

                Match match = Regex.Match(coordStr, @"(\-?\d+)_(\-?\d+)_(\-?\d+)_(\d{1,2})_(\d{1,2})");

                if (!match.Success)
                {
                    StarLog.DefaultLogger.Info(StarMain.Instance.CurrentLocalization["WarpToWorldCommandParseError"]);

                    return;
                }

                int xCoord = int.Parse(match.Groups[1].Value);
                int yCoord = int.Parse(match.Groups[2].Value);
                int zCoord = int.Parse(match.Groups[3].Value);
                int planet = int.Parse(match.Groups[4].Value);
                int system = int.Parse(match.Groups[5].Value);

                StarProxy proxy = StarMain.Instance.Server.Proxies.SingleOrDefault(x => x.Player != null && x.Player.Uuid.Id == uuid);

                if (proxy == null)
                {
                    StarLog.DefaultLogger.Info("Could not find player!");

                    return;
                }

                proxy.Player.WarpToWorld(new CelestialCoordinates
                {
                    Planet = planet,
                    System = system,
                    X = xCoord,
                    Y = yCoord,
                    Z = zCoord
                }, spawnX, spawnY);

                StarLog.DefaultLogger.Info("Player {0} has been warped to planet {1}, system {2}", proxy.Player.Name, planet, system);
            };
        }

        public override string Description
        {
            get
            {
                return StarMain.Instance.CurrentLocalization["WarpToWorldCommandDesc"];
            }
        }

        public override string GetHelp(string[] arguments)
        {
            return StarMain.Instance.CurrentLocalization["WarpToWorldCommandHelp"];
        }
    }
}
