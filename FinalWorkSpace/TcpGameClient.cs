using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Peace_of_Tanks
{
    internal sealed class TcpGameClient : IDisposable
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread receiveThread;
        private readonly object sendLock = new object();

        public event Action<string> LineReceived;
        public event Action<string> Disconnected;

        public bool IsConnected
        {
            get { return client != null && client.Connected; }
        }

        public bool Connect(string host, int port, string playerName, out string error)
        {
            error = string.Empty;
            try
            {
                client = new TcpClient();
                client.Connect(host, port);
                NetworkStream stream = client.GetStream();
                reader = new StreamReader(stream, Encoding.UTF8);
                writer = new StreamWriter(stream, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                SendLine("JOIN|" + Sanitize(playerName));

                receiveThread = new Thread(ReadLoop)
                {
                    IsBackground = true
                };
                receiveThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Dispose();
                return false;
            }
        }

        public void SendLine(string line)
        {
            lock (sendLock)
            {
                if (writer == null)
                {
                    return;
                }

                writer.WriteLine(line);
                writer.Flush();
            }
        }

        private void ReadLoop()
        {
            try
            {
                while (reader != null)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    Action<string> handler = LineReceived;
                    if (handler != null)
                    {
                        handler(line);
                    }
                }
            }
            catch (Exception ex)
            {
                NotifyDisconnected(ex.Message);
                return;
            }

            NotifyDisconnected("与服务器断开连接。");
        }

        private void NotifyDisconnected(string message)
        {
            Action<string> handler = Disconnected;
            if (handler != null)
            {
                handler(message);
            }
        }

        private static string Sanitize(string value)
        {
            return (value ?? string.Empty).Replace("|", "_").Trim();
        }

        public void Dispose()
        {
            try
            {
                if (client != null)
                {
                    client.Close();
                }
            }
            catch
            {
            }

            try
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }
            catch
            {
            }

            try
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }
            catch
            {
            }

            client = null;
            reader = null;
            writer = null;
            receiveThread = null;
        }
    }
}
