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

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace StarDotNet
{
    public static class StarNetPlayerExtensions
    {
        #region Regular Expressions

        // [ADMIN COMMAND] [ERROR] player Koderzzzzzzzzzzzzzz not online, and no offline save state found
        private static readonly Regex RegexNoPlayerFound = new Regex(@"^\[ADMIN COMMAND\] \[ERROR\] player (?:.*) not online, and no offline save state found$", StarNetHelpersCommon.InternalRegexOptions);

        // [PL] LOGIN: [time=Thu Mar 19 00:20:30 EDT 2015, ip=/0.0.0.0, starmadeName=Koderz]
        private static readonly Regex RegexPlayerLogin = new Regex(@"^\[PL\] LOGIN: \[time=(?<time>[A-Za-z0-9: ]*), ip=/(?<ipAddress>[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}), starmadeName=(?<accountName>.*)\]$", StarNetHelpersCommon.InternalRegexOptions);

        // [PL] CONTROLLING-POS: (0.0, 0.0, 0.0)
        private static readonly Regex RegexPlayerControllingPosition = new Regex(@"^\[PL\] CONTROLLING-POS: \((?<xPos>[0-9\.\-E\+]*), (?<yPos>[0-9\.\-E\+]*), (?<zPos>[0-9\.\-E\+]*)\)$", StarNetHelpersCommon.InternalRegexOptions);

        // [PL] CONTROLLING: SpaceStation[ENTITY_SPACESTATION_SpawnStation_5_5_40(54)]
        // [PL] CONTROLLING: PlayerCharacter[(ENTITY_PLAYERCHARACTER_Koderz)(2886)]
        // [PL] CONTROLLING: Ship[Koderz_1426799715759](15989)
        // [PL] CONTROLLING: Planet(903)[s894]Planet  (r35)[10000000hp]
        private static readonly Regex RegexPlayerControlling = new Regex(@"^\[PL\] CONTROLLING: (?:(?<controllingType>SpaceStation|PlayerCharacter|Ship)\[\(?(?<controlling>[^\]\(\)]*)\)?(?:\([0-9]*\))?\](?:\([0-9]*\))?|(?<controllingType>Planet)\((?<controlling>[0-9]*)\)\[.*\]Planet\s*\(.*\)\[.*\])$", StarNetHelpersCommon.InternalRegexOptions);

        //[PL] SECTOR: (5, 5, 40)
        private static readonly Regex RegexPlayerSector = new Regex(@"^\[PL\] SECTOR: \((?<xSector>[0-9\-]*), (?<ySector>[0-9\-]*), (?<zSector>[0-9\-]*)\)$", StarNetHelpersCommon.InternalRegexOptions);

        //[PL] FACTION: Faction [id=10000, name=Red Shift, description=The server faction that controls server owned zones/entities!, size: 5; FP: 2147483647]
        private static readonly Regex RegexPlayerFaction = new Regex(@"^\[PL\] FACTION: (?<faction>(?:Faction \[id=(?<factionId>.*), name=(?<factionName>.*), description=(?<description>(?:.|\n|\r)*), size: (?<size>[0-9]*); FP: (?<factionPoints>[0-9]*)\])|null)$", StarNetHelpersCommon.InternalRegexOptions);

        //[PL] CREDITS: 327025577
        private static readonly Regex RegexPlayerCredits = new Regex(@"^\[PL\] CREDITS: (?<credits>[0-9]*)$", StarNetHelpersCommon.InternalRegexOptions);

        //[PL] UPGRADED: true
        private static readonly Regex RegexPlayerUpgraded = new Regex(@"^\[PL\] UPGRADED: (?<upgraded>false|true)$", StarNetHelpersCommon.InternalRegexOptions);

        //[PL] SM-NAME: Koderz
        private static readonly Regex RegexPlayerAccount = new Regex(@"^\[PL\] SM-NAME: (?<accountName>.*)$", StarNetHelpersCommon.InternalRegexOptions);

        //[PL] IP: /123.123.123.123
        private static readonly Regex RegexPlayerIp = new Regex(@"^\[PL\] IP: /?(?<ipAddress>(?:[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})|null)$", StarNetHelpersCommon.InternalRegexOptions);

        //[PL] Name: Koderz
        private static readonly Regex RegexPlayerName = new Regex(@"^\[PL\] Name: (?<playerName>.*)$", StarNetHelpersCommon.InternalRegexOptions);

        #endregion Regular Expressions

        private static PlayerInfo ParsePlayer(string[] response, ref int currentLine)
        {
            Match match;
            var logins = new List<PlayerLogin>();

            // Get all player logins
            while ((match = RegexPlayerLogin.Match(response[currentLine])).Success)
            {
                logins.Add(new PlayerLogin
                {
                    LoginTime = match.Groups["time"].Value,
                    IpAddress = IPAddress.Parse(match.Groups["ipAddress"].Value),
                    StarMadeName = match.Groups["accountName"].Value.ClearStringNull(),
                });

                // Increment the current line;
                currentLine++;
            }

            var playerInfo = new PlayerInfo { Logins = logins };

            // Check if this line is the sector (this happens if the player is in the spawn screen)
            if (response[currentLine].IndexOf("SECTOR", StringComparison.InvariantCultureIgnoreCase) < 0)
            {
                // Get the controlling position
                match = RegexPlayerControllingPosition.Match(response[currentLine++]);
                StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match,
                    "[PL] CONTROLLING-POS: ({{positionX}}, {{positionY}}, {{positionZ}})", response[currentLine - 1]);
                playerInfo.Position = new Vector3<float>(float.Parse(match.Groups["xPos"].Value),
                    float.Parse(match.Groups["yPos"].Value), float.Parse(match.Groups["zPos"].Value));

                // Get the controlling type and id
                match = RegexPlayerControlling.Match(response[currentLine++]);
                StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match,
                    "[PL] CONTROLLING: {{ControllingType}}[{{ControllingID}}(0)]", response[currentLine - 1]);
                playerInfo.ControllingId = match.Groups["controlling"].Value;
                playerInfo.Controlling =
                    (PlayerControlling)Enum.Parse(typeof(PlayerControlling), match.Groups["controllingType"].Value);
            }

            // Get player sector
            match = RegexPlayerSector.Match(response[currentLine++]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "[PL] SECTOR: ({{positionX}}, {{positionY}}, {{positionZ}})", response[currentLine - 1]);
            playerInfo.Sector = new Vector3<int>(int.Parse(match.Groups["xSector"].Value),
                int.Parse(match.Groups["ySector"].Value), int.Parse(match.Groups["zSector"].Value));

            // Get player faction
            match = RegexPlayerFaction.Match(response[currentLine++]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match,
                "[PL] FACTION: Faction [id={{factionId}}, name={{factionName}}, description={{factionDescription}}, size: {{factionSize}}; FP: {{factionPoints}}]",
                response[currentLine - 1]);
            if (!match.Groups["faction"].Value.IsStringNull())
            {
                playerInfo.Faction = new PlayerFaction
                {
                    Id = int.Parse(match.Groups["factionId"].Value),
                    Name = match.Groups["factionName"].Value,
                    Description = match.Groups["description"].Value.ClearStringNull(),
                    Size = int.Parse(match.Groups["size"].Value),
                    FactionPoints = int.Parse(match.Groups["factionPoints"].Value)
                };
            }

            // Get player credits
            match = RegexPlayerCredits.Match(response[currentLine++]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "[PL] CREDITS: {{Credits}}", response[currentLine - 1]);
            playerInfo.Credits = int.Parse(match.Groups["credits"].Value);

            // Get upgraded
            match = RegexPlayerUpgraded.Match(response[currentLine++]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "[PL] UPGRADED: {{Upgraded}}", response[currentLine - 1]);
            playerInfo.AccountUpgraded = bool.Parse(match.Groups["upgraded"].Value);

            // Get StarMade Name
            match = RegexPlayerAccount.Match(response[currentLine++]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "[PL] SM-NAME: {{Account}}", response[currentLine - 1]);
            playerInfo.StarMadeName = match.Groups["accountName"].Value.ClearStringNull();

            // Get player ip
            match = RegexPlayerIp.Match(response[currentLine++]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "[PL] IP: /{{Account}}", response[currentLine - 1]);
            if (!match.Groups["ipAddress"].Value.IsStringNull())
                playerInfo.LastIpAddress = IPAddress.Parse(match.Groups["ipAddress"].Value);

            // Get player name
            match = RegexPlayerName.Match(response[currentLine++]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "[PL] Name: {{name}}", response[currentLine - 1]);
            playerInfo.Name = match.Groups["playerName"].Value;

            return playerInfo;
        }

        /// <summary>
        /// Executes the /player_info command for a supplied player
        /// </summary>
        /// <returns>Information about the player or null if the player didn't exist</returns>
        public static PlayerInfo PlayerInfo(this StarNet.IStarNetSession session, string playerName)
        {
            // Get the command response
            string[] response = session.ExecuteAdminCommand("/player_info " + playerName);

            // Make sure we got a response
            if (response == null || response.Length <= 0)
                throw new InvalidOperationException("No response to command.");

            // Check if the command failed.
            if (StarNetHelpersCommon.AdminCommandFailed.IsMatch(response[0]))
                throw new AdminCommandFailedException("/player_info " + playerName);

            // Was the player not found?
            if (RegexNoPlayerFound.IsMatch(response[0]))
                return null;

            int currentLine = 0;

            // Parse and return the player
            return ParsePlayer(response, ref currentLine);
        }

        /// <summary>
        /// Executs the /player_list command and returns the list of players and information about each of them.
        /// </summary>
        /// <returns>List of players and their associated information</returns>
        public static PlayerInfo[] PlayerList(this StarNet.IStarNetSession session)
        {
            // Get the command response
            string[] response = session.ExecuteAdminCommand("/player_list");

            // Make sure we got a response
            if (response == null || response.Length <= 0)
                throw new InvalidOperationException("No response to command.");

            // Check if the command failed.
            if (StarNetHelpersCommon.AdminCommandFailed.IsMatch(response[0]))
                throw new AdminCommandFailedException("/player_list");

            // Was the player not found?
            if (RegexNoPlayerFound.IsMatch(response[0]))
                return null;

            var players = new List<PlayerInfo>();
            int currentLine = 0;

            // Loop and parse all the players.
            while (currentLine < response.Length)
            {
                players.Add(ParsePlayer(response, ref currentLine));
            }

            return players.ToArray();
        }
    }

    /// <summary>
    /// Contains information about a player
    /// </summary>
    public class PlayerInfo
    {
        /// <summary>
        /// Player name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Most current IP address the player logged in from
        /// </summary>
        public IPAddress LastIpAddress { get; set; }

        /// <summary>
        /// StarMade uplink name
        /// </summary>
        public string StarMadeName { get; set; }

        /// <summary>
        /// Whether the player owns the game.
        /// </summary>
        public bool AccountUpgraded { get; set; }

        /// <summary>
        /// Players credits
        /// </summary>
        public int Credits { get; set; }

        /// <summary>
        /// Information about the players faction
        /// </summary>
        public PlayerFaction Faction { get; set; }

        /// <summary>
        /// Sector coordinates of the player
        /// </summary>
        public Vector3<int> Sector { get; set; }

        /// <summary>
        /// Type of entity the player is controlling
        /// </summary>
        public PlayerControlling Controlling { get; set; }

        /// <summary>
        /// UID of the entity the player is controlling
        /// </summary>
        public string ControllingId { get; set; }

        /// <summary>
        /// Position within the sector of the player of the entity they're controlling
        /// </summary>
        public Vector3<float> Position { get; set; }

        /// <summary>
        /// List of recent login details
        /// </summary>
        public List<PlayerLogin> Logins { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Type of entity
    /// </summary>
    public enum PlayerControlling
    {
        Unknown,
        PlayerCharacter,
        Ship,
        SpaceStation,
        Planet,
    }

    /// <summary>
    /// Defines a player login including the time, ip address and starmade account
    /// </summary>
    public class PlayerLogin
    {
        /// <summary>
        /// Time the player connected
        /// </summary>
        public string LoginTime { get; set; }

        /// <summary>
        /// IP address of the player
        /// </summary>
        public IPAddress IpAddress { get; set; }

        /// <summary>
        /// Players StarMade registry uplink account.
        /// </summary>
        public string StarMadeName { get; set; }

        public override string ToString()
        {
            return string.Format("LOGIN: [LoginTime:{0}, IpAddress:{1}, StarMadeName:{2}]", LoginTime, IpAddress, StarMadeName);
        }
    }

    /// <summary>
    /// Contains information about a players faction
    /// </summary>
    public class PlayerFaction
    {
        /// <summary>
        /// Faction id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Faction name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Faction description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Number of players in the faction
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Current points for the faction
        /// </summary>
        public int FactionPoints { get; set; }

        public override string ToString()
        {
            return string.Format("Faction: [Id:{0}, Name:{1}, Size:{2}, FactionPoints:{3}, Description:{4}]", Id, Name,
                Size, FactionPoints, Description);
        }
    }
}