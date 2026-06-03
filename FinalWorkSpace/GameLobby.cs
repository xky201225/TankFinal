using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    public partial class GameLobby : Form
    {
        private const string DefaultChatHost = "127.0.0.1";
        private const int DefaultChatPort = 51888;

        private readonly PlayerData playerData;
        private readonly List<Control> pageControls = new List<Control>();
        private TankType selectedTank;
        private GameMode selectedMode = GameMode.SinglePlayer;
        private GameLaunchOptions launchOptions;
        private Label expLabel;
        private TcpGameClient chatClient;
        private RichTextBox chatLog;
        private TextBox chatInput;
        private Label chatStatusLabel;
        private string chatPlayerName;
        private string lobbyServerHost = DefaultChatHost;
        private int lobbyServerPort = DefaultChatPort;
        private bool lobbyServerPromptShown;
        private bool useAuthenticatedServer;

        public GameMode SelectedMode { get { return selectedMode; } }
        public TankType SelectedTank { get { return selectedTank; } }
        internal GameLaunchOptions LaunchOptions
        {
            get
            {
                return launchOptions ?? new GameLaunchOptions
                {
                    Mode = selectedMode,
                TankType = selectedTank,
                    StartImmediately = true
                };
            }
        }

        public GameLobby()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            playerData = DataManager.CurrentPlayer;
            selectedTank = playerData.SelectedTank;
            useAuthenticatedServer = DataManager.HasAuthenticatedServer;
            if (useAuthenticatedServer)
            {
                lobbyServerHost = DataManager.CurrentServerHost;
                lobbyServerPort = DataManager.CurrentServerPort;
                lobbyServerPromptShown = true;
            }
            chatPlayerName = string.IsNullOrWhiteSpace(playerData.UserName) ? "玩家" + Environment.UserName : playerData.UserName;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "坦克大厅";
            ClientSize = new Size(1280, 720);
            MinimumSize = new Size(1100, 660);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            DoubleBuffered = true;
            Resize += (s, e) => Invalidate();
            Shown += GameLobby_Shown;

            BuildChrome();
            ShowHomePage();
        }

        private void GameLobby_Shown(object sender, EventArgs e)
        {
            if (useAuthenticatedServer)
            {
                ConnectLobbyChat();
                return;
            }

            PromptForLobbyServerIfNeeded();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            CloseLobbyChat();
            base.OnFormClosed(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Image bg = Peace_of_Tanks.Properties.Resources.back;
            if (bg != null)
            {
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                Rectangle dest = GetCoverRectangle(bg.Size, ClientSize);
                e.Graphics.DrawImage(bg, dest);
            }

            using (LinearGradientBrush shade = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(165, 5, 12, 22),
                Color.FromArgb(70, 20, 28, 44),
                LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(shade, ClientRectangle);
            }
        }

        private static Rectangle GetCoverRectangle(Size imageSize, Size targetSize)
        {
            if (imageSize.Width <= 0 || imageSize.Height <= 0 || targetSize.Width <= 0 || targetSize.Height <= 0)
            {
                return new Rectangle(Point.Empty, targetSize);
            }

            float scale = Math.Max(targetSize.Width / (float)imageSize.Width, targetSize.Height / (float)imageSize.Height);
            int width = (int)Math.Ceiling(imageSize.Width * scale);
            int height = (int)Math.Ceiling(imageSize.Height * scale);
            return new Rectangle((targetSize.Width - width) / 2, (targetSize.Height - height) / 2, width, height);
        }

        private void BuildChrome()
        {
            expLabel = new Label
            {
                Font = new Font("Microsoft YaHei UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 231, 145),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(18, 18)
            };
            Controls.Add(expLabel);

            Label titleLabel = new Label
            {
                Text = "钢铁战神",
                Font = new Font("Microsoft YaHei UI", 22, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(18, 44)
            };
            Controls.Add(titleLabel);

            RefreshTopExp();
        }

        private void ClearPage()
        {
            foreach (Control control in pageControls)
            {
                Controls.Remove(control);
                control.Dispose();
            }

            pageControls.Clear();
            chatLog = null;
            chatInput = null;
            chatStatusLabel = null;
        }

        private void ShowHomePage()
        {
            ClearPage();
            RefreshTopExp();

            pageControls.Add(CreateMainButton(
                "开始游戏",
                "匹配对战 >>",
                Peace_of_Tanks.Properties.Resources.lobby_icon_match,
                new Point(24, 150),
                true,
                (s, e) => ShowMatchPage()));

            pageControls.Add(CreateMainButton(
                "单人挑战",
                "单人闯关 >>",
                Peace_of_Tanks.Properties.Resources.lobby_icon_solo,
                new Point(24, 255),
                false,
                (s, e) => ShowSinglePlayerSetupPage()));

            pageControls.Add(CreateMainButton(
                "坦克库",
                "选择 / 升级 / 解锁 >>",
                Peace_of_Tanks.Properties.Resources.lobby_icon_tanklib,
                new Point(24, 360),
                false,
                (s, e) => ShowTankLibraryPage()));

            pageControls.Add(CreateChatPanel());

            AddPageControls();
            if (lobbyServerPromptShown)
            {
                ConnectLobbyChat();
            }
        }

        private void PromptForLobbyServerIfNeeded()
        {
            if (lobbyServerPromptShown || IsDisposed)
            {
                return;
            }

            lobbyServerPromptShown = true;
            if (chatClient != null && chatClient.IsConnected)
            {
                SetChatStatus("聊天服务器已连接");
                return;
            }

            ShowLobbyServerDialog();
        }

        private bool ShowLobbyServerDialog()
        {
            if (chatClient != null && chatClient.IsConnected)
            {
                SetChatStatus("聊天服务器已连接");
                return true;
            }

            using (Form dialog = new Form())
            {
                dialog.Text = "服务器设置";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(330, 145);
                dialog.ShowInTaskbar = false;

                Label hostLabel = new Label { Text = "服务器地址", Left = 22, Top = 22, Width = 90, AutoSize = true };
                TextBox hostText = new TextBox { Left = 120, Top = 18, Width = 180, Text = lobbyServerHost };
                Label portLabel = new Label { Text = "端口", Left = 22, Top = 62, Width = 90, AutoSize = true };
                TextBox portText = new TextBox { Left = 120, Top = 58, Width = 180, Text = lobbyServerPort.ToString() };

                Button okButton = new Button { Text = "确定", Left = 120, Top = 100, Width = 80, DialogResult = DialogResult.OK };
                Button cancelButton = new Button { Text = "取消", Left = 220, Top = 100, Width = 80, DialogResult = DialogResult.Cancel };

                dialog.Controls.AddRange(new Control[] { hostLabel, hostText, portLabel, portText, okButton, cancelButton });
                dialog.AcceptButton = okButton;
                dialog.CancelButton = cancelButton;

                while (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string host = hostText.Text.Trim();
                    int port;
                    if (host.Length == 0)
                    {
                        MessageBox.Show(this, "请输入服务器地址。", "服务器设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    if (!int.TryParse(portText.Text.Trim(), out port) || port <= 0 || port > 65535)
                    {
                        MessageBox.Show(this, "请输入有效端口。", "服务器设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    lobbyServerHost = host;
                    lobbyServerPort = port;

                    string error;
                    if (ReconnectLobbyChat(out error))
                    {
                        return true;
                    }

                    MessageBox.Show(
                        this,
                        "连接服务器失败：" + error + Environment.NewLine + "当前只能进行单机游戏，请重新输入服务器地址。",
                        "服务器设置",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            return ReconnectLobbyChat();
        }

        private bool ReconnectLobbyChat()
        {
            string error;
            return ReconnectLobbyChat(out error);
        }

        private bool ReconnectLobbyChat(out string error)
        {
            if (chatClient != null && chatClient.IsConnected)
            {
                error = string.Empty;
                SetChatStatus("聊天服务器已连接");
                return true;
            }

            CloseLobbyChat();
            return ConnectLobbyChat(out error);
        }

        private bool EnsureServerReady(string host, int port)
        {
            string targetHost = string.IsNullOrWhiteSpace(host) ? DefaultChatHost : host.Trim();
            bool serverChanged = !string.Equals(lobbyServerHost, targetHost, StringComparison.OrdinalIgnoreCase) || lobbyServerPort != port;
            if (!serverChanged && chatClient != null && chatClient.IsConnected)
            {
                SetChatStatus("聊天服务器已连接");
                return true;
            }

            lobbyServerHost = targetHost;
            lobbyServerPort = port;

            if (serverChanged)
            {
                CloseLobbyChat();
            }

            string error;
            if (ConnectLobbyChat(out error))
            {
                return true;
            }

            MessageBox.Show(
                this,
                "连接服务器失败：" + error + Environment.NewLine + "当前只能进行单机游戏，请重新输入服务器地址。",
                "服务器连接失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return ShowLobbyServerDialog();
        }

        private Control CreateChatPanel()
        {
            HudPanel panel = new HudPanel
            {
                Size = new Size(520, 108),
                Location = new Point(24, ClientSize.Height - 122),
                BackColor = Color.FromArgb(14, 24, 36)
            };

            chatLog = new RichTextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(14, 24, 36),
                ForeColor = Color.FromArgb(255, 223, 130),
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
                Location = new Point(14, 10),
                Size = new Size(492, 42),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            panel.Controls.Add(chatLog);

            chatInput = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(25, 36, 50),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Regular),
                Location = new Point(14, 62),
                Size = new Size(370, 25)
            };
            chatInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SendLobbyChat();
                }
            };
            panel.Controls.Add(chatInput);

            Button send = CreateSmallButton("发送", new Point(394, 60), true);
            send.Size = new Size(72, 28);
            send.Click += (s, e) => SendLobbyChat();
            panel.Controls.Add(send);

            chatStatusLabel = new Label
            {
                Text = "聊天服务器未连接",
                Font = new Font("Microsoft YaHei UI", 8, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 180, 195),
                BackColor = Color.FromArgb(14, 24, 36),
                AutoSize = true,
                Location = new Point(14, 88)
            };
            panel.Controls.Add(chatStatusLabel);

            AppendChat("系统", "大厅聊天会通过服务器广播给所有在线用户。");
            return panel;
        }

        private HudMainButton CreateMainButton(string title, string subtitle, Image icon, Point location, bool primary, EventHandler click)
        {
            HudMainButton button = new HudMainButton
            {
                Title = title,
                Subtitle = subtitle,
                Icon = icon,
                Primary = primary,
                Size = new Size(245, 88),
                Location = location
            };
            button.Click += click;
            return button;
        }

        private static object[] CreateSystemMapItems(bool includeCustom)
        {
            object[] items = new object[BattleMapFactory.SystemMapCount + (includeCustom ? 1 : 0)];
            for (int i = 0; i < BattleMapFactory.SystemMapCount; i++)
            {
                items[i] = BattleMapFactory.GetSystemMapName(i);
            }

            if (includeCustom)
            {
                items[items.Length - 1] = "自定义地图";
            }

            return items;
        }

        private static GameMapConfig[] CreateSystemMaps()
        {
            GameMapConfig[] maps = new GameMapConfig[BattleMapFactory.SystemMapCount];
            for (int i = 0; i < maps.Length; i++)
            {
                maps[i] = BattleMapFactory.CreateSystemMap(i);
            }

            return maps;
        }

        private void ShowMatchPage()
        {
            ClearPage();

            pageControls.Add(new HudHeader
            {
                Title = "匹配模式",
                Subtitle = "创建房间、搜索缺人房间，或输入房间号加入",
                Size = new Size(650, 74),
                Location = new Point(330, 82)
            });

            pageControls.Add(CreatePanelButton("创建房间", "设置服务器、房间号和难度，创建后等待玩家加入。", new Point(330, 190), Peace_of_Tanks.Properties.Resources.lobby_icon_people, (s, e) => ShowCreateRoomPage()));

            pageControls.Add(CreatePanelButton("搜索房间", "自动搜索当前服务器里缺人的房间。", new Point(650, 190), Peace_of_Tanks.Properties.Resources.lobby_icon_match, (s, e) =>
            {
                StartNetworkMatch(NetworkJoinMode.Search, string.Empty, NetworkDifficulty.Easy);
            }));

            pageControls.Add(CreateJoinRoomPanel());

            pageControls.Add(CreateBackButton());
            AddPageControls();
        }

        private Control CreateJoinRoomPanel()
        {
            HudPanel panel = new HudPanel
            {
                Size = new Size(600, 118),
                Location = new Point(330, 360),
                BackColor = Color.FromArgb(14, 24, 36)
            };

            Label title = CreatePanelLabel("输入房间号加入", new Point(22, 18), 16, Color.White, FontStyle.Bold);
            panel.Controls.Add(title);

            TextBox roomInput = CreateHudTextBox(new Point(22, 60), new Size(260, 28), "ROOM-001");
            panel.Controls.Add(roomInput);

            Button join = CreateSmallButton("加入房间", new Point(304, 58), true);
            join.Size = new Size(96, 30);
            join.Click += (s, e) =>
            {
                string roomId = roomInput.Text.Trim();
                if (roomId.Length == 0)
                {
                    MessageBox.Show(this, "请输入房间号。", "加入房间", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                StartNetworkMatch(NetworkJoinMode.JoinByRoomId, roomId, NetworkDifficulty.Easy);
            };
            panel.Controls.Add(join);

            Label note = CreatePanelLabel("房间号会发送到服务器，用于加入指定房间。", new Point(420, 62), 9, Color.FromArgb(175, 190, 210), FontStyle.Regular);
            panel.Controls.Add(note);
            return panel;
        }

        private void ShowCreateRoomPage()
        {
            ClearPage();

            pageControls.Add(new HudHeader
            {
                Title = "创建房间",
                Subtitle = "设置房间信息后直接进入等待页面",
                Size = new Size(650, 74),
                Location = new Point(330, 82)
            });

            HudPanel panel = new HudPanel { Size = new Size(420, 470), Location = new Point(285, 160), BackColor = Color.FromArgb(14, 24, 36) };
            TextBox host = AddField(panel, "服务器地址", lobbyServerHost, 24, 24);
            TextBox port = AddField(panel, "端口", lobbyServerPort.ToString(), 230, 24);
            TextBox name = AddField(panel, "玩家名称", chatPlayerName, 24, 92);
            TextBox roomId = AddField(panel, "房间号", "ROOM-" + DateTime.Now.ToString("HHmm"), 230, 92);

            ComboBox playerCount = AddCombo(panel, "匹配人数", new object[] { "双人模式", "三人模式" }, 24, 164);
            ComboBox battleRule = AddCombo(panel, "对抗模式", new object[] { "自由战场", "阵地防守" }, 230, 164);
            ComboBox difficulty = AddCombo(panel, "难度", new object[] { "简单", "中等", "困难" }, 24, 236);
            ComboBox mapSelect = AddCombo(panel, "地图", CreateSystemMapItems(true), 230, 236);

            GameMapConfig[] systemMaps = CreateSystemMaps();
            GameMapConfig customMap = BattleMapFactory.CreateEmpty("自定义地图");

            MapEditorControl mapPreview = new MapEditorControl
            {
                Size = new Size(420, 420),
                Location = new Point(735, 160),
                BackColor = Color.FromArgb(14, 24, 36),
                Map = systemMaps[0],
                Editable = false,
                SelectedTile = MapTileType.Wall
            };

            Label previewTitle = CreatePanelLabel("地图预览", new Point(735, 132), 12, Color.White, FontStyle.Bold);
            pageControls.Add(previewTitle);
            pageControls.Add(mapPreview);

            HudPanel materialPanel = new HudPanel { Size = new Size(420, 64), Location = new Point(735, 592), BackColor = Color.FromArgb(14, 24, 36) };
            MapTileType[] materials = new[] { MapTileType.Empty, MapTileType.Wall, MapTileType.Water, MapTileType.Grass, MapTileType.Iron, MapTileType.Base };
            string[] materialNames = new[] { "空地", "砖墙", "水域", "草丛", "钢铁", "阵地" };
            for (int i = 0; i < materials.Length; i++)
            {
                Button materialButton = CreateSmallButton(materialNames[i], new Point(10 + i * 66, 18), true);
                materialButton.Size = new Size(58, 28);
                MapTileType tile = materials[i];
                materialButton.Click += (s, e) => mapPreview.SelectedTile = tile;
                materialPanel.Controls.Add(materialButton);
            }
            pageControls.Add(materialPanel);

            mapSelect.SelectedIndexChanged += (s, e) =>
            {
                bool custom = mapSelect.SelectedIndex >= systemMaps.Length;
                int selectedIndex = Math.Max(0, Math.Min(systemMaps.Length - 1, mapSelect.SelectedIndex));
                mapPreview.Map = custom ? customMap : systemMaps[selectedIndex];
                mapPreview.Editable = custom;
                mapPreview.Invalidate();
            };

            Button create = CreateSmallButton("创建并等待", new Point(24, 334), true);
            create.Size = new Size(128, 34);
            create.Click += (s, e) =>
            {
                int parsedPort;
                if (!int.TryParse(port.Text.Trim(), out parsedPort) || parsedPort <= 0 || parsedPort > 65535)
                {
                    MessageBox.Show(this, "请输入有效端口。", "创建房间", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                StartNetworkMatch(
                    NetworkJoinMode.Create,
                    roomId.Text.Trim(),
                    (NetworkDifficulty)difficulty.SelectedIndex,
                    host.Text.Trim(),
                    parsedPort,
                    name.Text.Trim(),
                    playerCount.SelectedIndex == 1 ? 3 : 2,
                    battleRule.SelectedIndex == 1 ? BattleRuleMode.BaseDefense : BattleRuleMode.FreeBattle,
                    mapPreview.Map);
            };
            panel.Controls.Add(create);

            Label hint = CreatePanelLabel("自由战场：先达到 15 击杀获胜\n阵地防守：阵地被毁则淘汰", new Point(24, 390), 9, Color.FromArgb(180, 195, 210), FontStyle.Regular);
            panel.Controls.Add(hint);

            pageControls.Add(panel);
            pageControls.Add(CreateBackButton());
            AddPageControls();
        }

        private void ShowSinglePlayerSetupPage()
        {
            ClearPage();
            RefreshTopExp();

            pageControls.Add(new HudHeader
            {
                Title = "单人挑战",
                Subtitle = "先选择战场地图，再选择挑战难度",
                Size = new Size(650, 74),
                Location = new Point(330, 82)
            });

            HudPanel panel = new HudPanel { Size = new Size(420, 330), Location = new Point(285, 180), BackColor = Color.FromArgb(14, 24, 36) };
            ComboBox mapSelect = AddCombo(panel, "地图", CreateSystemMapItems(false), 24, 32);
            ComboBox difficulty = AddCombo(panel, "模式", new object[] { "简单", "中等", "困难" }, 230, 32);

            Label hint = CreatePanelLabel("简单：场上保持 3 辆敌方坦克\n中等：场上保持 5 辆敌方坦克\n困难：场上保持 8 辆敌方坦克", new Point(24, 112), 10, Color.FromArgb(190, 205, 225), FontStyle.Regular);
            panel.Controls.Add(hint);

            GameMapConfig[] systemMaps = CreateSystemMaps();
            MapEditorControl mapPreview = new MapEditorControl
            {
                Size = new Size(420, 420),
                Location = new Point(735, 160),
                BackColor = Color.FromArgb(14, 24, 36),
                Map = systemMaps[0],
                Editable = false,
                SelectedTile = MapTileType.Wall
            };

            Label previewTitle = CreatePanelLabel("地图预览", new Point(735, 132), 12, Color.White, FontStyle.Bold);
            pageControls.Add(previewTitle);
            pageControls.Add(mapPreview);

            mapSelect.SelectedIndexChanged += (s, e) =>
            {
                int selectedIndex = Math.Max(0, Math.Min(systemMaps.Length - 1, mapSelect.SelectedIndex));
                mapPreview.Map = systemMaps[selectedIndex];
                mapPreview.Invalidate();
            };

            Button start = CreateSmallButton("开始挑战", new Point(24, 240), true);
            start.Size = new Size(128, 34);
            start.Click += (s, e) =>
            {
                int selectedIndex = Math.Max(0, Math.Min(systemMaps.Length - 1, mapSelect.SelectedIndex));
                StartSinglePlayer(systemMaps[selectedIndex], (NetworkDifficulty)difficulty.SelectedIndex);
            };
            panel.Controls.Add(start);

            pageControls.Add(panel);
            pageControls.Add(CreateBackButton());
            AddPageControls();
        }

        private void ShowTankLibraryPage()
        {
            ClearPage();
            RefreshTopExp();

            pageControls.Add(new HudHeader
            {
                Title = "坦克库",
                Subtitle = "选择、升级、解锁你的坦克",
                Size = new Size(650, 74),
                Location = new Point(330, 72)
            });

            int x = 300;
            int y = 175;
            int index = 0;
            foreach (TankType type in Enum.GetValues(typeof(TankType)))
            {
                pageControls.Add(CreateTankCard(type, new Point(x + (index % 3) * 280, y + (index / 3) * 205)));
                index++;
            }

            pageControls.Add(CreateBackButton());
            AddPageControls();
        }

        private Control CreatePanelButton(string title, string subtitle, Point location, Image icon, EventHandler click)
        {
            HudMainButton button = new HudMainButton
            {
                Title = title,
                Subtitle = subtitle,
                Icon = icon,
                Primary = title.Contains("创建"),
                Size = new Size(280, 130),
                Location = location
            };
            button.Click += click;
            return button;
        }

        private Control CreateTankCard(TankType type, Point location)
        {
            PlayerTankData tankData = playerData.Tanks[type];
            TankConfig config = TankConfigs.GetConfig(type);
            bool canUnlock = !tankData.IsUnlocked && playerData.TotalExp >= config.UnlockExp;

            TankCardControl card = new TankCardControl
            {
                TankType = type,
                TankName = GetTankName(type),
                TankColor = GetTankColor(type),
                TankImage = GetTankImage(type),
                TankData = tankData,
                TankConfig = config,
                PlayerData = playerData,
                IsSelected = selectedTank == type,
                CanUnlock = canUnlock,
                Size = new Size(250, 175),
                Location = location
            };

            card.SelectOrUnlockClicked += selectedType =>
            {
                PlayerTankData currentTank = playerData.Tanks[selectedType];
                if (currentTank.IsUnlocked)
                {
                    selectedTank = selectedType;
                    playerData.SelectedTank = selectedType;
                    DataManager.SaveData();
                    ShowTankLibraryPage();
                }
                else if (playerData.UnlockTank(selectedType))
                {
                    DataManager.SaveData();
                    RefreshTopExp();
                    ShowTankLibraryPage();
                }
            };

            card.UpgradeClicked += selectedType =>
            {
                if (playerData.UpgradeTank(selectedType))
                {
                    DataManager.SaveData();
                    RefreshTopExp();
                    ShowTankLibraryPage();
                }
            };

            return card;
        }

        private Button CreateSmallButton(string text, Point location, bool enabled)
        {
            Button button = new Button
            {
                Text = text,
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = enabled ? Color.FromArgb(190, 126, 84, 26) : Color.FromArgb(120, 55, 58, 65),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(88, 34),
                Location = location,
                Enabled = enabled
            };
            button.FlatAppearance.BorderColor = enabled ? Color.FromArgb(255, 205, 99) : Color.FromArgb(95, 105, 120);
            button.FlatAppearance.BorderSize = 1;
            return button;
        }

        private TextBox AddField(Control parent, string label, string value, int x, int y)
        {
            parent.Controls.Add(CreatePanelLabel(label, new Point(x, y), 10, Color.FromArgb(190, 205, 225), FontStyle.Bold));
            TextBox textBox = CreateHudTextBox(new Point(x, y + 24), new Size(160, 28), value);
            parent.Controls.Add(textBox);
            return textBox;
        }

        private ComboBox AddCombo(Control parent, string label, object[] items, int x, int y)
        {
            parent.Controls.Add(CreatePanelLabel(label, new Point(x, y), 10, Color.FromArgb(190, 205, 225), FontStyle.Bold));
            ComboBox combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Regular),
                Location = new Point(x, y + 24),
                Size = new Size(160, 30)
            };
            combo.Items.AddRange(items);
            combo.SelectedIndex = 0;
            parent.Controls.Add(combo);
            return combo;
        }

        private TextBox CreateHudTextBox(Point location, Size size, string text)
        {
            return new TextBox
            {
                Text = text,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(25, 36, 50),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Regular),
                Location = location,
                Size = size
            };
        }

        private Label CreatePanelLabel(string text, Point location, float size, Color color, FontStyle style)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei UI", size, style),
                ForeColor = color,
                BackColor = Color.FromArgb(14, 24, 36),
                AutoSize = true,
                Location = location
            };
        }

        private Control CreateBackButton()
        {
            HudMainButton button = new HudMainButton
            {
                Title = "返回大厅",
                Subtitle = "回到主界面",
                Icon = Peace_of_Tanks.Properties.Resources.lobby_icon_match,
                Primary = false,
                Size = new Size(210, 72),
                Location = new Point(28, 570)
            };
            button.Click += (s, e) => ShowHomePage();
            return button;
        }

        private void AddPageControls()
        {
            foreach (Control control in pageControls)
            {
                Controls.Add(control);
                control.BringToFront();
            }
        }

        private void RefreshTopExp()
        {
            expLabel.Text = "经验值 " + playerData.TotalExp;
        }

        private void StartSinglePlayer(GameMapConfig mapConfig, NetworkDifficulty difficulty)
        {
            selectedMode = GameMode.SinglePlayer;
            launchOptions = new GameLaunchOptions
            {
                Mode = GameMode.SinglePlayer,
                TankType = selectedTank,
                StartImmediately = true,
                UseNetwork = false,
                Difficulty = difficulty,
                MapConfig = mapConfig
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        private void StartNetworkMatch(NetworkJoinMode joinMode, string roomId, NetworkDifficulty difficulty)
        {
            StartNetworkMatch(joinMode, roomId, difficulty, lobbyServerHost, lobbyServerPort, chatPlayerName, 2, BattleRuleMode.FreeBattle, BattleMapFactory.CreateSystemMap(0));
        }

        private void StartNetworkMatch(NetworkJoinMode joinMode, string roomId, NetworkDifficulty difficulty, string host, int port, string playerName, int playerCount, BattleRuleMode battleRule, GameMapConfig mapConfig)
        {
            if (!EnsureServerReady(host, port))
            {
                return;
            }

            selectedMode = playerCount == 3 ? GameMode.ThreePlayer : GameMode.TwoPlayer;
            launchOptions = new GameLaunchOptions
            {
                Mode = selectedMode,
                TankType = selectedTank,
                StartImmediately = true,
                UseNetwork = true,
                Difficulty = difficulty,
                PlayerCount = playerCount,
                BattleRule = battleRule,
                MapConfig = mapConfig,
                NetworkConfig = new NetworkConfig
                {
                    ServerHost = string.IsNullOrWhiteSpace(host) ? DefaultChatHost : host.Trim(),
                    ServerPort = port,
                    PlayerName = string.IsNullOrWhiteSpace(playerName) ? chatPlayerName : playerName.Trim(),
                    TankType = selectedTank,
                    Difficulty = difficulty,
                    JoinMode = joinMode,
                    RoomId = roomId ?? string.Empty,
                    PlayerCount = playerCount,
                    BattleRule = battleRule,
                    MapConfig = mapConfig
                }
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool ConnectLobbyChat()
        {
            string error;
            return ConnectLobbyChat(out error);
        }

        private bool ConnectLobbyChat(out string error)
        {
            error = string.Empty;
            if (chatClient != null && chatClient.IsConnected)
            {
                SetChatStatus("聊天服务器已连接");
                return true;
            }

            CloseLobbyChat();
            chatClient = new TcpGameClient();
            chatClient.LineReceived += ChatClient_LineReceived;
            chatClient.Disconnected += ChatClient_Disconnected;

            if (!chatClient.ConnectLobby(lobbyServerHost, lobbyServerPort, chatPlayerName, out error))
            {
                CloseLobbyChat();
                SetChatStatus("聊天服务器未连接：" + error);
                return false;
            }

            SetChatStatus("聊天服务器已连接");
            return true;
        }

        private void SendLobbyChat()
        {
            if (chatInput == null)
            {
                return;
            }

            string message = chatInput.Text.Trim();
            if (message.Length == 0)
            {
                return;
            }

            if (!ConnectLobbyChat())
            {
                AppendChat("系统", "无法连接聊天服务器，消息未发送。");
                return;
            }

            chatClient.SendLine("CHAT|" + EncodeChatPart(chatPlayerName) + "|" + EncodeChatPart(message));
            chatInput.Clear();
            chatInput.Focus();
        }

        private void ChatClient_LineReceived(string line)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<string>(ChatClient_LineReceived), line);
                }
                catch
                {
                }
                return;
            }

            if (line.StartsWith("CHAT|", StringComparison.Ordinal))
            {
                string[] parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    AppendChat(DecodeChatPart(parts[1]), DecodeChatPart(parts[2]));
                }
            }
        }

        private void ChatClient_Disconnected(string message)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<string>(ChatClient_Disconnected), message);
                }
                catch
                {
                }
                return;
            }

            CloseLobbyChat();
            SetChatStatus("聊天服务器已断开");
        }

        private void CloseLobbyChat()
        {
            if (chatClient == null)
            {
                return;
            }

            chatClient.LineReceived -= ChatClient_LineReceived;
            chatClient.Disconnected -= ChatClient_Disconnected;
            chatClient.Dispose();
            chatClient = null;
        }

        private void AppendChat(string sender, string message)
        {
            if (chatLog == null)
            {
                return;
            }

            chatLog.AppendText("[" + sender + "] " + message + Environment.NewLine);
            chatLog.SelectionStart = chatLog.TextLength;
            chatLog.ScrollToCaret();
        }

        private void SetChatStatus(string status)
        {
            if (chatStatusLabel != null)
            {
                chatStatusLabel.Text = status;
            }
        }

        private static string EncodeChatPart(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static string DecodeChatPart(string value)
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

        private Image GetTankImage(TankType type)
        {
            switch (type)
            {
                case TankType.Green: return Peace_of_Tanks.Properties.Resources.Lv;
                case TankType.Yellow: return Peace_of_Tanks.Properties.Resources.Huang;
                case TankType.Blue: return Peace_of_Tanks.Properties.Resources.Lan;
                case TankType.Red: return Peace_of_Tanks.Properties.Resources.Hong;
                case TankType.Purple: return Peace_of_Tanks.Properties.Resources.Zi;
                default: return Peace_of_Tanks.Properties.Resources.Lv;
            }
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
                default: return "未知坦克";
            }
        }

        private Color GetTankColor(TankType type)
        {
            switch (type)
            {
                case TankType.Green: return Color.FromArgb(98, 225, 130);
                case TankType.Yellow: return Color.FromArgb(255, 216, 84);
                case TankType.Blue: return Color.FromArgb(88, 196, 255);
                case TankType.Red: return Color.FromArgb(255, 105, 96);
                case TankType.Purple: return Color.FromArgb(205, 130, 255);
                default: return Color.White;
            }
        }

        private static void DrawPanelFrame(Graphics g, Rectangle bounds, bool primary)
        {
            using (GraphicsPath path = BeveledPath(bounds, 10))
            using (SolidBrush fill = new SolidBrush(primary ? Color.FromArgb(185, 145, 92, 20) : Color.FromArgb(225, 8, 18, 30)))
            using (Pen border = new Pen(primary ? Color.FromArgb(255, 208, 92) : Color.FromArgb(150, 178, 204, 235), 1.5f))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }
        }

        private static GraphicsPath BeveledPath(Rectangle bounds, int bevel)
        {
            Rectangle r = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            GraphicsPath path = new GraphicsPath();
            path.AddLine(r.Left + bevel, r.Top, r.Right - bevel, r.Top);
            path.AddLine(r.Right - bevel, r.Top, r.Right, r.Top + bevel);
            path.AddLine(r.Right, r.Top + bevel, r.Right, r.Bottom - bevel);
            path.AddLine(r.Right, r.Bottom - bevel, r.Right - bevel, r.Bottom);
            path.AddLine(r.Right - bevel, r.Bottom, r.Left + bevel, r.Bottom);
            path.AddLine(r.Left + bevel, r.Bottom, r.Left, r.Bottom - bevel);
            path.AddLine(r.Left, r.Bottom - bevel, r.Left, r.Top + bevel);
            path.AddLine(r.Left, r.Top + bevel, r.Left + bevel, r.Top);
            path.CloseFigure();
            return path;
        }

        private sealed class HudPanel : Panel
        {
            public HudPanel()
            {
                DoubleBuffered = true;
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                e.Graphics.Clear(BackColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawPanelFrame(e.Graphics, ClientRectangle, false);
            }
        }

        private sealed class HudHeader : Control
        {
            public string Title { get; set; }
            public string Subtitle { get; set; }

            public HudHeader()
            {
                DoubleBuffered = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Font titleFont = new Font("Microsoft YaHei UI", 26, FontStyle.Bold))
                using (Font subFont = new Font("Microsoft YaHei UI", 14, FontStyle.Bold))
                using (SolidBrush titleBrush = new SolidBrush(Color.White))
                using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(235, 226, 236, 250)))
                {
                    e.Graphics.DrawString(Title ?? string.Empty, titleFont, titleBrush, 0, 0);
                    e.Graphics.DrawString(Subtitle ?? string.Empty, subFont, subBrush, 2, 48);
                }
            }
        }

        private sealed class MapEditorControl : Control
        {
            public GameMapConfig Map { get; set; }
            public bool Editable { get; set; }
            public MapTileType SelectedTile { get; set; }

            public MapEditorControl()
            {
                DoubleBuffered = true;
                Cursor = Cursors.Cross;
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                PaintTile(e.Location);
                base.OnMouseDown(e);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    PaintTile(e.Location);
                }
                base.OnMouseMove(e);
            }

            private void PaintTile(Point point)
            {
                if (!Editable || Map == null)
                {
                    return;
                }

                Rectangle grid = GetGridBounds();
                if (!grid.Contains(point))
                {
                    return;
                }

                int x = (point.X - grid.Left) * Map.Width / grid.Width;
                int y = (point.Y - grid.Top) * Map.Height / grid.Height;
                BattleMapFactory.SetTile(Map, x, y, SelectedTile);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawPanelFrame(e.Graphics, ClientRectangle, false);

                Rectangle grid = GetGridBounds();
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(38, 47, 42)))
                {
                    e.Graphics.FillRectangle(bg, grid);
                }

                if (Map != null)
                {
                    float cellW = grid.Width / (float)Map.Width;
                    float cellH = grid.Height / (float)Map.Height;
                    for (int y = 0; y < Map.Height; y++)
                    {
                        for (int x = 0; x < Map.Width; x++)
                        {
                            MapTileType tile = BattleMapFactory.GetTile(Map, x, y);
                            if (tile == MapTileType.Empty)
                            {
                                continue;
                            }

                            using (SolidBrush brush = new SolidBrush(GetTileColor(tile)))
                            {
                                e.Graphics.FillRectangle(brush, grid.Left + x * cellW, grid.Top + y * cellH, Math.Max(1, cellW), Math.Max(1, cellH));
                            }
                        }
                    }
                }

                using (Pen gridPen = new Pen(Color.FromArgb(30, 255, 255, 255), 1))
                {
                    for (int i = 0; i <= 12; i++)
                    {
                        int x = grid.Left + i * grid.Width / 12;
                        int y = grid.Top + i * grid.Height / 12;
                        e.Graphics.DrawLine(gridPen, x, grid.Top, x, grid.Bottom);
                        e.Graphics.DrawLine(gridPen, grid.Left, y, grid.Right, y);
                    }
                }

                using (Font font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(Editable ? Color.FromArgb(255, 231, 145) : Color.FromArgb(175, 190, 210)))
                {
                    e.Graphics.DrawString(Editable ? "自定义：拖拽放置材质" : "系统地图预览", font, brush, 16, Height - 28);
                }
            }

            private Rectangle GetGridBounds()
            {
                int size = Math.Min(Width - 28, Height - 52);
                return new Rectangle((Width - size) / 2, 16, size, size);
            }

            private static Color GetTileColor(MapTileType tile)
            {
                switch (tile)
                {
                    case MapTileType.Wall: return Color.FromArgb(184, 112, 62);
                    case MapTileType.Water: return Color.FromArgb(52, 144, 168);
                    case MapTileType.Grass: return Color.FromArgb(75, 142, 57);
                    case MapTileType.Iron: return Color.FromArgb(132, 138, 144);
                    case MapTileType.Base: return Color.FromArgb(218, 178, 80);
                    default: return Color.Transparent;
                }
            }
        }

        private sealed class TankCardControl : Control
        {
            private readonly Rectangle selectRect = new Rectangle(18, 126, 94, 34);
            private readonly Rectangle upgradeRect = new Rectangle(138, 126, 94, 34);
            private bool hoverSelect;
            private bool hoverUpgrade;

            public TankType TankType { get; set; }
            public string TankName { get; set; }
            public Color TankColor { get; set; }
            public Image TankImage { get; set; }
            public PlayerTankData TankData { get; set; }
            public TankConfig TankConfig { get; set; }
            public PlayerData PlayerData { get; set; }
            public bool IsSelected { get; set; }
            public bool CanUnlock { get; set; }

            public event Action<TankType> SelectOrUnlockClicked;
            public event Action<TankType> UpgradeClicked;

            public TankCardControl()
            {
                DoubleBuffered = true;
                Cursor = Cursors.Default;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                bool selectEnabled = TankData != null && (TankData.IsUnlocked || CanUnlock);
                bool upgradeEnabled = PlayerData != null && PlayerData.CanUpgradeTank(TankType);
                bool newHoverSelect = selectEnabled && selectRect.Contains(e.Location);
                bool newHoverUpgrade = upgradeEnabled && upgradeRect.Contains(e.Location);

                if (newHoverSelect != hoverSelect || newHoverUpgrade != hoverUpgrade)
                {
                    hoverSelect = newHoverSelect;
                    hoverUpgrade = newHoverUpgrade;
                    Invalidate();
                }

                Cursor = hoverSelect || hoverUpgrade ? Cursors.Hand : Cursors.Default;
                base.OnMouseMove(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hoverSelect = false;
                hoverUpgrade = false;
                Cursor = Cursors.Default;
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                bool selectEnabled = TankData != null && (TankData.IsUnlocked || CanUnlock);
                bool upgradeEnabled = PlayerData != null && PlayerData.CanUpgradeTank(TankType);

                if (selectEnabled && selectRect.Contains(e.Location))
                {
                    Action<TankType> handler = SelectOrUnlockClicked;
                    if (handler != null)
                    {
                        handler(TankType);
                    }
                }
                else if (upgradeEnabled && upgradeRect.Contains(e.Location))
                {
                    Action<TankType> handler = UpgradeClicked;
                    if (handler != null)
                    {
                        handler(TankType);
                    }
                }

                base.OnMouseDown(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawPanelFrame(e.Graphics, ClientRectangle, IsSelected);

                if (TankImage != null)
                {
                    e.Graphics.DrawImage(TankImage, 18, 28, 82, 66);
                }

                using (Font nameFont = new Font("Microsoft YaHei UI", 15, FontStyle.Bold))
                using (Font statsFont = new Font("Microsoft YaHei UI", 10, FontStyle.Bold))
                using (SolidBrush nameBrush = new SolidBrush(TankColor))
                using (SolidBrush statsBrush = new SolidBrush(TankData != null && TankData.IsUnlocked ? Color.White : Color.FromArgb(180, 195, 210)))
                {
                    e.Graphics.DrawString(TankName ?? string.Empty, nameFont, nameBrush, 112, 24);

                    string stats = string.Empty;
                    if (TankData != null && TankConfig != null)
                    {
                        stats = TankData.IsUnlocked
                            ? "等级 " + TankData.Level + "/5\n升级 " + GetUpgradeCostText() + "\n经验 " + (PlayerData != null ? PlayerData.TotalExp : 0)
                            : "未解锁\n需要经验 " + TankConfig.UnlockExp;
                    }

                    e.Graphics.DrawString(stats, statsFont, statsBrush, new RectangleF(112, 58, 118, 60));
                }

                if (IsSelected)
                {
                    using (Font selectedFont = new Font("Microsoft YaHei UI", 8, FontStyle.Bold))
                    using (SolidBrush selectedBrush = new SolidBrush(Color.FromArgb(255, 231, 145)))
                    {
                        e.Graphics.DrawString("已选择", selectedFont, selectedBrush, 18, 98);
                    }
                }

                bool selectEnabled = TankData != null && (TankData.IsUnlocked || CanUnlock);
                bool upgradeEnabled = PlayerData != null && PlayerData.CanUpgradeTank(TankType);
                string selectText = TankData != null && TankData.IsUnlocked ? "选择" : CanUnlock ? "解锁" : "锁定";
                DrawActionButton(e.Graphics, selectRect, selectText, selectEnabled, hoverSelect);
                DrawActionButton(e.Graphics, upgradeRect, "升级", upgradeEnabled, hoverUpgrade);
            }

            private string GetUpgradeCostText()
            {
                if (TankData == null || TankData.Level >= 5)
                {
                    return "已满级";
                }

                return TankData.GetExpForNextLevel().ToString();
            }

            private static void DrawActionButton(Graphics g, Rectangle bounds, string text, bool enabled, bool hover)
            {
                Color fill = enabled
                    ? (hover ? Color.FromArgb(225, 156, 101, 26) : Color.FromArgb(190, 126, 84, 26))
                    : Color.FromArgb(120, 55, 58, 65);
                Color border = enabled ? Color.FromArgb(255, 205, 99) : Color.FromArgb(95, 105, 120);
                Color textColor = enabled ? Color.White : Color.FromArgb(145, 150, 160);

                using (SolidBrush fillBrush = new SolidBrush(fill))
                using (Pen borderPen = new Pen(border, 1))
                using (Font font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(textColor))
                using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.FillRectangle(fillBrush, bounds);
                    g.DrawRectangle(borderPen, bounds);
                    g.DrawString(text, font, textBrush, bounds, format);
                }
            }
        }

        private sealed class HudMainButton : Control
        {
            public string Title { get; set; }
            public string Subtitle { get; set; }
            public Image Icon { get; set; }
            public bool Primary { get; set; }
            private bool hover;

            public HudMainButton()
            {
                DoubleBuffered = true;
                Cursor = Cursors.Hand;
                ForeColor = Color.White;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                hover = true;
                Invalidate();
                base.OnMouseEnter(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hover = false;
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawPanelFrame(e.Graphics, ClientRectangle, Primary || hover);
                if (Icon != null)
                {
                    e.Graphics.DrawImage(Icon, 16, 18, 52, 52);
                }

                using (Font titleFont = new Font("Microsoft YaHei UI", 20, FontStyle.Bold))
                using (Font subFont = new Font("Microsoft YaHei UI", 11, FontStyle.Bold))
                using (SolidBrush titleBrush = new SolidBrush(Color.White))
                using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(230, 226, 236, 250)))
                {
                    e.Graphics.DrawString(Title ?? string.Empty, titleFont, titleBrush, 82, 18);
                    e.Graphics.DrawString(Subtitle ?? string.Empty, subFont, subBrush, 84, 54);
                }
            }
        }
    }
}
