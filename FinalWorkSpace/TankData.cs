using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
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
        public int UnlockCoin { get; set; }
        public const int MaxUpgradeLevel = 5;
        private const double SpeedUpgradeMultiplier = 1.45;
        private const int AttackUpgradeBonus = 10;
        private const int HealthUpgradeBonus = 15;
        private const double BaseCooldownSeconds = 3.0;
        private const double CooldownReductionPerLevel = 0.5;

        public int GetAttackAtLevel(int attackLevel)
        {
            return BaseAttack + attackLevel * AttackUpgradeBonus;
        }

        public int GetHealthAtLevel(int healthLevel)
        {
            return BaseHealth + healthLevel * HealthUpgradeBonus;
        }

        public static double GetSpeedMultiplierAtLevel(int speedLevel)
        {
            return Math.Pow(SpeedUpgradeMultiplier, speedLevel);
        }

        public static double GetBulletSpeedMultiplierAtLevel(int bulletSpeedLevel)
        {
            return Math.Pow(SpeedUpgradeMultiplier, bulletSpeedLevel);
        }

        public static double GetCooldownSecondsAtLevel(int cooldownLevel)
        {
            return Math.Max(0.5, BaseCooldownSeconds - cooldownLevel * CooldownReductionPerLevel);
        }

        public static int GetExpForUpgrade(int currentUpgradeLevel)
        {
            int[] costs = { 20, 35, 55, 80, 100 };
            if (currentUpgradeLevel < 0 || currentUpgradeLevel >= costs.Length)
            {
                return -1;
            }

            return costs[currentUpgradeLevel];
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
                    UnlockCoin = 0
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
                    UnlockCoin = 50
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
                    UnlockCoin = 80
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
                    UnlockCoin = 120
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
                    UnlockCoin = 150
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
        public int SpeedLevel { get; set; } = 0;
        public int AttackLevel { get; set; } = 0;
        public int HealthLevel { get; set; } = 0;
        public int BulletSpeedLevel { get; set; } = 0;
        public int CooldownLevel { get; set; } = 0;
        public int CurrentExp { get; set; } = 0;
        public bool IsUnlocked { get; set; } = false;

        public int GetExpForNextLevel(TankUpgradeType upgradeType)
        {
            int currentLevel = GetUpgradeLevel(upgradeType);
            if (currentLevel >= TankConfig.MaxUpgradeLevel)
            {
                return -1;
            }

            return TankConfig.GetExpForUpgrade(currentLevel);
        }

        public int GetUpgradeLevel(TankUpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case TankUpgradeType.Speed: return SpeedLevel;
                case TankUpgradeType.Attack: return AttackLevel;
                case TankUpgradeType.Health: return HealthLevel;
                case TankUpgradeType.BulletSpeed: return BulletSpeedLevel;
                case TankUpgradeType.Cooldown: return CooldownLevel;
                default: return 0;
            }
        }

        public void IncreaseUpgradeLevel(TankUpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case TankUpgradeType.Speed:
                    SpeedLevel++;
                    break;
                case TankUpgradeType.Attack:
                    AttackLevel++;
                    break;
                case TankUpgradeType.Health:
                    HealthLevel++;
                    break;
                case TankUpgradeType.BulletSpeed:
                    BulletSpeedLevel++;
                    break;
                case TankUpgradeType.Cooldown:
                    CooldownLevel++;
                    break;
            }
        }
    }

    public enum TankUpgradeType
    {
        Speed,
        Attack,
        Health,
        BulletSpeed,
        Cooldown
    }

    public class StageRecord
    {
        public int StageId { get; set; }
        public int BestScore { get; set; }
        public int BestTimeSeconds { get; set; }
        public int Rank { get; set; }
    }

    public class LeaderboardEntry
    {
        public int StageId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int BestTimeSeconds { get; set; }
        public int Rank { get; set; }
    }

    public class PlayerData
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TotalExp { get; set; } = 0;
        public int HighestUnlockedStage { get; set; } = 1;
        public Dictionary<TankType, PlayerTankData> Tanks { get; set; }
        public List<StageRecord> StageRecords { get; set; } = new List<StageRecord>();
        public List<LeaderboardEntry> LeaderboardEntries { get; set; } = new List<LeaderboardEntry>();
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
                    SpeedLevel = 0,
                    AttackLevel = 0,
                    HealthLevel = 0,
                    BulletSpeedLevel = 0,
                    CooldownLevel = 0,
                    CurrentExp = 0,
                    IsUnlocked = type == TankType.Green
                };
            }
        }

        public void AddCoins(int coins)
        {
            TotalExp += coins;
        }

        public void RecordStageResult(int stageId, int score, int timeSeconds)
        {
            StageRecord record = StageRecords.FirstOrDefault(r => r.StageId == stageId);
            if (record == null)
            {
                record = new StageRecord { StageId = stageId, BestScore = score, BestTimeSeconds = timeSeconds <= 0 ? 0 : timeSeconds };
                StageRecords.Add(record);
                return;
            }

            record.BestScore = Math.Max(record.BestScore, score);
            if (timeSeconds > 0 && (record.BestTimeSeconds <= 0 || timeSeconds < record.BestTimeSeconds))
            {
                record.BestTimeSeconds = timeSeconds;
            }
        }

        public StageRecord GetStageRecord(int stageId)
        {
            return StageRecords.FirstOrDefault(r => r.StageId == stageId);
        }

        public bool IsStageUnlocked(int stageId)
        {
            return stageId <= HighestUnlockedStage;
        }

        public void UnlockNextStage(int completedStageId)
        {
            HighestUnlockedStage = Math.Max(HighestUnlockedStage, completedStageId + 1);
            HighestUnlockedStage = Math.Min(3, HighestUnlockedStage);
        }

        public bool UpgradeTank(TankType type, TankUpgradeType upgradeType)
        {
            if (!Tanks.ContainsKey(type))
            {
                return false;
            }

            PlayerTankData tank = Tanks[type];
            if (!CanUpgradeTank(type, upgradeType))
            {
                return false;
            }

            int expNeeded = tank.GetExpForNextLevel(upgradeType);
            TotalExp -= expNeeded;
            tank.IncreaseUpgradeLevel(upgradeType);
            return true;
        }

        public bool CanUpgradeTank(TankType type, TankUpgradeType upgradeType)
        {
            if (!Tanks.ContainsKey(type))
            {
                return false;
            }

            PlayerTankData tank = Tanks[type];
            int expNeeded = tank.GetExpForNextLevel(upgradeType);
            return tank.IsUnlocked && tank.GetUpgradeLevel(upgradeType) < TankConfig.MaxUpgradeLevel && expNeeded > 0 && TotalExp >= expNeeded;
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
            if (TotalExp < config.UnlockCoin)
            {
                return false;
            }

            TotalExp -= config.UnlockCoin;
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
            return config.GetAttackAtLevel(tank.AttackLevel);
        }

        public int GetSelectedTankHealth()
        {
            PlayerTankData tank = Tanks[SelectedTank];
            TankConfig config = TankConfigs.GetConfig(SelectedTank);
            return config.GetHealthAtLevel(tank.HealthLevel);
        }

        public double GetSelectedTankSpeedMultiplier()
        {
            PlayerTankData tank = Tanks[SelectedTank];
            return TankConfig.GetSpeedMultiplierAtLevel(tank.SpeedLevel);
        }

        public double GetSelectedTankBulletSpeedMultiplier()
        {
            PlayerTankData tank = Tanks[SelectedTank];
            return TankConfig.GetBulletSpeedMultiplierAtLevel(tank.BulletSpeedLevel);
        }

        public double GetSelectedTankCooldownSeconds()
        {
            PlayerTankData tank = Tanks[SelectedTank];
            return TankConfig.GetCooldownSecondsAtLevel(tank.CooldownLevel);
        }
    }

    public enum GameMode
    {
        SinglePlayer,
        NetworkVersus,
        NetworkCoop
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
        private static readonly object databaseSyncLock = new object();

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
                SaveStageProgressToDatabase(currentPlayerData);
                SaveLeaderboardToDatabase(currentPlayerData);
                SyncWithServer();
                SaveJsonFallback();
            }
            catch (Exception ex)
            {
                SaveJsonFallback();
                MessageBox.Show("保存到服务端数据库失败，已临时保存到本地 JSON：" + ex.Message);
            }
        }

        public static void RefreshLeaderboard()
        {
            if (currentPlayerData == null)
            {
                return;
            }

            LoadLeaderboardFromDatabase(currentPlayerData);
            SaveJsonFallback();
        }

        public static int CalculateRewardExp(GameMode mode, int rank, int score, int stageId, int elapsedSeconds, bool cleared)
        {
            int baseCoins;
            switch (mode)
            {
                case GameMode.SinglePlayer:
                    baseCoins = 24 + stageId * 12;
                    break;
                case GameMode.NetworkVersus:
                    baseCoins = rank == 1 ? 28 : 16;
                    break;
                case GameMode.NetworkCoop:
                    baseCoins = 26 + stageId * 10;
                    break;
                default:
                    baseCoins = 0;
                    break;
            }

            int scoreBonus = Math.Max(0, score / 220);
            int timeBonus = cleared ? Math.Max(0, 160 - elapsedSeconds / 3) : 0;
            return Math.Max(0, baseCoins + scoreBonus + timeBonus);
        }

        public static int RewardExp(GameMode mode, int rank, int score, int stageId, int elapsedSeconds, bool cleared)
        {
            int exp = CalculateRewardExp(mode, rank, score, stageId, elapsedSeconds, cleared);

            if (cleared)
            {
                CurrentPlayer.UnlockNextStage(stageId);
            }

            if (exp > 0)
            {
                CurrentPlayer.AddCoins(exp);
            }

            return exp;
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

            ApplyPayloadToPlayerData(currentPlayerData, serverPayload, currentPlayerData.UserId);
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
                SelectedTank = (int)playerData.SelectedTank,
                HighestUnlockedStage = playerData.HighestUnlockedStage
            };

            foreach (StageRecord stageRecord in playerData.StageRecords)
            {
                payload.StageRecords.Add(new StageRecordPayload
                {
                    StageId = stageRecord.StageId,
                    BestScore = stageRecord.BestScore,
                    BestTimeSeconds = stageRecord.BestTimeSeconds,
                    Rank = stageRecord.Rank
                });
            }

            foreach (KeyValuePair<TankType, PlayerTankData> pair in playerData.Tanks)
            {
                payload.Tanks.Add(new TankSyncPayload
                {
                    TankType = (int)pair.Key,
                    Level = pair.Value.Level,
                    SpeedLevel = pair.Value.SpeedLevel,
                    AttackLevel = pair.Value.AttackLevel,
                    HealthLevel = pair.Value.HealthLevel,
                    BulletSpeedLevel = pair.Value.BulletSpeedLevel,
                    CooldownLevel = pair.Value.CooldownLevel,
                    CurrentExp = pair.Value.CurrentExp,
                    IsUnlocked = pair.Value.IsUnlocked
                });
            }

            return payload;
        }

        private static PlayerData MapPayloadToPlayerData(UserSyncPayload payload, int existingUserId)
        {
            PlayerData playerData = new PlayerData();
            ApplyPayloadToPlayerData(playerData, payload, existingUserId);
            return playerData;
        }

        private static void ApplyPayloadToPlayerData(PlayerData playerData, UserSyncPayload payload, int existingUserId)
        {
            if (playerData == null || payload == null)
            {
                return;
            }

            playerData.UserId = existingUserId;
            playerData.UserName = payload.UserName ?? string.Empty;
            playerData.TotalExp = payload.TotalExp;
            playerData.SelectedTank = ParseTankType(payload.SelectedTank);
            playerData.HighestUnlockedStage = Math.Max(1, Math.Min(3, payload.HighestUnlockedStage <= 0 ? 1 : payload.HighestUnlockedStage));
            playerData.StageRecords = new List<StageRecord>();
            foreach (StageRecordPayload record in payload.StageRecords ?? new List<StageRecordPayload>())
            {
                playerData.StageRecords.Add(new StageRecord
                {
                    StageId = record.StageId,
                    BestScore = record.BestScore,
                    BestTimeSeconds = record.BestTimeSeconds,
                    Rank = record.Rank
                });
            }

            foreach (TankSyncPayload tank in payload.Tanks ?? new List<TankSyncPayload>())
            {
                TankType tankType = ParseTankType(tank.TankType);
                playerData.Tanks[tankType] = new PlayerTankData
                {
                    Type = tankType,
                    Level = tank.Level,
                    SpeedLevel = ClampUpgradeLevel(tank.SpeedLevel),
                    AttackLevel = ClampUpgradeLevel(tank.AttackLevel),
                    HealthLevel = ClampUpgradeLevel(tank.HealthLevel),
                    BulletSpeedLevel = ClampUpgradeLevel(tank.BulletSpeedLevel),
                    CooldownLevel = ClampUpgradeLevel(tank.CooldownLevel),
                    CurrentExp = tank.CurrentExp,
                    IsUnlocked = tank.IsUnlocked
                };
            }

            LoadStageProgressFromDatabase(playerData);
            LoadLeaderboardFromDatabase(playerData);
        }

        private static void LoadStageProgressFromDatabase(PlayerData playerData)
        {
            if (playerData == null || string.IsNullOrWhiteSpace(playerData.UserName))
            {
                return;
            }

            lock (databaseSyncLock)
            {
                try
                {
                    EnsureStageProgressSchema();
                    using (SqlConnection connection = new SqlConnection(AppPaths.LocalDatabaseConnectionString))
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand("SELECT TOP 1 HighestUnlockedStage FROM Users WHERE UserName = @UserName", connection))
                        {
                            command.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = playerData.UserName;
                            object value = command.ExecuteScalar();
                            if (value != null && value != DBNull.Value)
                            {
                                int unlockedStage = Convert.ToInt32(value);
                                if (unlockedStage > 0)
                                {
                                    playerData.HighestUnlockedStage = Math.Max(playerData.HighestUnlockedStage, Math.Min(3, unlockedStage));
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private static void SaveStageProgressToDatabase(PlayerData playerData)
        {
            if (playerData == null || string.IsNullOrWhiteSpace(playerData.UserName))
            {
                return;
            }

            lock (databaseSyncLock)
            {
                try
                {
                    EnsureStageProgressSchema();
                    using (SqlConnection connection = new SqlConnection(AppPaths.LocalDatabaseConnectionString))
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand("UPDATE Users SET HighestUnlockedStage = CASE WHEN ISNULL(HighestUnlockedStage, 0) > @HighestUnlockedStage THEN HighestUnlockedStage ELSE @HighestUnlockedStage END, UpdatedAt = SYSUTCDATETIME() WHERE UserName = @UserName", connection))
                        {
                            command.Parameters.Add("@HighestUnlockedStage", SqlDbType.Int).Value = Math.Max(1, Math.Min(3, playerData.HighestUnlockedStage));
                            command.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = playerData.UserName;
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private static void EnsureStageProgressSchema()
        {
            using (SqlConnection connection = new SqlConnection(AppPaths.LocalDatabaseConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(@"IF COL_LENGTH('Users', 'HighestUnlockedStage') IS NULL
BEGIN
    ALTER TABLE Users ADD HighestUnlockedStage INT NOT NULL CONSTRAINT DF_Users_HighestUnlockedStage DEFAULT(1)
END", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void EnsureLeaderboardSchema()
        {
            using (SqlConnection connection = new SqlConnection(AppPaths.LocalDatabaseConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(@"IF COL_LENGTH('BattleRecords', 'StageId') IS NULL
BEGIN
    ALTER TABLE BattleRecords ADD StageId INT NOT NULL CONSTRAINT DF_BattleRecords_StageId DEFAULT(1)
END
IF COL_LENGTH('BattleRecords', 'BestTimeSeconds') IS NULL
BEGIN
    ALTER TABLE BattleRecords ADD BestTimeSeconds INT NOT NULL CONSTRAINT DF_BattleRecords_BestTimeSeconds DEFAULT(0)
END
IF COL_LENGTH('BattleRecords', 'Rank') IS NULL
BEGIN
    ALTER TABLE BattleRecords ADD Rank INT NOT NULL CONSTRAINT DF_BattleRecords_Rank DEFAULT(0)
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BattleRecords_UserStage' AND object_id = OBJECT_ID('BattleRecords'))
BEGIN
    CREATE UNIQUE INDEX UX_BattleRecords_UserStage ON BattleRecords(UserId, StageId)
END", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void SaveLeaderboardToDatabase(PlayerData playerData)
        {
            SaveLeaderboardToDatabase(playerData, 0);
        }

        public static void SaveStageLeaderboard(int stageId)
        {
            if (currentPlayerData == null)
            {
                return;
            }

            SaveLeaderboardToDatabase(currentPlayerData, stageId);
            SyncWithServer();
            SaveJsonFallback();
        }

        private static void SaveLeaderboardToDatabase(PlayerData playerData, int stageId)
        {
            if (playerData == null || string.IsNullOrWhiteSpace(playerData.UserName))
            {
                return;
            }

            StageRecord latestRecord = stageId > 0 ? playerData.GetStageRecord(stageId) : playerData.StageRecords.OrderByDescending(r => r.BestScore).ThenByDescending(r => r.StageId).FirstOrDefault();
            if (latestRecord == null)
            {
                LoadLeaderboardFromDatabase(playerData);
                return;
            }

            lock (databaseSyncLock)
            {
                try
                {
                    EnsureLeaderboardSchema();
                    using (SqlConnection connection = new SqlConnection(AppPaths.LocalDatabaseConnectionString))
                    {
                        connection.Open();
                        int userId = EnsureDatabaseUserId(connection, playerData.UserName);
                        if (userId <= 0)
                        {
                            return;
                        }

                        UpsertBattleRecord(connection, userId, playerData.UserName, latestRecord.StageId, latestRecord.BestScore, latestRecord.BestTimeSeconds);
                        RebuildStageRanks(connection, latestRecord.StageId);
                        LoadLeaderboardFromDatabase(playerData, connection);
                        ApplyRankToLocalStageRecord(playerData, latestRecord.StageId);
                    }
                }
                catch
                {
                }
            }
        }

        private static int EnsureDatabaseUserId(SqlConnection connection, string userName)
        {
            using (SqlCommand command = new SqlCommand("SELECT TOP 1 Id FROM Users WHERE UserName = @UserName", connection))
            {
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = userName;
                object value = command.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt32(value);
            }
        }

        private static bool UpsertBattleRecord(SqlConnection connection, int userId, string userName, int stageId, int score, int bestTimeSeconds)
        {
            using (SqlCommand mergeCommand = new SqlCommand(@"MERGE BattleRecords AS target
USING (SELECT @UserId AS UserId, @StageId AS StageId) AS source
ON target.UserId = source.UserId AND target.StageId = source.StageId
WHEN MATCHED AND @Score > target.Points THEN
    UPDATE SET Points = @Score,
               PlayerName = @PlayerName,
               BestTimeSeconds = CASE WHEN @BestTimeSeconds > 0 AND (target.BestTimeSeconds <= 0 OR @BestTimeSeconds < target.BestTimeSeconds) THEN @BestTimeSeconds ELSE target.BestTimeSeconds END,
               GameMode = @GameMode,
               Difficulty = @Difficulty,
               Hearts = 0,
               CreatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (UserId, PlayerName, Points, GameMode, Difficulty, Hearts, CreatedAt, StageId, BestTimeSeconds, Rank)
    VALUES (@UserId, @PlayerName, @Score, @GameMode, @Difficulty, 0, SYSUTCDATETIME(), @StageId, @BestTimeSeconds, 0);", connection))
            {
                mergeCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                mergeCommand.Parameters.Add("@StageId", SqlDbType.Int).Value = stageId;
                mergeCommand.Parameters.Add("@PlayerName", SqlDbType.NVarChar, 50).Value = userName;
                mergeCommand.Parameters.Add("@Score", SqlDbType.Int).Value = Math.Max(0, score);
                mergeCommand.Parameters.Add("@BestTimeSeconds", SqlDbType.Int).Value = Math.Max(0, bestTimeSeconds);
                mergeCommand.Parameters.Add("@GameMode", SqlDbType.Int).Value = (int)GameMode.SinglePlayer;
                mergeCommand.Parameters.Add("@Difficulty", SqlDbType.Int).Value = Math.Max(0, stageId - 1);
                return mergeCommand.ExecuteNonQuery() > 0;
            }
        }

        private static void RebuildStageRanks(SqlConnection connection, int stageId)
        {
            using (SqlCommand command = new SqlCommand(@"WITH Ranked AS (
    SELECT Id,
           ROW_NUMBER() OVER (ORDER BY Points DESC, CASE WHEN BestTimeSeconds <= 0 THEN 2147483647 ELSE BestTimeSeconds END ASC, CreatedAt ASC, Id ASC) AS NewRank
    FROM BattleRecords
    WHERE StageId = @StageId
)
UPDATE br
SET Rank = r.NewRank
FROM BattleRecords br
INNER JOIN Ranked r ON br.Id = r.Id;", connection))
            {
                command.Parameters.Add("@StageId", SqlDbType.Int).Value = stageId;
                command.ExecuteNonQuery();
            }
        }

        private static void LoadLeaderboardFromDatabase(PlayerData playerData)
        {
            LoadLeaderboardFromDatabase(playerData, null);
        }

        private static void LoadLeaderboardFromDatabase(PlayerData playerData, SqlConnection existingConnection)
        {
            if (playerData == null)
            {
                return;
            }

            bool ownsConnection = existingConnection == null;
            SqlConnection connection = existingConnection;
            try
            {
                EnsureLeaderboardSchema();
                if (ownsConnection)
                {
                    connection = new SqlConnection(AppPaths.LocalDatabaseConnectionString);
                    connection.Open();
                }

                List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
                using (SqlCommand command = new SqlCommand(@"SELECT StageId, PlayerName, Points, BestTimeSeconds, Rank
FROM BattleRecords
WHERE GameMode = @GameMode
ORDER BY StageId ASC, Rank ASC, Points DESC, BestTimeSeconds ASC, Id ASC", connection))
                {
                    command.Parameters.Add("@GameMode", SqlDbType.Int).Value = (int)GameMode.SinglePlayer;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entries.Add(new LeaderboardEntry
                            {
                                StageId = reader.IsDBNull(0) ? 1 : reader.GetInt32(0),
                                UserName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                Score = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                                BestTimeSeconds = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                                Rank = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
                            });
                        }
                    }
                }

                playerData.LeaderboardEntries = entries;
                foreach (StageRecord record in playerData.StageRecords)
                {
                    LeaderboardEntry selfEntry = entries.FirstOrDefault(e => e.StageId == record.StageId && string.Equals(e.UserName, playerData.UserName, StringComparison.OrdinalIgnoreCase));
                    record.Rank = selfEntry == null ? 0 : selfEntry.Rank;
                }
            }
            catch
            {
                if (playerData.LeaderboardEntries == null)
                {
                    playerData.LeaderboardEntries = new List<LeaderboardEntry>();
                }
            }
            finally
            {
                if (ownsConnection && connection != null)
                {
                    connection.Dispose();
                }
            }
        }

        private static void ApplyRankToLocalStageRecord(PlayerData playerData, int stageId)
        {
            if (playerData == null)
            {
                return;
            }

            StageRecord record = playerData.GetStageRecord(stageId);
            if (record == null)
            {
                return;
            }

            LeaderboardEntry selfEntry = playerData.LeaderboardEntries.FirstOrDefault(e => e.StageId == stageId && string.Equals(e.UserName, playerData.UserName, StringComparison.OrdinalIgnoreCase));
            record.Rank = selfEntry == null ? 0 : selfEntry.Rank;
        }

        private static int ClampUpgradeLevel(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > TankConfig.MaxUpgradeLevel)
            {
                return TankConfig.MaxUpgradeLevel;
            }

            return value;
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
