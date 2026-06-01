using System;
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
        private Thread acceptThread;
        private bool isRunning;
        private ClientSession waitingSession;

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
                string playerName = line.Substring("JOIN|".Length);
                PairSession(session, playerName);
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

        private void PairSession(ClientSession session, string playerName)
        {
            lock (syncRoot)
            {
                session.PlayerName = string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();

                if (waitingSession == null)
                {
                    waitingSession = session;
                    session.Role = SessionRole.Host;
                    session.Send("WAITING");
                    Console.WriteLine(session.PlayerName + " 已进入等待队列。");
                    return;
                }

                ClientSession host = waitingSession;
                waitingSession = null;

                host.Role = SessionRole.Host;
                session.Role = SessionRole.Guest;
                host.Peer = session;
                session.Peer = host;

                host.Send("START|HOST");
                session.Send("START|GUEST");
                Console.WriteLine("已完成配对：" + host.PlayerName + " <-> " + session.PlayerName);
            }
        }

        internal void RemoveSession(ClientSession session)
        {
            lock (syncRoot)
            {
                if (waitingSession == session)
                {
                    waitingSession = null;
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
            public SessionRole Role { get; set; }
            public ClientSession Peer { get; set; }

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
