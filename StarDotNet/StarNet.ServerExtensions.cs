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
using System.Text.RegularExpressions;

namespace StarDotNet
{
    public static class StarNetServerExtensions
    {
        //PhysicsInMem: 0; Rep: 9
        private static readonly Regex RegexStatusPhysicsInMem = new Regex(@"^PhysicsInMem: (?<physicsInMem>[0-9]*); Rep: (?<rep>[0-9]*)$", StarNetHelpersCommon.InternalRegexOptions);

        //Total queued NT Packages: -1037
        private static readonly Regex RegexStatusNetPackages = new Regex(@"^Total queued NT Packages: (?<queuedPackages>[0-9\-]*)$", StarNetHelpersCommon.InternalRegexOptions);

        //Loaded !empty Segs / free: 70622 / 7300
        private static readonly Regex RegexStatusSegments = new Regex(@"^Loaded !empty Segs / free: (?<notEmptySegs>[0-9\-]*) / (?<freeSegs>[0-9\-]*)$", StarNetHelpersCommon.InternalRegexOptions);

        //Loaded Objects: 1894
        private static readonly Regex RegexStatusLoadedObjects = new Regex(@"^Loaded Objects: (?<loadedObjects>[0-9\-]*)$", StarNetHelpersCommon.InternalRegexOptions);

        //Players: 32 / 100
        private static readonly Regex RegexStatusPlayers = new Regex(@"^Players: (?<currentPlayers>[0-9]*) / (?<maxPlayers>[0-9]*)$", StarNetHelpersCommon.InternalRegexOptions);

        //Mem (MB)[free, taken, total]: [3987, 4672, 8659]
        private static readonly Regex RegexStatusMemory = new Regex(@"^Mem \(MB\)\[free, taken, total\]: \[(?<freeMemory>[0-9\-]*), (?<takenMemory>[0-9\-]*), (?<totalMemory>[0-9\-]*)\]$", StarNetHelpersCommon.InternalRegexOptions);

        /// <summary>
        /// Executes the /status command for the server and returns the reply
        /// </summary>
        /// <returns>Server status information</returns>
        public static ServerStatus Status(this StarNet.IStarNetSession session)
        {
            // Get command response
            string[] response = session.ExecuteAdminCommand("/status");

            // Make sure we got a response
            if (response == null || response.Length <= 0)
                throw new InvalidOperationException("No response to command.");

            var status = new ServerStatus();
            Match match;

            // Get physics in memory
            match = RegexStatusPhysicsInMem.Match(response[0]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "PhysicsInMemory: {{physics}}; Rep: {{rep}}", response[0]);
            status.PhysicsInMemory = int.Parse(match.Groups["physicsInMem"].Value);
            status.Rep = int.Parse(match.Groups["rep"].Value);

            // Get queued nt packages
            match = RegexStatusNetPackages.Match(response[1]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Total queued NT Packages: {{queuedPackages}}", response[1]);
            status.TotalQueuedNTPackages = int.Parse(match.Groups["queuedPackages"].Value);

            // Get segments
            match = RegexStatusSegments.Match(response[2]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Loaded !empty Segs / free: {{notEmptySegs}} / {{freeSegs}}", response[2]);
            status.NotEmptySegments = int.Parse(match.Groups["notEmptySegs"].Value);
            status.FreeSegments = int.Parse(match.Groups["freeSegs"].Value);

            // Get loaded objects
            match = RegexStatusLoadedObjects.Match(response[3]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Loaded Objects: {{loadedObjects}}", response[3]);
            status.LoadedObjects = int.Parse(match.Groups["loadedObjects"].Value);

            // Get players
            match = RegexStatusPlayers.Match(response[4]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Players: {{currentPlayers}} / {{maxPlayers}}", response[4]);
            status.CurrentPlayers = int.Parse(match.Groups["currentPlayers"].Value);
            status.MaxPlayers = int.Parse(match.Groups["maxPlayers"].Value);

            // Get memory
            match = RegexStatusMemory.Match(response[5]);
            StarNetHelpersCommon.ThrowUnexpectedResponseIfNotSuccess(match, "Mem (MB)[free, taken, total]: [{{freeMemory}}, {{takenMemory}}, {{totalMemory}}]", response[5]);
            status.MemoryFree = int.Parse(match.Groups["freeMemory"].Value);
            status.MemoryTaken = int.Parse(match.Groups["takenMemory"].Value);
            status.MemoryTotal = int.Parse(match.Groups["totalMemory"].Value);

            return status;
        }
    }

    /// <summary>
    /// Contains state information about a running server.
    /// </summary>
    [Serializable]
    public class ServerStatus
    {
        public int PhysicsInMemory { get; set; }

        public int Rep { get; set; }

        // ReSharper disable once InconsistentNaming
        public int TotalQueuedNTPackages { get; set; }

        public int NotEmptySegments { get; set; }

        public int FreeSegments { get; set; }

        public int LoadedObjects { get; set; }

        public int CurrentPlayers { get; set; }

        public int MaxPlayers { get; set; }

        public int MemoryFree { get; set; }

        public int MemoryTaken { get; set; }

        public int MemoryTotal { get; set; }
    }
}
#pragma warning restore 1591