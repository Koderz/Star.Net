using System;
using System.IO;

namespace StarDotNet.Tests
{
    internal class Program
    {
        // Connection info to use for testing.
        private const string Address = "localhost";

        private const ushort Port = 4242;
        private const string Password = "mypassword";

        private static void Main(string[] args)
        {
            // Get the server info and display
            StarNet.ServerInfo serverInfo = StarNet.GetServerInfo(Address, Port);
            Console.WriteLine("Server: [Version: {0}, Players: {1}/{2}]", serverInfo.Version, serverInfo.PlayerCount, serverInfo.MaxPlayers);

            // Execute the /status command as a raw command and desplay
            string[] response = StarNet.ExecuteAdminCommand(Address, Port, Password, "/status");
            Console.WriteLine();
            Console.WriteLine("Raw command: /status");
            using (var writer = new StreamWriter("Output.txt"))
            {
                foreach (var line in response)
                {
                    Console.WriteLine(line);
                    writer.WriteLine(line);
                }
            }
            Console.WriteLine();
            Console.WriteLine();

            // Create a session
            using (var session = StarNet.CreateSession(Address, Port, Password))
            {
                // Get the player list and display
                var playerList = session.PlayerList();
                Console.WriteLine("Players:");
                foreach (var player in playerList)
                {
                    Console.Write(player + ", ");
                }
                Console.WriteLine();
                Console.WriteLine();

                // Test player info for Koderz
                var playerInfo = session.PlayerInfo("Koderz");
                Console.WriteLine(playerInfo);

                // Test server status
                var serverStatus = session.Status();
                Console.WriteLine("Players [{0}/{1}]  Memory(Free/Taken/Total)[{2}, {3}, {4}]", serverStatus.CurrentPlayers, serverStatus.MaxPlayers, serverStatus.MemoryFree, serverStatus.MemoryTaken, serverStatus.MemoryTotal);
            }

            Console.ReadLine();
        }
    }
}