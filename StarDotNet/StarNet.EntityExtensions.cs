#region License

// Copyright (c) 2015 Koderz ( http://koderz.me/ )
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#endregion License

#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StarDotNet
{
    public static class StarNetEntityExtensions
    {
        // [ADMIN COMMAND] [ERROR] Sector (34234, 2342342, 23423423) not in database
        private static readonly Regex RegexNoPlayerFound = new Regex(@"^\[ADMIN COMMAND\] \[ERROR\] Sector \((?<sectorX>[0-9\-]*), (?<sectorY>[0-9\-]*), (?<sectorZ>[0-9\-]*)\) not in database$", StarNetHelpersCommon.InternalRegexOptions);

        //[SERVER, LoadedEntity [uid=ENTITY_PLAYERCHARACTER_Koderz, type=Astronaut], 0]
        private static readonly Regex RegexSectorInfoPlayer = new Regex(@"^(?:LoadedEntity|DatabaseEntry) \[uid=ENTITY_PLAYERCHARACTER_(?<playerName>.*), type=Astronaut\]$", StarNetHelpersCommon.InternalRegexOptions);

        //[SERVER, LoadedEntity [uid=ENTITY_PLANETCORE_60_10_104, type=Planet Core], 0]
        private static readonly Regex RegexSectorInfoPlanetCore = new Regex(@"^(?:LoadedEntity|DatabaseEntry) \[uid=ENTITY_PLANETCORE_(?<planetCoreUid>.*), type=Planet Core\]$", StarNetHelpersCommon.InternalRegexOptions);

        private static readonly Regex RegexSectorInfoCreature = new Regex(@"^(?:LoadedEntity|DatabaseEntry) \[uid=ENTITY_CREATURE_(?<creatureUid>.*), type=NPC\]$", StarNetHelpersCommon.InternalRegexOptions);

        //[SERVER, LoadedEntity [uid=ENTITY_SHIP_Njord_1428957916093, type=Ship, seed=0, lastModifier=, spawner=ENTITY_PLAYERSTATE_Njord, realName=Njord_1428957916093, touched=true, faction=0, pos=(-72.59979, -0.28221005, 63.872917), minPos=(-2, -2, -2), maxPos=(2, 2, 2), creatorID=0], 0]
        //[SERVER, LoadedEntity [uid=ENTITY_SPACESTATION_SpawnStation, type=Space Station, seed=0, lastModifier=, spawner=, realName=SpawnStation, touched=true, faction=10000, pos=(0.0, 0.0, 0.0), minPos=(-14, -3, -14), maxPos=(14, 2, 14), creatorID=1], 0]
        private static readonly Regex RegexSectorInfoEntity = new Regex(@"^(?:LoadedEntity|DatabaseEntry) \[uid=ENTITY_(?:SHIP|SPACESTATION|SHOP|PLANET|FLOATINGROCK)_(?<entityUID>[^,]*), (?:sectorPos=\([0-9\-]*, [0-9\-]*, [0-9\-]*\), )?type=(?<entityType>Ship|Space Station|Shop|Planet Segment|Asteroid|[0-9]*), seed=[0-9\-]*, lastModifier=(?:ENTITY_PLAYER(?:STATE|CHARACTER)_)?(?<lastModifier>.*), spawner=(?:ENTITY_PLAYER(?:STATE|CHARACTER)_)?(?<spawner>.*), realName=(?<entityName>.*), touched=(?<touched>true|false), faction=(?<factionId>[0-9\-]*), pos=\((?<positionX>[0-9\.\-]*), (?<positionY>[0-9\.\-]*), (?<positionZ>[0-9\.\-]*)\), minPos=\((?<minPositionX>[0-9\-]*), (?<minPositionY>[0-9\-]*), (?<minPositionZ>[0-9\-]*)\), maxPos=\((?<maxPositionX>[0-9\-]*), (?<maxPositionY>[0-9\-]*), (?<maxPositionZ>[0-9\-]*)\), creatorID=(?<creatorID>[0-9\-]*)\]$", StarNetHelpersCommon.InternalRegexOptions);

        //[SERVER, LOADED SECTOR INFO: Sector[87](5, 5, 40); Protected: true; Peace: true; Seed: -4159515285743916928; Type: ASTEROID;, 0]
        private static readonly Regex RegexSectorInfo = new Regex(@"^(?:(?<loaded>LOADED) )?SECTOR INFO: Sector\[(?<sectorID>[0-9\-]*)\]\((?<sectorX>[0-9\-]*), (?<sectorY>[0-9\-]*), (?<sectorZ>[0-9\-]*)\); Protected: (?<protected>true|false); Peace: (?<peace>true|false); Seed: (?<seed>[0-9\-]*); Type: (?<sectorType>.*);$", StarNetHelpersCommon.InternalRegexOptions);

        //UID Not Found in DB: ENTITY_SHIP_Koderz; checking unsaved objects
        private static readonly Regex RegexEntityInfoNotFoundInDb = new Regex(@"^UID Not Found in DB: (?<entity>.*); checking unsaved objects$", StarNetHelpersCommon.InternalRegexOptions);

        //UID also not found in unsaved objects
        private static readonly Regex RegexEntityInfoNotUnsaved = new Regex(@"^UID also not found in unsaved objects$", StarNetHelpersCommon.InternalRegexOptions);

        //Loaded: true
        private static readonly Regex RegexEntityInfoLoaded = new Regex(@"^Loaded: (?<loaded>true|false)$", StarNetHelpersCommon.InternalRegexOptions);

        //Attached: [PlS[Koderz [Koderz]*; id(86)(2)f(10000)], PlS[Kodeeeee ; id(30289)(89)f(0)]]
        private static readonly Regex RegexEntityInfoAttached = new Regex(@"^Attached: \[(?<attachedPlayers>.*)\]$", StarNetHelpersCommon.InternalRegexOptions);

        //[PlS[Koderz [Koderz]*; id(86)(2)f(10000)], PlS[Kodeeeee ; id(30289)(89)f(0)]]
        private static readonly Regex RegexEntityInfoAttachedParser = new Regex(@"PlS\[(?<playerName>[^;\[]*) (?:\[(?<smName>.*)\]\*)?; id\([0-9\-]*\)\([0-9\-]*\)f\([0-9\-]*\)\]", StarNetHelpersCommon.InternalRegexOptions);

        //DockedUIDs: ENTITY_SHIP_SpruceTree_1428717569087, ENTITY_SHIP_Chaospainter813_1428520723844,
        private static readonly Regex RegexEntityInfoDocked = new Regex(@"^DockedUIDs: (?<dockedUIDs>.*)$", StarNetHelpersCommon.InternalRegexOptions);

        //ENTITY_SHIP_SpruceTree_1428717569087, ENTITY_SHIP_Chaospainter813_1428520723844,
        private static readonly Regex RegexEntityInfoDockedParser = new Regex(@"ENTITY_SHIP_(?<entityName>[^,]*),", StarNetHelpersCommon.InternalRegexOptions);

        //Blocks: 455208
        private static readonly Regex RegexEntityInfoBlocks = new Regex(@"^Blocks: (?<blocks>[0-9\-]*)$", StarNetHelpersCommon.InternalRegexOptions);

        //Mass: 0.0
        private static readonly Regex RegexEntityInfoMass = new Regex(@"^Mass: (?<mass>[0-9\-\.]*)$", StarNetHelpersCommon.InternalRegexOptions);

        //Sector: 87 -> Sector[87](5, 5, 40)
        private static readonly Regex RegexEntityInfoSector = new Regex(@"^Sector: [0-9\-]* -> Sector\[[0-9\-]*\]\((?<sectorX>[0-9\-]*), (?<sectorY>[0-9\-]*), (?<sectorZ>[0-9\-]*)\)$", StarNetHelpersCommon.InternalRegexOptions);

        //Orientation: (0.0, 0.0, 0.0, 1.0)
        private static readonly Regex RegexEntityInfoOrientation = new Regex(@"^Orientation: \((?<orientationX>[0-9\-\.]*), (?<orientationY>[0-9\-\.]*), (?<orientationZ>[0-9\-\.]*), (?<orientationW>[0-9\-\.]*)\)$", StarNetHelpersCommon.InternalRegexOptions);

        // ReSharper disable once FunctionComplexityOverflow
        public static EntityInfoExtended GetEntityInfo(this StarNet.IStarNetSession session, EntityType type, string uid)
        {
            // Get the command response
            string[] response = session.ExecuteAdminCommand("/ship_info_uid \"" + StarNet.CreateFullEntityUid(type, uid) + "\"");

            // Make sure we got a response
            if (response == null || response.Length <= 0)
                throw new InvalidOperationException("No response to command.");

            // Check if the command failed.
            if (StarNetHelpersCommon.AdminCommandFailed.IsMatch(response[0]))
                throw new AdminCommandFailedException("/ship_info_uid \"" + StarNet.CreateFullEntityUid(type, uid) + "\"");

            // Was the entity not found?
            if (RegexEntityInfoNotFoundInDb.IsMatch(response[0]) && (response.Length <= 1 || RegexEntityInfoNotUnsaved.IsMatch(response[1])))
                return null;

            var entityInfo = new EntityInfoExtended();

            // Get Loaded
            Match match = RegexEntityInfoLoaded.Match(response[0]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Loaded: (true|false)", response[0]);
            entityInfo.Loaded = bool.Parse(match.Groups["loaded"].Value);

            // Get general information
            match = RegexSectorInfoEntity.Match(response[1]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Entity [uid= ... ]", response[1]);
            entityInfo.Uid = match.Groups["entityUID"].Value;
            entityInfo.Type = GetEntityTypeFromString(match.Groups["entityType"].Value);
            entityInfo.LastModifiedBy = match.Groups["lastModifier"].Value;
            entityInfo.Creator = match.Groups["spawner"].Value;
            entityInfo.RealName = match.Groups["entityName"].Value;
            entityInfo.Touched = bool.Parse(match.Groups["touched"].Value);
            entityInfo.Faction = int.Parse(match.Groups["factionId"].Value);
            entityInfo.Position = new Vector3<float>(float.Parse(match.Groups["positionX"].Value), float.Parse(match.Groups["positionY"].Value), float.Parse(match.Groups["positionZ"].Value));
            entityInfo.MinChunkPosition = new Vector3<int>(int.Parse(match.Groups["minPositionX"].Value), int.Parse(match.Groups["minPositionY"].Value), int.Parse(match.Groups["minPositionZ"].Value));
            entityInfo.MaxChunkPosition = new Vector3<int>(int.Parse(match.Groups["maxPositionX"].Value), int.Parse(match.Groups["maxPositionY"].Value), int.Parse(match.Groups["maxPositionZ"].Value));
            entityInfo.CreatorId = int.Parse(match.Groups["creatorID"].Value);

            // Early out for entities like shops that don't have the rest of the information.
            if (response[2].Equals("Admin command execution ended", StringComparison.InvariantCultureIgnoreCase))
                return entityInfo;

            // Get attached
            match = RegexEntityInfoAttached.Match(response[2]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Attached: [...]", response[2]);
            string attached = match.Groups["attachedPlayers"].Value;
            if (!string.IsNullOrEmpty(attached))
            {
                MatchCollection matches = RegexEntityInfoAttachedParser.Matches(attached);

                entityInfo.AttachedPlayers = (from Match attachedPerson in matches select attachedPerson.Groups["playerName"].Value).ToArray();
            }
            else
            {
                entityInfo.AttachedPlayers = new string[0];
            }

            // Get docked
            match = RegexEntityInfoDocked.Match(response[3]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Attached: [...]", response[3]);
            string docked = match.Groups["dockedUIDs"].Value;
            if (!string.IsNullOrEmpty(docked))
            {
                MatchCollection matches = RegexEntityInfoDockedParser.Matches(docked);

                entityInfo.DockedShips = (from Match dockedShip in matches select dockedShip.Groups["entityName"].Value).ToArray();
            }
            else
            {
                entityInfo.DockedShips = new string[0];
            }

            // Get blocks
            match = RegexEntityInfoBlocks.Match(response[4]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Blocks: 97987", response[4]);
            entityInfo.BlockCount = int.Parse(match.Groups["blocks"].Value);

            // Get mass
            match = RegexEntityInfoMass.Match(response[5]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Mass: 9798.7", response[5]);
            entityInfo.Mass = float.Parse(match.Groups["mass"].Value);

            // Get Sector
            match = RegexEntityInfoSector.Match(response[8]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Sector: 10 => Sector[10](x, y, z)", response[8]);
            entityInfo.Sector = new Vector3<int>(int.Parse(match.Groups["sectorX"].Value), int.Parse(match.Groups["sectorY"].Value), int.Parse(match.Groups["sectorZ"].Value));

            // Get orientation
            match = RegexEntityInfoOrientation.Match(response[15]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Orientation: (x, y, z, w)", response[15]);
            entityInfo.Orientation = new Vector4<float>(float.Parse(match.Groups["orientationX"].Value), float.Parse(match.Groups["orientationY"].Value), float.Parse(match.Groups["orientationZ"].Value), float.Parse(match.Groups["orientationW"].Value));

            return entityInfo;
        }

        public static SectorInfo GetSectorInfo(this StarNet.IStarNetSession session, int x, int y, int z)
        {
            // Get the command response
            string[] response = session.ExecuteAdminCommand("/sector_info " + x + " " + y + " " + z);

            // Make sure we got a response
            if (response == null || response.Length <= 0)
                throw new InvalidOperationException("No response to command.");

            // Check if the command failed.
            if (StarNetHelpersCommon.AdminCommandFailed.IsMatch(response[0]))
                throw new AdminCommandFailedException("/sector_info " + x + " " + y + " " + z);

            // Was the sector not found?
            if (RegexNoPlayerFound.IsMatch(response[0]))
                return null;

            var entities = new List<EntityInfoBasic>();
            var players = new List<string>();
            var creatures = new List<string>();
            bool hasPlanetCore = false;

            Match match;
            int counter = 0;
            while (true)
            {
                if ((match = RegexSectorInfoPlayer.Match(response[counter])).Success)
                {
                    players.Add(match.Groups["playerName"].Value);
                }
                else if ((match = RegexSectorInfoEntity.Match(response[counter])).Success)
                {
                    entities.Add(new EntityInfoBasic
                    {
                        Uid = match.Groups["entityUID"].Value,
                        Type = GetEntityTypeFromString(match.Groups["entityType"].Value),
                        LastModifiedBy = match.Groups["lastModifier"].Value,
                        Creator = match.Groups["spawner"].Value,
                        RealName = match.Groups["entityName"].Value,
                        Touched = bool.Parse(match.Groups["touched"].Value),
                        Faction = int.Parse(match.Groups["factionId"].Value),
                        Position = new Vector3<float>(float.Parse(match.Groups["positionX"].Value), float.Parse(match.Groups["positionY"].Value), float.Parse(match.Groups["positionZ"].Value)),
                        MinChunkPosition = new Vector3<int>(int.Parse(match.Groups["minPositionX"].Value), int.Parse(match.Groups["minPositionY"].Value), int.Parse(match.Groups["minPositionZ"].Value)),
                        MaxChunkPosition = new Vector3<int>(int.Parse(match.Groups["maxPositionX"].Value), int.Parse(match.Groups["maxPositionY"].Value), int.Parse(match.Groups["maxPositionZ"].Value)),
                        CreatorId = int.Parse(match.Groups["creatorID"].Value),
                        Sector = new Vector3<int>(x, y, z)
                    });
                }
                else if (RegexSectorInfoPlanetCore.IsMatch(response[counter]))
                {
                    hasPlanetCore = true;
                }
                else if ((match = RegexSectorInfoCreature.Match(response[counter])).Success)
                {
                    players.Add(match.Groups["creatureUid"].Value);
                }
                else
                {
                    break;
                }
                counter++;
            }

            match = RegexSectorInfo.Match(response[counter]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Entity [uid= ... ]", response[counter]);
            if (match.Success)
            {
                return new SectorInfo
                {
                    Loaded = match.Groups["loaded"].Value.Equals("LOADED", StringComparison.InvariantCultureIgnoreCase),
                    Type = GetSectorTypeFromString(match.Groups["sectorType"].Value),
                    HasPlanetCore = hasPlanetCore,
                    SectorId = int.Parse(match.Groups["sectorID"].Value),
                    SectorCoordinates = new Vector3<int>(int.Parse(match.Groups["sectorX"].Value), int.Parse(match.Groups["sectorY"].Value), int.Parse(match.Groups["sectorZ"].Value)),
                    Protected = bool.Parse(match.Groups["protected"].Value),
                    Peace = bool.Parse(match.Groups["peace"].Value),
                    Seed = long.Parse(match.Groups["seed"].Value),
                    Entities = entities.ToArray(),
                    Astronauts = players.ToArray(),
                    Creatures = creatures.ToArray()
                };
            }

            return null;
        }

        private static EntityType GetEntityTypeFromString(string name)
        {
            int type;
            if (int.TryParse(name, out type))
                return (EntityType)type;

            if (name.Equals("Ship", StringComparison.InvariantCultureIgnoreCase))
                return EntityType.Ship;
            if (name.Equals("Asteroid", StringComparison.InvariantCultureIgnoreCase))
                return EntityType.Asteroid;
            if (name.Equals("Space Station", StringComparison.InvariantCultureIgnoreCase))
                return EntityType.SpaceStation;
            if (name.Equals("Shop", StringComparison.InvariantCultureIgnoreCase))
                return EntityType.Shop;
            if (name.Equals("Planet Segment", StringComparison.InvariantCultureIgnoreCase))
                return EntityType.PlanetSegment;

            return EntityType.Unknown;
        }

        private static SectorType GetSectorTypeFromString(string type)
        {
            if (type.Equals("ASTEROID", StringComparison.InvariantCultureIgnoreCase))
                return SectorType.Asteroid;
            if (type.Equals("VOID", StringComparison.InvariantCultureIgnoreCase))
                return SectorType.Void;
            if (type.Equals("PLANET", StringComparison.InvariantCultureIgnoreCase))
                return SectorType.Planet;
            if (type.Equals("BLACK_HOLE", StringComparison.InvariantCultureIgnoreCase))
                return SectorType.BlackHole;
            if (type.Equals("SUN", StringComparison.InvariantCultureIgnoreCase))
                return SectorType.Star;
            if (type.Equals("MAIN", StringComparison.InvariantCultureIgnoreCase))
                return SectorType.Main;

            return SectorType.Unknown;
        }
    }

    [Serializable]
    public class SectorInfo
    {
        public bool Loaded { get; set; }

        public SectorType Type { get; set; }

        public bool HasPlanetCore { get; set; }

        public int SectorId { get; set; }

        public Vector3<int> SectorCoordinates { get; set; }

        public bool Protected { get; set; }

        public bool Peace { get; set; }

        public long Seed { get; set; }

        public EntityInfoBasic[] Entities { get; set; }

        public string[] Astronauts { get; set; }

        public string[] Creatures { get; set; }
    }

    [Serializable]
    public class EntityInfoBasic
    {
        public string Uid { get; set; }

        public EntityType Type { get; set; }

        public string LastModifiedBy { get; set; }

        public string Creator { get; set; }

        public string RealName { get; set; }

        public bool Touched { get; set; }

        public int Faction { get; set; }

        public Vector3<float> Position { get; set; }

        public Vector3<int> MinChunkPosition { get; set; }

        public Vector3<int> MaxChunkPosition { get; set; }

        public int CreatorId { get; set; }

        public Vector3<int> Sector { get; set; }
    }

    [Serializable]
    public class EntityInfoExtended : EntityInfoBasic
    {
        public bool Loaded { get; set; }

        public string[] AttachedPlayers { get; set; }

        public string[] DockedShips { get; set; }

        public int BlockCount { get; set; }

        public float Mass { get; set; }

        public Vector4<float> Orientation { get; set; }
    }

    public enum SectorType
    {
        Unknown = 0,
        Asteroid = 1,
        Star = 2,
        Planet = 3,
        BlackHole = 4,
        Void = 5,
        Main = 6
    }

    public enum EntityType
    {
        Unknown = 0,
        Ship = 5,
        SpaceStation = 2,
        Shop = 1,
        Asteroid = 3,
        PlanetSegment = 4
    }
}
#pragma warning restore 1591