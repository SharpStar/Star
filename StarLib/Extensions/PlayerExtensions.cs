﻿// SharpStar. A Starbound wrapper.
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
using StarLib.Database.Models;
using StarLib.DataTypes;
using StarLib.Misc;
using StarLib.Packets;
using StarLib.Packets.Starbound;
using StarLib.Starbound;
using StarLib.Starbound.Warp;
using StarLib.Starbound.World;

namespace StarLib.Extensions
{
    public static class PlayerExtensions
    {
        public static void WarpToPlayerShip(this Player player, Player otherPlayer)
        {
            player.Proxy.ServerConnection.SendPacket(new PlayerWarpPacket
            {
                Action = new Any<WarpToWorldAction, WarpToPlayerAction, WarpAliasAction>
                {
                    Value = new WarpToWorldAction
                    {
                        WorldId = new Any<UniqueWorldId, CelestialWorldId, ClientShipWorldId, MissionWorldId>
                        {
                            Value = new ClientShipWorldId
                            {
                                Id = otherPlayer.Uuid
                            }
                        },
                        SpawnTarget = new SpawnTarget
                        {
                            Target = new Any<SpawnTargetUniqueEntity, SpawnTargetPosition>()
                        }
                    }
                }
            });
        }

        public static void WarpToPlayer(this Player player, Player otherPlayer)
        {
            player.Proxy.ServerConnection.SendPacket(new PlayerWarpPacket
            {
                Action = new Any<WarpToWorldAction, WarpToPlayerAction, WarpAliasAction>
                {
                    Value = new WarpToPlayerAction
                    {
                        Uuid = otherPlayer.Uuid
                    }
                }
            });
        }

        public static void WarpToWorld(this Player player, CelestialCoordinates coords, float spawnX, float spawnY)
        {
            player.Proxy.ServerConnection.SendPacket(new PlayerWarpPacket
            {
                Action = new Any<WarpToWorldAction, WarpToPlayerAction, WarpAliasAction>
                {
                    Value = new WarpToWorldAction
                    {
                        WorldId = new Any<UniqueWorldId, CelestialWorldId, ClientShipWorldId, MissionWorldId>
                        {
                            Value = new CelestialWorldId
                            {
                                Coordinates = coords
                            }
                        },
                        SpawnTarget = new SpawnTarget
                        {
                            Target = new Any<SpawnTargetUniqueEntity, SpawnTargetPosition>
                            {
                                Value = new SpawnTargetPosition
                                {
                                    Position = new Vec2F { X = spawnX, Y = spawnY }
                                }
                            }
                        }
                    }
                }
            });
        }

        public static async Task<bool> HasPermissionsAsync(this Player player, params string[] permissions)
        {
            if (player.Account == null)
                return false;

            bool hasGroupPerm = false;
            if (player.Account.Group != null)
            {
                Group group = player.Account.Group;

                if (group != null)
                {
                    hasGroupPerm = group.Permissions.Any(p => permissions.Any(x => p.Name.Equals(x, StringComparison.OrdinalIgnoreCase) && p.Allowed));
                }
            }

            return hasGroupPerm || player.Account.Permissions.Any(p => permissions.Any(x => p.Name.Equals(x, StringComparison.OrdinalIgnoreCase) && p.Allowed));
        }
    }
}
