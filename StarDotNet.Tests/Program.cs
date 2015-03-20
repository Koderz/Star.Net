using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StarDotNet.Tests
{
    class Program
    {
        // Connection info to use for testing.
        private const string Address = "localhost";
        private const ushort Port = 4242;
        private const string Password = "mypassword";


        static void Main(string[] args)
        {



            // Get the server info and display
            var serverInfo = StarNet.GetServerInfo(Address, Port);
            Console.WriteLine("Server: [Version: {0}, Players: {1}/{2}]", serverInfo.Version, serverInfo.PlayerCount, serverInfo.MaxPlayers);


            // Execute the /status command as a raw command and desplay
            var response = StarNet.ExecuteAdminCommand(Address, Port, Password, "/status");
            Console.WriteLine("/status");
            foreach(var line in response)
            {
                Console.WriteLine(line);
            }
            Console.WriteLine();
            Console.WriteLine();


            // Create a session
            using (var session = StarNet.CreateSession(Address, Port, Password))
            {

                // Get the player list and display
                var playerList = session.PlayerList();
                Console.WriteLine("Players:");
                foreach(var player in playerList)
                {
                    Console.Write(player + ", ");
                }
                Console.WriteLine();
                Console.WriteLine();
            }


            Console.ReadLine();
        }
    }
}
