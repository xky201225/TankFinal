using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Peace_of_Tanks_Server.Models;

namespace Peace_of_Tanks_Server
{
    internal sealed class TankMatchServer
    {
        private readonly TcpListener listener;
        private readonly object syncRoot = new object();
        private readonly Dictionary<string, ClientSession> waitingRooms = new Dictionary<string, ClientSession>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> authTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
            if (line.StartsWith("REGISTER|", StringComparison.Ordinal))
            {
                HandleRegister(session, line);
                session.Dispose();
                return;
            }

            if (line.StartsWith("LOGIN|", StringComparison.Ordinal))
            {
                HandleLogin(session, line);
                session.Dispose();
                return;
            }

            if (line.StartsWith("SYNC|", StringComparison.Ordinal))
            {
                HandleSync(session, line);
                session.Dispose();
                return;
            }

            if (line.StartsWith("JOIN|", StringComparison.Ordinal))
            {
                string[] parts = line.Split('|');
                string playerName = parts.Length > 1 ? parts[1] : string.Empty;
                string joinMode = parts.Length > 2 ? parts[2] : "Search";
                string roomId = parts.Length > 3 ? parts[3] : string.Empty;
                int tankType = 0;
                if (parts.Length > 4)
                {
                    int.TryParse(parts[4], out tankType);
                }
                PairSession(session, playerName, joinMode, roomId, tankType);
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

        private void HandleRegister(ClientSession session, string line)
        {
            string[] parts = line.Split('|');
            string userName = parts.Length > 1 ? DecodeProtocolPart(parts[1]).Trim() : string.Empty;
            string password = parts.Length > 2 ? DecodeProtocolPart(parts[2]) : string.Empty;
            if (userName.Length == 0 || userName.Length > 50)
            {
                session.Send("REGISTER_ERROR|用户名长度必须为 1 到 50 个字符。");
                return;
            }

            if (password.Length < 6 || password.Length > 50)
            {
                session.Send("REGISTER_ERROR|密码长度必须为 6 到 50 个字符。");
                return;
            }

            try
            {
                using (LocalDBMSSQLLocalDBContext db = new LocalDBMSSQLLocalDBContext())
                {
                    if (db.Users.Any(u => u.UserName == userName))
                    {
                        session.Send("REGISTER_EXISTS");
                        return;
                    }

                    DateTime now = DateTime.UtcNow;
                    User user = new User
                    {
                        UserName = userName,
                        PasswordHash = HashPassword(password),
                        TotalExp = 0,
                        SelectedTank = 0,
                        CreatedAt = now,
                        UpdatedAt = now,
                        LastLoginAt = now
                    };

                    db.Users.Add(user);
                    db.SaveChanges();

                    for (int tankType = 0; tankType <= 4; tankType++)
                    {
                        db.UserTanks.Add(new UserTank
                        {
                            UserId = user.Id,
                            TankType = tankType,
                            Level = 1,
                            CurrentExp = 0,
                            IsUnlocked = tankType == 0
                        });
                    }

                    db.SaveChanges();
                }

                session.Send("REGISTER_OK");
                Console.WriteLine("用户注册成功：" + userName);
            }
            catch (DbUpdateException)
            {
                session.Send("REGISTER_EXISTS");
            }
            catch (Exception ex)
            {
                session.Send("REGISTER_ERROR|" + ex.Message);
            }
        }

        private void HandleLogin(ClientSession session, string line)
        {
            string[] parts = line.Split('|');
            string userName = parts.Length > 1 ? DecodeProtocolPart(parts[1]).Trim() : string.Empty;
            string password = parts.Length > 2 ? DecodeProtocolPart(parts[2]) : string.Empty;

            try
            {
                using (LocalDBMSSQLLocalDBContext db = new LocalDBMSSQLLocalDBContext())
                {
                    User user = db.Users.FirstOrDefault(u => u.UserName == userName);
                    if (user == null || !VerifyPassword(password, user.PasswordHash))
                    {
                        session.Send("LOGIN_INVALID");
                        return;
                    }

                    user.LastLoginAt = DateTime.UtcNow;
                    db.SaveChanges();

                    string token = CreateAuthToken();
                    lock (syncRoot)
                    {
                        authTokens[user.UserName] = token;
                    }

                    session.Send("LOGIN_OK|" + EncodePayload(MapUserToPayload(db, user)) + "|" + EncodeProtocolPart(token));
                }
            }
            catch (Exception ex)
            {
                session.Send("LOGIN_ERROR|" + ex.Message);
            }
        }

        private void HandleSync(ClientSession session, string line)
        {
            string[] parts = line.Split('|');
            string userName = parts.Length > 1 ? DecodeProtocolPart(parts[1]).Trim() : string.Empty;
            string token = parts.Length > 2 ? DecodeProtocolPart(parts[2]) : string.Empty;
            UserSyncPayload payload = parts.Length > 3 ? DecodePayload(parts[3]) : null;
            if (payload == null || string.IsNullOrWhiteSpace(userName))
            {
                session.Send("SYNC_ERROR|同步数据无效。");
                return;
            }

            lock (syncRoot)
            {
                string expectedToken;
                if (!authTokens.TryGetValue(userName, out expectedToken) || expectedToken != token)
                {
                    session.Send("SYNC_ERROR|登录状态已失效，请重新登录。");
                    return;
                }
            }

            try
            {
                using (LocalDBMSSQLLocalDBContext db = new LocalDBMSSQLLocalDBContext())
                {
                    User user = db.Users.FirstOrDefault(u => u.UserName == userName);
                    if (user == null)
                    {
                        session.Send("SYNC_ERROR|用户不存在，请重新登录。");
                        return;
                    }

                    ApplyPayloadToUser(db, user, payload);
                    session.Send("SYNC_OK|" + EncodePayload(MapUserToPayload(db, user)));
                }
            }
            catch (Exception ex)
            {
                session.Send("SYNC_ERROR|" + ex.Message);
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

        private void PairSession(ClientSession session, string playerName, string joinMode, string roomId, int tankType)
        {
            lock (syncRoot)
            {
                session.PlayerName = string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
                session.RoomId = NormalizeRoomId(roomId);
                session.TankType = Math.Max(0, Math.Min(4, tankType));

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

            string tankPayload = "|" + host.TankType + "|" + guest.TankType;
            host.Send("START|HOST|" + host.RoomId + tankPayload);
            guest.Send("START|GUEST|" + host.RoomId + tankPayload);
            Console.WriteLine("完成配对：" + host.PlayerName + " <-> " + guest.PlayerName + " 房间：" + host.RoomId);
        }

        private static string NormalizeRoomId(string value)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Replace(" ", string.Empty).Replace("|", "_");
        }

        private static string DecodeProtocolPart(string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string EncodeProtocolPart(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static string EncodePayload(UserSyncPayload payload)
        {
            return EncodeProtocolPart(JsonSerializer.Serialize(payload ?? new UserSyncPayload()));
        }

        private static UserSyncPayload DecodePayload(string value)
        {
            try
            {
                string json = DecodeProtocolPart(value);
                return JsonSerializer.Deserialize<UserSyncPayload>(json);
            }
            catch
            {
                return null;
            }
        }

        private static UserSyncPayload MapUserToPayload(LocalDBMSSQLLocalDBContext db, User user)
        {
            UserSyncPayload payload = new UserSyncPayload
            {
                UserName = user.UserName,
                TotalExp = user.TotalExp,
                SelectedTank = user.SelectedTank
            };

            foreach (UserTank tank in db.UserTanks.Where(t => t.UserId == user.Id).ToList())
            {
                payload.Tanks.Add(new TankSyncPayload
                {
                    TankType = tank.TankType,
                    Level = tank.Level,
                    CurrentExp = tank.CurrentExp,
                    IsUnlocked = tank.IsUnlocked
                });
            }

            return payload;
        }

        private static void ApplyPayloadToUser(LocalDBMSSQLLocalDBContext db, User user, UserSyncPayload payload)
        {
            user.TotalExp = payload.TotalExp;
            user.SelectedTank = payload.SelectedTank;
            user.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();

            for (int tankType = 0; tankType <= 4; tankType++)
            {
                TankSyncPayload tankPayload = payload.Tanks.FirstOrDefault(t => t.TankType == tankType);
                UserTank tank = db.UserTanks.FirstOrDefault(t => t.UserId == user.Id && t.TankType == tankType);
                if (tank == null)
                {
                    tank = new UserTank
                    {
                        UserId = user.Id,
                        TankType = tankType
                    };
                    db.UserTanks.Add(tank);
                }

                tank.Level = tankPayload != null ? tankPayload.Level : 1;
                tank.CurrentExp = tankPayload != null ? tankPayload.CurrentExp : 0;
                tank.IsUnlocked = tankPayload != null ? tankPayload.IsUnlocked : tankType == 0;
            }

            db.SaveChanges();
        }

        private static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            const int iterations = 100000;
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return "PBKDF2$" + iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
            }
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            string[] parts = storedHash.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2")
            {
                return false;
            }

            int iterations;
            if (!int.TryParse(parts[1], out iterations))
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expectedHash = Convert.FromBase64String(parts[3]);
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                byte[] actualHash = pbkdf2.GetBytes(expectedHash.Length);
                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
        }

        private static string CreateAuthToken()
        {
            byte[] bytes = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Convert.ToBase64String(bytes);
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
            public int TankType { get; set; }
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
