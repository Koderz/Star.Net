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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace StarDotNet
{
    /// <summary>
    /// Provides the ability to connect to a StarMade server and interact with it through admin commands or status queries.
    /// </summary>
    public static class StarNet
    {
        /// <summary>
        /// Used to specify the local IP Address to bind to.
        /// </summary>
        /// <remarks>
        /// Useful when the server is bound to a specific address. Any other time you can ignore this property.
        /// </remarks>
        public static IPAddress LocalBindIp = IPAddress.Any;

        /// <summary>
        /// Used to specify the timeouts for sending/receiving data.
        /// </summary>
        public static int SendReceiveTimeout = int.MaxValue;


        public static string CreateFullEntityUid(EntityType type, string uid)
        {
            switch (type)
            {
                case EntityType.Asteroid:
                    return "ENTITY_FLOATINGROCK_" + uid;
                case EntityType.PlanetSegment:
                    return "ENTITY_PLANET_" + uid;
                case EntityType.Ship:
                    return "ENTITY_SHIP_" + uid;
                case EntityType.Shop:
                    return "ENTITY_SHOP_" + uid;
                case EntityType.SpaceStation:
                    return "ENTITY_SPACESTATION_" + uid;
                default:
                    throw new ArgumentException("unknown type");
            }
        }



        /// <summary>
        /// Connects to the server and executes one admin command and then disconnects
        /// </summary>
        /// <remarks>
        /// Useful for single random queries. To execute many commands on the same server we recommend using a StarNetSession
        /// </remarks>
        /// <param name="host">The hostname of the server.</param>
        /// <param name="port">The port of the server.</param>
        /// <param name="password">The servers super-admin password (found in the server.cfg file)</param>
        /// <param name="command">The admin command to send (Example:  /player_list )</param>
        /// <returns>Each line of the response</returns>
        public static string[] ExecuteAdminCommand(string host, int port, string password, string command)
        {
            // Open a session and return the response
            using (IStarNetSession session = CreateSession(host, port, password))
            {
                return session.ExecuteAdminCommand(command);
            }
        }

        /// <summary>
        /// Query the server to find out the current state
        /// </summary>
        /// <param name="host">The hostname of the server.</param>
        /// <param name="port">The port of the server.</param>
        /// <returns>Information about the server</returns>
        public static ServerInfo GetServerInfo(string host, ushort port)
        {
            // Create the client and connect to the server
            using (var client = new TcpClient(new IPEndPoint(LocalBindIp, 0)))
            {
                client.ReceiveTimeout = client.SendTimeout = SendReceiveTimeout;

                // Open the connection
                client.Connect(host, port);
                
                using (NetworkStream stream = client.GetStream())
                {
                    // Set the read timeout to keep from getting invite requests
                    stream.ReadTimeout = stream.WriteTimeout = SendReceiveTimeout;

                    using (var writer = new BinaryWriter(stream))
                    {
                        // Get the start time (Simple round trip time calculation)
                        DateTime started = DateTime.Now;

                        // Write the packet length
                        writer.WriteBe(9);

                        // Write the header
                        (new Header(Header.CommandType.Ping, Header.PacketType.Parameterized)).Write(writer);

                        // Write the command parameters
                        WriteParameterizedCommand(writer);

                        // Flush the command (send it to the server)
                        writer.Flush();

                        using (var reader = new BinaryReader(stream))
                        {
                            // Size of this part of the packet (don't really need to use it)
                            reader.ReadInt32Be();

                            // Timestamp (don't really need to use it)
                            reader.ReadInt64Be();

                            // Reads the packet header (again we don't really need to use it)
                            Header.Read(reader);

                            // Read all the return values of this packet part
                            object[] returnValues = ReadParameters(reader);

                            // Calculate the round trip time
                            DateTime end = DateTime.Now;
                            long roundTripTime = (long)(end - started).TotalMilliseconds;

                            // Return the server info
                            return new ServerInfo(host, port, roundTripTime, (sbyte)returnValues[0], (float)returnValues[1], (string)returnValues[2], (string)returnValues[3],
                                (long)returnValues[4], (int)returnValues[5], (int)returnValues[6]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Contains information about the current state of a server
        /// </summary>
        [Serializable]
        public class ServerInfo
        {
            /// <summary>
            /// Hostname of the server
            /// </summary>
            public string Host { get; private set; }

            /// <summary>
            /// Port of the server
            /// </summary>
            public ushort Port { get; private set; }

            /// <summary>
            /// Round trip time to query the server
            /// </summary>
            public long RoundTripTime { get; private set; }

            /// <summary>
            /// ServerInfo version
            /// </summary>
            public sbyte InfoVersion { get; private set; }

            /// <summary>
            /// Server version
            /// </summary>
            public float Version { get; private set; }

            /// <summary>
            /// Name of the server (set in the server.cfg file)
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Description of the server (set in the server.cfg file)
            /// </summary>
            public string Description { get; private set; }

            /// <summary>
            /// When the server was started as a unix timestamp
            /// </summary>
            public long StartTime { get; private set; }

            /// <summary>
            /// Current player count on the server
            /// </summary>
            public int PlayerCount { get; private set; }

            /// <summary>
            /// Max allowed players on the server
            /// </summary>
            public int MaxPlayers { get; private set; }

            public ServerInfo(string host, ushort port, long roundTripTime, sbyte infoVersion, float version, string name, string description, long startTime, int playerCount, int maxPlayers)
            {
                Host = host;
                Port = port;
                RoundTripTime = roundTripTime;
                InfoVersion = infoVersion;
                Version = version;
                Name = name;
                Description = description;
                StartTime = startTime;
                PlayerCount = playerCount;
                MaxPlayers = maxPlayers;
            }
        }

        #region StarNetSession

        /// <summary>
        /// Creates a StarNet session with a specific server to allow using more than one admin command in quick succession.
        /// </summary>
        /// <param name="host">The hostname of the server.</param>
        /// <param name="port">The port of the server.</param>
        /// <param name="password">The servers super-admin password (found in the server.cfg file)</param>
        /// <returns>StarNetSession object which can be used to send admin commands to the server.</returns>
        public static IStarNetSession CreateSession(string host, int port, string password)
        {
            return new StarNetSession(host, port, password);
        }

        /// <summary>
        /// Defines a StarNetSession
        /// </summary>
        public interface IStarNetSession : IDisposable
        {
            /// <summary>
            /// Executes an admin command against the connected server.
            /// </summary>
            /// <param name="command">The admin command to send (Example:  /player_list )</param>
            /// <returns>Each line of the response</returns>
            string[] ExecuteAdminCommand(string command);
        }

        [Serializable]
        private sealed class StarNetSession : IStarNetSession
        {
            // The reason for implementing idisposable even those there's not really anything to dispose
            // is to support session based connections later once StarMade supports them without chaning the
            // api, and allowing the  use of   (using)   to control its lifespan.

            private readonly string host;
            private readonly int port;
            private readonly string password;

            public StarNetSession(string host, int port, string password)
            {
                this.host = host;
                this.port = port;
                this.password = password;
            }

            public string[] ExecuteAdminCommand(string command)
            {
                // Create the client and connect to the server
                using (var client = new TcpClient(new IPEndPoint(LocalBindIp, 0)))
                {
                    // Open the connection
                    client.Connect(host, port);

                    using (var stream = client.GetStream())
                    {
                        // Set the read timeout to keep from getting invite requests
                        stream.ReadTimeout = stream.WriteTimeout = SendReceiveTimeout;

                        // Open the writer
                        using (var writer = new BinaryWriter(stream))
                        {
                            // Get the byte length of the command and password + 2 bytes for each for the length
                            var commandPasswordLength = Encoding.UTF8.GetByteCount(command) +
                                                        Encoding.UTF8.GetByteCount(password) + 4;

                            // Calculate the full packet length
                            var packetLength = 5 + 4 + 2 + commandPasswordLength;

                            // Write the packet length
                            writer.WriteBe(packetLength);

                            // Write the header
                            (new Header(Header.CommandType.Command, Header.PacketType.Parameterized)).Write(writer);

                            // Write the command parameters
                            WriteParameterizedCommand(writer, password, command);

                            // Flush the command (send it to the server)
                            writer.Flush();

                            // Open the reader
                            using (var reader = new BinaryReader(client.GetStream()))
                            {
                                var response = new List<string>();

                                while (true)
                                {
                                    // Size of this part of the packet (don't really need to use it)
                                    reader.ReadInt32Be();

                                    // Timestamp (don't really need to use it)
                                    reader.ReadInt64Be();

                                    // Reads the packet header (again we don't really need to use it)
                                    Header.Read(reader);

                                    // Read all the return values of this packet part
                                    var returnValues = ReadParameters(reader);

                                    // Check if this is the end of the command response
                                    if (returnValues.Length >= 2 && returnValues[1].ToString().StartsWith("END;", StringComparison.InvariantCultureIgnoreCase))
                                        break;

                                    // Add the response line if it is one
                                    if (returnValues.Length >= 2)
                                        response.Add(returnValues[1].ToString());
                                }

                                return response.ToArray();
                            }
                        }
                    }
                }
            }

            // This is currently irrelevant (Only allows for use of the using statement)
            // If we can hold the connection to the server open, this may be used later.
            void IDisposable.Dispose()
            {
            }
        }

        #endregion StarNetSession

        #region Internal Helpers

        private enum DataType
        {
            Int = 1,
            Long = 2,
            Float = 3,
            String = 4,
            Bool = 5,
            Byte = 6,
            Short = 7,
            ByteArray = 8
        }

        private static void WriteParameterizedCommand(BinaryWriter writer, params object[] attributes)
        {
            // Write parameter count
            writer.WriteBe(attributes.Length);

            // Write each parameter in turn.
            // Type as a byte followed by the data
            foreach (object t in attributes)
            {
                switch (Type.GetTypeCode(t.GetType()))
                {
                    case TypeCode.Int64:
                        writer.Write((sbyte)DataType.Long);
                        writer.WriteBe((long)t);
                        break;

                    case TypeCode.String:
                        writer.Write((sbyte)DataType.String);
                        writer.WriteStringWithLength((string)t);
                        break;

                    case TypeCode.Single:
                        writer.Write((sbyte)DataType.Float);
                        writer.WriteBe((float)t);
                        break;

                    case TypeCode.Int32:
                        writer.Write((sbyte)DataType.Int);
                        writer.WriteBe((int)t);
                        break;

                    case TypeCode.Boolean:
                        writer.Write((sbyte)DataType.Bool);
                        writer.Write((bool)t);
                        break;

                    case TypeCode.SByte:
                        writer.Write((sbyte)DataType.Byte);
                        writer.Write((sbyte)t);
                        break;

                    case TypeCode.Int16:
                        writer.Write((sbyte)DataType.Short);
                        writer.WriteBe((short)t);
                        break;

                    default:
                        if (t.GetType() != typeof(sbyte[]))
                            throw new ArgumentException();

                        writer.Write((byte)DataType.ByteArray);

                        // Write the length followed by the array
                        var bytes = (sbyte[])t;
                        writer.WriteBe(bytes.Length);
                        writer.WriteSBytes(bytes);
                        break;
                }
            }
        }

        private static object[] ReadParameters(BinaryReader reader)
        {
            // Read the paraemter count
            int parameterSize = reader.ReadInt32Be();

            // simple list to hold them temporarily
            var parameters = new List<object>();

            // Loop all and read in turn
            // Type as byte followed by the data
            for (var i = 0; i < parameterSize; i++)
            {
                // Read the data type
                var type = (DataType)reader.ReadSByte();

                // Read the data
                switch (type)
                {
                    case DataType.Long:
                        parameters.Add(reader.ReadInt64Be());
                        break;

                    case DataType.String:
                        parameters.Add(reader.ReadStringWithLength());
                        break;

                    case DataType.Float:
                        parameters.Add(reader.ReadSingleBe());
                        break;

                    case DataType.Int:
                        parameters.Add(reader.ReadInt32Be());
                        break;

                    case DataType.Bool:
                        parameters.Add(reader.ReadBoolean());
                        break;

                    case DataType.Byte:
                        parameters.Add(reader.ReadSByte());
                        break;

                    case DataType.Short:
                        parameters.Add(reader.ReadInt16Be());
                        break;

                    case DataType.ByteArray:
                        int length = reader.ReadInt32Be();
                        parameters.Add(reader.ReadSBytes(length));
                        break;

                    case 0: // This means the end
                        parameters.Add((byte)0);
                        break;

                    default:
                        throw new ArgumentException();
                }
            }

            return parameters.ToArray();
        }

        private class Header
        {
            // ReSharper disable UnusedMember.Local
            // ReSharper disable UnusedMethodReturnValue.Local

            public enum PacketType : sbyte
            {
                Parameterized = 111,
                Stream = 123,
            }

            public enum CommandType : byte
            {
                Ping = 1,
                Command = 2,
                Packet = 42,
            }

            private CommandType Command { get; set; }

            private short PacketId { get; set; }

            private PacketType Type { get; set; }

            private Header()
            {
            }

            public Header(CommandType command, sbyte packetId, PacketType type)
            {
                Command = command;
                PacketId = packetId;
                Type = type;
            }

            public Header(CommandType command, PacketType type)
            {
                Command = command;
                Type = type;
                PacketId = -1;
            }

            public static Header Read(BinaryReader reader)
            {
                if (CommandType.Packet != (CommandType)reader.ReadSByte())
                    throw new ArgumentException();

                var header = new Header
                {
                    PacketId = reader.ReadInt16Be(),
                    Command = (CommandType)reader.ReadSByte(),
                    Type = (PacketType)reader.ReadSByte()
                };

                return header;
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write((sbyte)CommandType.Packet);
                writer.WriteBe(PacketId);
                writer.Write((sbyte)Command);
                writer.Write((sbyte)Type);
            }

            public override string ToString()
            {
                return string.Format("CommandID: {0} Type: {1} PacketID: {2}", Command, Type, PacketId);
            }

            // ReSharper restore UnusedMethodReturnValue.Local
            // ReSharper restore UnusedMember.Local
        }

        #endregion Internal Helpers

        #region Reader/Writer Extensions

        private static void WriteBe(this BinaryWriter writer, int value)
        {
            // Reverse the byte ordering
            int temp = (int)((value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 | (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24);

            writer.Write(temp);
        }

        private static void WriteBe(this BinaryWriter writer, long value)
        {
            ulong val = (ulong)value;

            // Reverse the byte ordering
            long temp = (long)(
                (val & 0x00000000000000FFU) << 56 | (val & 0x000000000000FF00U) << 40 | (val & 0x0000000000FF0000U) << 24 | (val & 0x00000000FF000000U) << 8 |
                (val & 0x000000FF00000000U) >> 8 | (val & 0x0000FF0000000000U) >> 24 | (val & 0x00FF000000000000U) >> 40 | (val & 0xFF00000000000000U) >> 56);

            writer.Write(temp);
        }

        private static unsafe void WriteBe(this BinaryWriter writer, float value)
        {
            uint val = *((uint*)&value);

            // Reverse the byte ordering
            int temp = (int)((val & 0x000000FFU) << 24 | (val & 0x0000FF00U) << 8 | (val & 0x00FF0000U) >> 8 | (val & 0xFF000000U) >> 24);

            writer.Write((float)temp);
        }

        private static void WriteBe(this BinaryWriter writer, short value)
        {
            // Reverse the byte ordering
            short temp = (short)((value & 0x00FFU) << 8 | (value & 0xFF00U) >> 8);

            writer.Write(temp);
        }

        private static void WriteStringWithLength(this BinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);

            writer.WriteBe((short)bytes.Length);

            writer.Write(bytes);
        }

        private static void WriteSBytes(this BinaryWriter writer, sbyte[] bytes)
        {
            var copy = new byte[bytes.Length];

            Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);

            writer.Write(copy);
        }

        private static int ReadInt32Be(this BinaryReader reader)
        {
            int value = reader.ReadInt32();

            // Reverse the byte ordering
            return (int)((value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 | (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24);
        }

        private static long ReadInt64Be(this BinaryReader reader)
        {
            ulong val = (ulong)reader.ReadInt64();

            // Reverse the byte ordering
            return (long)(
                (val & 0x00000000000000FFU) << 56 | (val & 0x000000000000FF00U) << 40 | (val & 0x0000000000FF0000U) << 24 | (val & 0x00000000FF000000U) << 8 |
                (val & 0x000000FF00000000U) >> 8 | (val & 0x0000FF0000000000U) >> 24 | (val & 0x00FF000000000000U) >> 40 | (val & 0xFF00000000000000U) >> 56);
        }

        private static unsafe float ReadSingleBe(this BinaryReader reader)
        {
            uint value = reader.ReadUInt32();

            // Reverse the byte ordering
            uint temp = ((value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 | (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24);

            return *((float*)&temp);
        }

        private static short ReadInt16Be(this BinaryReader reader)
        {
            ushort value = (ushort)reader.ReadInt16();

            // Reverse the byte ordering
            return (short)((value & 0x00FFU) << 8 | (value & 0xFF00U) >> 8);
        }

        private static string ReadStringWithLength(this BinaryReader reader)
        {
            int count = reader.ReadInt16Be();

            byte[] buffer = reader.ReadBytes(count);

            return Encoding.UTF8.GetString(buffer);
        }

        private static sbyte[] ReadSBytes(this BinaryReader reader, int count)
        {
            byte[] temp = reader.ReadBytes(count);

            var copy = new sbyte[count];

            Buffer.BlockCopy(temp, 0, copy, 0, count);

            return copy;
        }

        #endregion Reader/Writer Extensions
    }
}
#pragma warning restore 1591