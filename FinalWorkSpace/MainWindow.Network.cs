using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    public partial class MainWindow
    {
        private Label networkHintLabel;
        private Label networkStatusLabel;
        private TcpGameClient networkClient;
        private NetworkConfig activeNetworkConfig;
        private readonly NetworkInputState remoteInputState = new NetworkInputState();
        private readonly NetworkInputState guestInputState = new NetworkInputState();
        private readonly object snapshotLock = new object();
        private GameSnapshot latestSnapshot;
        private bool networkMode;
        private bool networkHost;
        private bool networkWaiting;
        private bool networkSessionStarted;
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
            networkHintLabel.BringToFront();
            networkStatusLabel.BringToFront();
        }

        private void UpdateNetworkUiVisibility()
        {
            if (networkHintLabel == null || networkStatusLabel == null)
            {
                return;
            }

            networkHintLabel.Visible = modeMenu == 0 && !networkWaiting && !networkSessionStarted && (launchOptions == null || !launchOptions.StartImmediately);
            networkStatusLabel.Visible = networkWaiting || (networkSessionStarted && !string.IsNullOrEmpty(networkStatusLabel.Text));
            networkHintLabel.BringToFront();
            networkStatusLabel.BringToFront();
        }

        private bool TryHandleNetworkGlobalKeyDown(KeyEventArgs e)
        {
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

            if (networkSessionStarted && modeMenu >= 3)
            {
                return HandleNetworkGameplayKey(e.KeyCode, true);
            }

            return false;
        }

        private bool TryHandleNetworkGlobalKeyUp(KeyEventArgs e)
        {
            if (networkSessionStarted && modeMenu >= 3)
            {
                return HandleNetworkGameplayKey(e.KeyCode, false);
            }

            return false;
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
            BeginNetworkConnect(config, false);
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
                ResetToMainMenuFromNetwork(false);
                Close();
                return;
            }

            if (line.StartsWith("ROOM_EXISTS|", StringComparison.Ordinal))
            {
                string roomId = line.Substring("ROOM_EXISTS|".Length);
                MessageBox.Show(this, "房间号已存在：" + roomId, "创建房间", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetToMainMenuFromNetwork(false);
                Close();
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

            if (line.StartsWith("INPUT|", StringComparison.Ordinal) && IsNetworkHost)
            {
                string payload = line.Substring("INPUT|".Length);
                NetworkInputState input = NetworkSerialization.DeserializeFromBase64<NetworkInputState>(payload);
                CopyInput(input, remoteInputState);
                return;
            }

            if (line.StartsWith("STATE|", StringComparison.Ordinal) && IsNetworkGuest)
            {
                string payload = line.Substring("STATE|".Length);
                GameSnapshot snapshot = NetworkSerialization.DeserializeFromBase64<GameSnapshot>(payload);
                lock (snapshotLock)
                {
                    latestSnapshot = snapshot;
                }
                networkStatusLabel.Text = snapshot.StatusText;
                UpdateNetworkUiVisibility();
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
                MessageBox.Show(this, "另一名玩家已离线，联机会话结束。", "联机结束", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetToMainMenuFromNetwork(false);
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
                ResetToMainMenuFromNetwork(false);
            }
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
                networkStatusLabel.Text = "联机已匹配成功，你是主机端，负责同步游戏状态。";
                StartBattleDirectly();
            }
            else
            {
                setting.twoPlayer = true;
                networkStatusLabel.Text = "联机已匹配成功，你是副机端，等待主机同步状态...";
                modeMenu = 8;
                timer1.Interval = GameTiming.TimerIntervalMs;
                timer1.Enabled = true;
                RefreshBattleBackground();
                grMain.DrawImage(backgroundMemory, 0, 0);
            }

            UpdateNetworkUiVisibility();
        }

        private void ApplyDifficultySettings(NetworkDifficulty difficulty)
        {
            if (difficulty == NetworkDifficulty.Easy)
            {
                setting.difficulty = 0;
                setting.countEnemies = 15;
                activeEnemyLimit = 3;
                setting.hitPointsEnemy = 1;
                setting.level = 1;
                setting.lifesPlayer1 = 3;
                setting.lifesPlayer2 = 3;
                setting.pointsPlayer1 = 0;
                setting.pointsPlayer2 = 0;
                setting.speedEnemy = 2;
                setting.pointsForEnemy = 80;
                setting.timeSpawn = GameTiming.ScaleLegacyTicks(300);
                setting.pointsForPlayers = 500;
            }
            else if (difficulty == NetworkDifficulty.Medium)
            {
                setting.difficulty = 1;
                setting.countEnemies = 25;
                activeEnemyLimit = 5;
                setting.hitPointsEnemy = 1;
                setting.level = 1;
                setting.lifesPlayer1 = 2;
                setting.lifesPlayer2 = 2;
                setting.pointsPlayer1 = 0;
                setting.pointsPlayer2 = 0;
                setting.speedEnemy = 4;
                setting.pointsForEnemy = 90;
                setting.timeSpawn = GameTiming.ScaleLegacyTicks(100);
                setting.pointsForPlayers = 750;
            }
            else
            {
                setting.difficulty = 2;
                setting.countEnemies = 50;
                activeEnemyLimit = 8;
                setting.hitPointsEnemy = 2;
                setting.level = 1;
                setting.lifesPlayer1 = 1;
                setting.lifesPlayer2 = 1;
                setting.pointsPlayer1 = 0;
                setting.pointsPlayer2 = 0;
                setting.speedEnemy = 5;
                setting.pointsForEnemy = 100;
                setting.timeSpawn = GameTiming.ScaleLegacyTicks(50);
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
            return false;
        }

        private void SendGuestInputState()
        {
            if (networkClient == null || !networkClient.IsConnected)
            {
                return;
            }

            string payload = NetworkSerialization.SerializeToBase64(guestInputState);
            networkClient.SendLine("INPUT|" + payload);
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
        }

        private void ProcessPlayerFireInputs()
        {
            if (firstPlayer && player1.message.isSpace && player1.message.pauseSpace == 0)
            {
                player1.message.pauseSpace = GameTiming.ScaleLegacyTicks(10);
                bullets.Add(new Bullet(player1.pictureBullet, player1.x, player1.y, player1.direction, 1, player1.attack));
            }

            if (secondPlayer && player2.message.isSpace && player2.message.pauseSpace == 0)
            {
                player2.message.pauseSpace = GameTiming.ScaleLegacyTicks(10);
                bullets.Add(new Bullet(player2.pictureBullet, player2.x, player2.y, player2.direction, 2, player2.attack));
            }
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

            GameSnapshot snapshot = BuildSnapshot(gameOver);
            if (results != null)
            {
                snapshot.Results = results;
            }
            string payload = NetworkSerialization.SerializeToBase64(snapshot);
            networkClient.SendLine("STATE|" + payload);
        }

        private GameSnapshot BuildSnapshot(bool gameOver)
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
                GameOver = gameOver,
                StatusText = gameOver ? "主机端已结束本局游戏。" : "联机中：已同步主机画面"
            };

            snapshot.Player1 = new TankSnapshot
            {
                X = player1.x,
                Y = player1.y,
                Direction = player1.direction,
                HitPoints = player1.hitpoints,
                Active = firstPlayer,
                Kind = 1,
                TankType = (int)networkPlayer1TankType
            };
            snapshot.Player2 = new TankSnapshot
            {
                X = player2.x,
                Y = player2.y,
                Direction = player2.direction,
                HitPoints = player2.hitpoints,
                Active = secondPlayer,
                Kind = 2,
                TankType = (int)networkPlayer2TankType
            };

            for (int i = 0; i < enemies.Count; i++)
            {
                snapshot.Enemies.Add(new TankSnapshot
                {
                    X = enemies[i].x,
                    Y = enemies[i].y,
                    Direction = enemies[i].direction,
                    HitPoints = enemies[i].hitpoints,
                    Active = true,
                    Kind = 3
                });
            }

            for (int i = 0; i < bullets.Count; i++)
            {
                snapshot.Bullets.Add(new BulletSnapshot
                {
                    X = bullets[i].x,
                    Y = bullets[i].y,
                    Direction = GetBulletDirection(bullets[i]),
                    Who = bullets[i].who
                });
            }

            for (int i = 0; i < boxes.Count; i++)
            {
                snapshot.Boxes.Add(new BoxSnapshot
                {
                    X = boxes[i].x,
                    Y = boxes[i].y,
                    Type = GetBoxType(boxes[i])
                });
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

            return snapshot;
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

            DrawSnapshotHud(snapshot);

            for (int i = 0; i < snapshot.Boxes.Count; i++)
            {
                if (snapshot.Boxes[i].Type != 2)
                {
                    grMemory.DrawImage(GetBoxImage(snapshot.Boxes[i].Type), snapshot.Boxes[i].X, snapshot.Boxes[i].Y, 25, 25);
                }
            }

            for (int i = 0; i < snapshot.Bullets.Count; i++)
            {
                DrawRotatedImage(grMemory, ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan,
                    snapshot.Bullets[i].X, snapshot.Bullets[i].Y, 10, 10, snapshot.Bullets[i].Direction);
            }

            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                DrawRotatedImage(grMemory, Peace_of_Tanks.Properties.Resources.enemy11,
                    snapshot.Enemies[i].X, snapshot.Enemies[i].Y, 50, 50, snapshot.Enemies[i].Direction);
            }

            if (snapshot.Player1.Active)
            {
                DrawTankGlow(snapshot.Player1.X, snapshot.Player1.Y, IsNetworkGuest ? Color.FromArgb(255, 82, 82) : Color.FromArgb(80, 235, 120));
                DrawRotatedImage(grMemory, GetTankSnapshotImage(snapshot.Player1),
                    snapshot.Player1.X, snapshot.Player1.Y, 50, 50, snapshot.Player1.Direction);
            }

            if (snapshot.Player2.Active)
            {
                DrawTankGlow(snapshot.Player2.X, snapshot.Player2.Y, IsNetworkGuest ? Color.FromArgb(80, 235, 120) : Color.FromArgb(255, 82, 82));
                DrawRotatedImage(grMemory, GetTankSnapshotImage(snapshot.Player2),
                    snapshot.Player2.X, snapshot.Player2.Y, 50, 50, snapshot.Player2.Direction);
            }

            for (int i = 0; i < snapshot.Boxes.Count; i++)
            {
                if (snapshot.Boxes[i].Type == 2)
                {
                    grMemory.DrawImage(GetBoxImage(snapshot.Boxes[i].Type), snapshot.Boxes[i].X, snapshot.Boxes[i].Y, 25, 25);
                }
            }

            for (int i = 0; i < snapshot.Animations.Count; i++)
            {
                DrawSnapshotAnimation(snapshot.Animations[i]);
            }

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
            int x = 150;
            int y = 760;
            for (int i = 0; i < snapshot.LivesPlayer1; i++)
            {
                grMemory.DrawImage(heart, x, y, 25, 25);
                x += 30;
            }

            x = 530;
            for (int i = 0; i < snapshot.LivesPlayer2; i++)
            {
                grMemory.DrawImage(heart, x, y, 25, 25);
                x += 30;
            }

            DrawNumber(snapshot.PointsPlayer1, 255, 760);
            DrawNumber(snapshot.PointsPlayer2, 633, 760);
            DrawKillHud(snapshot.KillsPlayer1, snapshot.KillsPlayer2);
        }

        private void DrawNumber(int value, int x, int y)
        {
            int[] digits;
            if (value == 0)
            {
                digits = new int[] { 0 };
            }
            else
            {
                List<int> items = new List<int>();
                while (value != 0)
                {
                    items.Insert(0, value % 10);
                    value /= 10;
                }
                digits = items.ToArray();
            }

            for (int i = 0; i < digits.Length; i++)
            {
                Numbers.Draw(grMemory, Numbers.ReturnNumPic(digits[i]), x, y);
                x += 30;
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
        }

        private void DrawRotatedImage(Graphics graphics, Bitmap source, int x, int y, int width, int height, int direction)
        {
            using (Bitmap image = new Bitmap(source))
            {
                image.MakeTransparent(Color.Black);
                RotateBitmap(image, direction);
                graphics.DrawImage(image, x, y, width, height);
            }
        }

        private void RotateBitmap(Bitmap image, int direction)
        {
            if (direction == 0) image.RotateFlip(RotateFlipType.Rotate90FlipX);
            if (direction == 1) image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            if (direction == 3) image.RotateFlip(RotateFlipType.Rotate180FlipNone);
        }

        private void CopyInput(NetworkInputState from, NetworkInputState to)
        {
            to.Up = from.Up;
            to.Down = from.Down;
            to.Left = from.Left;
            to.Right = from.Right;
            to.Fire = from.Fire;
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

            MovePlayer1 = null;
            MovePlayer2 = null;
            MoveEnemy = null;
            DelayEvent = null;

            firstPlayer = true;
            secondPlayer = false;
            ResetMatchStats();
            player1.x = -100;
            player1.y = -100;
            player2.x = -50;
            player2.y = -50;
            player1.hitpoints = 1;
            player2.hitpoints = 1;
            player1.message.clear();
            player2.message.clear();
            remoteInputState.Up = remoteInputState.Down = remoteInputState.Left = remoteInputState.Right = remoteInputState.Fire = false;
            guestInputState.Up = guestInputState.Down = guestInputState.Left = guestInputState.Right = guestInputState.Fire = false;

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

        private void CloseNetworkConnection()
        {
            if (networkClient != null)
            {
                networkClient.LineReceived -= NetworkClient_LineReceived;
                networkClient.Disconnected -= NetworkClient_Disconnected;
                networkClient.Dispose();
                networkClient = null;
            }
            UpdateNetworkUiVisibility();
        }
    }
}
