using System;
using System.Drawing;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    public partial class GameLobby : Form
    {
        private PlayerData playerData;
        private TankType selectedTank;
        private GameMode selectedMode = GameMode.SinglePlayer;

        private Panel tankPanel;
        private Panel upgradePanel;
        private Panel modePanel;
        private Label expLabel;
        private Button startGameButton;
        private Bitmap lobbyBackground;

        public GameMode SelectedMode { get { return selectedMode; } }
        public TankType SelectedTank { get { return selectedTank; } }

        public GameLobby()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            // 先加载数据
            playerData = DataManager.CurrentPlayer;
            selectedTank = playerData.SelectedTank;

            // 再初始化组件
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "坦克大战 - 游戏大厅";
            this.Size = new Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // 设置背景图片
            try
            {
                if (Peace_of_Tanks.Properties.Resources.back != null)
                {
                    lobbyBackground = Peace_of_Tanks.Properties.Resources.back;
                }
                else
                {
                    this.BackColor = Color.FromArgb(40, 40, 40);
                }
            }
            catch
            {
                this.BackColor = Color.FromArgb(40, 40, 40);
            }

            // 标题
            Label titleLabel = new Label
            {
                Text = "坦克大战",
                Font = new Font("微软雅黑", 32, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(520, 30),
                BackColor = Color.Transparent
            };
            this.Controls.Add(titleLabel);

            // 经验显示 - 放在右上角
            expLabel = new Label
            {
                Text = $"总经验: {playerData.TotalExp}",
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                ForeColor = Color.Gold,
                AutoSize = true,
                Location = new Point(1050, 30),
                BackColor = Color.Transparent
            };
            this.Controls.Add(expLabel);

            // 坦克选择面板
            CreateTankSelectionPanel();

            // 升级面板
            CreateUpgradePanel();

            // 模式选择面板
            CreateModeSelectionPanel();

            // 开始游戏按钮 - 仿照图片中的金色大按钮
            startGameButton = new Button
            {
                Text = "开始游戏",
                Font = new Font("微软雅黑", 20, FontStyle.Bold),
                Size = new Size(300, 70),
                Location = new Point(850, 630),
                BackColor = Color.FromArgb(218, 165, 32),  // 金色
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            startGameButton.FlatAppearance.BorderColor = Color.FromArgb(184, 134, 11);
            startGameButton.FlatAppearance.BorderSize = 3;
            startGameButton.Click += StartGameButton_Click;
            this.Controls.Add(startGameButton);

            // 退出按钮
            Button backButton = new Button
            {
                Text = "退出游戏",
                Font = new Font("微软雅黑", 16, FontStyle.Bold),
                Size = new Size(200, 70),
                Location = new Point(100, 630),
                BackColor = Color.FromArgb(150, 60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            backButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            backButton.FlatAppearance.BorderSize = 2;
            backButton.Click += BackButton_Click;
            this.Controls.Add(backButton);

            RefreshUI();
        }

        public void PrepareForIntroFade()
        {
            if (!IsHandleCreated)
            {
                CreateControl();
            }

            PerformLayout();
            Invalidate(true);
            Update();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (lobbyBackground != null)
            {
                e.Graphics.DrawImage(lobbyBackground, ClientRectangle);
            }
            else
            {
                base.OnPaintBackground(e);
            }
        }

        private void CreateTankSelectionPanel()
        {
            Label label = new Label
            {
                Text = "选择坦克",
                Font = new Font("微软雅黑", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(50, 100),
                BackColor = Color.Transparent
            };
            this.Controls.Add(label);

            tankPanel = new Panel
            {
                Location = new Point(50, 140),
                Size = new Size(1180, 160),
                BackColor = Color.FromArgb(120, 30, 30, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(tankPanel);

            // 创建坦克按钮
            int x = 20;
            int cardWidth = 220;
            int cardSpacing = 15;

            foreach (TankType type in Enum.GetValues(typeof(TankType)))
            {
                var tankData = playerData.Tanks[type];
                var config = TankConfigs.GetConfig(type);

                Panel tankCard = new Panel
                {
                    Location = new Point(x, 10),
                    Size = new Size(cardWidth, 140),
                    BackColor = Color.FromArgb(150, 50, 50, 50),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = type
                };

                // 坦克名称
                Label nameLabel = new Label
                {
                    Text = GetTankName(type),
                    Font = new Font("微软雅黑", 14, FontStyle.Bold),
                    ForeColor = GetTankColor(type),
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(cardWidth, 30),
                    Location = new Point(0, 5),
                    BackColor = Color.Transparent
                };
                tankCard.Controls.Add(nameLabel);

                // 状态标签
                Label statusLabel = new Label
                {
                    Font = new Font("微软雅黑", 10),
                    ForeColor = Color.White,
                    AutoSize = false,
                    Size = new Size(cardWidth, 65),
                    Location = new Point(0, 40),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };

                if (tankData.IsUnlocked)
                {
                    statusLabel.Text = $"等级: {tankData.Level}\n攻击: {config.GetAttackAtLevel(tankData.Level)}\n生命: {config.GetHealthAtLevel(tankData.Level)}";
                }
                else
                {
                    statusLabel.Text = $"未解锁\n需要经验: {config.UnlockExp}";
                    statusLabel.ForeColor = Color.Gray;
                    tankCard.BackColor = Color.FromArgb(100, 30, 30, 30);
                }
                tankCard.Controls.Add(statusLabel);

                // 选择按钮
                Button selectBtn = new Button
                {
                    Text = tankData.IsUnlocked ? "选择" : "锁定",
                    Size = new Size(cardWidth - 20, 35),
                    Location = new Point(10, 100),
                    Enabled = tankData.IsUnlocked,
                    BackColor = Color.FromArgb(200, 139, 69, 19),
                    ForeColor = Color.White,
                    Font = new Font("微软雅黑", 12, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Tag = type
                };
                selectBtn.FlatAppearance.BorderColor = Color.FromArgb(101, 67, 33);
                selectBtn.FlatAppearance.BorderSize = 2;
                selectBtn.Click += SelectTankButton_Click;
                tankCard.Controls.Add(selectBtn);

                tankPanel.Controls.Add(tankCard);
                x += cardWidth + cardSpacing;
            }
        }

        private void CreateUpgradePanel()
        {
            Label label = new Label
            {
                Text = "坦克升级",
                Font = new Font("微软雅黑", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(50, 320),
                BackColor = Color.Transparent
            };
            this.Controls.Add(label);

            upgradePanel = new Panel
            {
                Location = new Point(50, 360),
                Size = new Size(1180, 120),
                BackColor = Color.FromArgb(120, 30, 30, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(upgradePanel);
        }

        private void CreateModeSelectionPanel()
        {
            Label label = new Label
            {
                Text = "游戏模式",
                Font = new Font("微软雅黑", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(50, 500),
                BackColor = Color.Transparent
            };
            this.Controls.Add(label);

            modePanel = new Panel
            {
                Location = new Point(50, 540),
                Size = new Size(1180, 80),
                BackColor = Color.FromArgb(120, 30, 30, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(modePanel);

            // 模式按钮
            string[] modeNames = { "单人模式", "双人匹配", "三人匹配" };
            GameMode[] modes = { GameMode.SinglePlayer, GameMode.TwoPlayer, GameMode.ThreePlayer };

            for (int i = 0; i < modeNames.Length; i++)
            {
                Button modeBtn = new Button
                {
                    Text = modeNames[i],
                    Size = new Size(380, 60),
                    Location = new Point(20 + i * 390, 10),
                    BackColor = Color.FromArgb(200, 139, 69, 19),
                    ForeColor = Color.White,
                    Font = new Font("微软雅黑", 16, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Tag = modes[i]
                };
                modeBtn.FlatAppearance.BorderColor = Color.FromArgb(101, 67, 33);
                modeBtn.FlatAppearance.BorderSize = 2;
                modeBtn.Click += ModeButton_Click;
                modePanel.Controls.Add(modeBtn);
            }
        }

        private void SelectTankButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            selectedTank = (TankType)btn.Tag;
            playerData.SelectedTank = selectedTank;
            DataManager.SaveData();
            RefreshUI();
        }

        private void ModeButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            selectedMode = (GameMode)btn.Tag;
            RefreshModeButtons();
        }

        private void RefreshModeButtons()
        {
            foreach (Control ctrl in modePanel.Controls)
            {
                if (ctrl is Button btn)
                {
                    if ((GameMode)btn.Tag == selectedMode)
                    {
                        btn.BackColor = Color.FromArgb(218, 165, 32);  // 选中状态为金色
                        btn.FlatAppearance.BorderColor = Color.FromArgb(184, 134, 11);
                    }
                    else
                    {
                        btn.BackColor = Color.FromArgb(200, 139, 69, 19);  // 未选中为棕色
                        btn.FlatAppearance.BorderColor = Color.FromArgb(101, 67, 33);
                    }
                }
            }
        }

        private void RefreshUI()
        {
            expLabel.Text = $"总经验: {playerData.TotalExp}";

            // 刷新坦克选择面板
            foreach (Control ctrl in tankPanel.Controls)
            {
                if (ctrl is Panel tankCard)
                {
                    TankType type = (TankType)tankCard.Tag;
                    if (type == selectedTank)
                    {
                        tankCard.BackColor = Color.FromArgb(150, 0, 100, 0);  // 选中状态为绿色
                    }
                    else if (playerData.Tanks[type].IsUnlocked)
                    {
                        tankCard.BackColor = Color.FromArgb(150, 50, 50, 50);
                    }
                }
            }

            // 刷新升级面板
            upgradePanel.Controls.Clear();

            var selectedTankData = playerData.Tanks[selectedTank];
            var config = TankConfigs.GetConfig(selectedTank);

            Label infoLabel = new Label
            {
                Text = $"当前选中: {GetTankName(selectedTank)} | 等级: {selectedTankData.Level}/5 | " +
                       $"攻击力: {config.GetAttackAtLevel(selectedTankData.Level)} | " +
                       $"生命力: {config.GetHealthAtLevel(selectedTankData.Level)}",
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 20),
                BackColor = Color.Transparent
            };
            upgradePanel.Controls.Add(infoLabel);

            if (selectedTankData.Level < 5)
            {
                int expNeeded = selectedTankData.GetExpForNextLevel();
                Label expInfo = new Label
                {
                    Text = $"升级进度: {selectedTankData.CurrentExp}/{expNeeded}",
                    Font = new Font("微软雅黑", 12),
                    ForeColor = Color.Yellow,
                    AutoSize = true,
                    Location = new Point(20, 55),
                    BackColor = Color.Transparent
                };
                upgradePanel.Controls.Add(expInfo);

                Button upgradeBtn = new Button
                {
                    Text = "升级",
                    Size = new Size(150, 50),
                    Location = new Point(1000, 35),
                    BackColor = selectedTankData.CanUpgrade() ? Color.FromArgb(218, 165, 32) : Color.Gray,
                    ForeColor = Color.White,
                    Font = new Font("微软雅黑", 14, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Enabled = selectedTankData.CanUpgrade()
                };
                upgradeBtn.FlatAppearance.BorderColor = Color.FromArgb(184, 134, 11);
                upgradeBtn.FlatAppearance.BorderSize = 2;
                upgradeBtn.Click += UpgradeButton_Click;
                upgradePanel.Controls.Add(upgradeBtn);
            }
            else
            {
                Label maxLabel = new Label
                {
                    Text = "已达最高等级！",
                    Font = new Font("微软雅黑", 14, FontStyle.Bold),
                    ForeColor = Color.Gold,
                    AutoSize = true,
                    Location = new Point(20, 55),
                    BackColor = Color.Transparent
                };
                upgradePanel.Controls.Add(maxLabel);
            }

            RefreshModeButtons();
        }

        private void UpgradeButton_Click(object sender, EventArgs e)
        {
            if (playerData.UpgradeTank(selectedTank))
            {
                DataManager.SaveData();
                MessageBox.Show($"{GetTankName(selectedTank)}升级成功！", "升级", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshUI();
            }
        }

        private void StartGameButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private string GetTankName(TankType type)
        {
            switch (type)
            {
                case TankType.Green: return "绿色坦克";
                case TankType.Yellow: return "黄色坦克";
                case TankType.Blue: return "蓝色坦克";
                case TankType.Red: return "红色坦克";
                case TankType.Purple: return "紫色坦克";
                default: return "未知";
            }
        }

        private Color GetTankColor(TankType type)
        {
            switch (type)
            {
                case TankType.Green: return Color.LimeGreen;
                case TankType.Yellow: return Color.Yellow;
                case TankType.Blue: return Color.DeepSkyBlue;
                case TankType.Red: return Color.Red;
                case TankType.Purple: return Color.Purple;
                default: return Color.White;
            }
        }
    }
}
