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
        public int TankType;
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
    internal class AnimationSnapshot
    {
        public int X;
        public int Y;
        public int Type;
        public int Delay;
    }

    [Serializable]
    internal class PlayerResultSnapshot
    {
        public string Name = string.Empty;
        public int Damage;
        public int Kills;
        public int Deaths;
        public int Rank;
        public bool IsActive;
    }

    [Serializable]
    internal class GameSnapshot
    {
        public int LivesPlayer1;
        public int LivesPlayer2;
        public int PointsPlayer1;
        public int PointsPlayer2;
        public int KillsPlayer1;
        public int KillsPlayer2;
        public int EnemiesLeft;
        public bool GameOver;
        public string StatusText = string.Empty;
        public TankSnapshot Player1 = new TankSnapshot();
        public TankSnapshot Player2 = new TankSnapshot();
        public List<TankSnapshot> Enemies = new List<TankSnapshot>();
        public List<BulletSnapshot> Bullets = new List<BulletSnapshot>();
        public List<BoxSnapshot> Boxes = new List<BoxSnapshot>();
        public List<AnimationSnapshot> Animations = new List<AnimationSnapshot>();
        public List<PlayerResultSnapshot> Results = new List<PlayerResultSnapshot>();
    }

    public enum NetworkDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    public enum NetworkJoinMode
    {
        Search,
        Create,
        JoinByRoomId
    }

    public enum BattleRuleMode
    {
        FreeBattle,
        BaseDefense
    }

    public enum MapTileType
    {
        Empty,
        Wall,
        Water,
        Grass,
        Iron,
        Base
    }

    [Serializable]
    public sealed class GameMapConfig
    {
        public string Name = "系统地图 1";
        public int Width = 96;
        public int Height = 96;
        public MapTileType[] Tiles = new MapTileType[96 * 96];
    }

    public sealed class NetworkConfig
    {
        public string ServerHost = "127.0.0.1";
        public int ServerPort = 51888;
        public string PlayerName = "玩家";
        public TankType TankType = TankType.Green;
        public NetworkDifficulty Difficulty = NetworkDifficulty.Easy;
        public NetworkJoinMode JoinMode = NetworkJoinMode.Search;
        public string RoomId = string.Empty;
        public int PlayerCount = 2;
        public BattleRuleMode BattleRule = BattleRuleMode.FreeBattle;
        public GameMapConfig MapConfig;
    }

    public sealed class GameLaunchOptions
    {
        public GameMode Mode = GameMode.SinglePlayer;
        public TankType TankType = TankType.Green;
        public bool StartImmediately;
        public bool UseNetwork;
        public NetworkDifficulty Difficulty = NetworkDifficulty.Easy;
        public NetworkConfig NetworkConfig;
        public int PlayerCount = 1;
        public BattleRuleMode BattleRule = BattleRuleMode.FreeBattle;
        public GameMapConfig MapConfig;
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
