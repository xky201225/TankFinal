using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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
            return Connect(host, port, playerName, NetworkJoinMode.Search, string.Empty, TankType.Green, out error);
        }

        public bool Connect(string host, int port, string playerName, NetworkJoinMode joinMode, string roomId, out string error)
        {
            return Connect(host, port, playerName, joinMode, roomId, TankType.Green, out error);
        }

        public bool Connect(string host, int port, string playerName, NetworkJoinMode joinMode, string roomId, TankType tankType, out string error)
        {
            string helloLine = "JOIN|" + Sanitize(playerName) + "|" + joinMode + "|" + Sanitize(roomId) + "|" + (int)tankType;
            return ConnectCore(host, port, helloLine, out error);
        }

        public bool ConnectLobby(string host, int port, string playerName, out string error)
        {
            return ConnectCore(host, port, "LOBBY|" + Sanitize(playerName), out error);
        }

        public static bool RegisterUser(string host, int port, string userName, out string error)
        {
            return RegisterUser(host, port, userName, string.Empty, out error);
        }

        public static bool RegisterUser(string host, int port, string userName, string password, out string error)
        {
            string response = SendRequest(host, port, "REGISTER|" + EncodeProtocolPart(userName) + "|" + EncodeProtocolPart(password), out error);
            if (response == "REGISTER_OK")
            {
                return true;
            }

            if (response == "REGISTER_EXISTS")
            {
                error = "该用户名已被注册，请换一个用户名。";
                return false;
            }

            return ParseErrorResponse(response, "服务器未返回有效注册结果。", ref error);
        }

        public static bool LoginUser(string host, int port, string userName, string password, out UserSyncPayload payload, out string authToken, out string error)
        {
            payload = null;
            authToken = string.Empty;
            string response = SendRequest(host, port, "LOGIN|" + EncodeProtocolPart(userName) + "|" + EncodeProtocolPart(password), out error);
            if (response != null && response.StartsWith("LOGIN_OK|", StringComparison.Ordinal))
            {
                string[] parts = response.Split('|');
                if (parts.Length < 3)
                {
                    error = "服务器返回的登录结果无效。";
                    return false;
                }

                payload = DecodePayload(parts[1]);
                if (payload == null)
                {
                    error = "服务器返回的用户数据无效。";
                    return false;
                }

                authToken = DecodeProtocolPart(parts[2]);
                return true;
            }

            if (response == "LOGIN_INVALID")
            {
                error = "用户名或密码错误。";
                return false;
            }

            return ParseErrorResponse(response, "服务器未返回有效登录结果。", ref error);
        }

        public static bool SyncUser(string host, int port, string userName, string authToken, UserSyncPayload payload, out UserSyncPayload serverPayload, out string error)
        {
            serverPayload = null;
            string response = SendRequest(host, port, "SYNC|" + EncodeProtocolPart(userName) + "|" + EncodeProtocolPart(authToken) + "|" + EncodePayload(payload), out error);
            if (response != null && response.StartsWith("SYNC_OK|", StringComparison.Ordinal))
            {
                serverPayload = DecodePayload(response.Substring("SYNC_OK|".Length));
                if (serverPayload == null)
                {
                    error = "服务器返回的同步数据无效。";
                    return false;
                }

                return true;
            }

            return ParseErrorResponse(response, "服务器未返回有效同步结果。", ref error);
        }

        private static string SendRequest(string host, int port, string request, out string error)
        {
            error = string.Empty;
            try
            {
                using (TcpClient requestClient = new TcpClient())
                {
                    requestClient.Connect(host, port);
                    NetworkStream stream = requestClient.GetStream();
                    using (StreamReader requestReader = new StreamReader(stream, Encoding.UTF8))
                    using (StreamWriter requestWriter = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        requestWriter.WriteLine(request);
                        return requestReader.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        private static bool ParseErrorResponse(string response, string fallbackMessage, ref string error)
        {
            if (response != null && response.Contains("_ERROR|"))
            {
                error = response.Substring(response.IndexOf("_ERROR|", StringComparison.Ordinal) + "_ERROR|".Length);
                return false;
            }

            if (string.IsNullOrWhiteSpace(error))
            {
                error = fallbackMessage;
            }

            return false;
        }

        private static string EncodeProtocolPart(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
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

        private static string EncodePayload(UserSyncPayload payload)
        {
            string json = JsonSerializer.Serialize(payload ?? new UserSyncPayload());
            return EncodeProtocolPart(json);
        }

        private static UserSyncPayload DecodePayload(string encodedPayload)
        {
            try
            {
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPayload ?? string.Empty));
                return JsonSerializer.Deserialize<UserSyncPayload>(json);
            }
            catch
            {
                return null;
            }
        }

        private bool ConnectCore(string host, int port, string helloLine, out string error)
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

                SendLine(helloLine);

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
            try
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
            catch (Exception ex)
            {
                NotifyDisconnected(ex.Message);
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
