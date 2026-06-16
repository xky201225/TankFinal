using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    public partial class GameLobby : Form
    {
        private const string DefaultChatHost = "127.0.0.1";
        private const int DefaultChatPort = 51888;
        private const int EM_LINESCROLL = 0x00B6;

        private readonly PlayerData playerData;
        private readonly List<Control> pageControls = new List<Control>();
        private TankType selectedTank;
        private GameMode selectedMode = GameMode.SinglePlayer;
        private GameLaunchOptions launchOptions;
        private Label expLabel;
        private TcpGameClient chatClient;
        private RichTextBox chatLog;
        private HudScrollIndicator chatScrollIndicator;
        private TextBox chatInput;
        private Label chatStatusLabel;
        private string chatPlayerName;
        private string lobbyServerHost = DefaultChatHost;
        private int lobbyServerPort = DefaultChatPort;
        private bool lobbyServerPromptShown;
        private bool useAuthenticatedServer;
        private BattleRuleMode selectedNetworkRule = BattleRuleMode.Versus;
        private int selectedNetworkStage = 1;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

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
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && DialogResult != DialogResult.OK)
            {
                Application.Exit();
            }

            base.OnFormClosing(e);
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
                Text = "坦克大战",
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
            chatScrollIndicator = null;
            chatInput = null;
            chatStatusLabel = null;
        }

        private void ShowHomePage()
        {
            ClearPage();
            RefreshTopExp();

            pageControls.Add(CreateMainButton(
                "单人模式",
                "单人闯关 >>",
                Peace_of_Tanks.Properties.Resources.lobby_icon_solo,
                new Point(24, 150),
                true,
                (s, e) => ShowSinglePlayerSetupPage()));

            pageControls.Add(CreateMainButton(
                "双人联机",
                "竞技 / 协作 >>",
                Peace_of_Tanks.Properties.Resources.lobby_icon_match,
                new Point(24, 255),
                false,
                (s, e) => ShowMatchPage()));

            pageControls.Add(CreateMainButton(
                "坦克库",
                "选择 / 升级 / 解锁 >>",
                Peace_of_Tanks.Properties.Resources.lobby_icon_tanklib,
                new Point(24, 360),
                false,
                (s, e) => ShowTankLibraryPage()));

            pageControls.Add(CreateSelectedTankInfoPanel());
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
                Size = new Size(520, 132),
                Location = new Point(24, ClientSize.Height - 146),
                BackColor = Color.FromArgb(14, 24, 36)
            };
            panel.MouseWheel += ChatLog_MouseWheel;

            chatLog = new RichTextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(14, 24, 36),
                ForeColor = Color.FromArgb(255, 223, 130),
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
                Location = new Point(14, 10),
                Size = new Size(482, 62),
                ScrollBars = RichTextBoxScrollBars.None
            };
            chatLog.TextChanged += (s, e) => UpdateChatScrollIndicator();
            chatLog.MouseEnter += (s, e) => chatLog.Focus();
            chatLog.MouseWheel += ChatLog_MouseWheel;
            chatLog.VScroll += (s, e) => UpdateChatScrollIndicator();
            panel.Controls.Add(chatLog);

            chatScrollIndicator = new HudScrollIndicator
            {
                Location = new Point(500, 10),
                Size = new Size(6, 62),
                BackColor = Color.FromArgb(10, 18, 28)
            };
            chatScrollIndicator.ScrollRequested += ScrollChatToLine;
            chatScrollIndicator.MouseEnter += (s, e) => chatLog.Focus();
            chatScrollIndicator.MouseWheel += ChatLog_MouseWheel;
            panel.Controls.Add(chatScrollIndicator);

            chatInput = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(25, 36, 50),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Regular),
                Location = new Point(14, 84),
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

            Button send = CreateSmallButton("发送", new Point(394, 82), true);
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
                Location = new Point(14, 112)
            };
            panel.Controls.Add(chatStatusLabel);

            AppendChat("系统", "大厅聊天会通过服务器广播给所有在线用户。");
            return panel;
        }

        private void UpdateChatScrollIndicator()
        {
            if (chatLog == null || chatScrollIndicator == null)
            {
                return;
            }

            int totalLines = Math.Max(1, chatLog.GetLineFromCharIndex(chatLog.TextLength) + 1);
            int firstLine = chatLog.GetLineFromCharIndex(chatLog.GetCharIndexFromPosition(new Point(1, 1)));
            int lastLine = chatLog.GetLineFromCharIndex(chatLog.GetCharIndexFromPosition(new Point(1, Math.Max(1, chatLog.Height - 2))));
            int visibleLines = Math.Max(1, lastLine - firstLine + 1);

            chatScrollIndicator.SetScrollState(firstLine, visibleLines, totalLines);
        }

        private void ScrollChatToLine(int targetLine)
        {
            if (chatLog == null)
            {
                return;
            }

            int clampedLine = Math.Max(0, Math.Min(targetLine, chatLog.Lines.Length - 1));
            int charIndex = chatLog.GetFirstCharIndexFromLine(clampedLine);
            if (charIndex < 0)
            {
                charIndex = chatLog.TextLength;
            }

            chatLog.SelectionStart = charIndex;
            chatLog.ScrollToCaret();
            UpdateChatScrollIndicator();
        }

        private void ChatLog_MouseWheel(object sender, MouseEventArgs e)
        {
            if (chatLog == null)
            {
                return;
            }

            int lines = e.Delta > 0 ? -3 : 3;
            SendMessage(chatLog.Handle, EM_LINESCROLL, IntPtr.Zero, new IntPtr(lines));
            UpdateChatScrollIndicator();
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
                Title = "双人联机",
                Subtitle = "选择联机竞技或联机协作模式",
                Size = new Size(650, 74),
                Location = new Point(330, 82)
            });

            pageControls.Add(CreatePanelLabel("竞技：双人对战，先达成击杀目标的一方获胜。    协作：双人清怪推进波次，击败最终 Boss 即通关。", new Point(330, 168), new Size(650, 44), 10, Color.FromArgb(205, 220, 235), FontStyle.Regular));

            pageControls.Add(CreatePanelButton("竞技模式", "双人对战，优先达成击杀目标者获胜。", new Point(330, 230), Peace_of_Tanks.Properties.Resources.lobby_icon_match, (s, e) =>
            {
                selectedNetworkRule = BattleRuleMode.Versus;
                ShowNetworkRoomPage();
            }));

            pageControls.Add(CreatePanelButton("协作模式", "双人组队打怪，共同推进六关并击败每关 Boss。", new Point(650, 230), Peace_of_Tanks.Properties.Resources.lobby_icon_people, (s, e) =>
            {
                selectedNetworkRule = BattleRuleMode.Survival;
                ShowCoopStagePage();
            }));

            pageControls.Add(CreateBackButton());
            AddPageControls();
        }

        private void ShowCoopStagePage()
        {
            ClearPage();
            RefreshTopExp();

            pageControls.Add(new HudHeader
            {
                Title = "联机协作关卡",
                Subtitle = "选择已解锁关卡后再创建或加入房间，协作通关会继续解锁后续关卡",
                Size = new Size(760, 74),
                Location = new Point(260, 82)
            });

            for (int stage = 1; stage <= BattleMapFactory.StageMapCount; stage++)
            {
                bool unlocked = playerData.IsStageUnlocked(stage);
                int column = (stage - 1) % 2;
                int row = (stage - 1) / 2;
                Button stageButton = CreateSmallButton(unlocked ? ("第" + stage + "关 - " + BattleMapFactory.GetStageMapName(stage)) : ("第" + stage + "关 未解锁"), new Point(260 + column * 390, 170 + row * 64), unlocked);
                stageButton.Size = new Size(370, 42);
                int capturedStage = stage;
                stageButton.Click += (s, e) => ShowNetworkRoomPage(capturedStage);
                pageControls.Add(stageButton);
            }

            pageControls.Add(CreateLeaderboardPanel());
            pageControls.Add(CreateBackButton((s, e) => ShowMatchPage()));
            AddPageControls();
        }

        private Control CreateSingleLeaderboardPanel()
        {
            DataManager.RefreshLeaderboard();

            HudPanel panel = new HudPanel
            {
                Size = new Size(240, 360),
                Location = new Point(40, 180),
                BackColor = Color.FromArgb(20, 28, 42)
            };

            panel.Controls.Add(CreatePanelLabel("单人关卡榜", new Point(12, 10), 10, Color.White, FontStyle.Bold));
            int y = 34;
            for (int stage = 1; stage <= BattleMapFactory.StageMapCount; stage++)
            {
                LeaderboardEntry best = playerData.LeaderboardEntries.Where(r => r.StageId == stage).OrderBy(r => r.Rank <= 0 ? int.MaxValue : r.Rank).ThenByDescending(r => r.Score).FirstOrDefault();
                string bestText = best == null ? "暂无" : (best.UserName + " " + best.Score + "分");
                string rankText = best == null || best.Rank <= 0 ? "-" : ("#" + best.Rank);
                panel.Controls.Add(CreatePanelLabel("第" + stage + "关 " + rankText, new Point(12, y), 9, Color.FromArgb(255, 231, 145), FontStyle.Bold));
                y += 18;
                panel.Controls.Add(CreatePanelLabel(bestText, new Point(12, y), 9, Color.FromArgb(210, 225, 235), FontStyle.Regular));
                y += 18;
            }

            return panel;
        }

        private void ShowNetworkRoomPage()
        {
            ShowNetworkRoomPage(1);
        }

        private void ShowNetworkRoomPage(int selectedStage)
        {
            ClearPage();

            bool versusMode = selectedNetworkRule == BattleRuleMode.Versus;
            selectedNetworkStage = Math.Max(1, Math.Min(BattleMapFactory.StageMapCount, selectedStage));
            pageControls.Add(new HudHeader
            {
                Title = versusMode ? "联机竞技" : "联机协作",
                Subtitle = versusMode ? "创建房间、搜索房间或加入指定房间进行对战" : ("当前关卡：第" + selectedStage + "关  " + BattleMapFactory.GetStageMapName(selectedStage)),
                Size = new Size(650, 74),
                Location = new Point(330, 82)
            });

            pageControls.Add(CreatePanelButton("创建房间", versusMode ? "设置服务器、房间号和地图，创建后等待另一名玩家加入。" : "设置服务器和房间号，使用当前关卡自带地图等待队友加入。", new Point(330, 190), Peace_of_Tanks.Properties.Resources.lobby_icon_people, (s, e) => ShowCreateRoomPage()));

            pageControls.Add(CreatePanelButton("搜索房间", "自动搜索当前服务器里可加入的房间。", new Point(650, 190), Peace_of_Tanks.Properties.Resources.lobby_icon_match, (s, e) =>
            {
                StartNetworkMatch(NetworkJoinMode.Search, string.Empty, versusMode ? NetworkDifficulty.Easy : GetDifficultyForStage(selectedNetworkStage));
            }));

            pageControls.Add(CreateJoinRoomPanel());
            pageControls.Add(CreateBackButton((s, e) => ShowMatchPage()));
            AddPageControls();
        }

        private Control CreateLeaderboardPanel()
        {
            DataManager.RefreshLeaderboard();

            HudPanel panel = new HudPanel
            {
                Size = new Size(600, 330),
                Location = new Point(330, 420),
                BackColor = Color.FromArgb(14, 24, 36)
            };

            panel.Controls.Add(CreatePanelLabel("关卡排行榜", new Point(22, 16), 16, Color.White, FontStyle.Bold));
            int y = 48;
            for (int stage = 1; stage <= BattleMapFactory.StageMapCount; stage++)
            {
                LeaderboardEntry best = playerData.LeaderboardEntries.Where(r => r.StageId == stage).OrderBy(r => r.Rank <= 0 ? int.MaxValue : r.Rank).ThenByDescending(r => r.Score).FirstOrDefault();
                string bestScoreText = best == null ? "暂无" : (best.UserName + "  " + best.Score + " 分");
                string bestTimeText = best == null || best.BestTimeSeconds <= 0 ? "暂无" : (best.BestTimeSeconds + " 秒");
                string rankText = best == null || best.Rank <= 0 ? "-" : ("#" + best.Rank);
                panel.Controls.Add(CreatePanelLabel("第" + stage + "关  排名：" + rankText + "   最高分：" + bestScoreText + "   最快通关：" + bestTimeText, new Point(22, y), 10, Color.FromArgb(210, 225, 235), FontStyle.Regular));
                y += 42;
            }

            return panel;
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

                StartNetworkMatch(NetworkJoinMode.JoinByRoomId, roomId, selectedNetworkRule == BattleRuleMode.Survival ? GetDifficultyForStage(selectedNetworkStage) : NetworkDifficulty.Easy);
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
                Title = selectedNetworkRule == BattleRuleMode.Versus ? "创建竞技房间" : "创建协作房间",
                Subtitle = selectedNetworkRule == BattleRuleMode.Versus ? "设置房间信息后等待另一名玩家加入对战" : "设置房间信息后等待另一名玩家加入协作",
                Size = new Size(650, 74),
                Location = new Point(330, 82)
            });

            HudPanel panel = new HudPanel { Size = new Size(420, 470), Location = new Point(285, 160), BackColor = Color.FromArgb(14, 24, 36) };
            bool versusMode = selectedNetworkRule == BattleRuleMode.Versus;
            TextBox host = AddField(panel, "服务器地址", lobbyServerHost, 24, 24);
            TextBox port = AddField(panel, "端口", lobbyServerPort.ToString(), 230, 24);
            TextBox name = AddField(panel, "玩家名称", chatPlayerName, 24, 92);
            TextBox roomId = AddField(panel, "房间号", "ROOM-" + DateTime.Now.ToString("HHmm"), 230, 92);

            ComboBox difficulty = AddCombo(panel, "难度", new object[] { "简单", "中等", "困难" }, 24, 164);
            ComboBox mapSelect = versusMode ? AddCombo(panel, "地图", CreateSystemMapItems(true), 230, 164) : null;

            GameMapConfig[] systemMaps = CreateSystemMaps();
            GameMapConfig customMap = BattleMapFactory.CreateEmpty("自定义地图");

            MapEditorControl mapPreview = new MapEditorControl
            {
                Size = new Size(420, 420),
                Location = new Point(735, 160),
                BackColor = Color.FromArgb(14, 24, 36),
                Map = CreatePreviewMap(versusMode ? systemMaps[0] : BattleMapFactory.CreateStageMap(selectedNetworkStage), selectedNetworkRule == BattleRuleMode.Survival),
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
            if (versusMode)
            {
                pageControls.Add(materialPanel);
                mapSelect.SelectedIndexChanged += (s, e) =>
                {
                    bool custom = mapSelect.SelectedIndex >= systemMaps.Length;
                    int selectedIndex = Math.Max(0, Math.Min(systemMaps.Length - 1, mapSelect.SelectedIndex));
                    mapPreview.Map = CreatePreviewMap(custom ? customMap : systemMaps[selectedIndex], false);
                    mapPreview.Editable = custom;
                    mapPreview.Invalidate();
                };
            }

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
                    2,
                    selectedNetworkRule,
                    versusMode ? mapPreview.Map : BattleMapFactory.CreateStageMap(selectedNetworkStage),
                    versusMode ? 1 : selectedNetworkStage);
            };
            panel.Controls.Add(create);

            Label hint = CreatePanelLabel(
                selectedNetworkRule == BattleRuleMode.Versus
                    ? "竞技模式：双人对战，先达到击杀目标或时间结束比分更高者获胜"
                    : "协作模式：双人组队清怪，推进波次并击败最终 Boss",
                new Point(24, 390),
                9,
                Color.FromArgb(180, 195, 210),
                FontStyle.Regular);
            panel.Controls.Add(hint);

            pageControls.Add(panel);
            pageControls.Add(CreateBackButton((s, e) => ShowNetworkRoomPage()));
            AddPageControls();
        }

        private static GameMapConfig CreatePreviewMap(GameMapConfig map, bool baseDefense)
        {
            return BattleMapFactory.CloneMap(map);
        }

        private static string GetStageDescription(int stage)
        {
            switch (stage)
            {
                case 1: return "废墟前线：适合起步熟悉走位与清怪。";
                case 2: return "钢铁水坝：障碍更多，侧翼压力更强。";
                case 3: return "要塞核心：核心战场，Boss压制最强。";
                case 4: return "林地迷洲：草丛密集，视野与伏击压力更高。";
                case 5: return "烈火峡口：通道狭窄，钢铁防线切割战场。";
                case 6: return "终极地堡：环形堡垒压迫，最终挑战强度最高。";
                default: return BattleMapFactory.GetStageMapName(stage);
            }
        }

        private static NetworkDifficulty GetDifficultyForStage(int stage)
        {
            if (stage >= 5) return NetworkDifficulty.Hard;
            if (stage >= 3) return NetworkDifficulty.Medium;
            return NetworkDifficulty.Easy;
        }

        private void ShowSinglePlayerSetupPage()
        {
            ClearPage();
            RefreshTopExp();

            pageControls.Add(new HudHeader
            {
                Title = "单人挑战",
                Subtitle = "六个关卡逐步解锁，通关前一关后开启下一关",
                Size = new Size(650, 74),
                Location = new Point(330, 82)
            });

            int selectedStage = 1;
            GameMapConfig selectedMap = BattleMapFactory.CreateStageMap(selectedStage);
            MapEditorControl mapPreview = new MapEditorControl
            {
                Size = new Size(360, 360),
                Location = new Point(880, 180),
                BackColor = Color.FromArgb(14, 24, 36),
                Map = selectedMap,
                Editable = false,
                SelectedTile = MapTileType.Wall
            };

            Label previewTitle = CreatePanelLabel("关卡预览", new Point(880, 154), 12, Color.White, FontStyle.Bold);
            pageControls.Add(previewTitle);
            pageControls.Add(mapPreview);

            HudPanel panel = new HudPanel { Size = new Size(520, 520), Location = new Point(305, 150), BackColor = Color.FromArgb(14, 24, 36) };
            Control leaderboardPanel = CreateSingleLeaderboardPanel();
            leaderboardPanel.Location = new Point(40, 180);
            Label title = CreatePanelLabel("选择关卡", new Point(24, 28), 14, Color.White, FontStyle.Bold);
            panel.Controls.Add(title);

            Action<int> refreshStageInfo = stageId =>
            {
                selectedStage = stageId;
                selectedMap = BattleMapFactory.CreateStageMap(stageId);
                mapPreview.Map = selectedMap;
                mapPreview.Invalidate();
            };

            for (int stage = 1; stage <= BattleMapFactory.StageMapCount; stage++)
            {
                bool unlocked = playerData.IsStageUnlocked(stage);
                Button stageButton = CreateSmallButton(unlocked ? ("第" + stage + "关") : ("第" + stage + "关 未解锁"), new Point(24, 68 + (stage - 1) * 56), unlocked);
                stageButton.Size = new Size(180, 42);
                int capturedStage = stage;
                stageButton.Click += (s, e) => refreshStageInfo(capturedStage);
                panel.Controls.Add(stageButton);

                string desc = GetStageDescription(capturedStage);
                panel.Controls.Add(CreatePanelLabel(desc, new Point(220, 74 + (capturedStage - 1) * 56), new Size(260, 42), 9, unlocked ? Color.FromArgb(200, 215, 230) : Color.FromArgb(120, 130, 145), FontStyle.Regular));
            }

            panel.Controls.Add(CreatePanelLabel("规则：通关当前关卡后解锁下一关，也可反复挑战已通关关卡。", new Point(24, 410), new Size(460, 36), 9, Color.FromArgb(190, 205, 225), FontStyle.Regular));

            Button start = CreateSmallButton("开始挑战", new Point(24, 458), true);
            start.Size = new Size(160, 40);
            start.Click += (s, e) =>
            {
                if (!playerData.IsStageUnlocked(selectedStage))
                {
                    MessageBox.Show(this, "请先通关前一关。", "关卡未解锁", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                StartSinglePlayer(selectedMap, GetDifficultyForStage(selectedStage), selectedStage);
            };
            panel.Controls.Add(start);

            pageControls.Add(panel);
            pageControls.Add(leaderboardPanel);
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
                pageControls.Add(CreateTankCard(type, new Point(x + (index % 3) * 280, y + (index / 3) * 255)));
                index++;
            }

            pageControls.Add(new ComingSoonTankCard
            {
                Size = new Size(250, 230),
                Location = new Point(x + (index % 3) * 280, y + (index / 3) * 255)
            });

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
                Primary = title.Contains("鍒涘缓"),
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
            bool canUnlock = !tankData.IsUnlocked && playerData.TotalExp >= config.UnlockCoin;

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
                Size = new Size(250, 230),
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

            card.UpgradeClicked += (selectedType, upgradeType) =>
            {
                if (playerData.UpgradeTank(selectedType, upgradeType))
                {
                    DataManager.SaveData();
                    RefreshTopExp();
                    ShowTankLibraryPage();
                }
            };

            return card;
        }

        private Control CreateSelectedTankInfoPanel()
        {
            TankType tankType = playerData.SelectedTank;
            PlayerTankData tankData = playerData.Tanks[tankType];
            TankConfig config = TankConfigs.GetConfig(tankType);
            var images = TankResourceLoader.GetPlayerTankImages(tankType);

            return new SelectedTankInfoPanel
            {
                Location = new Point(470, 188),
                Size = new Size(560, 260),
                TankType = tankType,
                TankName = GetTankName(tankType),
                TankColor = GetTankColor(tankType),
                TankImage = images.Item1,
                TankData = tankData,
                TankConfig = config
            };
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

        private Label CreatePanelLabel(string text, Point location, Size labelSize, float size, Color color, FontStyle style)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei UI", size, style),
                ForeColor = color,
                BackColor = Color.FromArgb(14, 24, 36),
                AutoSize = false,
                Size = labelSize,
                Location = location,
                TextAlign = ContentAlignment.TopLeft
            };
        }

        private Control CreateBackButton()
        {
            return CreateBackButton((s, e) => ShowHomePage());
        }

        private Control CreateBackButton(EventHandler click)
        {
            HudMainButton button = new HudMainButton
            {
                Title = "返回",
                Subtitle = "回到上一页",
                Icon = Peace_of_Tanks.Properties.Resources.lobby_icon_match,
                Primary = false,
                Size = new Size(210, 72),
                Location = new Point(28, 570)
            };
            button.Click += click;
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
            expLabel.Text = "金币 " + playerData.TotalExp;
        }

        private void StartSinglePlayer(GameMapConfig mapConfig, NetworkDifficulty difficulty)
        {
            StartSinglePlayer(mapConfig, difficulty, 1);
        }

        private void StartSinglePlayer(GameMapConfig mapConfig, NetworkDifficulty difficulty, int stageId)
        {
            selectedMode = GameMode.SinglePlayer;
            launchOptions = new GameLaunchOptions
            {
                Mode = GameMode.SinglePlayer,
                TankType = selectedTank,
                StartImmediately = true,
                UseNetwork = false,
                Difficulty = difficulty,
                StageId = stageId,
                BattleRule = BattleRuleMode.Survival,
                MapConfig = mapConfig
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        private void StartNetworkMatch(NetworkJoinMode joinMode, string roomId, NetworkDifficulty difficulty)
        {
            GameMapConfig map = selectedNetworkRule == BattleRuleMode.Survival
                ? BattleMapFactory.CreateStageMap(selectedNetworkStage)
                : BattleMapFactory.CreateSystemMap(0);
            int stageId = selectedNetworkRule == BattleRuleMode.Survival ? selectedNetworkStage : 1;
            StartNetworkMatch(joinMode, roomId, difficulty, lobbyServerHost, lobbyServerPort, chatPlayerName, 2, selectedNetworkRule, map, stageId);
        }

        private void StartNetworkMatch(NetworkJoinMode joinMode, string roomId, NetworkDifficulty difficulty, string host, int port, string playerName, int playerCount, BattleRuleMode battleRule, GameMapConfig mapConfig)
        {
            StartNetworkMatch(joinMode, roomId, difficulty, host, port, playerName, playerCount, battleRule, mapConfig, battleRule == BattleRuleMode.Survival ? selectedNetworkStage : 1);
        }

        private void StartNetworkMatch(NetworkJoinMode joinMode, string roomId, NetworkDifficulty difficulty, string host, int port, string playerName, int playerCount, BattleRuleMode battleRule, GameMapConfig mapConfig, int stageId)
        {
            if (!EnsureServerReady(host, port))
            {
                return;
            }

            selectedMode = battleRule == BattleRuleMode.Versus ? GameMode.NetworkVersus : GameMode.NetworkCoop;
            launchOptions = new GameLaunchOptions
            {
                Mode = selectedMode,
                TankType = selectedTank,
                StartImmediately = true,
                UseNetwork = true,
                Difficulty = difficulty,
                PlayerCount = playerCount,
                StageId = stageId,
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

            bool followTail = IsChatNearBottom();
            chatLog.AppendText("[" + sender + "] " + message + Environment.NewLine);
            if (followTail)
            {
                chatLog.SelectionStart = chatLog.TextLength;
                chatLog.ScrollToCaret();
            }
            UpdateChatScrollIndicator();
        }

        private bool IsChatNearBottom()
        {
            if (chatLog == null)
            {
                return true;
            }

            int totalLines = Math.Max(1, chatLog.GetLineFromCharIndex(chatLog.TextLength) + 1);
            int lastLine = chatLog.GetLineFromCharIndex(chatLog.GetCharIndexFromPosition(new Point(1, Math.Max(1, chatLog.Height - 2))));
            return lastLine >= totalLines - 2;
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

        private sealed class HudScrollIndicator : Control
        {
            private int firstVisibleLine;
            private int visibleLineCount = 1;
            private int totalLineCount = 1;
            private bool dragging;
            private int dragOffsetY;

            public event Action<int> ScrollRequested;

            public HudScrollIndicator()
            {
                DoubleBuffered = true;
                Cursor = Cursors.Hand;
            }

            public void SetScrollState(int firstVisible, int visibleCount, int totalCount)
            {
                firstVisibleLine = Math.Max(0, firstVisible);
                visibleLineCount = Math.Max(1, visibleCount);
                totalLineCount = Math.Max(1, totalCount);
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                if (totalLineCount <= visibleLineCount)
                {
                    return;
                }

                Rectangle thumb = GetThumbBounds();
                dragging = true;
                dragOffsetY = thumb.Contains(e.Location) ? e.Y - thumb.Top : thumb.Height / 2;
                RequestScrollFromMouse(e.Y);
                Capture = true;
                base.OnMouseDown(e);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                if (dragging)
                {
                    RequestScrollFromMouse(e.Y);
                }

                base.OnMouseMove(e);
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                dragging = false;
                Capture = false;
                base.OnMouseUp(e);
            }

            private void RequestScrollFromMouse(int mouseY)
            {
                Rectangle thumb = GetThumbBounds();
                int maxFirstLine = Math.Max(1, totalLineCount - visibleLineCount);
                int movableHeight = Math.Max(1, Height - thumb.Height);
                int desiredTop = Math.Max(0, Math.Min(movableHeight, mouseY - dragOffsetY));
                int targetLine = desiredTop * maxFirstLine / movableHeight;

                Action<int> handler = ScrollRequested;
                if (handler != null)
                {
                    handler(targetLine);
                }
            }

            private Rectangle GetThumbBounds()
            {
                if (totalLineCount <= visibleLineCount)
                {
                    return new Rectangle(0, 0, Width, Height);
                }

                int thumbHeight = Math.Max(12, Height * visibleLineCount / totalLineCount);
                int maxFirstLine = Math.Max(1, totalLineCount - visibleLineCount);
                int thumbTop = (Height - thumbHeight) * Math.Min(firstVisibleLine, maxFirstLine) / maxFirstLine;
                return new Rectangle(0, thumbTop, Width, thumbHeight);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle track = new Rectangle(1, 0, Math.Max(1, Width - 2), Height);

                using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(115, 23, 35, 50)))
                {
                    e.Graphics.FillRectangle(trackBrush, track);
                }

                if (totalLineCount <= visibleLineCount)
                {
                    return;
                }

                Rectangle thumb = GetThumbBounds();

                using (SolidBrush thumbBrush = new SolidBrush(Color.FromArgb(210, 173, 135, 67)))
                using (Pen edgePen = new Pen(Color.FromArgb(150, 255, 225, 150), 1))
                {
                    e.Graphics.FillRectangle(thumbBrush, thumb);
                    e.Graphics.DrawRectangle(edgePen, new Rectangle(thumb.X, thumb.Y, thumb.Width - 1, thumb.Height - 1));
                }
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

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(235, 5, 12, 22)))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawPanelFrame(e.Graphics, ClientRectangle, false);
                using (Font titleFont = new Font("Microsoft YaHei UI", 23, FontStyle.Bold))
                using (Font subFont = new Font("Microsoft YaHei UI", 11.5f, FontStyle.Bold))
                using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(255, 231, 145)))
                using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(235, 236, 244, 255)))
                using (StringFormat subFormat = new StringFormat())
                {
                    subFormat.Alignment = StringAlignment.Near;
                    subFormat.LineAlignment = StringAlignment.Near;
                    subFormat.Trimming = StringTrimming.EllipsisWord;

                    RectangleF titleBounds = new RectangleF(18, 4, Math.Max(1, Width - 36), 38);
                    RectangleF subBounds = new RectangleF(20, 43, Math.Max(1, Width - 40), Math.Max(24, Height - 50));
                    e.Graphics.DrawString(Title ?? string.Empty, titleFont, titleBrush, titleBounds);
                    e.Graphics.DrawString(Subtitle ?? string.Empty, subFont, subBrush, subBounds, subFormat);
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
                    e.Graphics.DrawString(Editable ? "自定义：拖拽设置材质" : "系统地图预览", font, brush, 16, Height - 28);
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
            private readonly Rectangle speedUpgradeRect = new Rectangle(126, 138, 52, 24);
            private readonly Rectangle attackUpgradeRect = new Rectangle(184, 138, 52, 24);
            private readonly Rectangle healthUpgradeRect = new Rectangle(126, 166, 52, 24);
            private readonly Rectangle bulletSpeedUpgradeRect = new Rectangle(184, 166, 52, 24);
            private readonly Rectangle cooldownUpgradeRect = new Rectangle(126, 194, 110, 24);
            private bool hoverCard;
            private TankUpgradeType? hoverUpgrade;

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
            public event Action<TankType, TankUpgradeType> UpgradeClicked;

            public TankCardControl()
            {
                DoubleBuffered = true;
                Cursor = Cursors.Default;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                bool selectEnabled = TankData != null && (TankData.IsUnlocked || CanUnlock);
                TankUpgradeType? upgradeArea = GetUpgradeTypeAt(e.Location);
                TankUpgradeType? newHoverUpgrade = upgradeArea;
                if (upgradeArea.HasValue && !CanUpgrade(upgradeArea.Value))
                {
                    newHoverUpgrade = null;
                }

                bool newHoverCard = selectEnabled && !upgradeArea.HasValue;
                if (newHoverCard != hoverCard || newHoverUpgrade != hoverUpgrade)
                {
                    hoverCard = newHoverCard;
                    hoverUpgrade = newHoverUpgrade;
                    Invalidate();
                }

                Cursor = hoverCard || hoverUpgrade.HasValue ? Cursors.Hand : Cursors.Default;
                base.OnMouseMove(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                hoverCard = false;
                hoverUpgrade = null;
                Cursor = Cursors.Default;
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                bool selectEnabled = TankData != null && (TankData.IsUnlocked || CanUnlock);
                TankUpgradeType? upgradeType = GetUpgradeTypeAt(e.Location);

                if (upgradeType.HasValue)
                {
                    if (CanUpgrade(upgradeType.Value))
                    {
                        Action<TankType, TankUpgradeType> handler = UpgradeClicked;
                        if (handler != null)
                        {
                            handler(TankType, upgradeType.Value);
                        }
                    }
                }
                else if (selectEnabled)
                {
                    Action<TankType> handler = SelectOrUnlockClicked;
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
                DrawPanelFrame(e.Graphics, ClientRectangle, IsSelected || hoverCard);

                if (TankImage != null)
                {
                    e.Graphics.DrawImage(TankImage, 18, 28, 82, 66);
                }

                using (Font nameFont = new Font("Microsoft YaHei UI", 15, FontStyle.Bold))
                using (Font statsFont = new Font("Microsoft YaHei UI", 8.8f, FontStyle.Bold))
                using (SolidBrush nameBrush = new SolidBrush(TankColor))
                using (SolidBrush statsBrush = new SolidBrush(TankData != null && TankData.IsUnlocked ? Color.White : Color.FromArgb(180, 195, 210)))
                {
                    e.Graphics.DrawString(TankName ?? string.Empty, nameFont, nameBrush, 112, 24);

                    string stats = string.Empty;
                    if (TankData != null && TankConfig != null)
                    {
                        int attack = TankConfig.GetAttackAtLevel(TankData.AttackLevel);
                        int health = TankConfig.GetHealthAtLevel(TankData.HealthLevel);
                        double cooldown = TankConfig.GetCooldownSecondsAtLevel(TankData.CooldownLevel);
                        TankSkillType skillType = TankSkillCatalog.GetSkillType(TankType);
                        string skillName = TankSkillCatalog.GetDisplayName(skillType);
                        stats = TankData.IsUnlocked
                            ? "速 " + TankData.SpeedLevel + "/5  弹 " + TankData.BulletSpeedLevel + "/5\n攻 " + attack + "  Lv" + TankData.AttackLevel + "/5\n命 " + health + "  Lv" + TankData.HealthLevel + "/5\n技能 " + skillName + "\nCD " + cooldown.ToString("0") + "s"
                            : "未解锁\n攻击 " + attack + "\n生命 " + health + "\n技能 " + skillName + "\n解锁 " + TankConfig.UnlockCoin + " 金币";
                    }

                    e.Graphics.DrawString(stats, statsFont, statsBrush, new RectangleF(112, 50, 128, 90));
                }

                if (IsSelected)
                {
                    using (Font selectedFont = new Font("Microsoft YaHei UI", 8, FontStyle.Bold))
                    using (SolidBrush selectedBrush = new SolidBrush(Color.FromArgb(255, 231, 145)))
                    {
                        e.Graphics.DrawString("已选择", selectedFont, selectedBrush, 18, 98);
                    }
                }

                DrawActionButton(e.Graphics, speedUpgradeRect, "速度+" + GetUpgradeCostText(TankUpgradeType.Speed), CanUpgrade(TankUpgradeType.Speed), hoverUpgrade == TankUpgradeType.Speed);
                DrawActionButton(e.Graphics, attackUpgradeRect, "攻击+" + GetUpgradeCostText(TankUpgradeType.Attack), CanUpgrade(TankUpgradeType.Attack), hoverUpgrade == TankUpgradeType.Attack);
                DrawActionButton(e.Graphics, healthUpgradeRect, "生命+" + GetUpgradeCostText(TankUpgradeType.Health), CanUpgrade(TankUpgradeType.Health), hoverUpgrade == TankUpgradeType.Health);
                DrawActionButton(e.Graphics, bulletSpeedUpgradeRect, "弹速+" + GetUpgradeCostText(TankUpgradeType.BulletSpeed), CanUpgrade(TankUpgradeType.BulletSpeed), hoverUpgrade == TankUpgradeType.BulletSpeed);
                DrawActionButton(e.Graphics, cooldownUpgradeRect, "CD+" + GetUpgradeCostText(TankUpgradeType.Cooldown), CanUpgrade(TankUpgradeType.Cooldown), hoverUpgrade == TankUpgradeType.Cooldown);
            }

            private TankUpgradeType? GetUpgradeTypeAt(Point point)
            {
                if (speedUpgradeRect.Contains(point)) return TankUpgradeType.Speed;
                if (attackUpgradeRect.Contains(point)) return TankUpgradeType.Attack;
                if (healthUpgradeRect.Contains(point)) return TankUpgradeType.Health;
                if (bulletSpeedUpgradeRect.Contains(point)) return TankUpgradeType.BulletSpeed;
                if (cooldownUpgradeRect.Contains(point)) return TankUpgradeType.Cooldown;
                return null;
            }

            private bool CanUpgrade(TankUpgradeType upgradeType)
            {
                return PlayerData != null && PlayerData.CanUpgradeTank(TankType, upgradeType);
            }

            private string GetUpgradeCostText(TankUpgradeType upgradeType)
            {
                if (TankData == null || TankData.GetUpgradeLevel(upgradeType) >= TankConfig.MaxUpgradeLevel)
                {
                    return "满";
                }

                return TankData.GetExpForNextLevel(upgradeType).ToString();
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
                using (Font font = new Font("Microsoft YaHei UI", bounds.Width <= 60 ? 8.2f : 10, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(textColor))
                using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.FillRectangle(fillBrush, bounds);
                    g.DrawRectangle(borderPen, bounds);
                    g.DrawString(text, font, textBrush, bounds, format);
                }
            }
        }

        private sealed class ComingSoonTankCard : Control
        {
            public ComingSoonTankCard()
            {
                DoubleBuffered = true;
                Cursor = Cursors.Default;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawPanelFrame(e.Graphics, ClientRectangle, false);

                using (Font titleFont = new Font("Microsoft YaHei UI", 15, FontStyle.Bold))
                using (Font subFont = new Font("Microsoft YaHei UI", 10, FontStyle.Bold))
                using (Font smallFont = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular))
                using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(190, 205, 225)))
                using (SolidBrush mutedBrush = new SolidBrush(Color.FromArgb(140, 154, 172)))
                using (SolidBrush goldBrush = new SolidBrush(Color.FromArgb(255, 231, 145)))
                using (Pen blueprintPen = new Pen(Color.FromArgb(150, 255, 231, 145), 2f))
                using (Pen faintPen = new Pen(Color.FromArgb(80, 190, 205, 225), 1f))
                using (StringFormat center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    blueprintPen.DashStyle = DashStyle.Dash;
                    Rectangle tank = new Rectangle(64, 50, 122, 70);
                    e.Graphics.DrawRectangle(blueprintPen, tank);
                    e.Graphics.DrawEllipse(blueprintPen, 92, 58, 48, 48);
                    e.Graphics.DrawLine(blueprintPen, 116, 58, 116, 24);
                    e.Graphics.DrawRectangle(faintPen, 46, 34, 158, 102);

                    e.Graphics.DrawString("待推出坦克", titleFont, titleBrush, new RectangleF(20, 142, Width - 40, 28), center);
                    e.Graphics.DrawString("研发中", subFont, goldBrush, new RectangleF(20, 170, Width - 40, 24), center);
                    e.Graphics.DrawString("新的定位与技能正在调试", smallFont, mutedBrush, new RectangleF(18, 198, Width - 36, 22), center);
                }
            }
        }

        private sealed class SelectedTankInfoPanel : Control
        {
            public TankType TankType { get; set; }
            public string TankName { get; set; }
            public Color TankColor { get; set; }
            public Image TankImage { get; set; }
            public PlayerTankData TankData { get; set; }
            public TankConfig TankConfig { get; set; }

            public SelectedTankInfoPanel()
            {
                DoubleBuffered = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawPanelFrame(e.Graphics, ClientRectangle, false);

                using (Font eyebrowFont = new Font("Microsoft YaHei UI", 10, FontStyle.Bold))
                using (Font titleFont = new Font("Microsoft YaHei UI", 21, FontStyle.Bold))
                using (Font labelFont = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold))
                using (Font valueFont = new Font("Microsoft YaHei UI", 11.5f, FontStyle.Bold))
                using (Font skillFont = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Regular))
                using (SolidBrush mutedBrush = new SolidBrush(Color.FromArgb(190, 205, 225)))
                using (SolidBrush titleBrush = new SolidBrush(TankColor))
                using (SolidBrush whiteBrush = new SolidBrush(Color.White))
                using (SolidBrush goldBrush = new SolidBrush(Color.FromArgb(255, 231, 145)))
                {
                    e.Graphics.DrawString("当前出战坦克", eyebrowFont, goldBrush, 22, 18);
                    e.Graphics.DrawString(TankName ?? string.Empty, titleFont, titleBrush, 22, 42);

                    if (TankImage != null)
                    {
                        e.Graphics.DrawImage(TankImage, 34, 95, 142, 112);
                    }

                    if (TankData == null || TankConfig == null)
                    {
                        return;
                    }

                    int attack = TankConfig.GetAttackAtLevel(TankData.AttackLevel);
                    int health = TankConfig.GetHealthAtLevel(TankData.HealthLevel);
                    double speed = TankConfig.GetSpeedMultiplierAtLevel(TankData.SpeedLevel);
                    double bulletSpeed = TankConfig.GetBulletSpeedMultiplierAtLevel(TankData.BulletSpeedLevel);
                    double cooldown = TankConfig.GetCooldownSecondsAtLevel(TankData.CooldownLevel);
                    TankSkillType skillType = TankSkillCatalog.GetSkillType(TankType);
                    string skillName = TankSkillCatalog.GetDisplayName(skillType);
                    string skillDescription = TankSkillCatalog.GetDescription(skillType);

                    DrawInfoRow(e.Graphics, labelFont, valueFont, mutedBrush, whiteBrush, "攻击", attack + "  Lv" + TankData.AttackLevel + "/5", 220, 52);
                    DrawInfoRow(e.Graphics, labelFont, valueFont, mutedBrush, whiteBrush, "生命", health + "  Lv" + TankData.HealthLevel + "/5", 380, 52);
                    DrawInfoRow(e.Graphics, labelFont, valueFont, mutedBrush, whiteBrush, "移速", speed.ToString("0.00") + "x  Lv" + TankData.SpeedLevel + "/5", 220, 106);
                    DrawInfoRow(e.Graphics, labelFont, valueFont, mutedBrush, whiteBrush, "弹速", bulletSpeed.ToString("0.00") + "x  Lv" + TankData.BulletSpeedLevel + "/5", 380, 106);
                    DrawInfoRow(e.Graphics, labelFont, valueFont, mutedBrush, whiteBrush, "开火冷却", cooldown.ToString("0.0") + "s  Lv" + TankData.CooldownLevel + "/5", 220, 160);

                    e.Graphics.DrawString("技能", labelFont, mutedBrush, 380, 160);
                    e.Graphics.DrawString(skillName, valueFont, goldBrush, 380, 180);
                    e.Graphics.DrawString(skillDescription, skillFont, mutedBrush, new RectangleF(220, 220, Width - 244, 28));
                }
            }

            private static void DrawInfoRow(Graphics g, Font labelFont, Font valueFont, Brush labelBrush, Brush valueBrush, string label, string value, int x, int y)
            {
                g.DrawString(label, labelFont, labelBrush, x, y);
                g.DrawString(value, valueFont, valueBrush, x, y + 20);
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
