using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Peace_of_Tanks_Server
{
    internal static class Program
    {
        private static void Main()
        {
            TankMatchServer server = new TankMatchServer(IPAddress.Any, 51888);
            server.Start();

            Console.WriteLine("Peace of Tanks 联机服务端已启动。");
            Console.WriteLine("本机测试可使用：127.0.0.1:51888");
            foreach (string address in GetIpv4Addresses())
            {
                Console.WriteLine("局域网联机可使用：" + address + ":51888");
            }
            Console.WriteLine("客户端输入同一服务器地址即可自动配对。");
            Console.WriteLine("按 Enter 键关闭服务端。");
            Console.ReadLine();

            server.Stop();
        }

        private static IEnumerable<string> GetIpv4Addresses()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
            for (int i = 0; i < addresses.Length; i++)
            {
                if (addresses[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    yield return addresses[i].ToString();
                }
            }
        }
    }
}
