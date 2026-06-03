using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Peace_of_Tanks_Server
{
    internal sealed class TankMatchServer
    {
        private readonly TcpListener listener;
        private readonly object syncRoot = new object();
        private readonly Dictionary<string, ClientSession> waitingRooms = new Dictionary<string, ClientSession>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ClientSession> lobbySessions = new List<ClientSession>();
        private Thread acceptThread;
        private bool isRunning;

        public TankMatchServer(IPAddress address, int port)
        {
            listener = new TcpListener(address, port);
        }

        public void Start()
        {
            listener.Start();
            isRunning = true;
            Console.WriteLine("开始监听客户端连接：" + listener.LocalEndpoint);

            acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true
            };
            acceptThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            try
            {
                listener.Stop();
            }
            catch
            {
            }
        }

        private void AcceptLoop()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient tcpClient = listener.AcceptTcpClient();
                    ClientSession session = new ClientSession(this, tcpClient);
                    session.Start();
                }
                catch (SocketException)
                {
                    if (isRunning)
                    {
                        Console.WriteLine("接受客户端连接时发生套接字错误。");
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        internal void ProcessLine(ClientSession session, string line)
        {
            if (line.StartsWith("JOIN|", StringComparison.Ordinal))
            {
                string[] parts = line.Split('|');
                string playerName = parts.Length > 1 ? parts[1] : string.Empty;
                string joinMode = parts.Length > 2 ? parts[2] : "Search";
                string roomId = parts.Length > 3 ? parts[3] : string.Empty;
                PairSession(session, playerName, joinMode, roomId);
                return;
            }

            if (line.StartsWith("LOBBY|", StringComparison.Ordinal))
            {
                string playerName = line.Substring("LOBBY|".Length);
                RegisterLobbySession(session, playerName);
                return;
            }

            if (line.StartsWith("CHAT|", StringComparison.Ordinal) && session.IsLobby)
            {
                BroadcastLobbyChat(line);
                return;
            }

            if (session.Peer == null)
            {
                return;
            }

            if (line.StartsWith("INPUT|", StringComparison.Ordinal) && session.Role == SessionRole.Guest)
            {
                session.Peer.Send(line);
                return;
            }

            if (line.StartsWith("STATE|", StringComparison.Ordinal) && session.Role == SessionRole.Host)
            {
                session.Peer.Send(line);
                return;
            }

            if (line == "GAMEOVER" && session.Role == SessionRole.Host)
            {
                session.Peer.Send(line);
            }
        }

        private void RegisterLobbySession(ClientSession session, string playerName)
        {
            lock (syncRoot)
            {
                session.PlayerName = string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
                session.IsLobby = true;
                lobbySessions.Add(session);
                Console.WriteLine(session.PlayerName + " 已进入大厅聊天。");
            }
        }

        private void BroadcastLobbyChat(string line)
        {
            List<ClientSession> sessions;
            lock (syncRoot)
            {
                sessions = new List<ClientSession>(lobbySessions);
            }

            foreach (ClientSession lobbySession in sessions)
            {
                lobbySession.Send(line);
            }
        }

        private void PairSession(ClientSession session, string playerName, string joinMode, string roomId)
        {
            lock (syncRoot)
            {
                session.PlayerName = string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
                session.RoomId = NormalizeRoomId(roomId);

                if (string.Equals(joinMode, "Create", StringComparison.OrdinalIgnoreCase))
                {
                    if (session.RoomId.Length == 0)
                    {
                        session.RoomId = CreateAutoRoomId("ROOM");
                    }

                    if (waitingRooms.ContainsKey(session.RoomId))
                    {
                        session.Send("ROOM_EXISTS|" + session.RoomId);
                        return;
                    }

                    AddWaitingRoom(session);
                    Console.WriteLine(session.PlayerName + " 创建房间：" + session.RoomId);
                    return;
                }

                if (string.Equals(joinMode, "JoinByRoomId", StringComparison.OrdinalIgnoreCase))
                {
                    ClientSession host;
                    if (waitingRooms.TryGetValue(session.RoomId, out host) && host != session)
                    {
                        waitingRooms.Remove(host.RoomId);
                        StartPair(host, session);
                        return;
                    }

                    session.Send("ROOM_NOT_FOUND|" + session.RoomId);
                    return;
                }

                ClientSession availableHost = GetFirstWaitingRoom();
                if (availableHost != null)
                {
                    waitingRooms.Remove(availableHost.RoomId);
                    StartPair(availableHost, session);
                    return;
                }

                session.RoomId = CreateAutoRoomId("AUTO");
                AddWaitingRoom(session);
                Console.WriteLine(session.PlayerName + " 搜索房间，进入等待队列：" + session.RoomId);
            }
        }

        private void AddWaitingRoom(ClientSession session)
        {
            waitingRooms[session.RoomId] = session;
            session.Role = SessionRole.Host;
            session.Send("WAITING|" + session.RoomId);
        }

        private ClientSession GetFirstWaitingRoom()
        {
            foreach (ClientSession session in waitingRooms.Values)
            {
                return session;
            }

            return null;
        }

        private void StartPair(ClientSession host, ClientSession guest)
        {
            host.Role = SessionRole.Host;
            guest.Role = SessionRole.Guest;
            guest.RoomId = host.RoomId;
            host.Peer = guest;
            guest.Peer = host;

            host.Send("START|HOST|" + host.RoomId);
            guest.Send("START|GUEST|" + host.RoomId);
            Console.WriteLine("完成配对：" + host.PlayerName + " <-> " + guest.PlayerName + " 房间：" + host.RoomId);
        }

        private static string NormalizeRoomId(string value)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Replace(" ", string.Empty).Replace("|", "_");
        }

        private static string CreateAutoRoomId(string prefix)
        {
            return prefix + "-" + DateTime.UtcNow.Ticks.ToString().Substring(10);
        }

        internal void RemoveSession(ClientSession session)
        {
            lock (syncRoot)
            {
                if (session.IsLobby)
                {
                    lobbySessions.Remove(session);
                }

                if (!string.IsNullOrWhiteSpace(session.RoomId))
                {
                    ClientSession waitingRoom;
                    if (waitingRooms.TryGetValue(session.RoomId, out waitingRoom) && waitingRoom == session)
                    {
                        waitingRooms.Remove(session.RoomId);
                    }
                }

                ClientSession peer = session.Peer;
                if (peer != null)
                {
                    session.Peer = null;
                    peer.Peer = null;
                    peer.Send("PEER_LEFT");
                }

                Console.WriteLine("客户端离线：" + session.PlayerName);
            }
        }

        internal sealed class ClientSession : IDisposable
        {
            private readonly TankMatchServer server;
            private readonly TcpClient client;
            private readonly StreamReader reader;
            private readonly StreamWriter writer;
            private readonly object sendLock = new object();
            private Thread readThread;

            public string PlayerName { get; set; }
            public string RoomId { get; set; }
            public SessionRole Role { get; set; }
            public ClientSession Peer { get; set; }
            public bool IsLobby { get; set; }

            public ClientSession(TankMatchServer server, TcpClient client)
            {
                this.server = server;
                this.client = client;
                NetworkStream stream = client.GetStream();
                reader = new StreamReader(stream, Encoding.UTF8);
                writer = new StreamWriter(stream, Encoding.UTF8)
                {
                    AutoFlush = true
                };
                PlayerName = client.Client.RemoteEndPoint.ToString();
                RoomId = string.Empty;
                Role = SessionRole.None;
            }

            public void Start()
            {
                Console.WriteLine("客户端已连接：" + client.Client.RemoteEndPoint);
                readThread = new Thread(ReadLoop)
                {
                    IsBackground = true
                };
                readThread.Start();
            }

            public void Send(string message)
            {
                lock (sendLock)
                {
                    try
                    {
                        writer.WriteLine(message);
                        writer.Flush();
                    }
                    catch
                    {
                    }
                }
            }

            private void ReadLoop()
            {
                try
                {
                    while (true)
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                        {
                            break;
                        }

                        server.ProcessLine(this, line);
                    }
                }
                catch
                {
                }
                finally
                {
                    server.RemoveSession(this);
                    Dispose();
                }
            }

            public void Dispose()
            {
                try
                {
                    client.Close();
                }
                catch
                {
                }
            }
        }

        internal enum SessionRole
        {
            None,
            Host,
            Guest
        }
    }
}
