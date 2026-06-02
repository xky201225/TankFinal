using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Peace_of_Tanks
{
    // 坦克类型枚举
    public enum TankType
    {
        Green = 0,   // 绿色 Lv.png
        Yellow = 1,  // 黄色 Huang.png
        Blue = 2,    // 蓝色 Lan.png
        Red = 3,     // 红色 Hong.png
        Purple = 4   // 紫色 Zi.png
    }

    // 坦克配置信息
    public class TankConfig
    {
        public TankType Type { get; set; }
        public string ImageName { get; set; }
        public int BaseAttack { get; set; }
        public int BaseHealth { get; set; }
        public int UnlockExp { get; set; }  // 解锁所需经验
        public int AttackPerLevel { get; set; } = 5;
        public int HealthPerLevel { get; set; } = 10;
        public int MaxLevel { get; set; } = 5;

        // 获取指定等级的属性
        public int GetAttackAtLevel(int level)
        {
            return BaseAttack + (level - 1) * AttackPerLevel;
        }

        public int GetHealthAtLevel(int level)
        {
            return BaseHealth + (level - 1) * HealthPerLevel;
        }

        // 获取升级所需经验
        public static int GetExpForUpgrade(int currentLevel)
        {
            // 第一次升级需要10，之后每次递增10
            return currentLevel * 10;
        }
    }

    // 坦克配置管理
    public static class TankConfigs
    {
        private static Dictionary<TankType, TankConfig> configs;

        static TankConfigs()
        {
            configs = new Dictionary<TankType, TankConfig>
            {
                {
                    TankType.Green,
                    new TankConfig
                    {
                        Type = TankType.Green,
                        ImageName = "Lv.png",
                        BaseAttack = 10,
                        BaseHealth = 30,
                        UnlockExp = 0  // 初始解锁
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
        }

        public static TankConfig GetConfig(TankType type)
        {
            return configs[type];
        }

        public static TankConfig[] GetAllConfigs()
        {
            return new TankConfig[]
            {
                configs[TankType.Green],
                configs[TankType.Yellow],
                configs[TankType.Blue],
                configs[TankType.Red],
                configs[TankType.Purple]
            };
        }
    }

    // 玩家坦克数据
    public class PlayerTankData
    {
        public TankType Type { get; set; }
        public int Level { get; set; } = 1;
        public int CurrentExp { get; set; } = 0;
        public bool IsUnlocked { get; set; } = false;

        public int GetExpForNextLevel()
        {
            if (Level >= 5) return -1;  // 已满级
            return TankConfig.GetExpForUpgrade(Level);
        }

        public bool CanUpgrade()
        {
            return Level < 5 && CurrentExp >= GetExpForNextLevel();
        }
    }

    // 玩家数据
    public class PlayerData
    {
        public int TotalExp { get; set; } = 0;
        public Dictionary<TankType, PlayerTankData> Tanks { get; set; }
        public TankType SelectedTank { get; set; } = TankType.Green;

        public PlayerData()
        {
            Tanks = new Dictionary<TankType, PlayerTankData>();

            // 初始化所有坦克数据
            foreach (TankType type in Enum.GetValues(typeof(TankType)))
            {
                Tanks[type] = new PlayerTankData
                {
                    Type = type,
                    Level = 1,
                    CurrentExp = 0,
                    IsUnlocked = type == TankType.Green  // 只有绿色坦克初始解锁
                };
            }
        }

        // 添加经验值
        public void AddExp(int exp)
        {
            TotalExp += exp;

            // 检查是否可以解锁新坦克
            foreach (var tank in Tanks.Values)
            {
                if (!tank.IsUnlocked)
                {
                    var config = TankConfigs.GetConfig(tank.Type);
                    if (TotalExp >= config.UnlockExp)
                    {
                        tank.IsUnlocked = true;
                    }
                }
            }
        }

        // 升级坦克
        public bool UpgradeTank(TankType type)
        {
            if (!Tanks.ContainsKey(type))
                return false;

            var tank = Tanks[type];
            if (!tank.CanUpgrade())
                return false;

            int expNeeded = tank.GetExpForNextLevel();
            tank.CurrentExp -= expNeeded;
            tank.Level++;
            return true;
        }

        // 添加坦克经验（用于坦克升级）
        public void AddTankExp(TankType type, int exp)
        {
            if (Tanks.ContainsKey(type) && Tanks[type].IsUnlocked)
            {
                Tanks[type].CurrentExp += exp;
            }
        }

        // 获取当前选中坦克的攻击力
        public int GetSelectedTankAttack()
        {
            var tank = Tanks[SelectedTank];
            var config = TankConfigs.GetConfig(SelectedTank);
            return config.GetAttackAtLevel(tank.Level);
        }

        // 获取当前选中坦克的生命力
        public int GetSelectedTankHealth()
        {
            var tank = Tanks[SelectedTank];
            var config = TankConfigs.GetConfig(SelectedTank);
            return config.GetHealthAtLevel(tank.Level);
        }
    }

    // 游戏模式
    public enum GameMode
    {
        SinglePlayer,  // 单人模式
        TwoPlayer,     // 双人匹配
        ThreePlayer    // 三人匹配
    }

    // 数据管理器
    public static class DataManager
    {
        private static string dataFilePath = "playerdata.json";
        private static PlayerData currentPlayerData;

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
                if (File.Exists(dataFilePath))
                {
                    string json = File.ReadAllText(dataFilePath);
                    currentPlayerData = JsonSerializer.Deserialize<PlayerData>(json);
                }
                else
                {
                    currentPlayerData = new PlayerData();
                    SaveData();
                }
            }
            catch (Exception)
            {
                currentPlayerData = new PlayerData();
            }
        }

        public static void SaveData()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(currentPlayerData, options);
                File.WriteAllText(dataFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("保存数据失败: " + ex.Message);
            }
        }

        // 奖励经验
        public static void RewardExp(GameMode mode, int rank)
        {
            int exp = 0;

            switch (mode)
            {
                case GameMode.TwoPlayer:
                    exp = rank == 1 ? 15 : 10;  // 获胜15，失败10
                    break;
                case GameMode.ThreePlayer:
                    // 第一名15，第二名10，第三名5
                    exp = rank == 1 ? 15 : (rank == 2 ? 10 : 5);
                    break;
            }

            if (exp > 0)
            {
                CurrentPlayer.AddExp(exp);
                // 也给当前使用的坦克添加经验
                CurrentPlayer.AddTankExp(CurrentPlayer.SelectedTank, exp);
                SaveData();
            }
        }
    }
}
