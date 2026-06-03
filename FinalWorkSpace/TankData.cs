using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    public enum TankType
    {
        Green = 0,
        Yellow = 1,
        Blue = 2,
        Red = 3,
        Purple = 4
    }

    public class TankConfig
    {
        public TankType Type { get; set; }
        public string ImageName { get; set; }
        public int BaseAttack { get; set; }
        public int BaseHealth { get; set; }
        public int UnlockExp { get; set; }
        public int AttackPerLevel { get; set; } = 5;
        public int HealthPerLevel { get; set; } = 10;
        public int MaxLevel { get; set; } = 5;

        public int GetAttackAtLevel(int level)
        {
            return BaseAttack + (level - 1) * AttackPerLevel;
        }

        public int GetHealthAtLevel(int level)
        {
            return BaseHealth + (level - 1) * HealthPerLevel;
        }

        public static int GetExpForUpgrade(int currentLevel)
        {
            return currentLevel * 10;
        }
    }

    public static class TankConfigs
    {
        private static readonly Dictionary<TankType, TankConfig> configs = new Dictionary<TankType, TankConfig>
        {
            {
                TankType.Green,
                new TankConfig
                {
                    Type = TankType.Green,
                    ImageName = "Lv.png",
                    BaseAttack = 10,
                    BaseHealth = 30,
                    UnlockExp = 0
                }
            },
            {
                TankType.Yellow,
                new TankConfig
                {
                    Type = TankType.Yellow,
                    ImageName = "Huang.png",
                    BaseAttack = 15,
                    BaseHealth = 40,
                    UnlockExp = 50
                }
            },
            {
                TankType.Blue,
                new TankConfig
                {
                    Type = TankType.Blue,
                    ImageName = "Lan.png",
                    BaseAttack = 20,
                    BaseHealth = 60,
                    UnlockExp = 80
                }
            },
            {
                TankType.Red,
                new TankConfig
                {
                    Type = TankType.Red,
                    ImageName = "Hong.png",
                    BaseAttack = 25,
                    BaseHealth = 80,
                    UnlockExp = 120
                }
            },
            {
                TankType.Purple,
                new TankConfig
                {
                    Type = TankType.Purple,
                    ImageName = "Zi.png",
                    BaseAttack = 30,
                    BaseHealth = 100,
                    UnlockExp = 150
                }
            }
        };

        public static TankConfig GetConfig(TankType type)
        {
            return configs[type];
        }

        public static TankConfig[] GetAllConfigs()
        {
            return new[]
            {
                configs[TankType.Green],
                configs[TankType.Yellow],
                configs[TankType.Blue],
                configs[TankType.Red],
                configs[TankType.Purple]
            };
        }
    }

    public class PlayerTankData
    {
        public TankType Type { get; set; }
        public int Level { get; set; } = 1;
        public int CurrentExp { get; set; } = 0;
        public bool IsUnlocked { get; set; } = false;

        public int GetExpForNextLevel()
        {
            if (Level >= 5)
            {
                return -1;
            }

            return TankConfig.GetExpForUpgrade(Level);
        }

        public bool CanUpgrade()
        {
            return Level < 5 && CurrentExp >= GetExpForNextLevel();
        }
    }

    public class PlayerData
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TotalExp { get; set; } = 0;
        public Dictionary<TankType, PlayerTankData> Tanks { get; set; }
        public TankType SelectedTank { get; set; } = TankType.Green;

        public PlayerData()
        {
            Tanks = new Dictionary<TankType, PlayerTankData>();
            foreach (TankType type in Enum.GetValues(typeof(TankType)))
            {
                Tanks[type] = new PlayerTankData
                {
                    Type = type,
                    Level = 1,
                    CurrentExp = 0,
                    IsUnlocked = type == TankType.Green
                };
            }
        }

        public void AddExp(int exp)
        {
            TotalExp += exp;
        }

        public bool UpgradeTank(TankType type)
        {
            if (!Tanks.ContainsKey(type))
            {
                return false;
            }

            PlayerTankData tank = Tanks[type];
            if (!tank.CanUpgrade())
            {
                return false;
            }

            int expNeeded = tank.GetExpForNextLevel();
            tank.CurrentExp -= expNeeded;
            tank.Level++;
            return true;
        }

        public bool UnlockTank(TankType type)
        {
            if (!Tanks.ContainsKey(type))
            {
                return false;
            }

            PlayerTankData tank = Tanks[type];
            if (tank.IsUnlocked)
            {
                return false;
            }

            TankConfig config = TankConfigs.GetConfig(type);
            if (TotalExp < config.UnlockExp)
            {
                return false;
            }

            TotalExp -= config.UnlockExp;
            tank.IsUnlocked = true;
            return true;
        }

        public void AddTankExp(TankType type, int exp)
        {
            if (Tanks.ContainsKey(type) && Tanks[type].IsUnlocked)
            {
                Tanks[type].CurrentExp += exp;
            }
        }

        public int GetSelectedTankAttack()
        {
            PlayerTankData tank = Tanks[SelectedTank];
            TankConfig config = TankConfigs.GetConfig(SelectedTank);
            return config.GetAttackAtLevel(tank.Level);
        }

        public int GetSelectedTankHealth()
        {
            PlayerTankData tank = Tanks[SelectedTank];
            TankConfig config = TankConfigs.GetConfig(SelectedTank);
            return config.GetHealthAtLevel(tank.Level);
        }
    }

    public enum GameMode
    {
        SinglePlayer,
        TwoPlayer,
        ThreePlayer
    }

    public static class DataManager
    {
        private static readonly string JsonFallbackPath = AppPaths.GetDataPath("playerdata.json");
        private const string DefaultRegisterHost = "127.0.0.1";
        private const int DefaultRegisterPort = 51888;
        private static string currentServerHost = DefaultRegisterHost;
        private static int currentServerPort = DefaultRegisterPort;
        private static string currentAuthToken = string.Empty;
        private static System.Threading.Timer syncTimer;
        private static PlayerData currentPlayerData;

        public static string CurrentServerHost
        {
            get { return currentServerHost; }
        }

        public static int CurrentServerPort
        {
            get { return currentServerPort; }
        }

        public static bool HasAuthenticatedServer
        {
            get { return !string.IsNullOrWhiteSpace(currentAuthToken); }
        }

        public static PlayerData CurrentPlayer
        {
            get
            {
                if (currentPlayerData == null)
                {
                    LoadData();
                }

                return currentPlayerData;
            }
        }

        public static void LoadData()
        {
            try
            {
                currentPlayerData = AuthenticateUser();
                SaveJsonFallback();
                StartSyncTimer();
            }
            catch (Exception ex)
            {
                LoadJsonFallback();
                MessageBox.Show("连接服务端数据库失败，已使用本地临时数据：" + ex.Message);
            }
        }

        public static void SaveData()
        {
            if (currentPlayerData == null)
            {
                return;
            }

            try
            {
                SyncWithServer();
                SaveJsonFallback();
            }
            catch (Exception ex)
            {
                SaveJsonFallback();
                MessageBox.Show("保存到服务端数据库失败，已临时保存到本地 JSON：" + ex.Message);
            }
        }

        public static void RewardExp(GameMode mode, int rank)
        {
            int exp = 0;
            switch (mode)
            {
                case GameMode.TwoPlayer:
                    exp = rank == 1 ? 15 : 10;
                    break;
                case GameMode.ThreePlayer:
                    exp = rank == 1 ? 15 : (rank == 2 ? 10 : 5);
                    break;
            }

            if (exp > 0)
            {
                CurrentPlayer.AddExp(exp);
                CurrentPlayer.AddTankExp(CurrentPlayer.SelectedTank, exp);
                SaveData();
            }
        }

        public static void SyncWithServer()
        {
            if (currentPlayerData == null || string.IsNullOrWhiteSpace(currentPlayerData.UserName))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(currentAuthToken))
            {
                SaveJsonFallback();
                return;
            }

            string error;
            UserSyncPayload serverPayload;
            if (!TcpGameClient.SyncUser(currentServerHost, currentServerPort, currentPlayerData.UserName, currentAuthToken, MapPlayerDataToPayload(currentPlayerData), out serverPayload, out error))
            {
                SaveJsonFallback();
                return;
            }

            currentPlayerData = MapPayloadToPlayerData(serverPayload, currentPlayerData.UserId);
            SaveJsonFallback();
        }

        private static PlayerData AuthenticateUser()
        {
            while (true)
            {
                AuthRequest request;
                if (!AuthDialog.ShowDialog(currentServerHost, currentServerPort, out request))
                {
                    MessageBox.Show("进入游戏必须登录或注册。", "用户登录", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }

                string error;
                currentServerHost = request.ServerHost;
                currentServerPort = request.ServerPort;

                if (request.IsRegister)
                {
                    if (!TcpGameClient.RegisterUser(request.ServerHost, request.ServerPort, request.UserName, request.Password, out error))
                    {
                        MessageBox.Show("注册失败：" + error, "用户注册", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }
                }

                UserSyncPayload payload;
                string authToken;
                if (!TcpGameClient.LoginUser(request.ServerHost, request.ServerPort, request.UserName, request.Password, out payload, out authToken, out error))
                {
                    MessageBox.Show("登录失败：" + error, "用户登录", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                currentAuthToken = authToken;
                return MapPayloadToPlayerData(payload, 0);
            }
        }

        private static void StartSyncTimer()
        {
            if (syncTimer != null)
            {
                return;
            }

            syncTimer = new System.Threading.Timer(_ => SyncWithServer(), null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
        }

        private static UserSyncPayload MapPlayerDataToPayload(PlayerData playerData)
        {
            UserSyncPayload payload = new UserSyncPayload
            {
                UserName = playerData.UserName,
                TotalExp = playerData.TotalExp,
                SelectedTank = (int)playerData.SelectedTank
            };

            foreach (KeyValuePair<TankType, PlayerTankData> pair in playerData.Tanks)
            {
                payload.Tanks.Add(new TankSyncPayload
                {
                    TankType = (int)pair.Key,
                    Level = pair.Value.Level,
                    CurrentExp = pair.Value.CurrentExp,
                    IsUnlocked = pair.Value.IsUnlocked
                });
            }

            return payload;
        }

        private static PlayerData MapPayloadToPlayerData(UserSyncPayload payload, int existingUserId)
        {
            if (payload == null)
            {
                return new PlayerData();
            }

            PlayerData playerData = new PlayerData
            {
                UserId = existingUserId,
                UserName = payload.UserName,
                TotalExp = payload.TotalExp,
                SelectedTank = ParseTankType(payload.SelectedTank)
            };

            foreach (TankSyncPayload tank in payload.Tanks)
            {
                TankType tankType = ParseTankType(tank.TankType);
                playerData.Tanks[tankType] = new PlayerTankData
                {
                    Type = tankType,
                    Level = tank.Level,
                    CurrentExp = tank.CurrentExp,
                    IsUnlocked = tank.IsUnlocked
                };
            }

            return playerData;
        }

        private static TankType ParseTankType(int value)
        {
            if (value >= 0 && value <= 4)
            {
                return (TankType)value;
            }

            return TankType.Green;
        }

        private static void LoadJsonFallback()
        {
            try
            {
                if (File.Exists(JsonFallbackPath))
                {
                    string json = File.ReadAllText(JsonFallbackPath);
                    currentPlayerData = JsonSerializer.Deserialize<PlayerData>(json);
                }
            }
            catch
            {
            }

            if (currentPlayerData == null)
            {
                currentPlayerData = new PlayerData
                {
                    UserName = "玩家" + Environment.UserName
                };
            }
        }

        private static void SaveJsonFallback()
        {
            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(currentPlayerData, options);
                File.WriteAllText(JsonFallbackPath, json);
            }
            catch
            {
            }
        }
    }
}
