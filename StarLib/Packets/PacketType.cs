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

namespace StarLib.Packets
{
    public enum PacketType : byte
    {
        ProtocolVersion = 0,
		ServerDisconnect = 1,
		ConnectionSuccess = 2,
		ConnectionFailure = 3,
        HandshakeChallenge = 4,
		ChatReceived = 5,
		UniverseTimeUpdate = 6,
        CelestialResponse = 7,
		PlayerWarpResult = 8,
        ClientConnect = 9,
        ClientDisconnect = 10,
		HandshakeResponse = 11,
		PlayerWarp = 12,
		FlyShip = 13,
        ChatSend = 14,
		CelestialRequest = 15,
		ClientContextUpdate = 16,
        WorldStart = 17,
        WorldStop = 18,
		CelestialStructureUpdate = 19,
        TileArrayUpdate = 20,
        TileUpdate = 21,
        TileLiquidUpdate = 22,
        TileDamageUpdate = 23,
        TileModificationFailure = 24,
        GiveItem = 25,
        SwapContainerResult = 26,
        EnvironmentUpdate = 27,
        EntityInteractResult = 28,
		UpdateTileProtection = 29,
        ModifyTileList = 30,
        DamageTileGroup = 31,
		CollectLiquid = 32,
        RequestDrop = 33,
        SpawnEntity = 34,
        EntityInteract = 35,
        ConnectWire = 36,
        DisconnectAllWires = 37,
        OpenContainer = 38,
        CloseContainer = 39,
        SwapContainer = 40,
        ItemApplyContainer = 41,
        StartCraftingContainer = 42,
        StopCraftingContainer = 43,
        BurnContainer = 44,
        ClearContainer = 45,
        WorldClientStateUpdate = 46,
        EntityCreate = 47,
        EntityUpdate = 48,
        EntityDestroy = 49,
		HitRequest = 50,
        DamageRequest = 51,
		DamageNotification = 52,
		CallScriptedEntity = 53,
        UpdateWorldProperties = 54,
        Heartbeat = 55
    }
}
