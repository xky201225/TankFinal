using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    public partial class MainWindow
    {
        private Label networkHintLabel;
        private Label networkStatusLabel;
        private Panel networkChatPanel;
        private RichTextBox networkChatLog;
        private TextBox networkChatInput;
        private Label networkVoiceLabel;
        private TcpGameClient networkClient;
        private NetworkConfig activeNetworkConfig;
        private const int NetworkRenderIntervalMs = 16;
        private const int NetworkSnapshotIntervalMs = 16;
        private const int NetworkBoxSnapshotIntervalMs = 500;
        private const int NetworkInputIntervalMs = 25;
        private readonly NetworkInputState remoteInputState = new NetworkInputState();
        private readonly NetworkInputState guestInputState = new NetworkInputState();
        private readonly HashSet<string> remoteExplosionSoundKeys = new HashSet<string>();
        private readonly List<BoxSnapshot> remoteBoxSnapshots = new List<BoxSnapshot>();
        private NetworkVoiceChat networkVoiceChat;
        private readonly object snapshotLock = new object();
        private GameSnapshot latestSnapshot;
        private DateTime lastSnapshotBroadcastAtUtc = DateTime.MinValue;
        private DateTime lastBoxSnapshotBroadcastAtUtc = DateTime.MinValue;
        private DateTime lastGuestInputSentAtUtc = DateTime.MinValue;
        private DateTime lastNetworkStatusUiUpdateAtUtc = DateTime.MinValue;
        private int lastSnapshotBoxCount = -1;
        private string lastNetworkStatusText = string.Empty;
        private bool networkMode;
        private bool networkHost;
        private bool networkWaiting;
        private bool networkSessionStarted;
        private bool networkVoiceHeld;
        private bool suppressDisconnectMessage;
        private TankType networkPlayer1TankType = TankType.Green;
        private TankType networkPlayer2TankType = TankType.Green;

        private bool IsNetworkGuest
        {
            get { return networkSessionStarted && networkMode && !networkHost; }
        }

        private bool IsNetworkHost
        {
            get { return networkSessionStarted && networkMode && networkHost; }
        }

        private void InitializeNetworkUi()
        {
            networkHintLabel = new Label
            {
                AutoSize = true,
                BackColor = Color.FromArgb(20, 30, 44),
                ForeColor = Color.FromArgb(230, 240, 255),
                Location = new Point(18, 15),
                Text = "按 N 开始网络联机，输入同一服务器地址即可匹配",
                Visible = true
            };

            networkStatusLabel = new Label
            {
                AutoSize = false,
                BackColor = Color.FromArgb(20, 30, 44),
                ForeColor = Color.FromArgb(255, 231, 145),
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
                Location = new Point(18, 18),
                Padding = new Padding(12, 8, 12, 8),
                Size = new Size(760, 42),
                Text = string.Empty,
                Visible = false
            };

            Controls.Add(networkHintLabel);
            Controls.Add(networkStatusLabel);
            CreateNetworkChatUi();
            networkHintLabel.BringToFront();
            networkStatusLabel.BringToFront();
        }

        private void CreateNetworkChatUi()
        {
            networkChatPanel = new Panel
            {
                BackColor = Color.FromArgb(185, 12, 20, 32),
                Location = GetNetworkChatPanelLocation(),
                Size = new Size(330, 104),
                Visible = false
            };

            networkChatLog = new RichTextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(18, 28, 42),
                ForeColor = Color.FromArgb(230, 240, 255),
                Font = new Font("Microsoft YaHei UI", 9, FontStyle.Regular),
                Location = new Point(8, 8),
                Size = new Size(314, 52),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            networkChatInput = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(25, 36, 50),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9, FontStyle.Regular),
                Location = new Point(8, 68),
                Size = new Size(190, 23)
            };
            networkChatInput.KeyDown += NetworkChatInput_KeyDown;

            Button sendButton = new Button
            {
                Text = "发送",
                Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(160, 126, 84, 26),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(204, 66),
                Size = new Size(56, 26)
            };
            sendButton.FlatAppearance.BorderColor = Color.FromArgb(255, 205, 99);
            sendButton.Click += (s, e) => SendNetworkChatMessage();

            networkVoiceLabel = new Label
            {
                Text = "Tab 语音",
                Font = new Font("Microsoft YaHei UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(190, 205, 225),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(266, 72)
            };

            networkChatPanel.Controls.Add(networkChatLog);
            networkChatPanel.Controls.Add(networkChatInput);
            networkChatPanel.Controls.Add(sendButton);
            networkChatPanel.Controls.Add(networkVoiceLabel);
            Controls.Add(networkChatPanel);
        }

        private Point GetNetworkChatPanelLocation()
        {
            int panelWidth = networkChatPanel != null ? networkChatPanel.Width : 330;
            int panelHeight = networkChatPanel != null ? networkChatPanel.Height : 104;
            int x = Math.Max(18, (ClientSize.Width - panelWidth) / 2);
            int y = Math.Max(96, (int)Math.Round(GetSkillPanelY() - panelHeight - 18));
            if (networkStatusLabel != null && networkStatusLabel.Visible)
            {
                y = Math.Max(y, networkStatusLabel.Bottom + 12);
            }

            return new Point(x, y);
        }

        private void UpdateNetworkUiVisibility()
        {
            if (networkHintLabel == null || networkStatusLabel == null)
            {
                return;
            }

            networkHintLabel.Visible = modeMenu == 0 && !networkWaiting && !networkSessionStarted && (launchOptions == null || !launchOptions.StartImmediately);
            networkStatusLabel.Visible = networkWaiting;
            if (networkChatPanel != null)
            {
                networkChatPanel.Visible = networkSessionStarted;
                networkChatPanel.Location = GetNetworkChatPanelLocation();
                if (networkChatPanel.Visible)
                {
                    networkChatPanel.BringToFront();
                }
            }
            networkHintLabel.BringToFront();
            networkStatusLabel.BringToFront();
        }

        private void NetworkChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendNetworkChatMessage();
            }
        }

        private void SendNetworkChatMessage()
        {
            if (networkClient == null || !networkClient.IsConnected || networkChatInput == null)
            {
                return;
            }

            string message = networkChatInput.Text.Trim();
            if (message.Length == 0)
            {
                return;
            }

            string sender = activeNetworkConfig != null && !string.IsNullOrWhiteSpace(activeNetworkConfig.PlayerName)
                ? activeNetworkConfig.PlayerName
                : "我";
            AppendNetworkChat(sender, message);
            networkClient.SendLine("MATCH_CHAT|" + EncodeProtocolPart(sender) + "|" + EncodeProtocolPart(message));
            networkChatInput.Clear();
            Focus();
        }

        private void AppendNetworkChat(string sender, string message)
        {
            if (networkChatLog == null)
            {
                return;
            }

            networkChatLog.AppendText("[" + sender + "] " + message + Environment.NewLine);
            networkChatLog.SelectionStart = networkChatLog.TextLength;
            networkChatLog.ScrollToCaret();
        }

        private bool TryHandleNetworkGlobalKeyDown(KeyEventArgs e)
        {
            if (networkWaiting && e.KeyCode == Keys.Escape)
            {
                CancelNetworkWaitingToLobby();
                return true;
            }

            if (networkSessionStarted && e.KeyCode == Keys.Tab)
            {
                StartNetworkVoice();
                return true;
            }

            if (modeMenu == 0 && e.KeyCode == Keys.N)
            {
                if (networkClient != null && networkClient.IsConnected)
                {
                    networkStatusLabel.Text = networkWaiting
                        ? "已连接服务器，正在等待其他玩家加入..."
                        : "服务器已经连接。";
                    UpdateNetworkUiVisibility();
                    return true;
                }

                if (networkWaiting || networkSessionStarted)
                {
                    UpdateNetworkUiVisibility();
                    return true;
                }

                BeginNetworkConnect();
                return true;
            }

            if (networkSessionStarted && modeMenu >= 3 && !IsNetworkChatInputFocused())
            {
                return HandleNetworkGameplayKey(e.KeyCode, true);
            }

            return false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (networkSessionStarted && (keyData & Keys.KeyCode) == Keys.Tab)
            {
                StartNetworkVoice();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (networkSessionStarted && (keyData & Keys.KeyCode) == Keys.Tab)
            {
                return true;
            }

            return base.IsInputKey(keyData);
        }

        private void CancelNetworkWaitingToLobby()
        {
            ReturnToLobbyAfterNetworkStop();
        }

        private bool TryHandleNetworkGlobalKeyUp(KeyEventArgs e)
        {
            if (networkSessionStarted && e.KeyCode == Keys.Tab)
            {
                StopNetworkVoice();
                return true;
            }

            if (networkSessionStarted && modeMenu >= 3 && !IsNetworkChatInputFocused())
            {
                return HandleNetworkGameplayKey(e.KeyCode, false);
            }

            return false;
        }

        private bool IsNetworkChatInputFocused()
        {
            return networkChatInput != null && networkChatInput.Focused;
        }

        private void BeginNetworkConnect()
        {
            if (networkClient != null && networkClient.IsConnected)
            {
                networkStatusLabel.Text = networkWaiting
                    ? "已连接服务器，正在等待其他玩家加入..."
                    : "服务器已经连接。";
                UpdateNetworkUiVisibility();
                return;
            }

            while (true)
            {
                NetworkConfig config;
                if (!NetworkConfigDialog.ShowDialog(this, out config))
                {
                    return;
                }

                if (BeginNetworkConnect(config, true))
                {
                    return;
                }
            }
        }

        private void BeginNetworkConnect(NetworkConfig config)
        {
            if (!BeginNetworkConnect(config, false))
            {
                ReturnToLobbyAfterNetworkStop();
            }
        }

        private bool BeginNetworkConnect(NetworkConfig config, bool allowRetry)
        {
            CloseNetworkConnection();

            networkClient = new TcpGameClient();
            networkClient.LineReceived += NetworkClient_LineReceived;
            networkClient.Disconnected += NetworkClient_Disconnected;

            string error;
            if (!networkClient.Connect(config.ServerHost, config.ServerPort, config.PlayerName, config.JoinMode, config.RoomId, config.TankType, out error))
            {
                string retryText = allowRetry ? "请重新输入服务器地址。" : "请返回大厅重新输入服务器地址。";
                MessageBox.Show(this, "连接服务器失败：" + error + Environment.NewLine + "当前只能进行单机游戏，" + retryText, "联机失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CloseNetworkConnection();
                return false;
            }

            activeNetworkConfig = config;
            networkMode = true;
            networkWaiting = true;
            networkStatusLabel.Text = "已连接服务器，等待另一名玩家加入...";
            UpdateNetworkUiVisibility();
            return true;
        }

        private void NetworkClient_LineReceived(string line)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                if (line.StartsWith("STATE|", StringComparison.Ordinal) && IsNetworkGuest)
                {
                    GameSnapshot snapshot;
                    if (!TryDeserializeSnapshotLine(line, out snapshot))
                    {
                        return;
                    }

                    try
                    {
                        BeginInvoke(new Action<GameSnapshot>(ApplyNetworkSnapshot), snapshot);
                    }
                    catch
                    {
                    }
                    return;
                }

                try
                {
                    BeginInvoke(new Action<string>(NetworkClient_LineReceived), line);
                }
                catch
                {
                }
                return;
            }

            if (line == "WAITING")
            {
                networkStatusLabel.Text = "已连接服务器，等待另一名玩家加入...";
                UpdateNetworkUiVisibility();
                return;
            }

            if (line.StartsWith("WAITING|", StringComparison.Ordinal))
            {
                string roomId = line.Substring("WAITING|".Length);
                networkStatusLabel.Text = "房间 " + roomId + " 已创建，等待另一名玩家加入...";
                UpdateNetworkUiVisibility();
                return;
            }

            if (line.StartsWith("ROOM_NOT_FOUND|", StringComparison.Ordinal))
            {
                string roomId = line.Substring("ROOM_NOT_FOUND|".Length);
                MessageBox.Show(this, "没有找到房间：" + roomId, "加入房间", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReturnToLobbyAfterNetworkStop();
                return;
            }

            if (line.StartsWith("ROOM_EXISTS|", StringComparison.Ordinal))
            {
                string roomId = line.Substring("ROOM_EXISTS|".Length);
                MessageBox.Show(this, "房间号已存在：" + roomId, "创建房间", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReturnToLobbyAfterNetworkStop();
                return;
            }

            if (line.StartsWith("START|", StringComparison.Ordinal))
            {
                string[] parts = line.Split('|');
                string role = parts.Length > 1 ? parts[1] : string.Empty;
                networkPlayer1TankType = ParseTankType(parts.Length > 3 ? parts[3] : null, selectedTankType);
                networkPlayer2TankType = ParseTankType(parts.Length > 4 ? parts[4] : null, TankType.Green);
                StartNetworkSession(role.Equals("HOST", StringComparison.OrdinalIgnoreCase));
                return;
            }

            if (line.StartsWith("MATCH_CHAT|", StringComparison.Ordinal))
            {
                string[] parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    AppendNetworkChat(DecodeProtocolPart(parts[1]), DecodeProtocolPart(parts[2]));
                }
                return;
            }

            if (line.StartsWith("VOICE_STATE|", StringComparison.Ordinal))
            {
                bool speaking = line.EndsWith("|1", StringComparison.Ordinal);
                if (networkVoiceLabel != null)
                {
                    networkVoiceLabel.Text = speaking ? "对方语音中" : "Tab 语音";
                    networkVoiceLabel.ForeColor = speaking ? Color.FromArgb(255, 231, 145) : Color.FromArgb(190, 205, 225);
                }
                return;
            }

            if (line.StartsWith("VOICE|", StringComparison.Ordinal))
            {
                if (networkVoiceChat == null)
                {
                    networkVoiceChat = new NetworkVoiceChat(SendNetworkVoiceChunk);
                }

                byte[] chunk = DecodeProtocolBytes(line.Substring("VOICE|".Length));
                if (chunk.Length > 0)
                {
                    networkVoiceChat.Play(chunk);
                }
                return;
            }

            if (line.StartsWith("INPUT|", StringComparison.Ordinal) && IsNetworkHost)
            {
                string payload = line.Substring("INPUT|".Length);
                NetworkInputState input = NetworkSerialization.DeserializeFromBase64<NetworkInputState>(payload);
                CopyInput(input, remoteInputState);
                return;
            }

            if (line.StartsWith("STATE|", StringComparison.Ordinal) && IsNetworkGuest)
            {
                GameSnapshot snapshot;
                if (TryDeserializeSnapshotLine(line, out snapshot))
                {
                    ApplyNetworkSnapshot(snapshot);
                }
                return;
            }

            if (line == "GAMEOVER")
            {
                if (IsNetworkGuest)
                {
                    MessageBox.Show(this, "本局游戏已结束。", "联机结束", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ResetToMainMenuFromNetwork(false);
                }
                return;
            }

            if (line == "PEER_LEFT")
            {
                SettlePeerEliminated();
                return;
            }
        }

        private bool TryDeserializeSnapshotLine(string line, out GameSnapshot snapshot)
        {
            snapshot = null;
            try
            {
                string payload = line.Substring("STATE|".Length);
                snapshot = NetworkSerialization.DeserializeFromBase64<GameSnapshot>(payload);
                return snapshot != null;
            }
            catch
            {
                return false;
            }
        }

        private void ApplyNetworkSnapshot(GameSnapshot snapshot)
        {
            if (snapshot == null || IsDisposed)
            {
                return;
            }

            lock (snapshotLock)
            {
                latestSnapshot = snapshot;
            }

            if (snapshot.IncludesBoxes)
            {
                remoteBoxSnapshots.Clear();
                remoteBoxSnapshots.AddRange(snapshot.Boxes);
            }

            string statusText = snapshot.StatusText ?? string.Empty;
            DateTime now = DateTime.UtcNow;
            if (statusText != lastNetworkStatusText || (now - lastNetworkStatusUiUpdateAtUtc).TotalMilliseconds >= 250)
            {
                lastNetworkStatusText = statusText;
                lastNetworkStatusUiUpdateAtUtc = now;
                if (networkStatusLabel != null && networkStatusLabel.Text != statusText)
                {
                    networkStatusLabel.Text = statusText;
                }
            }
        }

        private void NetworkClient_Disconnected(string message)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<string>(NetworkClient_Disconnected), message);
                }
                catch
                {
                }
                return;
            }

            if (suppressDisconnectMessage)
            {
                suppressDisconnectMessage = false;
                return;
            }

            if (networkWaiting || networkSessionStarted || networkMode)
            {
                MessageBox.Show(this, message, "网络连接", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ReturnToLobbyAfterNetworkStop();
            }
        }

        private void SettlePeerEliminated()
        {
            if (settlementShown)
            {
                return;
            }

            timer1.Enabled = false;
            TimerPause.Enabled = false;

            if (IsNetworkHost)
            {
                secondPlayer = false;
                player2.x = player2.y = -50;
                player2Deaths++;
                player1Kills++;
            }
            else if (IsNetworkGuest)
            {
                firstPlayer = false;
                player1.x = player1.y = -100;
                player1Deaths++;
                player2Kills++;
            }

            networkStatusLabel.Text = "另一名玩家已退出，本局按对方淘汰结算。";
            UpdateNetworkUiVisibility();

            suppressDisconnectMessage = true;
            CloseNetworkConnection();
            ShowSettlementPage(BuildSettlementResults());
        }

        private void StartNetworkSession(bool isHost)
        {
            networkWaiting = false;
            networkSessionStarted = true;
            networkHost = isHost;

            ClearRuntimeState();
            HideOverlayControls();

            if (isHost)
            {
                ApplyNetworkPlayerTankImages();
                ApplyDifficultySettings(activeNetworkConfig != null ? activeNetworkConfig.Difficulty : NetworkDifficulty.Easy);
                setting.twoPlayer = true;
                networkStatusLabel.Text = activeNetworkConfig != null && activeNetworkConfig.BattleRule == BattleRuleMode.Survival
                    ? "联机协作已匹配成功：击败所有波次并消灭最终Boss。你是主机端，负责同步敌人与战场状态。"
                    : "联机竞技已匹配成功：先达成10次击杀即可获胜。你是主机端，负责同步对战状态。";
                StartBattleDirectly();
            }
            else
            {
                setting.twoPlayer = true;
                networkStatusLabel.Text = activeNetworkConfig != null && activeNetworkConfig.BattleRule == BattleRuleMode.Survival
                    ? "联机协作已匹配成功：目标是清空波次并击败最终Boss。你是副机端，等待主机同步战场状态..."
                    : "联机竞技已匹配成功：先达成10次击杀即可获胜。你是副机端，等待主机同步对战状态...";
                modeMenu = 8;
                timer1.Interval = NetworkRenderIntervalMs;
                timer1.Enabled = true;
                RefreshBattleBackground();
                grMain.DrawImage(backgroundMemory, 0, 0);
            }

            UpdateNetworkUiVisibility();
        }

        private void ApplyDifficultySettings(NetworkDifficulty difficulty)
        {
            bool survivalMode = IsSurvivalBattle;
            if (difficulty == NetworkDifficulty.Easy)
            {
                setting.difficulty = 0;
                setting.countEnemies = survivalMode ? 15 : 0;
                activeEnemyLimit = survivalMode ? 3 : 0;
                setting.hitPointsEnemy = 1;
                setting.level = 1;
                setting.lifesPlayer1 = 3;
                setting.lifesPlayer2 = 3;
                setting.pointsPlayer1 = 0;
                setting.pointsPlayer2 = 0;
                setting.speedEnemy = survivalMode ? 2 : 0;
                setting.pointsForEnemy = 80;
                setting.timeSpawn = survivalMode ? GameTiming.ScaleLegacyTicks(300) : 0;
                setting.pointsForPlayers = 500;
            }
            else if (difficulty == NetworkDifficulty.Medium)
            {
                setting.difficulty = 1;
                setting.countEnemies = survivalMode ? 25 : 0;
                activeEnemyLimit = survivalMode ? 5 : 0;
                setting.hitPointsEnemy = 1;
                setting.level = 1;
                setting.lifesPlayer1 = 2;
                setting.lifesPlayer2 = 2;
                setting.pointsPlayer1 = 0;
                setting.pointsPlayer2 = 0;
                setting.speedEnemy = survivalMode ? 4 : 0;
                setting.pointsForEnemy = 90;
                setting.timeSpawn = survivalMode ? GameTiming.ScaleLegacyTicks(100) : 0;
                setting.pointsForPlayers = 750;
            }
            else
            {
                setting.difficulty = 2;
                setting.countEnemies = survivalMode ? 50 : 0;
                activeEnemyLimit = survivalMode ? 8 : 0;
                setting.hitPointsEnemy = 2;
                setting.level = 1;
                setting.lifesPlayer1 = 1;
                setting.lifesPlayer2 = 1;
                setting.pointsPlayer1 = 0;
                setting.pointsPlayer2 = 0;
                setting.speedEnemy = survivalMode ? 5 : 0;
                setting.pointsForEnemy = 100;
                setting.timeSpawn = survivalMode ? GameTiming.ScaleLegacyTicks(50) : 0;
                setting.pointsForPlayers = 1000;
            }
        }

        private bool HandleNetworkGameplayKey(Keys key, bool pressed)
        {
            if (IsNetworkHost)
            {
                return ApplyKeyToInput(key, pressed, player1.message);
            }

            if (IsNetworkGuest)
            {
                if (ApplyKeyToInput(key, pressed, guestInputState))
                {
                    UpdateGuestAimFromMouse(false);
                    SendGuestInputState();
                    return true;
                }
            }

            return false;
        }

        private bool ApplyKeyToInput(Keys key, bool pressed, TankEventArgs input)
        {
            if (key == Keys.Left || key == Keys.A)
            {
                input.isLeft = pressed;
                return true;
            }
            if (key == Keys.Right || key == Keys.D)
            {
                input.isRight = pressed;
                return true;
            }
            if (key == Keys.Up || key == Keys.W)
            {
                input.isUp = pressed;
                return true;
            }
            if (key == Keys.Down || key == Keys.S)
            {
                input.isDown = pressed;
                return true;
            }
            if (key == Keys.NumPad0 || key == Keys.Space)
            {
                input.isSpace = pressed;
                return true;
            }
            if (key == Keys.E)
            {
                input.isSkill = pressed;
                return true;
            }
            return false;
        }

        private bool ApplyKeyToInput(Keys key, bool pressed, NetworkInputState input)
        {
            if (key == Keys.Left || key == Keys.A)
            {
                input.Left = pressed;
                return true;
            }
            if (key == Keys.Right || key == Keys.D)
            {
                input.Right = pressed;
                return true;
            }
            if (key == Keys.Up || key == Keys.W)
            {
                input.Up = pressed;
                return true;
            }
            if (key == Keys.Down || key == Keys.S)
            {
                input.Down = pressed;
                return true;
            }
            if (key == Keys.NumPad0 || key == Keys.Space)
            {
                input.Fire = pressed;
                return true;
            }
            if (key == Keys.E)
            {
                input.Skill = pressed;
                return true;
            }
            return false;
        }

        private void SendGuestInputState()
        {
            SendGuestInputState(true);
        }

        private void SendGuestInputState(bool force)
        {
            if (networkClient == null || !networkClient.IsConnected)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            if (!force && (now - lastGuestInputSentAtUtc).TotalMilliseconds < NetworkInputIntervalMs)
            {
                return;
            }

            lastGuestInputSentAtUtc = now;
            string payload = NetworkSerialization.SerializeToBase64(guestInputState);
            networkClient.SendLine("INPUT|" + payload);
        }

        private void UpdateGuestAimFromMouse(bool sendWhenChanged)
        {
            if (!IsNetworkGuest || modeMenu < 3)
            {
                return;
            }

            int tankX;
            int tankY;
            float fallbackRotation;
            if (!TryGetGuestControlledTankAimReference(out tankX, out tankY, out fallbackRotation))
            {
                return;
            }

            Point mousePoint = ToBattlefieldPoint(PointToClient(MousePosition));
            float aimAngle = CalculateAimAngle(mousePoint, tankX, tankY, fallbackRotation);
            if (Math.Abs(guestInputState.AimAngleDegrees - aimAngle) <= 0.1f)
            {
                return;
            }

            guestInputState.AimAngleDegrees = aimAngle;
            if (sendWhenChanged)
            {
                SendGuestInputState(false);
            }
        }

        private bool TryGetGuestControlledTankAimReference(out int tankX, out int tankY, out float fallbackRotation)
        {
            lock (snapshotLock)
            {
                if (latestSnapshot != null && latestSnapshot.Player2.Active)
                {
                    tankX = latestSnapshot.Player2.X;
                    tankY = latestSnapshot.Player2.Y;
                    fallbackRotation = latestSnapshot.Player2.RotationDegrees;
                    return true;
                }
            }

            if (player2 != null)
            {
                tankX = player2.x;
                tankY = player2.y;
                fallbackRotation = player2.RotationDegrees;
                return true;
            }

            tankX = 0;
            tankY = 0;
            fallbackRotation = 0f;
            return false;
        }

        private void StartNetworkVoice()
        {
            if (networkVoiceHeld || networkClient == null || !networkClient.IsConnected)
            {
                return;
            }

            if (networkVoiceChat == null)
            {
                networkVoiceChat = new NetworkVoiceChat(SendNetworkVoiceChunk);
            }

            networkVoiceHeld = true;
            if (networkVoiceLabel != null)
            {
                networkVoiceLabel.Text = "正在说话";
                networkVoiceLabel.ForeColor = Color.FromArgb(156, 244, 190);
            }

            networkClient.SendLine("VOICE_STATE|1");
            if (!networkVoiceChat.StartCapture())
            {
                AppendNetworkChat("系统", "无法打开麦克风。");
                StopNetworkVoice();
            }
        }

        private void StopNetworkVoice()
        {
            if (!networkVoiceHeld)
            {
                return;
            }

            networkVoiceHeld = false;
            if (networkVoiceChat != null)
            {
                networkVoiceChat.StopCapture();
            }

            if (networkVoiceLabel != null)
            {
                networkVoiceLabel.Text = "Tab 语音";
                networkVoiceLabel.ForeColor = Color.FromArgb(190, 205, 225);
            }

            if (networkClient != null && networkClient.IsConnected)
            {
                networkClient.SendLine("VOICE_STATE|0");
            }
        }

        private void SendNetworkVoiceChunk(byte[] chunk)
        {
            if (networkClient == null || !networkClient.IsConnected || chunk == null || chunk.Length == 0)
            {
                return;
            }

            networkClient.SendLine("VOICE|" + Convert.ToBase64String(chunk));
        }

        private void ApplyRemoteInputToSecondPlayer()
        {
            if (!IsNetworkHost)
            {
                return;
            }

            player2.message.isUp = remoteInputState.Up;
            player2.message.isDown = remoteInputState.Down;
            player2.message.isLeft = remoteInputState.Left;
            player2.message.isRight = remoteInputState.Right;
            player2.message.isSpace = remoteInputState.Fire;
            player2.message.isSkill = remoteInputState.Skill;
            player2.UseMouseAim = true;
            player2.SetRotationDegrees(remoteInputState.AimAngleDegrees);
        }

        private void ProcessPlayerFireInputs()
        {
            if (firstPlayer && player1.message.isSpace)
            {
                TryFirePlayer(player1, 1);
            }

            if (secondPlayer && player2.message.isSpace)
            {
                TryFirePlayer(player2, 2);
            }
        }

        private bool TryFirePlayer(PlayerTank player, int who)
        {
            DateTime now = DateTime.UtcNow;
            if (player == null || now < player.nextFireAvailableAtUtc)
            {
                return false;
            }

            player.nextFireAvailableAtUtc = now.AddSeconds(player.GetCurrentCooldownSeconds());
            player.message.pauseSpace = 0;
            int attack = player.GetCurrentAttack();
            if (player.ConsumeBarrageShot())
            {
                bullets.Add(new Bullet(player.pictureBullet, player.x, player.y, player.RotationDegrees - 12f, who, attack, player.bulletSpeedMultiplier));
                bullets.Add(new Bullet(player.pictureBullet, player.x, player.y, player.RotationDegrees, who, attack, player.bulletSpeedMultiplier));
                bullets.Add(new Bullet(player.pictureBullet, player.x, player.y, player.RotationDegrees + 12f, who, attack, player.bulletSpeedMultiplier));
            }
            else
            {
                bullets.Add(new Bullet(player.pictureBullet, player.x, player.y, player.RotationDegrees, who, attack, player.bulletSpeedMultiplier));
            }
            return true;
        }

        private void BroadcastSnapshot(bool gameOver)
        {
            BroadcastSnapshot(gameOver, null);
        }

        private void BroadcastSnapshot(bool gameOver, List<PlayerResultSnapshot> results)
        {
            if (!IsNetworkHost || networkClient == null || !networkClient.IsConnected)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            if (!gameOver && results == null && (now - lastSnapshotBroadcastAtUtc).TotalMilliseconds < NetworkSnapshotIntervalMs)
            {
                return;
            }

            lastSnapshotBroadcastAtUtc = now;
            bool includeBoxes = ShouldIncludeBoxesInSnapshot(now, gameOver, results);
            GameSnapshot snapshot = BuildSnapshot(gameOver, includeBoxes);
            if (includeBoxes)
            {
                lastBoxSnapshotBroadcastAtUtc = now;
                lastSnapshotBoxCount = boxes.Count;
            }

            if (results != null)
            {
                snapshot.Results = results;
            }
            string payload = NetworkSerialization.SerializeToBase64(snapshot);
            networkClient.SendLine("STATE|" + payload);
        }

        private bool ShouldIncludeBoxesInSnapshot(DateTime now, bool gameOver, List<PlayerResultSnapshot> results)
        {
            return gameOver
                || results != null
                || boxes.Count != lastSnapshotBoxCount
                || (now - lastBoxSnapshotBroadcastAtUtc).TotalMilliseconds >= NetworkBoxSnapshotIntervalMs;
        }

        private GameSnapshot BuildSnapshot(bool gameOver, bool includeBoxes)
        {
            GameSnapshot snapshot = new GameSnapshot
            {
                LivesPlayer1 = setting.lifesPlayer1,
                LivesPlayer2 = setting.lifesPlayer2,
                PointsPlayer1 = setting.pointsPlayer1,
                PointsPlayer2 = setting.pointsPlayer2,
                KillsPlayer1 = player1Kills,
                KillsPlayer2 = player2Kills,
                EnemiesLeft = setting.countEnemies,
                SurvivalWave = survivalWave,
                WaveEnemiesLeft = Math.Max(0, setting.countEnemies),
                SurvivalBossWave = survivalBossWaveActive,
                SurvivalVictory = survivalVictoryAchieved,
                BattleBannerTicks = battleBannerTicks,
                GameOver = gameOver,
                StatusText = gameOver ? "主机端已结束本局游戏。" : GetBattleStatusText(),
                BattleBannerText = battleBannerText ?? string.Empty
            };
            snapshot.IncludesBoxes = includeBoxes;

            snapshot.Player1 = new TankSnapshot
            {
                X = player1.x,
                Y = player1.y,
                Direction = player1.direction,
                RotationDegrees = player1.RotationDegrees,
                HitPoints = player1.hitpoints,
                MaxHitPoints = player1.maxHitpoints,
                Active = firstPlayer,
                Kind = 1,
                TankType = (int)networkPlayer1TankType
            };
            FillSkillSnapshot(snapshot.Player1, player1);
            snapshot.Player2 = new TankSnapshot
            {
                X = player2.x,
                Y = player2.y,
                Direction = player2.direction,
                RotationDegrees = player2.RotationDegrees,
                HitPoints = player2.hitpoints,
                MaxHitPoints = player2.maxHitpoints,
                Active = secondPlayer,
                Kind = 2,
                TankType = (int)networkPlayer2TankType
            };
            FillSkillSnapshot(snapshot.Player2, player2);

            for (int i = 0; i < enemies.Count; i++)
            {
                snapshot.Enemies.Add(new TankSnapshot
                {
                    X = enemies[i].x,
                    Y = enemies[i].y,
                    Direction = enemies[i].direction,
                    RotationDegrees = enemies[i].RotationDegrees,
                    HitPoints = enemies[i].hitpoints,
                    MaxHitPoints = enemies[i].maxHitpoints,
                    Active = true,
                    Kind = 3,
                    TankType = (int)enemies[i].TankType
                });
            }

            for (int i = 0; i < bullets.Count; i++)
            {
                snapshot.Bullets.Add(new BulletSnapshot
                {
                    X = bullets[i].x,
                    Y = bullets[i].y,
                    Direction = GetBulletDirection(bullets[i]),
                    RotationDegrees = bullets[i].RotationDegrees,
                    Who = bullets[i].who,
                    IsBoss = bullets[i].DrawSize > 10
                });
            }

            if (includeBoxes)
            {
                for (int i = 0; i < boxes.Count; i++)
                {
                    snapshot.Boxes.Add(new BoxSnapshot
                    {
                        X = boxes[i].x,
                        Y = boxes[i].y,
                        Type = GetBoxType(boxes[i])
                    });
                }
            }

            for (int i = 0; i < animations.Count; i++)
            {
                snapshot.Animations.Add(new AnimationSnapshot
                {
                    X = animations[i].message.x,
                    Y = animations[i].message.y,
                    Type = animations[i].message.type,
                    Delay = animations[i].message.delay
                });
            }

            for (int i = 0; i < damageTexts.Count; i++)
            {
                snapshot.DamageTexts.Add(new DamageTextSnapshot
                {
                    X = (int)damageTexts[i].X,
                    Y = (int)damageTexts[i].Y,
                    Value = damageTexts[i].Value,
                    IsPlayerTarget = damageTexts[i].IsPlayerTarget,
                    Ticks = damageTexts[i].Ticks
                });
            }

            return snapshot;
        }

        private void FillSkillSnapshot(TankSnapshot snapshot, PlayerTank player)
        {
            if (snapshot == null || player == null)
            {
                return;
            }

            snapshot.SkillType = (int)player.SkillType;
            snapshot.SkillReady = player.IsSkillReady;
            snapshot.RemainingSkillCooldownSeconds = (float)player.GetRemainingSkillCooldownSeconds();
            snapshot.OverdriveActive = player.IsOverdriveActive;
            snapshot.LifestealActive = player.IsLifestealActive;
            snapshot.BarrageShotsRemaining = player.BarrageShotsRemaining;
        }

        private int GetBoxType(Box box)
        {
            if (box is Box_Wall) return 0;
            if (box is Box_Water) return 1;
            if (box is Box_Green) return 2;
            if (box is Box_Base) return 4;
            return 3;
        }

        private int GetBulletDirection(Bullet bullet)
        {
            return bullet.Direction;
        }

        private void RenderRemoteSnapshotFrame()
        {
            GameSnapshot snapshot;
            lock (snapshotLock)
            {
                snapshot = latestSnapshot;
            }

            grMemory.DrawImage(backgroundMain, 0, 0);

            if (snapshot == null)
            {
                grMain.DrawImage(backgroundMemory, 0, 0);
                grMemory.DrawImage(backgroundMain, 0, 0);
                return;
            }

            PlayRemoteSnapshotExplosionSounds(snapshot);
            DrawSnapshotHud(snapshot);
            List<BoxSnapshot> boxesToDraw = snapshot.IncludesBoxes ? snapshot.Boxes : remoteBoxSnapshots;

            Rectangle battlefieldViewport = GetBattlefieldViewport();
            GraphicsState worldState = grMemory.Save();
            grMemory.TranslateTransform(battlefieldViewport.X, battlefieldViewport.Y);

            for (int i = 0; i < boxesToDraw.Count; i++)
            {
                if (boxesToDraw[i].Type != 2)
                {
                    grMemory.DrawImage(GetBoxImage(boxesToDraw[i].Type), boxesToDraw[i].X, boxesToDraw[i].Y, 25, 25);
                }
            }

            for (int i = 0; i < snapshot.Bullets.Count; i++)
            {
                int bulletSize = snapshot.Bullets[i].IsBoss ? 20 : 14;
                DrawRotatedImage(grMemory, ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan,
                    snapshot.Bullets[i].X, snapshot.Bullets[i].Y, bulletSize, bulletSize, snapshot.Bullets[i].RotationDegrees + Bullet.VisualRotationOffsetDegrees);
            }

            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                int enemySize = snapshot.Enemies[i].MaxHitPoints >= 180 ? 74 : 50;
                DrawRotatedImage(grMemory, GetTankSnapshotImage(snapshot.Enemies[i]),
                    snapshot.Enemies[i].X, snapshot.Enemies[i].Y, enemySize, enemySize, snapshot.Enemies[i].RotationDegrees);
            }

            DrawSnapshotWorldHealthBars(snapshot);

            if (snapshot.Player1.Active)
            {
                DrawTankGlow(snapshot.Player1.X, snapshot.Player1.Y, IsNetworkGuest ? Color.FromArgb(255, 82, 82) : Color.FromArgb(80, 235, 120));
                DrawRotatedImage(grMemory, GetTankSnapshotImage(snapshot.Player1),
                    snapshot.Player1.X, snapshot.Player1.Y, 50, 50, snapshot.Player1.RotationDegrees);
            }

            if (snapshot.Player2.Active)
            {
                DrawTankGlow(snapshot.Player2.X, snapshot.Player2.Y, IsNetworkGuest ? Color.FromArgb(80, 235, 120) : Color.FromArgb(255, 82, 82));
                DrawRotatedImage(grMemory, GetTankSnapshotImage(snapshot.Player2),
                    snapshot.Player2.X, snapshot.Player2.Y, 50, 50, snapshot.Player2.RotationDegrees);
            }

            for (int i = 0; i < boxesToDraw.Count; i++)
            {
                if (boxesToDraw[i].Type == 2)
                {
                    grMemory.DrawImage(GetBoxImage(boxesToDraw[i].Type), boxesToDraw[i].X, boxesToDraw[i].Y, 25, 25);
                }
            }

            for (int i = 0; i < snapshot.Animations.Count; i++)
            {
                DrawSnapshotAnimation(snapshot.Animations[i]);
            }

            DrawSnapshotDamageTexts(snapshot);
            grMemory.Restore(worldState);

            if (snapshot.GameOver)
            {
                ShowSettlementPage(snapshot.Results);
                return;
            }

            grMain.DrawImage(backgroundMemory, 0, 0);
            grMemory.DrawImage(backgroundMain, 0, 0);

        }

        private void DrawSnapshotHud(GameSnapshot snapshot)
        {
            player1.hitpoints = snapshot.Player1.HitPoints;
            player1.maxHitpoints = Math.Max(player1.maxHitpoints, snapshot.Player1.HitPoints);
            player2.hitpoints = snapshot.Player2.HitPoints;
            player2.maxHitpoints = Math.Max(player2.maxHitpoints, snapshot.Player2.HitPoints);
            DrawPlayerHealthBar(player1, snapshot.Player1.Active, false, snapshot.LivesPlayer1);
            DrawPlayerHealthBar(player2, snapshot.Player2.Active, true, snapshot.LivesPlayer2);

            if (snapshot.SurvivalBossWave && snapshot.Enemies.Count > 0)
            {
                TankSnapshot boss = snapshot.Enemies.OrderByDescending(enemy => enemy.HitPoints).FirstOrDefault();
                if (boss != null)
                {
                    float width = Math.Min(520f, ClientSize.Width * 0.48f);
                    float x = (ClientSize.Width - width) / 2f;
                    float y = 16f;
                    float ratio = boss.MaxHitPoints <= 0 ? 0f : Math.Max(0f, Math.Min(1f, boss.HitPoints / (float)boss.MaxHitPoints));
                    using (SolidBrush backBrush = new SolidBrush(Color.FromArgb(190, 25, 18, 22)))
                    using (SolidBrush fillBrush = new SolidBrush(Color.FromArgb(186, 72, 210)))
                    using (Pen borderPen = new Pen(Color.FromArgb(255, 240, 220, 120), 2f))
                    {
                        grMemory.FillRectangle(backBrush, x, y, width, 18f);
                        grMemory.FillRectangle(fillBrush, x + 2f, y + 2f, Math.Max(0f, (width - 4f) * ratio), 14f);
                        grMemory.DrawRectangle(borderPen, x, y, width, 18f);
                    }

                    string bossText = "Boss " + Math.Max(0, boss.HitPoints) + "/" + Math.Max(1, boss.MaxHitPoints);
                    SizeF bossSize = grMemory.MeasureString(bossText, battleHudFont);
                    grMemory.DrawString(bossText, battleHudFont, Brushes.White, x + (width - bossSize.Width) / 2f, y - 20f);
                }
            }

            int hudNumberX = ClientSize.Width - 360;
            int hudNumberY = ClientSize.Height - 36;
            DrawNumber(snapshot.PointsPlayer1, hudNumberX, hudNumberY);
            DrawNumber(snapshot.PointsPlayer2, hudNumberX, ClientSize.Height - 72);
            DrawKillHud(snapshot.KillsPlayer1, snapshot.KillsPlayer2);
            DrawSurvivalHud(snapshot.SurvivalWave, snapshot.WaveEnemiesLeft, snapshot.SurvivalBossWave, snapshot.SurvivalVictory);
            DrawSnapshotSkillHud(snapshot.Player1, false);
            DrawSnapshotSkillHud(snapshot.Player2, true);
            if (snapshot.BattleBannerTicks > 0 && !string.IsNullOrEmpty(snapshot.BattleBannerText))
            {
                DrawBattleBanner(snapshot.BattleBannerText);
            }
        }

        private void DrawSnapshotSkillHud(TankSnapshot player, bool rightSide)
        {
            if (player == null)
            {
                return;
            }

            TankSkillType skillType = (TankSkillType)Math.Max(0, player.SkillType);
            string title = TankSkillCatalog.GetDisplayName(skillType);
            string state;
            SolidBrush brush;

            if (!player.Active)
            {
                state = "Inactive";
                brush = skillCooldownBrush;
            }
            else if (player.SkillReady)
            {
                state = "CD Ready";
                brush = skillReadyBrush;
            }
            else
            {
                state = "CD " + Math.Max(0f, player.RemainingSkillCooldownSeconds).ToString("0.0") + "s";
                brush = skillCooldownBrush;
            }

            string extra = "Key E";
            if (player.OverdriveActive)
            {
                extra += "  |  Overdrive";
            }
            else if (player.LifestealActive)
            {
                extra += "  |  Lifesteal";
            }
            else if (player.BarrageShotsRemaining > 0)
            {
                extra += "  |  Shots " + player.BarrageShotsRemaining;
            }

            float panelWidth = GetPlayerHudPanelWidth();
            float panelHeight = 78f;
            float x = GetPlayerHudPanelX(rightSide, panelWidth);
            DrawSkillPanel(x, GetSkillPanelY(), panelWidth, panelHeight, title, state, extra, brush, player.Active);
        }

        private void DrawSnapshotDamageTexts(GameSnapshot snapshot)
        {
            for (int i = 0; i < snapshot.DamageTexts.Count; i++)
            {
                Brush brush = snapshot.DamageTexts[i].IsPlayerTarget ? Brushes.OrangeRed : Brushes.Gold;
                grMemory.DrawString("-" + snapshot.DamageTexts[i].Value, damageTextFont, Brushes.Black, snapshot.DamageTexts[i].X + 1, snapshot.DamageTexts[i].Y + 1);
                grMemory.DrawString("-" + snapshot.DamageTexts[i].Value, damageTextFont, brush, snapshot.DamageTexts[i].X, snapshot.DamageTexts[i].Y);
            }
        }

        private void DrawSnapshotWorldHealthBars(GameSnapshot snapshot)
        {
            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                DrawWorldHealthBar(snapshot.Enemies[i].X + 5, snapshot.Enemies[i].Y - 10, 40, snapshot.Enemies[i].HitPoints, Math.Max(snapshot.Enemies[i].MaxHitPoints, 1), Color.FromArgb(226, 86, 86));
            }

            if (snapshot.Player1.Active)
            {
                DrawWorldHealthBar(snapshot.Player1.X + 5, snapshot.Player1.Y - 10, 40, snapshot.Player1.HitPoints, Math.Max(snapshot.Player1.MaxHitPoints, 1), Color.FromArgb(90, 220, 120));
            }

            if (snapshot.Player2.Active)
            {
                DrawWorldHealthBar(snapshot.Player2.X + 5, snapshot.Player2.Y - 10, 40, snapshot.Player2.HitPoints, Math.Max(snapshot.Player2.MaxHitPoints, 1), Color.FromArgb(90, 170, 255));
            }
        }

        private void DrawNumber(int value, int x, int y)
        {
            DrawNumber(value, x, y, 30);
        }

        private void DrawNumber(int value, int x, int y, int spacing)
        {
            if (value == 0)
            {
                Numbers.Draw(grMemory, Numbers.ReturnNumPic(0), x, y);
                return;
            }

            int divisor = 1;
            while (value / divisor >= 10)
            {
                divisor *= 10;
            }

            while (divisor > 0)
            {
                int digit = value / divisor;
                Numbers.Draw(grMemory, Numbers.ReturnNumPic(digit), x, y);
                value %= divisor;
                divisor /= 10;
                x += spacing;
            }
        }

        private Bitmap GetBoxImage(int type)
        {
            if (type == 0) return ResourceManager.TerrainWall ?? Peace_of_Tanks.Properties.Resources.Qiang;
            if (type == 1) return ResourceManager.TerrainWater ?? Peace_of_Tanks.Properties.Resources.Shui;
            if (type == 2) return ResourceManager.TerrainGrass ?? Peace_of_Tanks.Properties.Resources.Cao;
            if (type == 4) return ResourceManager.TerrainBase ?? Peace_of_Tanks.Properties.Resources.base_tile;
            return ResourceManager.TerrainIron ?? Peace_of_Tanks.Properties.Resources.Tie;
        }

        private void DrawSnapshotAnimation(AnimationSnapshot animation)
        {
            Bitmap image = Animation.GetFrameImage(animation.Type, animation.Delay);
            if (image == null)
            {
                return;
            }

            if (animation.Type == 0)
            {
                grMemory.DrawImage(image, animation.X - 10, animation.Y - 10, 60, 60);
            }
            else
            {
                grMemory.DrawImage(image, animation.X - 30, animation.Y - 30, 120, 120);
            }
        }

        private void PlayRemoteSnapshotExplosionSounds(GameSnapshot snapshot)
        {
            HashSet<string> currentExplosionKeys = new HashSet<string>();
            for (int i = 0; i < snapshot.Animations.Count; i++)
            {
                AnimationSnapshot animation = snapshot.Animations[i];
                if (animation.Type == 0)
                {
                    continue;
                }

                string key = animation.Type + ":" + animation.X + ":" + animation.Y;
                currentExplosionKeys.Add(key);
                if (!remoteExplosionSoundKeys.Contains(key))
                {
                    TankAudio.PlayTankDestroyed();
                }
            }

            remoteExplosionSoundKeys.Clear();
            foreach (string key in currentExplosionKeys)
            {
                remoteExplosionSoundKeys.Add(key);
            }
        }

        private void DrawTankGlow(int x, int y, Color color)
        {
            BaseTank.DrawGlow(grMemory, x, y, 50, 50, color);
        }

        private Bitmap GetTankSnapshotImage(TankSnapshot snapshot)
        {
            TankType type = ParseTankType(snapshot.TankType.ToString(), TankType.Green);
            return TankResourceLoader.GetPlayerTankImages(type).Item1;
        }

        private TankType ParseTankType(string value, TankType fallback)
        {
            int parsed;
            if (int.TryParse(value, out parsed) && parsed >= 0 && parsed <= 4)
            {
                return (TankType)parsed;
            }

            return fallback;
        }

        private void ApplyNetworkPlayerTankImages()
        {
            var player1Images = TankResourceLoader.GetPlayerTankImages(networkPlayer1TankType);
            var player2Images = TankResourceLoader.GetPlayerTankImages(networkPlayer2TankType);
            player1.SetImages(player1Images.Item1, player1Images.Item2);
            player2.SetImages(player2Images.Item1, player2Images.Item2);

            ApplyTankStats(player1, networkPlayer1TankType, true);
            ApplyTankStats(player2, networkPlayer2TankType, false);
        }

        private void ApplyTankStats(PlayerTank tank, TankType tankType, bool useLocalProgress)
        {
            TankConfig config = TankConfigs.GetConfig(tankType);
            int speedLevel = 0;
            int attackLevel = 0;
            int healthLevel = 0;
            int bulletSpeedLevel = 0;
            int cooldownLevel = 0;
            if (useLocalProgress)
            {
                PlayerTankData tankData;
                if (DataManager.CurrentPlayer.Tanks.TryGetValue(tankType, out tankData))
                {
                    speedLevel = tankData.SpeedLevel;
                    attackLevel = tankData.AttackLevel;
                    healthLevel = tankData.HealthLevel;
                    bulletSpeedLevel = tankData.BulletSpeedLevel;
                    cooldownLevel = tankData.CooldownLevel;
                }
            }

            tank.maxHitpoints = config.GetHealthAtLevel(healthLevel);
            tank.hitpoints = tank.maxHitpoints;
            tank.attack = config.GetAttackAtLevel(attackLevel);
            tank.speedMultiplier = TankConfig.GetSpeedMultiplierAtLevel(speedLevel);
            tank.bulletSpeedMultiplier = TankConfig.GetBulletSpeedMultiplierAtLevel(bulletSpeedLevel);
            tank.cooldownSeconds = TankConfig.GetCooldownSecondsAtLevel(cooldownLevel);
            tank.ConfigureSkill(tankType);
        }

        private void DrawRotatedImage(Graphics graphics, Bitmap source, int x, int y, int width, int height, float angleDegrees)
        {
            var state = graphics.Save();
            graphics.TranslateTransform(x + width / 2f, y + height / 2f);
            graphics.RotateTransform(angleDegrees);
            graphics.DrawImage(source, -width / 2f, -height / 2f, width, height);
            graphics.Restore(state);
        }

        private void CopyInput(NetworkInputState from, NetworkInputState to)
        {
            to.Up = from.Up;
            to.Down = from.Down;
            to.Left = from.Left;
            to.Right = from.Right;
            to.Fire = from.Fire;
            to.Skill = from.Skill;
            to.AimAngleDegrees = from.AimAngleDegrees;
        }

        private void HideOverlayControls()
        {
            PicButton1Player.Visible = false;
            PicButton2Player.Visible = false;
            PicButtonStat.Visible = false;
            PicTank.Visible = false;
            PicRules.Visible = false;
            PicAbout.Visible = false;
            PicBackToLobby.Visible = false;
            PicButtonEasy.Visible = false;
            PicButtonHard.Visible = false;
            PicButtonReal.Visible = false;
            PicLevel.Visible = false;
            RichAbout.Visible = false;
            RichRules.Visible = false;
            EnterName1.Visible = false;
            EnterName2.Visible = false;
            buttonName1.Visible = false;
            buttonName2.Visible = false;
            PictureStat1.Visible = false;
            PictureStat2.Visible = false;
            labelStat1.Visible = false;
            labelStat2.Visible = false;
            labelStat3.Visible = false;
            labelStat4.Visible = false;
            labelStat5.Visible = false;
            labelStat6.Visible = false;
            labelStat7.Visible = false;
            labelStat8.Visible = false;
            labelStat9.Visible = false;
            labelStat10.Visible = false;
        }

        private void ClearRuntimeState()
        {
            timer1.Enabled = false;
            TimerPause.Enabled = false;

            boxes.Clear();
            bullets.Clear();
            enemies.Clear();
            tanks.Clear();
            animations.Clear();

            MoveEnemy = null;
            DelayEvent = null;

            firstPlayer = true;
            secondPlayer = false;
            ResetMatchStats();
            player1.x = -100;
            player1.y = -100;
            player2.x = -50;
            player2.y = -50;
            player1.hitpoints = player1.maxHitpoints;
            player2.hitpoints = player2.maxHitpoints;
            player1.message.clear();
            player2.message.clear();
            player1.message.pauseSpace = 0;
            player2.message.pauseSpace = 0;
            player1.nextFireAvailableAtUtc = DateTime.MinValue;
            player2.nextFireAvailableAtUtc = DateTime.MinValue;
            player1.ResetSkillState();
            player2.ResetSkillState();
            remoteInputState.Up = remoteInputState.Down = remoteInputState.Left = remoteInputState.Right = remoteInputState.Fire = remoteInputState.Skill = false;
            guestInputState.Up = guestInputState.Down = guestInputState.Left = guestInputState.Right = guestInputState.Fire = guestInputState.Skill = false;
            remoteInputState.AimAngleDegrees = 0f;
            guestInputState.AimAngleDegrees = 0f;
            remoteExplosionSoundKeys.Clear();
            remoteBoxSnapshots.Clear();
            lastSnapshotBroadcastAtUtc = DateTime.MinValue;
            lastBoxSnapshotBroadcastAtUtc = DateTime.MinValue;
            lastGuestInputSentAtUtc = DateTime.MinValue;
            lastNetworkStatusUiUpdateAtUtc = DateTime.MinValue;
            lastSnapshotBoxCount = -1;
            lastNetworkStatusText = string.Empty;

            RefreshBattleBackground();
            grMain.DrawImage(backgroundMemory, 0, 0);
        }

        private void ResetToMainMenuFromNetwork(bool closeConnection)
        {
            ClearRuntimeState();
            networkWaiting = false;
            networkSessionStarted = false;
            networkHost = false;
            networkMode = false;
            networkStatusLabel.Text = string.Empty;
            latestSnapshot = null;
            modeMenu = 0;

            if (closeConnection)
            {
                CloseNetworkConnection();
            }
            else
            {
                suppressDisconnectMessage = true;
                CloseNetworkConnection();
            }

            MenuWorking();
        }

        private void ReturnToLobbyAfterNetworkStop()
        {
            if (IsDisposed)
            {
                return;
            }

            timer1.Enabled = false;
            TimerPause.Enabled = false;
            networkWaiting = false;
            networkSessionStarted = false;
            networkHost = false;
            networkMode = false;
            latestSnapshot = null;
            returnToLobby = true;

            suppressDisconnectMessage = true;
            CloseNetworkConnection();
            suppressDisconnectMessage = false;

            Close();
        }

        private void CloseNetworkConnection()
        {
            StopNetworkVoice();
            if (networkClient != null)
            {
                networkClient.LineReceived -= NetworkClient_LineReceived;
                networkClient.Disconnected -= NetworkClient_Disconnected;
                networkClient.Dispose();
                networkClient = null;
            }
            UpdateNetworkUiVisibility();
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

        private static byte[] DecodeProtocolBytes(string value)
        {
            try
            {
                return Convert.FromBase64String(value ?? string.Empty);
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }
    }
}
