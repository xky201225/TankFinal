using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Peace_of_Tanks
{
    [Serializable]
    internal class NetworkInputState
    {
        public bool Up;
        public bool Down;
        public bool Left;
        public bool Right;
        public bool Fire;
    }

    [Serializable]
    internal class TankSnapshot
    {
        public int X;
        public int Y;
        public int Direction;
        public int HitPoints;
        public bool Active;
        public int Kind;
    }

    [Serializable]
    internal class BulletSnapshot
    {
        public int X;
        public int Y;
        public int Direction;
        public int Who;
    }

    [Serializable]
    internal class BoxSnapshot
    {
        public int X;
        public int Y;
        public int Type;
    }

    [Serializable]
    internal class GameSnapshot
    {
        public int LivesPlayer1;
        public int LivesPlayer2;
        public int PointsPlayer1;
        public int PointsPlayer2;
        public int EnemiesLeft;
        public bool GameOver;
        public string StatusText = string.Empty;
        public TankSnapshot Player1 = new TankSnapshot();
        public TankSnapshot Player2 = new TankSnapshot();
        public List<TankSnapshot> Enemies = new List<TankSnapshot>();
        public List<BulletSnapshot> Bullets = new List<BulletSnapshot>();
        public List<BoxSnapshot> Boxes = new List<BoxSnapshot>();
    }

    internal enum NetworkDifficulty
    {
        Easy,
        Hard,
        Reality
    }

    internal sealed class NetworkConfig
    {
        public string ServerHost = "127.0.0.1";
        public int ServerPort = 51888;
        public string PlayerName = "玩家";
        public NetworkDifficulty Difficulty = NetworkDifficulty.Easy;
    }

    internal static class NetworkSerialization
    {
        public static string SerializeToBase64<T>(T value)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, value);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static T DeserializeFromBase64<T>(string value)
        {
            byte[] bytes = Convert.FromBase64String(value);
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(ms);
            }
        }
    }
}
