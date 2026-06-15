using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Peace_of_Tanks
{
    public partial class MainWindow : Form
    {
        BinaryFormatter formatter = new BinaryFormatter();
        private const int PlayerMovementStepsPerFrame = 8;
        private const int BattlefieldWidth = 1000;
        private const int BattlefieldHeight = 740;
        private const int BattleBannerDurationMs = 1500;
        private bool timerResolutionRaised;

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint period);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint period);

        public static event AnimateEvent DelayEvent;             // 写械谢械谐邪褌 蟹邪写械褉卸泻邪 邪薪懈屑邪褑懈懈 (芯褌褉懈褋芯胁泻邪 褋 锌邪褍蟹邪屑懈 屑械卸写褍 褉懈褋褍薪泻邪屑懈)
        public static event EnemyEvent MoveEnemy;                // 写械谢械谐邪褌 写胁懈卸械薪懈械 胁褉邪谐芯胁

        int menu = 0;  // 胁褘斜褉邪薪薪褘泄 锌褍薪泻褌 屑械薪褞
        int modeMenu = 0; // 褌械泻褍褖械械 屑械薪褞
        bool secondPlayer = false, firstPlayer = true; // 芯褌芯斜褉邪卸械薪懈械 锌械褉胁芯谐芯 懈 胁褌芯褉芯谐芯 懈谐褉芯泻邪
        private const int LevelTransitionDelayMs = 3000;
        private const int VersusEnemyActiveLimit = 3;
        private const int VersusEnemySpawnDelayLegacyTicks = 160;
        private const int GameOverSettlementDelayLegacyTicks = 12;
        int gameoverX = 355, gameoverY = 10, pauseGameOver = GameTiming.ScaleLegacyTicks(GameOverSettlementDelayLegacyTicks); // 泻芯芯褉写懈薪邪褌褘 锌谢邪褕泻邪 GameOver

        Statistics stats = new Statistics();

        PrivateFontCollection font;

        PlayerTank player1, player2;
        Settings setting = new Settings();
        LevelDesign level = new LevelDesign();

        // 经验系统相关
        private GameMode currentGameMode = GameMode.SinglePlayer;
        private TankType selectedTankType = TankType.Green;
        private bool returnToLobby = false;  // 标记是否返回大厅
        private GameLaunchOptions launchOptions;
        private int player1Kills;
        private int player2Kills;
        private int activeEnemyLimit = 3;
        private int player1Damage;
        private int player2Damage;
        private int player1Deaths;
        private int player2Deaths;
        private const int FinalSurvivalWave = 4;
        private bool settlementShown;
        private bool expRewarded;
        private int lastSettlementExpGain;
        private int survivalWave;
        private bool survivalBossWaveActive;
        private bool survivalVictoryAchieved;
        private int currentStageId = 1;
        private int stageScore;
        private int stageDamageTaken;
        private DateTime stageStartedAtUtc = DateTime.UtcNow;
        private DateTime survivalNextWaveAtUtc = DateTime.MinValue;
        private string battleBannerText = string.Empty;
        private int battleBannerTicks;
        private int emergencySpawnCooldownTicks;
        private string survivalResultText = string.Empty;
        private sealed class DamageText
        {
            public float X;
            public float Y;
            public int Value;
            public int Ticks;
            public bool IsPlayerTarget;
            public bool IsHeal;
        }
        private readonly List<DamageText> damageTexts = new List<DamageText>();
        private readonly List<Control> settlementControls = new List<Control>();
        private readonly Random random = new Random();
        private readonly Font killHudFont = new Font("Microsoft YaHei UI", 11, FontStyle.Bold);
        private readonly Font battleHudFont = new Font("Microsoft YaHei UI", 11, FontStyle.Bold);
        private readonly Font battleBannerFont = new Font("Microsoft YaHei UI", 18, FontStyle.Bold);
        private readonly Font healthBarFont = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
        private readonly Font damageTextFont = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold);
        private readonly Font skillHudFont = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold);
        private readonly SolidBrush killHudSelfBrush = new SolidBrush(Color.FromArgb(255, 231, 145));
        private readonly SolidBrush killHudOtherBrush = new SolidBrush(Color.FromArgb(210, 230, 245));
        private readonly SolidBrush battleHudBrush = new SolidBrush(Color.FromArgb(190, 236, 255));
        private readonly SolidBrush battleBannerBrush = new SolidBrush(Color.FromArgb(255, 227, 145));
        private readonly SolidBrush skillReadyBrush = new SolidBrush(Color.FromArgb(255, 231, 145));
        private readonly SolidBrush skillCooldownBrush = new SolidBrush(Color.FromArgb(205, 218, 235));
        private readonly SolidBrush skillActiveBrush = new SolidBrush(Color.FromArgb(156, 244, 190));
        private bool player1SkillHeld;
        private bool player2SkillHeld;

        public bool ReturnToLobby { get { return returnToLobby; } }

        private bool IsVersusBattle
        {
            get { return launchOptions != null && launchOptions.BattleRule == BattleRuleMode.Versus; }
        }

        private bool IsSurvivalBattle
        {
            get { return launchOptions == null || launchOptions.BattleRule == BattleRuleMode.Survival; }
        }

        List<BaseTank> tanks = new List<BaseTank>();
        List<EnemyTank> enemies = new List<EnemyTank>();
        List<Bullet> bullets = new List<Bullet>();
        List<Box> boxes = new List<Box>();
        List<Animation> animations = new List<Animation>();
        private readonly Dictionary<Point, Box> bulletBlockingBoxes = new Dictionary<Point, Box>();

        Graphics grMain, grMemory;
        Bitmap backgroundMain, backgroundMemory;

        Bitmap heart;
        Bitmap gameover;

        int timeSpawn;

        //写胁懈卸械薪懈械 懈 屑械薪褞 (泻谢邪胁懈褕懈)

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (TryHandleNetworkGlobalKeyDown(e))
            {
                return;
            }

            if (modeMenu == 0)
            {
                if (e.KeyCode == Keys.Up && menu != 0)
                {
                    PicTank.Location = new Point(270, PicTank.Location.Y - 40);
                    menu--;
                }
                if (e.KeyCode == Keys.Down && menu != 5)  // 当前共有 6 个选项（0-5）
                {
                    PicTank.Location = new Point(270, PicTank.Location.Y + 40);
                    menu++;
                }
                if (e.KeyCode == Keys.Enter)
                {
                    if (menu == 0)
                        setting.twoPlayer = false;
                    else if (menu == 1)
                        setting.twoPlayer = true;

                    if (menu == 2)
                        modeMenu = 5;
                    else if (menu == 3)
                        modeMenu = 6;
                    else if (menu == 4)
                        modeMenu = 7;
                    else if (menu == 5)  // 返回大厅
                    {
                        returnToLobby = true;
                        this.Close();
                        return;
                    }
                    else modeMenu = 1;

                    MenuWorking();
                }
            }
            else
            if (modeMenu == 1)
            {
                if (e.KeyCode == Keys.Up && menu != 0)
                {
                    PicTank.Location = new Point(270, PicTank.Location.Y - 40);
                    menu--;
                }
                if (e.KeyCode == Keys.Down && menu != 2)
                {
                    PicTank.Location = new Point(270, PicTank.Location.Y + 40);
                    menu++;
                }
                if (e.KeyCode == Keys.Enter)
                {
                    if (menu == 0)
                        ApplyDifficultySettings(NetworkDifficulty.Easy);
                    else if (menu == 1)
                        ApplyDifficultySettings(NetworkDifficulty.Medium);
                    else if (menu == 2)
                        ApplyDifficultySettings(NetworkDifficulty.Hard);

                    modeMenu = 2;
                    MenuWorking();
                }
            }
            else if (modeMenu == 4)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    EnterName1.Enabled = EnterName1.Visible = buttonName1.Visible = EnterName2.Enabled = EnterName2.Visible = buttonName2.Visible = false;
                    modeMenu = 0;
                    grMemory.DrawImage(BackgroundImage, 0, 0);
                    grMain.DrawImage(BackgroundImage, 0, 0);
                    PictureStat1.Visible = PictureStat2.Visible = false;
                    MenuWorking();
                }
            }
            else if (modeMenu == 5)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    labelStat1.Visible = labelStat2.Visible = labelStat3.Visible = labelStat4.Visible = labelStat5.Visible = labelStat6.Visible = false;
                    labelStat7.Visible = labelStat8.Visible = labelStat9.Visible = labelStat9.Visible = labelStat10.Visible = false;

                    modeMenu = 0;
                    grMemory.DrawImage(BackgroundImage, 0, 0);
                    grMain.DrawImage(BackgroundImage, 0, 0);
                    
                    MenuWorking();
                }
            }
            else if (modeMenu == 6)
            {
                RichRules.Visible = false;

                modeMenu = 0;
                grMemory.DrawImage(BackgroundImage, 0, 0);
                grMain.DrawImage(BackgroundImage, 0, 0);
                    
                MenuWorking();
            }
            else if (modeMenu == 7)
            {
                RichAbout.Visible = false;

                modeMenu = 0;
                grMemory.DrawImage(BackgroundImage, 0, 0);
                grMain.DrawImage(BackgroundImage, 0, 0);

                MenuWorking();
            }
            else
            {
                // 游戏中按 ESC 键退出当前对局
                if (e.KeyCode == Keys.Escape)
                {
                    HandleEscapeExitBattle();
                    return;
                }

                if (firstPlayer)
                {
                    if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W)
                        player1.message.isUp = true;
                    else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S)
                        player1.message.isDown = true;
                    else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A)
                        player1.message.isLeft = true;
                    else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D)
                        player1.message.isRight = true;
                    else if (e.KeyCode == Keys.NumPad0)
                        player1.message.isSpace = true;
                    else if (e.KeyCode == Keys.E)
                        player1.message.isSkill = true;
                }

                if (secondPlayer)
                {
                    if (e.KeyCode == Keys.W)
                        player2.message.isUp = true;
                    else if (e.KeyCode == Keys.S)
                        player2.message.isDown = true;
                    else if (e.KeyCode == Keys.A)
                        player2.message.isLeft = true;
                    else if (e.KeyCode == Keys.D)
                        player2.message.isRight = true;
                    else if (e.KeyCode == Keys.Space)
                        player2.message.isSpace = true;
                    else if (e.KeyCode == Keys.RControlKey)
                        player2.message.isSkill = true;
                }
            }
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (TryHandleNetworkGlobalKeyUp(e))
            {
                return;
            }

            if (modeMenu == 3)
            {
                if (firstPlayer)
                {
                    if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W)
                        player1.message.isUp = false;
                    else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S)
                        player1.message.isDown = false;
                    else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A)
                        player1.message.isLeft = false;
                    else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D)
                        player1.message.isRight = false;

                    else if (e.KeyCode == Keys.NumPad0)
                        player1.message.isSpace = false;
                    else if (e.KeyCode == Keys.E)
                        player1.message.isSkill = false;
                }

                if (secondPlayer)
                {
                    if (e.KeyCode == Keys.W)
                        player2.message.isUp = false;
                    else if (e.KeyCode == Keys.S)
                        player2.message.isDown = false;
                    else if (e.KeyCode == Keys.A)
                        player2.message.isLeft = false;
                    else if (e.KeyCode == Keys.D)
                        player2.message.isRight = false;

                    else if (e.KeyCode == Keys.Space)
                        player2.message.isSpace = false;
                    else if (e.KeyCode == Keys.RControlKey)
                        player2.message.isSkill = false;
                }
            }
        }

        private void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {
            Focus();
            if (e.Button == MouseButtons.Left)
            {
                HandlePrimaryFireInput(true);
            }
        }

        private void MainWindow_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                HandlePrimaryFireInput(false);
            }
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (modeMenu < 3)
            {
                return;
            }

            if (IsNetworkGuest)
            {
                float aimAngle = CalculateAimAngle(ToBattlefieldPoint(e.Location), guestInputState.Up || guestInputState.Down || guestInputState.Left || guestInputState.Right ? player1 : null);
                if (Math.Abs(guestInputState.AimAngleDegrees - aimAngle) > 0.1f)
                {
                    guestInputState.AimAngleDegrees = aimAngle;
                    SendGuestInputState();
                }

                return;
            }

            if (firstPlayer)
            {
                UpdatePlayerAimFromMouse(player1, e.Location);
            }
        }

        private void HandlePrimaryFireInput(bool pressed)
        {
            if (modeMenu < 3)
            {
                return;
            }

            if (IsNetworkGuest)
            {
                if (guestInputState.Fire != pressed)
                {
                    guestInputState.Fire = pressed;
                    SendGuestInputState();
                }
                return;
            }

            if (firstPlayer)
            {
                player1.message.isSpace = pressed;
                if (pressed)
                {
                    TryFirePlayer(player1, 1);
                }
            }
        }

        private void UpdatePlayerAimFromMouse(PlayerTank player, Point mousePosition)
        {
            if (player == null)
            {
                return;
            }

            player.SetRotationDegrees(CalculateAimAngle(ToBattlefieldPoint(mousePosition), player));
        }

        private Rectangle GetBattlefieldViewport()
        {
            int width = Math.Min(ClientSize.Width, BattlefieldWidth);
            int height = Math.Min(ClientSize.Height, BattlefieldHeight);
            return new Rectangle(
                Math.Max(0, (ClientSize.Width - width) / 2),
                Math.Max(0, (ClientSize.Height - height) / 2),
                width,
                height);
        }

        private Point ToBattlefieldPoint(Point clientPoint)
        {
            Rectangle viewport = GetBattlefieldViewport();
            return new Point(clientPoint.X - viewport.X, clientPoint.Y - viewport.Y);
        }

        private static float CalculateAimAngle(Point mousePosition, BaseTank tank)
        {
            if (tank == null)
            {
                return 0f;
            }

            float centerX = tank.x + 25f;
            float centerY = tank.y + 25f;
            float dx = mousePosition.X - centerX;
            float dy = mousePosition.Y - centerY;
            if (Math.Abs(dx) < float.Epsilon && Math.Abs(dy) < float.Epsilon)
            {
                return tank.RotationDegrees;
            }

            return (float)((Math.Atan2(dy, dx) * 180.0 / Math.PI + 90.0 + 360.0) % 360.0);
        }

        private void fontsProjects()
        {
            font = new PrivateFontCollection();
            font.AddFontFile(AppPaths.GetContentPath("font", "FontBit.ttf"));
        }

        private void fonts()
        {
            //袟邪写邪械屑 褕褉懈褎褌 褌械泻褋褌邪, 芯褌芯斜褉邪卸邪械屑芯谐芯 褝谢械屑械薪褌芯屑 褍锌褉邪胁谢械薪懈褟.
            EnterName1.Font = new Font(font.Families[0], 16);
            EnterName2.Font = new Font(font.Families[0], 16);
            buttonName1.Font = new Font(font.Families[0], 16);
            buttonName2.Font = new Font(font.Families[0], 16);

            RichAbout.Font = new Font(font.Families[0], 20);
            RichRules.Font = new Font(font.Families[0], 16);

            labelStat1.Font = new Font(font.Families[0], 18);
            labelStat2.Font = new Font(font.Families[0], 18);
            labelStat3.Font = new Font(font.Families[0], 18);
            labelStat4.Font = new Font(font.Families[0], 18);
            labelStat5.Font = new Font(font.Families[0], 18);
            labelStat6.Font = new Font(font.Families[0], 18);
            labelStat7.Font = new Font(font.Families[0], 18);
            labelStat8.Font = new Font(font.Families[0], 18);
            labelStat9.Font = new Font(font.Families[0], 18);
            labelStat10.Font = new Font(font.Families[0], 18);
        }

        private Bitmap CreateTextBitmap(string text, int width, int height, float fontSize)
        {
            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (Font menuFont = new Font("Microsoft YaHei UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
            using (StringFormat format = new StringFormat())
            {
                graphics.Clear(Color.Black);
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                graphics.DrawString(text, menuFont, Brushes.White, new RectangleF(0, 0, width, height), format);
            }

            return bitmap;
        }

        private void ConfigureChineseLayout()
        {
            const int menuCenterX = 393;
            const int menuWidth = 180;
            const int menuHeight = 35;
            const int menuStartY = 325;
            const int menuStepY = 40;

            PicButton1Player.SetBounds(menuCenterX - menuWidth / 2, menuStartY + menuStepY * 0, menuWidth, menuHeight);
            PicButton2Player.SetBounds(menuCenterX - menuWidth / 2, menuStartY + menuStepY * 1, menuWidth, menuHeight);
            PicButtonStat.SetBounds(menuCenterX - menuWidth / 2, menuStartY + menuStepY * 2, menuWidth, menuHeight);
            PicRules.SetBounds(menuCenterX - menuWidth / 2, menuStartY + menuStepY * 3, menuWidth, menuHeight);
            PicAbout.SetBounds(menuCenterX - menuWidth / 2, menuStartY + menuStepY * 4, menuWidth, menuHeight);
            PicBackToLobby.SetBounds(menuCenterX - menuWidth / 2, menuStartY + menuStepY * 5, menuWidth, menuHeight);  // 返回大厅

            PicButtonEasy.SetBounds(menuCenterX - menuWidth / 2, menuStartY + menuStepY * 0, menuWidth, menuHeight);
            PicButtonHard.SetBounds(menuCenterX - menuWidth / 2, menuStartY + menuStepY * 1, menuWidth, menuHeight);
            PicButtonReal.SetBounds(menuCenterX - menuWidth / 2, menuStartY + menuStepY * 2, menuWidth, menuHeight);

            PicLevel.SetBounds(menuCenterX - 70, 365, 140, 24);
            PicTank.Location = new Point(menuCenterX - menuWidth / 2 - 55, menuStartY);
        }

        private void ApplyChineseUiImages()
        {
            PicButton1Player.Image = CreateTextBitmap("单人模式", PicButton1Player.Width, PicButton1Player.Height, 19);
            PicButton2Player.Image = CreateTextBitmap("双人模式", PicButton2Player.Width, PicButton2Player.Height, 19);
            PicButtonStat.Image = CreateTextBitmap("战绩排行", PicButtonStat.Width, PicButtonStat.Height, 15);
            PicRules.Image = CreateTextBitmap("游戏规则", PicRules.Width, PicRules.Height, 15);
            PicAbout.Image = CreateTextBitmap("项目说明", PicAbout.Width, PicAbout.Height, 15);
            PicBackToLobby.Image = CreateTextBitmap("返回大厅", PicBackToLobby.Width, PicBackToLobby.Height, 15);  // 返回大厅
            PicturePlayer1_.Image = CreateTextBitmap("一号玩家", PicturePlayer1_.Width, PicturePlayer1_.Height, 17);
            picturePlayer2_.Image = CreateTextBitmap("二号玩家", picturePlayer2_.Width, picturePlayer2_.Height, 17);

            PicButtonEasy.Image = CreateTextBitmap("简单模式", PicButtonEasy.Width, PicButtonEasy.Height, 16);
            PicButtonHard.Image = CreateTextBitmap("中等模式", PicButtonHard.Width, PicButtonHard.Height, 16);
            PicButtonReal.Image = CreateTextBitmap("困难模式", PicButtonReal.Width, PicButtonReal.Height, 16);
            PicLevel.Image = CreateTextBitmap("第一关", PicLevel.Width, PicLevel.Height, 16);
        }

        //屑械薪褞 胁胁械褉褏薪械械

        private void MainWindow_Load(object sender, EventArgs e)
        {
            heart = new Bitmap(Peace_of_Tanks.Properties.Resources.heart);
            gameover = CreateTextBitmap("游戏结束", 100, 50, 20);

            // 使用新的背景图（菜单页面使用 xiazai.png，游戏中使用 war.png）
            if (ResourceManager.MenuBackground != null && ResourceManager.MenuBackground.Width > 1)
                BackgroundImage = new Bitmap(ResourceManager.MenuBackground);
            else
                BackgroundImage = Peace_of_Tanks.Properties.Resources.blackSquare;

            Text = "坦克大战";

            RichAbout.Text = "        项目说明        \n\n" +
                             "本项目基于原坦克项目的核心玩法，\n\n" +
                             "将主要代码迁移到当前课程工程，\n\n" +
                             "并整合了中文界面、联机对战和经验系统。\n\n" +
                             "适合作为 C# 网络应用编程课程的\n\n" +
                             "期末展示项目。";

            RichRules.Text = "游戏目标\n\n" +
                             "击毁敌方坦克并尽量获得更高分数。\n\n" +
                             "当我方生命耗尽，或地图中的敌人全部被消灭后，\n\n" +
                             "本局游戏结束。\n\n" +
                             "单人模式：方向键移动，鼠标左键发射炮弹。\n\n" +
                             "本地双人模式：二号玩家使用 W/A/S/D 移动，\n\n" +
                             "空格键发射炮弹。\n\n" +
                             "联机模式：在大厅中创建或加入同一服务器房间，\n\n" +
                             "两名玩家即可开始游戏。";

            //屑械薪褞 0
            ConfigureChineseLayout();
            ApplyChineseUiImages();
            picturePlayer2_.SizeMode = PictureBoxSizeMode.StretchImage;
            PicRules.SizeMode = PictureBoxSizeMode.StretchImage;
            PicAbout.SizeMode = PictureBoxSizeMode.StretchImage;
            PicBackToLobby.SizeMode = PictureBoxSizeMode.StretchImage;  // 返回大厅
            PicButtonStat.SizeMode = PictureBoxSizeMode.StretchImage;
            PicTank.Image = Peace_of_Tanks.Properties.Resources.PicTank;
            PicTank.SizeMode = PictureBoxSizeMode.StretchImage;
            //屑械薪褞 1
            PicButtonEasy.SizeMode = PictureBoxSizeMode.StretchImage;
            PicButtonHard.SizeMode = PictureBoxSizeMode.StretchImage;
            PicButtonReal.SizeMode = PictureBoxSizeMode.StretchImage;
            //屑械薪褞 2
            PicLevel.SizeMode = PictureBoxSizeMode.StretchImage;
            StartFromLaunchOptions();
        }

        private void MenuWorking()
        {
            if (modeMenu == 0)
            {
                PicButton1Player.Visible = PicButton2Player.Visible = PicButtonStat.Visible = PicRules.Visible = PicAbout.Visible = PicBackToLobby.Visible = true;
                PicButton1Player.Show();
                PicButton2Player.Show();
                PicButtonStat.Show();
                PicTank.Show();
                PicTank.Location = new Point(PicButton1Player.Left - 55, PicButton1Player.Top);
                menu = 0;
            }
            else if (modeMenu == 1)
            {
                PicButton1Player.Visible = PicButton2Player.Visible = PicButtonStat.Visible = PicRules.Visible = PicAbout.Visible = PicBackToLobby.Visible = false;
                PicTank.Location = new Point(PicButtonEasy.Left - 55, PicButtonEasy.Top);
                menu = 0;
                PicButtonEasy.Visible = PicButtonHard.Visible = PicButtonReal.Visible = true;
            }
            else if(modeMenu == 2)
            {
                PicButtonEasy.Visible = PicButtonHard.Visible = PicButtonReal.Visible =  PicTank.Visible = PicRules.Visible = PicAbout.Visible = PicBackToLobby.Visible = false;
                PicTank.Location = new Point(PicButton1Player.Left - 55, PicButton1Player.Top);
                menu = 0;
                PicLevel.Visible = true;
                TimerPause.Enabled = true;
            }
            else if(modeMenu == 3)
            {
                RefreshBattleBackground();

                isReadyTo1Level();
                timer1.Interval = GameTiming.TimerIntervalMs;
                timer1.Enabled = true;
            }
            else if (modeMenu == 4)
            {
                // 鍒囧洖鑿滃崟鑳屾櫙
                if (ResourceManager.MenuBackground != null && ResourceManager.MenuBackground.Width > 1)
                    BackgroundImage = new Bitmap(ResourceManager.MenuBackground);
                else
                    BackgroundImage = Peace_of_Tanks.Properties.Resources.blackSquare;

                timer1.Enabled = false;
                picturePlayer2_.Visible = false;
                PicturePlayer1_.Visible = false;
                grMemory.DrawImage(BackgroundImage, 0, 0);
                timer1.Enabled = false;
                StatisticMenu();
                grMain.DrawImage(backgroundMemory, 0, 0);
            }
            else if (modeMenu == 5)
            {
                grMemory.DrawImage(BackgroundImage, 0, 0);

                PicButton1Player.Visible = PicButton2Player.Visible = PicButtonStat.Visible = PicTank.Visible = PicRules.Visible = PicAbout.Visible = PicBackToLobby.Visible = false;

                ShowStatistic();
                grMain.DrawImage(backgroundMemory, 0, 0);
            }
            else if (modeMenu == 6)
            {
                grMemory.DrawImage(BackgroundImage, 0, 0);

                PicButton1Player.Visible = PicButton2Player.Visible = PicButtonStat.Visible = PicTank.Visible = PicRules.Visible = PicAbout.Visible = PicBackToLobby.Visible = false;

                RichRules.Visible = true;
                RichRules.ReadOnly = true;
                grMain.DrawImage(backgroundMemory, 0, 0);
            }
            else if (modeMenu == 7)
            {
                grMemory.DrawImage(BackgroundImage, 0, 0);

                PicButton1Player.Visible = PicButton2Player.Visible = PicButtonStat.Visible = PicTank.Visible = PicRules.Visible = PicAbout.Visible = PicBackToLobby.Visible = false;

                RichAbout.Visible = true;
                RichAbout.ReadOnly = true;
                grMain.DrawImage(backgroundMemory, 0, 0);
            }

            UpdateNetworkUiVisibility();
        }

        private Image GetWarBackgroundImage()
        {
            if (ResourceManager.WarBackground != null && ResourceManager.WarBackground.Width > 1)
            {
                return ResourceManager.WarBackground;
            }

            return Peace_of_Tanks.Properties.Resources.war;
        }

        private void RefreshBattleBackground()
        {
            BackgroundImage = null;

            if (backgroundMain != null)
            {
                backgroundMain.Dispose();
            }

            backgroundMain = CreateCoverBackground(GetWarBackgroundImage(), ClientSize);
            BackgroundImage = backgroundMain;
            BackgroundImageLayout = ImageLayout.Stretch;

            if (backgroundMemory == null || backgroundMemory.Width != backgroundMain.Width || backgroundMemory.Height != backgroundMain.Height)
            {
                if (backgroundMemory != null)
                {
                    backgroundMemory.Dispose();
                }

                backgroundMemory = new Bitmap(backgroundMain.Width, backgroundMain.Height);
                if (grMemory != null)
                {
                    grMemory.Dispose();
                }

                grMemory = Graphics.FromImage(backgroundMemory);
                ConfigureFrameGraphics(grMemory);
            }

            grMemory.DrawImage(backgroundMain, 0, 0);
        }

        private static void ConfigureFrameGraphics(Graphics graphics)
        {
            if (graphics == null)
            {
                return;
            }

            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
        }

        private static Bitmap CreateCoverBackground(Image source, Size targetSize)
        {
            int width = Math.Max(1, targetSize.Width);
            int height = Math.Max(1, targetSize.Height);
            Bitmap result = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.Clear(Color.Black);

                if (source == null || source.Width <= 0 || source.Height <= 0)
                {
                    return result;
                }

                float scale = Math.Max(width / (float)source.Width, height / (float)source.Height);
                int drawWidth = (int)Math.Ceiling(source.Width * scale);
                int drawHeight = (int)Math.Ceiling(source.Height * scale);
                int drawX = (width - drawWidth) / 2;
                int drawY = (height - drawHeight) / 2;

                g.DrawImage(source, new Rectangle(drawX, drawY, drawWidth, drawHeight));
            }

            return result;
        }

        private void TimerPause_Tick(object sender, EventArgs e)
        {
            TimerPause.Enabled = false;
            PicLevel.Visible = false;
            modeMenu = 3;
            MenuWorking();
        }

        private void AddHitEffect(int x, int y)
        {
            AddAnimation(x, y, 0, 3);
        }

        private void AddExplosionEffect(int x, int y)
        {
            AddAnimation(x, y, 1, 5);
        }

        private void AddAnimation(int x, int y, int type, int delay)
        {
            if (animations.Count >= 48)
            {
                return;
            }

            Animation anim = new Animation();
            anim.message.x = x;
            anim.message.y = y;
            anim.message.type = type;
            anim.message.delay = delay;
            DelayEvent += anim.Delay;
            animations.Add(anim);
        }

        private void TrackDamage(int shooter, int damage)
        {
            if (shooter == 1)
            {
                player1Damage += Math.Max(0, damage);
            }
            else if (shooter == 2)
            {
                player2Damage += Math.Max(0, damage);
            }
        }

        private void RebuildBulletBlockingBoxIndex()
        {
            bulletBlockingBoxes.Clear();
            for (int i = 0; i < boxes.Count; i++)
            {
                Box box = boxes[i];
                if (box is Box_Water || box is Box_Green)
                {
                    continue;
                }

                bulletBlockingBoxes[new Point(box.x, box.y)] = box;
            }
        }

        private bool TryHandleBulletBoxCollision(int bulletIndex, Bullet bullet)
        {
            const int tileSize = 25;
            const int bulletSize = 10;

            int left = Math.Max(0, bullet.x / tileSize);
            int right = Math.Max(left, (bullet.x + bulletSize - 1) / tileSize);
            int top = Math.Max(0, bullet.y / tileSize);
            int bottom = Math.Max(top, (bullet.y + bulletSize - 1) / tileSize);

            for (int gridY = top; gridY <= bottom; gridY++)
            {
                for (int gridX = left; gridX <= right; gridX++)
                {
                    Box box;
                    if (bulletBlockingBoxes.TryGetValue(new Point(gridX * tileSize, gridY * tileSize), out box))
                    {
                        if (Collisions.check_touch(this, bullet, box) == 2)
                        {
                            box.hitPoint--;
                            AddHitEffect(box.x, box.y);
                            if (box.hitPoint == 0)
                            {
                                boxes.Remove(box);
                                bulletBlockingBoxes.Remove(new Point(box.x, box.y));
                            }

                            bullets.RemoveAt(bulletIndex);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HandleBulletCollision(int bulletIndex)
        {
            Bullet bullet = bullets[bulletIndex];

            if (Collisions.check_touch(this, bullet))
            {
                AddHitEffect(bullet.x, bullet.y);
                bullets.RemoveAt(bulletIndex);
                return true;
            }

            if (TryHandleBulletBoxCollision(bulletIndex, bullet))
            {
                return true;
            }

            for (int j = 0; j < enemies.Count; j++)
            {
                if (Collisions.check_touch(bullet, enemies[j]))
                {
                    if (bullet.who == 1)
                    {
                        setting.pointsPlayer1 += setting.pointsForEnemy;
                    }
                    else if (bullet.who == 2)
                    {
                        setting.pointsPlayer2 += setting.pointsForEnemy;
                    }

                    if (bullet.who != 0)
                    {
                        AddHitEffect(enemies[j].x, enemies[j].y);
                        TrackDamage(bullet.who, bullet.damage);
                        stageScore += 120;
                        AddDamageText(enemies[j].x + 12, enemies[j].y, bullet.damage, false);
                        enemies[j].hitpoints -= bullet.damage;
                        PlayerTank enemyAttacker = bullet.who == 1 ? player1 : (bullet.who == 2 ? player2 : null);
                        if (enemyAttacker != null)
                        {
                            int lifestealHeal = enemyAttacker.ApplyLifesteal(bullet.damage);
                            if (lifestealHeal > 0)
                            {
                                AddDamageText(enemyAttacker.x + 12, enemyAttacker.y - 18, lifestealHeal, false, true);
                            }
                        }
                        if (enemies[j].hitpoints <= 0)
                        {
                            RegisterEnemyKill(bullet.who);
                        }

                        bullets.RemoveAt(bulletIndex);
                        return true;
                    }
                }
            }

            if (firstPlayer && bullet.who != 1 && Collisions.check_touch(bullet, player1))
            {
                if (bullet.who == 2)
                {
                    setting.pointsPlayer2 += setting.pointsForPlayers;
                }

                AddHitEffect(player1.x, player1.y);
                int player1DamageTaken = ApplyDamage(player1, bullet.damage);
                stageDamageTaken += player1DamageTaken;
                stageScore = Math.Max(0, stageScore - player1DamageTaken * 3);
                TrackDamage(bullet.who, player1DamageTaken);
                AddDamageText(player1.x + 12, player1.y, player1DamageTaken, true);
                player1.hitpoints -= player1DamageTaken;
                if (player1.hitpoints <= 0 && bullet.who == 2)
                {
                    player2Kills++;
                }

                bullets.RemoveAt(bulletIndex);
                return true;
            }

            if (secondPlayer && bullet.who != 2 && Collisions.check_touch(bullet, player2))
            {
                if (bullet.who == 1)
                {
                    setting.pointsPlayer1 += setting.pointsForPlayers;
                }

                AddHitEffect(player2.x, player2.y);
                int player2DamageTaken = ApplyDamage(player2, bullet.damage);
                stageDamageTaken += player2DamageTaken;
                stageScore = Math.Max(0, stageScore - player2DamageTaken * 3);
                TrackDamage(bullet.who, player2DamageTaken);
                AddDamageText(player2.x + 12, player2.y, player2DamageTaken, true);
                player2.hitpoints -= player2DamageTaken;
                if (player2.hitpoints <= 0 && bullet.who == 1)
                {
                    player1Kills++;
                }

                bullets.RemoveAt(bulletIndex);
                return true;
            }

            return false;
        }

        private void DrawKillHud(int player1KillCount, int player2KillCount)
        {
            string selfText = IsNetworkGuest ? "己方击杀 " + player2KillCount : "己方击杀 " + player1KillCount;
            string otherText = IsNetworkGuest ? "对手击杀 " + player1KillCount : "对手击杀 " + player2KillCount;
            grMemory.DrawString(selfText, killHudFont, killHudSelfBrush, 310, 12);
            grMemory.DrawString(otherText, killHudFont, killHudOtherBrush, 470, 12);
        }

        private void DrawPlayerHealthBar(PlayerTank player, bool active, bool rightSide, int lives)
        {
            if (player == null)
            {
                return;
            }

            float width = 220f;
            float height = 18f;
            float x = ClientSize.Width - width - 18f;
            float y = rightSide ? ClientSize.Height - 72f : ClientSize.Height - 36f;
            float ratio = player.maxHitpoints <= 0 ? 0f : Math.Max(0f, Math.Min(1f, player.hitpoints / (float)player.maxHitpoints));
            using (SolidBrush backBrush = new SolidBrush(Color.FromArgb(180, 28, 32, 38)))
            using (SolidBrush fillBrush = new SolidBrush(active ? (rightSide ? Color.FromArgb(90, 170, 255) : Color.FromArgb(70, 220, 120)) : Color.FromArgb(120, 120, 120)))
            using (Pen borderPen = new Pen(Color.FromArgb(220, 235, 235, 235), 2f))
            {
                grMemory.FillRectangle(backBrush, x, y, width, height);
                grMemory.FillRectangle(fillBrush, x + 2, y + 2, Math.Max(0, (width - 4) * ratio), height - 4);
                grMemory.DrawRectangle(borderPen, x, y, width, height);
            }

            string text = (rightSide ? "P2 " : "P1 ") + Math.Max(0, player.hitpoints) + "/" + Math.Max(1, player.maxHitpoints) + "  生命 " + Math.Max(0, lives);
            grMemory.DrawString(text, healthBarFont, Brushes.White, x, y - 18f);
        }

        private void DrawWorldHealthBar(int x, int y, int width, int current, int max, Color fillColor)
        {
            if (max <= 0)
            {
                return;
            }

            float ratio = Math.Max(0f, Math.Min(1f, current / (float)max));
            using (SolidBrush backBrush = new SolidBrush(Color.FromArgb(180, 22, 24, 30)))
            using (SolidBrush fillBrush = new SolidBrush(fillColor))
            using (Pen borderPen = new Pen(Color.FromArgb(210, 240, 240, 240), 1f))
            {
                grMemory.FillRectangle(backBrush, x, y, width, 7);
                grMemory.FillRectangle(fillBrush, x + 1, y + 1, Math.Max(0, (width - 2) * ratio), 5);
                grMemory.DrawRectangle(borderPen, x, y, width, 7);
            }
        }

        private void DrawActorWorldHealthBars()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                int barWidth = enemies[i].IsBossEntity ? 64 : 40;
                int barOffsetX = enemies[i].IsBossEntity ? -7 : 5;
                DrawWorldHealthBar(enemies[i].x + barOffsetX, enemies[i].y - 10, barWidth, enemies[i].hitpoints, enemies[i].maxHitpoints, enemies[i].IsBossEntity ? Color.FromArgb(186, 72, 210) : Color.FromArgb(226, 86, 86));
            }

            if (setting.twoPlayer || networkMode)
            {
                if (firstPlayer)
                {
                    DrawWorldHealthBar(player1.x + 5, player1.y - 10, 40, player1.hitpoints, player1.maxHitpoints, Color.FromArgb(90, 220, 120));
                }

                if (secondPlayer)
                {
                    DrawWorldHealthBar(player2.x + 5, player2.y - 10, 40, player2.hitpoints, player2.maxHitpoints, Color.FromArgb(90, 170, 255));
                }
            }
        }

        private void DrawBossHealthBar()
        {
            if (!survivalBossWaveActive || enemies.Count == 0)
            {
                return;
            }

            EnemyTank boss = enemies.Where(enemy => enemy.IsBossEntity).OrderByDescending(enemy => enemy.maxHitpoints).FirstOrDefault() ?? enemies.OrderByDescending(enemy => enemy.maxHitpoints).FirstOrDefault();
            if (boss == null)
            {
                return;
            }

            float width = Math.Min(520f, ClientSize.Width * 0.48f);
            float x = (ClientSize.Width - width) / 2f;
            float y = 16f;
            float ratio = boss.maxHitpoints <= 0 ? 0f : Math.Max(0f, Math.Min(1f, boss.hitpoints / (float)boss.maxHitpoints));
            using (SolidBrush backBrush = new SolidBrush(Color.FromArgb(190, 25, 18, 22)))
            using (SolidBrush fillBrush = new SolidBrush(Color.FromArgb(186, 72, 210)))
            using (Pen borderPen = new Pen(Color.FromArgb(255, 240, 220, 120), 2f))
            {
                grMemory.FillRectangle(backBrush, x, y, width, 18f);
                grMemory.FillRectangle(fillBrush, x + 2f, y + 2f, Math.Max(0f, (width - 4f) * ratio), 14f);
                grMemory.DrawRectangle(borderPen, x, y, width, 18f);
            }

            string text = "Boss " + Math.Max(0, boss.hitpoints) + "/" + Math.Max(1, boss.maxHitpoints);
            SizeF size = grMemory.MeasureString(text, battleHudFont);
            grMemory.DrawString(text, battleHudFont, Brushes.White, x + (width - size.Width) / 2f, y - 20f);
        }

        private void DrawSkillPanel(float x, float y, float width, float height, string title, string state, string extra, SolidBrush stateBrush, bool active)
        {
            Color fillColor = active ? Color.FromArgb(200, 18, 30, 45) : Color.FromArgb(150, 40, 46, 58);
            Color borderColor = active ? Color.FromArgb(255, 215, 120) : Color.FromArgb(150, 110, 125, 145);
            using (SolidBrush panelBrush = new SolidBrush(fillColor))
            using (Pen borderPen = new Pen(borderColor, 2f))
            {
                grMemory.FillRectangle(panelBrush, x, y, width, height);
                grMemory.DrawRectangle(borderPen, x, y, width, height);
                grMemory.FillEllipse(Brushes.WhiteSmoke, x + 10, y + 12, 34, 34);
                grMemory.DrawEllipse(Pens.Gray, x + 10, y + 12, 34, 34);
                grMemory.DrawString(title, skillHudFont, Brushes.White, x + 54, y + 8);
                grMemory.DrawString(state, skillHudFont, stateBrush, x + 54, y + 28);
                if (!string.IsNullOrEmpty(extra))
                {
                    grMemory.DrawString(extra, skillHudFont, skillActiveBrush, x + 54, y + 48);
                }
            }
        }

        private void DrawSkillHud()
        {
            DrawSkillHud(player1, firstPlayer, false);
            if (setting.twoPlayer || networkMode)
            {
                DrawSkillHud(player2, secondPlayer, true);
            }
        }

        private void DrawSkillHud(PlayerTank player, bool active, bool rightSide)
        {
            if (player == null)
            {
                return;
            }

            string title = TankSkillCatalog.GetDisplayName(player.SkillType);
            string state;
            SolidBrush brush;

            if (!active)
            {
                state = "已淘汰";
                brush = skillCooldownBrush;
            }
            else if (player.IsSkillReady)
            {
                state = "CD 就绪";
                brush = skillReadyBrush;
            }
            else
            {
                state = "CD " + player.GetRemainingSkillCooldownSeconds().ToString("0.0") + "s";
                brush = skillCooldownBrush;
            }

            string extra = rightSide ? "释放键 Ctrl" : "释放键 E";
            if (player.IsOverdriveActive)
            {
                extra += "  |  过载中";
            }
            else if (player.IsLifestealActive)
            {
                extra += "  |  吸血中";
            }
            else if (player.BarrageShotsRemaining > 0)
            {
                extra += "  |  齐射 " + player.BarrageShotsRemaining;
            }

            float panelWidth = Math.Min(300f, Math.Max(220f, ClientSize.Width * 0.22f));
            float panelHeight = 78f;
            float x = rightSide ? ClientSize.Width - panelWidth - 18f : 18f;
            float y = ClientSize.Height - panelHeight - 22f;
            DrawSkillPanel(x, y, panelWidth, panelHeight, title, state, extra, brush, active);
        }

        private string GetBattleStatusText()
        {
            if (IsSurvivalBattle)
            {
                if (survivalVictoryAchieved)
                {
                    return "生存模式：已击破最终Boss";
                }

                if (networkMode && CountActiveNetworkPlayers() == 0)
                {
                    return "协作失败：双方已全部被击破";
                }

                string waveText = survivalBossWaveActive ? "Boss波" : "第 " + Math.Max(1, survivalWave) + " 波";
                return waveText + " | 剩余敌人 " + Math.Max(0, setting.countEnemies);
            }

            int remainToWin = Math.Max(0, 10 - Math.Max(player1Kills, player2Kills));
            return "竞技模式进行中 | 距离胜利还需 " + remainToWin + " 击杀";
        }

        private void DrawSurvivalHud()
        {
            DrawSurvivalHud(survivalWave, setting.countEnemies, survivalBossWaveActive, survivalVictoryAchieved);
        }

        private void DrawSurvivalHud(int wave, int enemiesLeft, bool bossWave, bool victory)
        {
            if (wave <= 0 && !victory)
            {
                return;
            }

            string title = victory ? "生存模式完成" : (bossWave ? "Boss波" : "第 " + wave + " 波");
            string detail;
            if (victory)
            {
                detail = string.IsNullOrEmpty(survivalResultText) ? "准备结算" : survivalResultText;
            }
            else if (!string.IsNullOrEmpty(survivalResultText) && (networkMode || currentGameMode == GameMode.NetworkCoop) && CountActiveNetworkPlayers() == 0)
            {
                detail = survivalResultText;
            }
            else if (bossWave)
            {
                detail = "最终Boss剩余血量阶段 | 敌人 " + Math.Max(0, enemiesLeft);
            }
            else
            {
                detail = "剩余敌人 " + Math.Max(0, enemiesLeft);
            }
            grMemory.DrawString(title, battleHudFont, battleHudBrush, 18, 12);
            grMemory.DrawString(detail, battleHudFont, battleHudBrush, 18, 32);
        }

        private void DrawBattleBanner(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            SizeF textSize = grMemory.MeasureString(text, battleBannerFont);
            float x = (ClientSize.Width - textSize.Width) / 2f;
            grMemory.DrawString(text, battleBannerFont, battleBannerBrush, x, 70f);
        }

        private void ResetSurvivalFlow()
        {
            survivalWave = 0;
            survivalBossWaveActive = false;
            survivalVictoryAchieved = false;
            survivalNextWaveAtUtc = DateTime.MinValue;
            battleBannerText = string.Empty;
            battleBannerTicks = 0;
            emergencySpawnCooldownTicks = 0;
            survivalResultText = string.Empty;
        }

        private void ShowBattleBanner(string text)
        {
            battleBannerText = text ?? string.Empty;
            battleBannerTicks = string.IsNullOrEmpty(battleBannerText) ? 0 : Math.Max(1, BattleBannerDurationMs / GameTiming.TimerIntervalMs);
        }

        private void InitializeBattleFlow()
        {
            ResetSurvivalFlow();
            if (IsSurvivalBattle)
            {
                StartNextSurvivalWave();
            }
            else
            {
                if (ShouldSpawnNeutralEnemiesInVersus())
                {
                    setting.countEnemies = VersusEnemyActiveLimit;
                    activeEnemyLimit = VersusEnemyActiveLimit;
                    setting.speedEnemy = Math.Max(2, setting.speedEnemy);
                    setting.timeSpawn = GameTiming.ScaleLegacyTicks(VersusEnemySpawnDelayLegacyTicks);
                    timeSpawn = setting.timeSpawn;
                    RefillEnemiesIfNeeded();
                }
                else
                {
                    setting.countEnemies = 0;
                    activeEnemyLimit = 0;
                    timeSpawn = 0;
                }
            }
        }

        private void UpdateBattleFlow()
        {
            if (battleBannerTicks > 0)
            {
                battleBannerTicks--;
            }

            if (!IsSurvivalBattle || survivalVictoryAchieved)
            {
                return;
            }

            if (survivalNextWaveAtUtc != DateTime.MinValue)
            {
                if (DateTime.UtcNow >= survivalNextWaveAtUtc)
                {
                    survivalNextWaveAtUtc = DateTime.MinValue;
                    StartNextSurvivalWave();
                }
                return;
            }

            if (survivalWave > 0 && setting.countEnemies <= 0 && enemies.Count == 0)
            {
                if (survivalBossWaveActive || survivalWave >= FinalSurvivalWave)
                {
                    survivalVictoryAchieved = true;
                    survivalResultText = networkMode || currentGameMode == GameMode.NetworkCoop ? "协作完成，已击破最终Boss" : "单人通关，已击破最终Boss";
                    ShowBattleBanner("Boss已被击破");
                    return;
                }

                ApplySurvivalWaveUpgrade(survivalWave);
                survivalNextWaveAtUtc = DateTime.UtcNow.AddMilliseconds(LevelTransitionDelayMs);
            }
        }

        private void StartNextSurvivalWave()
        {
            if (!IsSurvivalBattle || survivalVictoryAchieved)
            {
                return;
            }

            survivalWave++;
            survivalBossWaveActive = survivalWave >= FinalSurvivalWave;
            ConfigureSurvivalWave();
            ShowBattleBanner(survivalBossWaveActive ? "最终Boss来袭" : "第 " + survivalWave + " 波开始");
        }

        private void ConfigureSurvivalWave()
        {
            if (survivalBossWaveActive)
            {
                ConfigureBossWave();
                return;
            }

            setting.countEnemies = GetSurvivalWaveEnemyCount(survivalWave);
            activeEnemyLimit = GetSurvivalWaveActiveLimit(survivalWave);
            setting.timeSpawn = GetSurvivalSpawnDelayTicks(survivalWave);
            timeSpawn = 0;
            SpawnWaveFrontline();
        }

        private int GetSurvivalWaveEnemyCount(int wave)
        {
            if (!networkMode && currentGameMode == GameMode.SinglePlayer)
            {
                if (wave <= 1) return 5;
                if (wave == 2) return 7;
                if (wave == 3) return 10;
            }

            int count = 6 + wave * 3 + setting.difficulty * 2;
            if (currentGameMode == GameMode.NetworkCoop || networkMode)
            {
                count += 3;
            }

            return count;
        }

        private int GetSurvivalWaveActiveLimit(int wave)
        {
            int limit = 2 + wave + setting.difficulty;
            if (currentGameMode == GameMode.NetworkCoop || networkMode)
            {
                limit++;
            }

            return Math.Min(8, Math.Max(2, limit));
        }

        private int GetSurvivalSpawnDelayTicks(int wave)
        {
            int legacyTicks = 180 - wave * 25 - setting.difficulty * 20;
            return GameTiming.ScaleLegacyTicks(Math.Max(35, legacyTicks));
        }

        private void SpawnWaveFrontline()
        {
            int spawnCount = Math.Min(activeEnemyLimit, Math.Max(0, setting.countEnemies));
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnEnemyAt(i);
            }
        }

        private void ConfigureBossWave()
        {
            int health = 180 + setting.difficulty * 50;
            int attack = 32 + setting.difficulty * 8;
            double bossSpeed = 1.2;
            double bossCooldown = 0.65;
            if (currentGameMode == GameMode.NetworkCoop || networkMode)
            {
                health += 60;
                attack += 6;
                bossSpeed = 1.28;
                bossCooldown = 0.58;
            }

            if (setting.difficulty >= 2)
            {
                health += 35;
                attack += 6;
                bossSpeed += 0.08;
                bossCooldown = Math.Max(0.42, bossCooldown - 0.06);
            }

            setting.countEnemies = 1;
            activeEnemyLimit = 1;
            setting.timeSpawn = 0;
            timeSpawn = 0;
            SpawnEnemyAt(0, TankType.Purple, health, attack, bossSpeed, bossCooldown);
            ShowBattleBanner("最终Boss来袭：高血量、高火力、强压制");
        }

        private void ApplySurvivalWaveUpgrade(int clearedWave)
        {
            switch (clearedWave)
            {
                case 1:
                    UpgradeSurvivalPlayers(12, 0, 1.0, 1.0, 1.0);
                    ShowBattleBanner("波次奖励：生命上限 +12，立即恢复 12 生命");
                    break;
                case 2:
                    UpgradeSurvivalPlayers(0, 6, 1.0, 1.08, 1.05);
                    ShowBattleBanner("波次奖励：攻击 +6，移速 +8%，弹速 +5%");
                    break;
                default:
                    UpgradeSurvivalPlayers(8, 5, 0.85, 1.05, 1.08);
                    ShowBattleBanner("波次奖励：生命上限 +8，攻击 +5，冷却 -15%，移速 +5%，弹速 +8%");
                    break;
            }
        }

        private void UpgradeSurvivalPlayers(int healthBonus, int attackBonus, double cooldownMultiplier, double speedMultiplierBonus, double bulletSpeedMultiplierBonus)
        {
            ApplyUpgradeToPlayer(player1, firstPlayer, healthBonus, attackBonus, cooldownMultiplier, speedMultiplierBonus, bulletSpeedMultiplierBonus);
            ApplyUpgradeToPlayer(player2, secondPlayer, healthBonus, attackBonus, cooldownMultiplier, speedMultiplierBonus, bulletSpeedMultiplierBonus);
        }

        private static void ApplyUpgradeToPlayer(PlayerTank player, bool active, int healthBonus, int attackBonus, double cooldownMultiplier, double speedMultiplierBonus, double bulletSpeedMultiplierBonus)
        {
            if (!active || player == null)
            {
                return;
            }

            if (healthBonus > 0)
            {
                player.maxHitpoints += healthBonus;
                player.hitpoints = Math.Min(player.maxHitpoints, player.hitpoints + healthBonus);
            }

            if (attackBonus > 0)
            {
                player.attack += attackBonus;
            }

            if (cooldownMultiplier > 0 && Math.Abs(cooldownMultiplier - 1.0) > 0.001)
            {
                player.cooldownSeconds = Math.Max(0.25, player.cooldownSeconds * cooldownMultiplier);
            }

            if (speedMultiplierBonus > 0 && Math.Abs(speedMultiplierBonus - 1.0) > 0.001)
            {
                player.speedMultiplier *= speedMultiplierBonus;
            }

            if (bulletSpeedMultiplierBonus > 0 && Math.Abs(bulletSpeedMultiplierBonus - 1.0) > 0.001)
            {
                player.bulletSpeedMultiplier *= bulletSpeedMultiplierBonus;
            }
        }

        private static double CalculateKd(int kills, int deaths)
        {
            return deaths <= 0 ? kills : kills / (double)deaths;
        }

        private List<PlayerResultSnapshot> BuildSettlementResults()
        {
            List<PlayerResultSnapshot> results = new List<PlayerResultSnapshot>
            {
                new PlayerResultSnapshot
                {
                    Name = networkMode || currentGameMode != GameMode.SinglePlayer ? "玩家1" : "己方",
                    Damage = player1Damage,
                    Kills = player1Kills,
                    Deaths = player1Deaths,
                    IsActive = firstPlayer
                }
            };

            if (setting.twoPlayer || currentGameMode != GameMode.SinglePlayer || networkMode)
            {
                results.Add(new PlayerResultSnapshot
                {
                    Name = "玩家2",
                    Damage = player2Damage,
                    Kills = player2Kills,
                    Deaths = player2Deaths,
                    IsActive = secondPlayer
                });
            }

            if (IsVersusBattle)
            {
                results = results
                    .OrderByDescending(r => r.Kills)
                    .ThenByDescending(r => r.Damage)
                    .ThenBy(r => r.Deaths)
                    .ToList();
            }
            else
            {
                results = results
                    .OrderByDescending(r => r.IsActive)
                    .ThenByDescending(r => r.Kills)
                    .ThenByDescending(r => r.Damage)
                    .ThenBy(r => r.Deaths)
                    .ToList();
            }

            for (int i = 0; i < results.Count; i++)
            {
                results[i].Rank = i + 1;
            }

            if (IsSurvivalBattle && survivalVictoryAchieved)
            {
                foreach (PlayerResultSnapshot result in results)
                {
                    if (result.IsActive)
                    {
                        result.Name += "（通关）";
                    }
                }
            }
            else if (!networkMode && IsSurvivalBattle && !survivalVictoryAchieved)
            {
                foreach (PlayerResultSnapshot result in results)
                {
                    result.Name += "（失败）";
                }
            }
            else if (networkMode && IsSurvivalBattle && CountActiveNetworkPlayers() == 0)
            {
                foreach (PlayerResultSnapshot result in results)
                {
                    result.Name += "（失败）";
                }
            }

            return results;
        }

        private int CountActiveNetworkPlayers()
        {
            int activePlayers = 0;
            if (firstPlayer)
            {
                activePlayers++;
            }

            if (secondPlayer)
            {
                activePlayers++;
            }

            return activePlayers;
        }

        private void HandleEscapeExitBattle()
        {
            DialogResult result = MessageBox.Show(this, "确定要退出当前对局吗？", "退出对局", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }

            if (networkMode)
            {
                returnToLobby = true;
                Close();
                return;
            }

            ShowSettlementPage(BuildSettlementResults());
        }

        private void ShowSettlementPage(List<PlayerResultSnapshot> results)
        {
            if (settlementShown)
            {
                return;
            }

            lastSettlementExpGain = RewardExpForSettlement(results);
            settlementShown = true;
            timer1.Enabled = false;
            HideOverlayControls();
            ClearSettlementControls();

            Panel shade = new Panel
            {
                BackColor = Color.FromArgb(225, 12, 18, 28),
                Location = new Point(0, 0),
                Size = ClientSize,
                Dock = DockStyle.Fill
            };
            settlementControls.Add(shade);
            Controls.Add(shade);
            shade.BringToFront();

            Panel content = new Panel
            {
                BackColor = Color.Transparent,
                Location = new Point(Math.Max(24, (ClientSize.Width - 760) / 2), Math.Max(24, (ClientSize.Height - 560) / 2)),
                Size = new Size(760, 560),
                Anchor = AnchorStyles.None
            };
            shade.Controls.Add(content);

            Label title = new Label
            {
                Text = "战斗结算",
                Font = new Font("Microsoft YaHei UI", 28, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(260, 20)
            };
            content.Controls.Add(title);

            Label expGain = new Label
            {
                Text = "本局金币 +" + lastSettlementExpGain + "   分数 " + stageScore,
                Font = new Font("Microsoft YaHei UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 224, 146),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(440, 35)
            };
            content.Controls.Add(expGain);

            string[] headers = { "排名", "玩家", "伤害", "击杀", "死亡", "KD" };
            int[] xs = { 62, 148, 285, 402, 512, 612 };
            for (int i = 0; i < headers.Length; i++)
            {
                Label header = CreateSettlementLabel(headers[i], new Point(xs[i], 120), 13, Color.FromArgb(255, 224, 146), FontStyle.Bold);
                content.Controls.Add(header);
            }

            for (int i = 0; i < results.Count; i++)
            {
                PlayerResultSnapshot result = results[i];
                int y = 168 + i * 56;
                Color rowColor = i == 0 ? Color.White : Color.FromArgb(210, 224, 236);
                string displayName = result.Name;
                if (networkMode && ((IsNetworkGuest && result.Name == "玩家2") || (IsNetworkHost && result.Name == "玩家1")))
                {
                    displayName += "（己方）";
                }
                content.Controls.Add(CreateSettlementLabel(result.Rank.ToString(), new Point(xs[0], y), 13, rowColor, FontStyle.Bold));
                content.Controls.Add(CreateSettlementLabel(displayName, new Point(xs[1], y), 13, rowColor, FontStyle.Bold));
                content.Controls.Add(CreateSettlementLabel(result.Damage.ToString(), new Point(xs[2], y), 13, rowColor, FontStyle.Regular));
                content.Controls.Add(CreateSettlementLabel(result.Kills.ToString(), new Point(xs[3], y), 13, rowColor, FontStyle.Regular));
                content.Controls.Add(CreateSettlementLabel(result.Deaths.ToString(), new Point(xs[4], y), 13, rowColor, FontStyle.Regular));
                content.Controls.Add(CreateSettlementLabel(CalculateKd(result.Kills, result.Deaths).ToString("0.00"), new Point(xs[5], y), 13, rowColor, FontStyle.Regular));
            }

            Button back = new Button
            {
                Text = "返回大厅",
                Font = new Font("Microsoft YaHei UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(190, 126, 84, 26),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(305, 470),
                Size = new Size(150, 42)
            };
            back.FlatAppearance.BorderColor = Color.FromArgb(255, 205, 99);
            back.Click += (s, e) =>
            {
                returnToLobby = true;
                Close();
            };
            content.Controls.Add(back);
            back.BringToFront();
        }

        private Label CreateSettlementLabel(string text, Point location, float size, Color color, FontStyle style)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei UI", size, style),
                ForeColor = color,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = location
            };
        }

        private int RewardExpForSettlement(List<PlayerResultSnapshot> results)
        {
            if (expRewarded || results == null)
            {
                return 0;
            }

            PlayerResultSnapshot localResult = results.FirstOrDefault(r => r.Name == GetLocalSettlementName());
            int rank = localResult != null && localResult.Rank > 0 ? localResult.Rank : 1;
            int elapsedSeconds = Math.Max(1, (int)(DateTime.UtcNow - stageStartedAtUtc).TotalSeconds);
            int clearBonus = survivalVictoryAchieved ? Math.Max(0, 2500 - elapsedSeconds * 4) : 0;
            int lowDamageBonus = Math.Max(0, 1200 - stageDamageTaken * 2);
            int rankBonus = Math.Max(0, 600 - (rank - 1) * 180);
            int finalScore = Math.Max(0, stageScore + clearBonus + lowDamageBonus + rankBonus);
            if (survivalVictoryAchieved)
            {
                DataManager.CurrentPlayer.UnlockNextStage(currentStageId);
            }
            DataManager.CurrentPlayer.RecordStageResult(currentStageId, finalScore, survivalVictoryAchieved ? elapsedSeconds : 0);
            DataManager.SaveStageLeaderboard(currentStageId);
            int exp = DataManager.RewardExp(currentGameMode, rank, finalScore, currentStageId, elapsedSeconds, survivalVictoryAchieved);
            DataManager.SaveData();
            stageScore = finalScore;
            expRewarded = true;
            return exp;
        }

        private string GetLocalSettlementName()
        {
            if (networkMode && IsNetworkGuest)
            {
                return "玩家2";
            }

            if (!networkMode && currentGameMode == GameMode.SinglePlayer)
            {
                return "己方";
            }

            return "玩家1";
        }

        private void ClearSettlementControls()
        {
            foreach (Control control in settlementControls)
            {
                Controls.Remove(control);
                control.Dispose();
            }

            settlementControls.Clear();
        }

        private void ResetMatchStats()
        {
            player1Kills = 0;
            player2Kills = 0;
            player1Damage = 0;
            player2Damage = 0;
            player1Deaths = 0;
            player2Deaths = 0;
            settlementShown = false;
            expRewarded = false;
            lastSettlementExpGain = 0;
            player1SkillHeld = false;
            player2SkillHeld = false;
            ResetSurvivalFlow();
            if (player1 != null) player1.ResetSkillState();
            if (player2 != null) player2.ResetSkillState();
            ClearSettlementControls();
        }

        private static int ApplyDamage(PlayerTank player, int damage)
        {
            if (player == null)
            {
                return damage;
            }

            int actualDamage = player.AbsorbDamage(damage);
            return Math.Max(0, actualDamage);
        }

        private void AddDamageText(int x, int y, int value, bool isPlayerTarget, bool isHeal = false)
        {
            if (value <= 0)
            {
                return;
            }

            damageTexts.Add(new DamageText
            {
                X = x + 10,
                Y = y - 8,
                Value = value,
                Ticks = GameTiming.ScaleLegacyTicks(30),
                IsPlayerTarget = isPlayerTarget,
                IsHeal = isHeal
            });
        }

        private void UpdateDamageTexts()
        {
            for (int i = damageTexts.Count - 1; i >= 0; i--)
            {
                damageTexts[i].Y -= 0.8f;
                damageTexts[i].Ticks--;
                if (damageTexts[i].Ticks <= 0)
                {
                    damageTexts.RemoveAt(i);
                }
            }
        }

        private void DrawDamageTexts()
        {
            for (int i = 0; i < damageTexts.Count; i++)
            {
                DamageText damageText = damageTexts[i];
                string text = damageText.IsHeal ? "+" + damageText.Value : "-" + damageText.Value;
                Brush brush = damageText.IsHeal ? Brushes.LimeGreen : (damageText.IsPlayerTarget ? Brushes.OrangeRed : Brushes.Gold);
                grMemory.DrawString(text, damageTextFont, Brushes.Black, damageText.X + 1, damageText.Y + 1);
                grMemory.DrawString(text, damageTextFont, brush, damageText.X, damageText.Y);
            }
        }

        private void ProcessPlayerSkillInputs()
        {
            ProcessPlayerSkillInput(player1, firstPlayer, ref player1SkillHeld);
            ProcessPlayerSkillInput(player2, secondPlayer, ref player2SkillHeld);
        }

        private void ProcessPlayerSkillInput(PlayerTank player, bool active, ref bool skillHeld)
        {
            if (player == null)
            {
                skillHeld = false;
                return;
            }

            player.ExpireTimedEffects();
            if (!active)
            {
                skillHeld = false;
                return;
            }

            if (player.message.isSkill)
            {
                if (!skillHeld)
                {
                    player.TryActivateSkill(this, boxes, tanks);
                }

                skillHeld = true;
            }
            else
            {
                skillHeld = false;
            }
        }

        private static bool IsBlockingBox(Box box)
        {
            return !(box is Box_Green);
        }

        private static bool RectsOverlap(int x1, int y1, int w1, int h1, int x2, int y2, int w2, int h2)
        {
            return x1 < x2 + w2 && x1 + w1 > x2 && y1 < y2 + h2 && y1 + h1 > y2;
        }

        private bool IsTankAreaBlocked(int x, int y, IEnumerable<BaseTank> existingTanks)
        {
            Rectangle bounds = GetBattlefieldViewport();
            bounds.Location = Point.Empty;
            if (x < bounds.Left || y < bounds.Top || x + 50 > bounds.Right || y + 50 > bounds.Bottom)
            {
                return true;
            }

            foreach (Box box in boxes)
            {
                if (IsBlockingBox(box) && RectsOverlap(x, y, 50, 50, box.x, box.y, 25, 25))
                {
                    return true;
                }
            }

            if (existingTanks != null)
            {
                foreach (BaseTank tank in existingTanks)
                {
                    if (RectsOverlap(x, y, 50, 50, tank.x, tank.y, 50, 50))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ClearSpawnArea(int centerX, int centerY)
        {
            int safeX = centerX - 25;
            int safeY = centerY - 25;
            for (int i = boxes.Count - 1; i >= 0; i--)
            {
                if (!(boxes[i] is Box_Base) && IsBlockingBox(boxes[i]) && RectsOverlap(safeX, safeY, 100, 100, boxes[i].x, boxes[i].y, 25, 25))
                {
                    boxes.RemoveAt(i);
                }
            }
        }

        private void ClearAllSpawnAreas()
        {
            for (int i = 0; i < 4; i++)
            {
                ClearSpawnArea(level.masPointsSpawnPlayers[i, 0] + 25, level.masPointsSpawnPlayers[i, 1] + 25);
                ClearSpawnArea(level.masPointsSpawnEnemy[i, 0] + 25, level.masPointsSpawnEnemy[i, 1] + 25);
            }
        }

        private Point FindSafeSpawnPoint(int desiredX, int desiredY, IEnumerable<BaseTank> existingTanks)
        {
            Rectangle bounds = GetBattlefieldViewport();
            bounds.Location = Point.Empty;
            int[] offsets = { 0, 60, -60, 120, -120, 180, -180 };
            foreach (int offsetY in offsets)
            {
                foreach (int offsetX in offsets)
                {
                    int x = Math.Max(bounds.Left + 25, Math.Min(bounds.Right - 75, desiredX + offsetX));
                    int y = Math.Max(bounds.Top + 25, Math.Min(bounds.Bottom - 75, desiredY + offsetY));
                    if (!IsTankAreaBlocked(x, y, existingTanks))
                    {
                        return new Point(x, y);
                    }
                }
            }

            ClearSpawnArea(desiredX + 25, desiredY + 25);
            return new Point(desiredX, desiredY);
        }

        private void RelocateOverlappingTanks()
        {
            List<BaseTank> placed = new List<BaseTank>();
            foreach (BaseTank tank in GetActiveTanks())
            {
                Point safePoint = FindSafeSpawnPoint(tank.x, tank.y, placed);
                tank.x = safePoint.X;
                tank.y = safePoint.Y;
                placed.Add(tank);
            }
        }

        private IEnumerable<BaseTank> GetActiveTanks()
        {
            if (firstPlayer)
            {
                yield return player1;
            }

            if (secondPlayer)
            {
                yield return player2;
            }

            foreach (EnemyTank enemy in enemies)
            {
                yield return enemy;
            }
        }

        private void SpawnEnemyAt(int spawnIndex, TankType? forcedTankType = null, int? healthOverride = null, int? attackOverride = null, double speedMultiplierOverride = 1.0, double? cooldownOverride = null)
        {
            int index = Math.Abs(spawnIndex) % 4;
            int layer = Math.Abs(spawnIndex) / 4;
            int offset = layer * 60;
            TankType enemyType = forcedTankType ?? TankResourceLoader.GetRandomEnemyTankType();
            var images = TankResourceLoader.GetEnemyTankImages(enemyType);
            var config = TankConfigs.GetConfig(enemyType);
            int x = level.masPointsSpawnEnemy[index, 0];
            int y = level.masPointsSpawnEnemy[index, 1];

            if (index == 0 || index == 1)
            {
                y = Math.Max(50, Math.Min(BattlefieldHeight - 100, y + offset));
            }
            else
            {
                x = Math.Max(50, Math.Min(BattlefieldWidth - 100, x + offset));
            }
            Point spawnPoint = FindSafeSpawnPoint(x, y, tanks.Concat(enemies));

            EnemyTank enemy = new EnemyTank(images.Item1, images.Item2,
                ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan,
                spawnPoint.X,
                spawnPoint.Y,
                2,
                healthOverride ?? config.BaseHealth,
                attackOverride ?? config.BaseAttack);
            enemy.TankType = enemyType;
            enemy.message.aggressionLevel = setting != null ? setting.difficulty + 1 : 1;
            int originalAggroRange = 220 + enemy.message.aggressionLevel * 70;
            enemy.message.aggroRange = (!networkMode && currentGameMode == GameMode.SinglePlayer)
                ? Collisions.BattlefieldWidth + Collisions.BattlefieldHeight
                : originalAggroRange;
            enemy.message.attackRange = 150 + enemy.message.aggressionLevel * 45;
            enemy.message.isBoss = healthOverride.HasValue && (healthOverride.Value >= 180 || enemyType == TankType.Purple);
            enemy.IsBossEntity = enemy.message.isBoss;
            enemy.speedMultiplier = enemy.message.isBoss ? Math.Max(1.28, speedMultiplierOverride * 1.28) : Math.Max(0.6, speedMultiplierOverride);
            enemy.bulletSpeedMultiplier = enemy.message.isBoss ? 1.18 : 1.0;
            if (cooldownOverride.HasValue)
            {
                enemy.cooldownSeconds = Math.Max(0.25, cooldownOverride.Value);
                enemy.nextFireAvailableAtUtc = DateTime.UtcNow.AddSeconds(enemy.cooldownSeconds);
            }

            enemies.Add(enemy);
            tanks.Add(enemy);
            MoveEnemy += enemy.Move;
        }

        private bool ShouldKeepEnemyCountStable()
        {
            return ShouldSpawnNeutralEnemiesInVersus();
        }

        private bool ShouldSpawnNeutralEnemiesInVersus()
        {
            return networkMode && IsVersusBattle;
        }

        private bool TryEmergencySpawnEnemy()
        {
            if (setting.countEnemies <= 0 || activeEnemyLimit <= 0 || enemies.Count > 0)
            {
                return false;
            }

            try
            {
                int spawnIndex = random.Next(0, 4);
                SpawnEnemyAt(spawnIndex);
                emergencySpawnCooldownTicks = GameTiming.ScaleLegacyTicks(18);
                return true;
            }
            catch
            {
                timeSpawn = Math.Min(setting.timeSpawn, GameTiming.ScaleLegacyTicks(24));
                emergencySpawnCooldownTicks = GameTiming.ScaleLegacyTicks(18);
                return false;
            }
        }

        private void RegisterEnemyKill(int shooter)
        {
            if (!ShouldKeepEnemyCountStable())
            {
                setting.countEnemies--;
            }

            if (ShouldSpawnNeutralEnemiesInVersus())
            {
                return;
            }

            if (shooter == 1)
            {
                player1Kills++;
            }
            else if (shooter == 2)
            {
                player2Kills++;
            }
        }

        private void RemoveDefeatedEnemies()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].hitpoints > 0)
                {
                    continue;
                }

                EnemyTank defeatedEnemy = enemies[i];
                AddExplosionEffect(defeatedEnemy.x, defeatedEnemy.y);
                MoveEnemy -= defeatedEnemy.Move;
                tanks.Remove(defeatedEnemy);
                enemies.RemoveAt(i);
                i--;
                RefillEnemiesIfNeeded();
            }
        }

        private void RefillEnemiesIfNeeded()
        {
            if (!ShouldKeepEnemyCountStable())
            {
                return;
            }

            while (enemies.Count < activeEnemyLimit)
            {
                int spawnIndex = ShouldSpawnNeutralEnemiesInVersus() ? random.Next(0, Math.Max(4, activeEnemyLimit * 2)) : enemies.Count;
                SpawnEnemyAt(spawnIndex);
            }
        }

        private int GetEnemySpawnIndex(int enemyNumber)
        {
            if (enemyNumber < 4)
            {
                return enemyNumber;
            }

            return enemyNumber % 4;
        }

        private void buttonName1_Click(object sender, EventArgs e)
        {
            setting.stats1.name = EnterName1.Text;
            EnterName1.Enabled = false;
            stats.Add_new_record(setting.stats1);
        }

        private void buttonName2_Click(object sender, EventArgs e)
        {
            setting.stats2.name = EnterName2.Text;
            EnterName2.Enabled = false;
            stats.Add_new_record(setting.stats2);
        }

        //蟹邪泻褉褘褌懈械 褎芯褉屑褘 (褋芯褏褉邪薪械薪懈械)
        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseNetworkConnection();
            using (FileStream fs = new FileStream(AppPaths.GetDataPath("stats.dat"), FileMode.OpenOrCreate))
            {
                formatter.Serialize(fs, stats);
            }
        }

        private void WriteNameLabelStat(int i, string name)
        {
            if (i == 0) labelStat1.Text = name;
            if (i == 1) labelStat2.Text = name;
            if (i == 2) labelStat3.Text = name;
            if (i == 3) labelStat4.Text = name;
            if (i == 4) labelStat5.Text = name;
            if (i == 5) labelStat6.Text = name;
            if (i == 6) labelStat7.Text = name;
            if (i == 7) labelStat8.Text = name;
            if (i == 8) labelStat9.Text = name;
            if (i == 9) labelStat10.Text = name;
        }

        private void ShowStatistic()
        {
            int y = 200, x = 320;

            labelStat1.Visible = labelStat2.Visible = labelStat3.Visible = labelStat4.Visible = labelStat5.Visible = labelStat6.Visible = true;
            labelStat7.Visible = labelStat8.Visible = labelStat9.Visible = labelStat9.Visible = labelStat10.Visible = true;

            for (int i = 0; i < 10; i++)
            {
                x = 320;

                WriteNameLabelStat(i, stats.statistic[i].name);
                DrawNumber(stats.statistic[i].points, x + 30, y, 40);

                y += 40;
            }
        }

        private void StatisticMenu()
        {
            PictureStat1.Visible =  true;
            PictureStat1.Image = CreateTextBitmap("一号玩家", PictureStat1.Width, PictureStat1.Height, 17);

            if (stats.CheckRecord(setting.stats1))
                EnterName1.Enabled = EnterName1.Visible = buttonName1.Visible  = true;
            if (stats.CheckRecord(setting.stats2))
                EnterName2.Enabled = EnterName2.Visible = buttonName2.Visible= true;

            int x = 300;
            DrawNumber(setting.pointsPlayer1, x, 180);

            if (setting.twoPlayer)
            {
                PictureStat2.Visible = true;
                PictureStat2.Image = CreateTextBitmap("二号玩家", PictureStat2.Width, PictureStat2.Height, 17);
                DrawNumber(setting.pointsPlayer2, 300, 240);
            }
        }

        private void isReadyTo1Level()
        {
            if (setting.level == 1)
            {
                gameoverX = 355; gameoverY = 10; pauseGameOver = GameTiming.ScaleLegacyTicks(GameOverSettlementDelayLegacyTicks);

                firstPlayer = true;
                secondPlayer = false;
                tanks.Clear();
                enemies.Clear();
                tanks.Add(player1);

                player1.hitpoints = player1.maxHitpoints;
                player1.message.pauseSpace = 0;
                player2.message.pauseSpace = 0;
                player1.nextFireAvailableAtUtc = DateTime.MinValue;
                player2.nextFireAvailableAtUtc = DateTime.MinValue;

                if (launchOptions != null && launchOptions.MapConfig != null)
                {
                    level.SetDefaultSpawnPoints();
                    BattleMapFactory.ApplyMap(launchOptions.MapConfig, boxes, launchOptions.BattleRule);
                }
                else
                {
                    level.Level1(this, boxes);
                }
                ClearAllSpawnAreas();

                timeSpawn = setting.timeSpawn;

                Point player1Spawn = FindSafeSpawnPoint(level.masPointsSpawnPlayers[0, 0], level.masPointsSpawnPlayers[0, 1], null);
                player1.x = player1Spawn.X;
                player1.y = player1Spawn.Y;

                PicturePlayer1_.Visible = true;

                if (setting.twoPlayer)
                {
                    player2.hitpoints = player2.maxHitpoints;
                    Point player2Spawn = FindSafeSpawnPoint(level.masPointsSpawnPlayers[2, 0], level.masPointsSpawnPlayers[2, 1], tanks);
                    player2.x = player2Spawn.X;
                    player2.y = player2Spawn.Y;
                    tanks.Add(player2);
                    picturePlayer2_.Visible = true;
                    secondPlayer = true;
                }

                InitializeBattleFlow();

                RelocateOverlappingTanks();
            }
        }
        //褌邪泄屑械褉 (胁褋械 谢芯谐懈泻邪)

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (IsNetworkGuest)
            {
                RenderRemoteSnapshotFrame();
                return;
            }

            ExecuteGameStep();
        }

        private void UpdateEnemyPlayerTargets()
        {
            if (firstPlayer)
            {
                EnemyEventArgs.player1X = player1.x;
                EnemyEventArgs.player1Y = player1.y;
                EnemyEventArgs.players = 1;
            }
            if (secondPlayer)
            {
                EnemyEventArgs.player2X = player2.x;
                EnemyEventArgs.player2Y = player2.y;
                EnemyEventArgs.players = 2;
            }
            if (firstPlayer && secondPlayer)
            {
                EnemyEventArgs.player1X = player1.x;
                EnemyEventArgs.player1Y = player1.y;
                EnemyEventArgs.player2X = player2.x;
                EnemyEventArgs.player2Y = player2.y;
                EnemyEventArgs.players = 3;
            }
            else if (!firstPlayer && !secondPlayer)
            {
                EnemyEventArgs.players = 0;
            }
        }

        private void ExecuteGameStep()
        {
            grMemory.DrawImage(backgroundMain, 0, 0);
            RebuildBulletBlockingBoxIndex();
            if (IsNetworkHost)
            {
                ApplyRemoteInputToSecondPlayer();
            }

            ProcessPlayerSkillInputs();
            if (firstPlayer) player1.Move(this, null, player1.message, boxes, tanks, PlayerMovementStepsPerFrame);
            if (secondPlayer) player2.Move(this, null, player2.message, boxes, tanks, PlayerMovementStepsPerFrame);
            UpdateEnemyPlayerTargets();
            if (MoveEnemy != null)
            {
                for (int i = 0; i < setting.speedEnemy; i++)
                {
                    MoveEnemy(this, boxes, bullets, tanks);
                }
            }
            UpdateBattleFlow();
            UpdateDamageTexts();
            if (emergencySpawnCooldownTicks > 0)
            {
                emergencySpawnCooldownTicks--;
            }

            ///////////////////////////////////////////////芯褌褉懈褋芯胁泻邪 薪懈卸薪械泄 锌邪薪械谢褜

            DrawPlayerHealthBar(player1, firstPlayer, false, setting.lifesPlayer1);
            if (setting.twoPlayer || networkMode)
            {
                DrawPlayerHealthBar(player2, secondPlayer, true, setting.lifesPlayer2);
            }

            int hudNumberX = ClientSize.Width - 360;
            int hudNumberY = ClientSize.Height - 36;
            DrawNumber(setting.pointsPlayer1, hudNumberX, hudNumberY);

            if(setting.twoPlayer)
            {
                DrawNumber(setting.pointsPlayer2, hudNumberX, ClientSize.Height - 72);
            }

            /////////////////////////////////////////////////////褋锌邪胁薪 斜芯褌芯胁
            if (setting.twoPlayer || networkMode)
            {
                DrawKillHud(player1Kills, player2Kills);
            }

            if (IsSurvivalBattle)
            {
                DrawSurvivalHud();
            }

            if (battleBannerTicks > 0 && !string.IsNullOrEmpty(battleBannerText))
            {
                DrawBattleBanner(battleBannerText);
            }

            DrawBossHealthBar();
            DrawSkillHud();

            if (!ShouldKeepEnemyCountStable() && setting.countEnemies > 0 && enemies.Count < activeEnemyLimit)
            {
                if (enemies.Count == 0 && emergencySpawnCooldownTicks <= 0)
                {
                    if (TryEmergencySpawnEnemy())
                    {
                        timeSpawn = Math.Min(setting.timeSpawn, GameTiming.ScaleLegacyTicks(20));
                    }
                }
                else if (timeSpawn <= 0)
                {
                    bool[] mas = new bool[4] { false, false, false, false };
                    int num = random.Next(0, 4);

                    for (int i = 0; i < tanks.Count; i++)
                        for (int j = 0; j < 3; j++)
                            if (Collisions.check_touch(level.masPointsSpawnEnemy[j, 0], level.masPointsSpawnEnemy[j, 1], tanks[i].x, tanks[i].y))
                                mas[j] = true;

                    num = random.Next(0, 4);
                    if (mas[0] == true && mas[1] == true && mas[2] == true)
                    {
                        timeSpawn = Math.Min(setting.timeSpawn, GameTiming.ScaleLegacyTicks(20));
                    }
                    else
                    {
                        while (mas[num] == true)
                        {
                            num = random.Next(0, 4);
                        }

                        try
                        {
                            SpawnEnemyAt(num);
                            timeSpawn = setting.timeSpawn;
                        }
                        catch
                        {
                            timeSpawn = Math.Min(setting.timeSpawn, GameTiming.ScaleLegacyTicks(24));
                        }
                    }
                }
                else
                {
                    timeSpawn--;
                }
            }
            else if (timeSpawn > 0)
            {
                timeSpawn--;
            }

            //////////////////////////////////////////////锌褉芯胁械褉泻邪 褋褌芯谢泻薪芯胁械薪懈泄
            for (int i = 0; i < bullets.Count; i++) //锌褉芯胁械褉泻邪 褋褌芯谢泻薪芯胁械薪懈褟 锌褍谢褜 懈 褟褖懈泻芯胁
            {
                bool bulletDeath = HandleBulletCollision(i);
                if (!bulletDeath)
                {
                    int stepCount = bullets[i].MoveStepCount;
                    for (int step = 0; step < stepCount; step++)
                    {
                        bullets[i].MoveStep(step);
                        if (HandleBulletCollision(i))
                        {
                            bulletDeath = true;
                            break;
                        }
                    }
                }

                if (bulletDeath)
                {
                    i--;
                }
            }

            RemoveDefeatedEnemies();

            //蟹邪写械褉卸泻邪 芯褌褉懈褋芯胁泻懈 邪薪懈屑邪褑懈懈  (写械谢械谐邪褌)
            if(DelayEvent != null)
                DelayEvent(); 

            Rectangle battlefieldViewport = GetBattlefieldViewport();
            GraphicsState worldState = grMemory.Save();
            grMemory.TranslateTransform(battlefieldViewport.X, battlefieldViewport.Y);

            /////////////////////////////////////// 芯褌褉懈褋芯胁泻邪

            for (int i = 0; i < boxes.Count; i++) //芯褌褉懈褋芯胁泻邪 褟褖懈泻芯胁
            {
                if (!(boxes[i] is Box_Green))
                    boxes[i].Draw(grMemory);
            }
            for (int i = 0; i < bullets.Count; i++) //芯褌褉懈褋芯胁泻邪 锌褍谢褜
            {
                bullets[i].Draw(grMemory);
            }
            for (int i = 0; i < enemies.Count; i++) //芯褌褉懈褋芯胁泻邪 斜芯褌芯胁
            {
                int enemySize = enemies[i].IsBossEntity ? 74 : 50;
                if (enemies[i].IsBossEntity)
                {
                    BaseTank.DrawGlow(grMemory, enemies[i].x, enemies[i].y, enemySize, enemySize, Color.FromArgb(180, 120, 60, 220));
                    DrawRotatedImage(grMemory, enemies[i].CurrentPicture, enemies[i].x, enemies[i].y, enemySize, enemySize, enemies[i].RotationDegrees);
                }
                else
                {
                    enemies[i].Draw(grMemory, Color.Empty);
                }
            }

            if (firstPlayer) player1.Draw(grMemory, Color.FromArgb(80, 235, 120));                    // 芯褌褉懈褋芯胁泻邪 褌邪薪泻芯胁 懈谐褉芯泻芯胁
            if (secondPlayer) player2.Draw(grMemory, networkMode ? Color.FromArgb(255, 82, 82) : Color.Empty);
            DrawActorWorldHealthBars();

            for (int i = 0; i < boxes.Count; i++) // 芯褌褉懈褋芯胁泻邪 谢械褋薪褘褏 褟褖懈泻芯胁, 锌芯蟹卸械 芯褌褉懈褋芯胁泻懈 褌邪薪泻芯胁, 褔褌芯斜褘 褌邪薪泻 屑芯谐 锌褉褟褌邪褌褜褋褟 胁 谢械褋褍
            {
                if (boxes[i] is Box_Green)
                    boxes[i].Draw(grMemory);
            }

            for(int i = 0; i < animations.Count; i++) // 芯褌褉懈褋芯胁泻邪 邪薪懈屑邪褑懈懈
                animations[i].Draw(grMemory);
            DrawDamageTexts();
            grMemory.Restore(worldState);

            ////////////////////////////////////////////////////////////////////////锌褉芯胁械褉泻邪 泻芯薪褑邪 懈谐褉褘

            bool versusFinished = IsVersusBattle && (player1Kills >= 10 || player2Kills >= 10);
            bool survivalCleared = IsSurvivalBattle && survivalVictoryAchieved;
            bool networkVersusFinished = networkMode && IsVersusBattle && CountActiveNetworkPlayers() <= 1;
            bool networkCoopFailed = networkMode && IsSurvivalBattle && CountActiveNetworkPlayers() == 0;

            bool lifeLimitFinished = !networkMode && ((setting.lifesPlayer1 == 0 && !setting.twoPlayer) || ((setting.lifesPlayer2 == 0  && setting.lifesPlayer1 == 0) && setting.twoPlayer));
            if ((networkCoopFailed || (lifeLimitFinished && IsSurvivalBattle)) && string.IsNullOrEmpty(survivalResultText))
            {
                survivalResultText = networkMode ? "协作失败：双方已全部被击破" : "挑战失败：生命耗尽";
                ShowBattleBanner(networkMode ? "协作失败" : "挑战失败");
            }

            if (lifeLimitFinished
                || versusFinished || networkVersusFinished || networkCoopFailed
                || survivalCleared)
            {
                if (!survivalCleared)
                {
                    gameoverY = Math.Min(140, gameoverY + 24);
                    grMemory.DrawImage(gameover, battlefieldViewport.X + gameoverX, battlefieldViewport.Y + gameoverY);
                }

                pauseGameOver -= 1;

                if (pauseGameOver <= 0)   // 保存统计信息和奖励经验
                {
                    stats bufStat = new stats();
                    bufStat.difficulty = setting.difficulty;
                    bufStat.hearts = setting.lifesPlayer1;
                    if (setting.twoPlayer)
                        bufStat.mode = 2;
                    else bufStat.mode = 1;
                    bufStat.points = setting.pointsPlayer1;

                    setting.stats1 = bufStat;

                    bufStat = new stats();
                    bufStat.hearts = setting.lifesPlayer2;
                    bufStat.points = setting.pointsPlayer2;

                    setting.stats2 = bufStat;

                    for (int i = 0; i < enemies.Count; i++)
                        MoveEnemy -= enemies[i].Move;
                    enemies.Clear();
                    boxes.Clear();
                    tanks.Clear();
                    bullets.Clear();

                    PicturePlayer1_.Visible = picturePlayer2_.Visible = false;

                    List<PlayerResultSnapshot> settlementResults = BuildSettlementResults();
                    if (IsNetworkHost)
                    {
                        BroadcastSnapshot(true, settlementResults);
                    }

                    ShowSettlementPage(settlementResults);

                    return;
                }
            }

            ////////////////////////////////////////////////////////////// 谢芯谐懈泻邪

            ProcessPlayerFireInputs();

            if (player1.message.pauseSpace > 0) player1.message.pauseSpace--;                    // 锌邪褍蟹邪 屑械卸写褍 胁褘褋褌褉械谢邪屑懈 褍 懈谐褉芯泻芯胁
            if (secondPlayer && player2.message.pauseSpace > 0) player2.message.pauseSpace--;     //

            {
                if (firstPlayer)                                               //褋芯芯斜褖械薪懈械 泻褌芯 械褋褌褜 胁 懈谐褉械 (褌芯谢褜泻 1 懈谢懈 2 懈谐褉芯泻, 芯斜邪, 懈谢懈 薪懈泻芯谐芯)
                {
                    EnemyEventArgs.player1X = player1.x;
                    EnemyEventArgs.player1Y = player1.y;
                    EnemyEventArgs.players = 1;
                }
                if (secondPlayer)
                {
                    EnemyEventArgs.player2X = player2.x;
                    EnemyEventArgs.player2Y = player2.y;
                    EnemyEventArgs.players = 2;
                }
                if (firstPlayer && secondPlayer)
                {
                    EnemyEventArgs.player1X = player1.x;
                    EnemyEventArgs.player1Y = player1.y;
                    EnemyEventArgs.player2X = player2.x;
                    EnemyEventArgs.player2Y = player2.y;
                    EnemyEventArgs.players = 3;
                }
                else if (firstPlayer == false && secondPlayer == false)
                    EnemyEventArgs.players = 0;
            }

            RemoveDefeatedEnemies();

            if (secondPlayer && player2.hitpoints <= 0)  // 褍薪懈褔褌芯卸械薪懈械 胁褌芯褉芯谐芯 懈谐褉芯泻邪
            {
                if (!networkMode)
                {
                    setting.lifesPlayer2--;
                }
                player2Deaths++;
                AddExplosionEffect(player2.x, player2.y);

                int num = random.Next(0, 4);
                if (networkMode || setting.lifesPlayer2 > 0)
                {
                    Point respawn = FindSafeSpawnPoint(level.masPointsSpawnPlayers[num, 0], level.masPointsSpawnPlayers[num, 1], tanks.Where(t => t != player2));
                    player2.x = respawn.X;
                    player2.y = respawn.Y;
                    player2.hitpoints = player2.maxHitpoints;
                }
                else
                {
                    player2.x = player2.y = -50;
                    secondPlayer = false;
                    tanks.Remove(player2);
                }
                    
            }
            if (firstPlayer && player1.hitpoints <= 0) // 褍薪懈褔褌芯卸械薪懈械 锌械褉胁芯谐芯 懈谐褉芯泻邪
            {
                if (!networkMode)
                {
                    setting.lifesPlayer1--;
                }
                player1Deaths++;
                AddExplosionEffect(player1.x, player1.y);

                int num = random.Next(0, 4);
                if (networkMode || setting.lifesPlayer1 > 0)
                {
                    Point respawn = FindSafeSpawnPoint(level.masPointsSpawnPlayers[num, 0], level.masPointsSpawnPlayers[num, 1], tanks.Where(t => t != player1));
                    player1.x = respawn.X;
                    player1.y = respawn.Y;
                    player1.hitpoints = player1.maxHitpoints;
                }
                else
                {
                    player1.x = player1.y = -50;
                    firstPlayer = false;
                    tanks.Remove(player1);
                }
            }

            for(int i = 0; i < animations.Count; i++)   // 褍薪懈褔褌芯卸械薪懈械 邪薪懈屑邪褑懈懈, 械褋谢懈 芯薪邪 蟹邪胁械褉褕懈谢邪褋褜 (芯褌锌懈褋泻邪, 写械谢械谐邪褌)
            {
                if (animations[i].message.delay <= 0)
                {
                    DelayEvent -= animations[i].Delay;
                    animations.RemoveAt(i);
                }
            }

            if (IsNetworkHost)
            {
                BroadcastSnapshot(false);
            }

            grMain.DrawImage(backgroundMemory, 0, 0);
            grMemory.DrawImage(backgroundMain, 0, 0);
        }

        //薪邪褔邪谢褜薪邪褟 懈薪懈褑懈邪谢懈蟹邪褑懈褟
        public MainWindow() : this(GameMode.SinglePlayer, TankType.Green)
        {
        }

        public MainWindow(GameMode mode, TankType tankType) : this(new GameLaunchOptions { Mode = mode, TankType = tankType, StartImmediately = false })
        {
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (timerResolutionRaised)
            {
                timeEndPeriod(1);
                timerResolutionRaised = false;
            }

            base.OnFormClosed(e);
        }

        public MainWindow(GameLaunchOptions options)
        {
            InitializeComponent();
            TimerPause.Interval = LevelTransitionDelayMs;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            ResizeRedraw = true;
            KeyPreview = true;
            UpdateStyles();
            Resize += MainWindow_Resize;
            timerResolutionRaised = timeBeginPeriod(1) == 0;

            launchOptions = options ?? new GameLaunchOptions();
            currentGameMode = launchOptions.Mode;
            currentStageId = Math.Max(1, launchOptions.StageId);
            stageScore = 0;
            stageDamageTaken = 0;
            stageStartedAtUtc = DateTime.UtcNow;
            selectedTankType = launchOptions.TankType;
            if (launchOptions.MapConfig == null)
            {
                launchOptions.MapConfig = BattleMapFactory.CreateSystemMap(0);
            }

            backgroundMain = CreateCoverBackground(GetWarBackgroundImage(), ClientSize);
            backgroundMemory = new Bitmap(backgroundMain.Width, backgroundMain.Height);

            grMain = CreateGraphics();
            grMemory = Graphics.FromImage(backgroundMemory);
            ConfigureFrameGraphics(grMemory);
            grMemory.DrawImage(backgroundMain, 0, 0);

            // 使用选中的坦克类型创建玩家坦克
            var playerData = DataManager.CurrentPlayer;
            var playerTankData = playerData.Tanks[selectedTankType];
            var config = TankConfigs.GetConfig(selectedTankType);

            int attack = config.GetAttackAtLevel(playerTankData.AttackLevel);
            int health = config.GetHealthAtLevel(playerTankData.HealthLevel);

            // 加载对应颜色的坦克图片
            var (tankImg1, tankImg2) = TankResourceLoader.GetPlayerTankImages(selectedTankType);

            player1 = new PlayerTank(tankImg1, tankImg2, ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan, -100, -100, 2, health, attack);
            player1.UseMouseAim = true;
            player1.speedMultiplier = TankConfig.GetSpeedMultiplierAtLevel(playerTankData.SpeedLevel);
            player1.bulletSpeedMultiplier = TankConfig.GetBulletSpeedMultiplierAtLevel(playerTankData.BulletSpeedLevel);
            player1.cooldownSeconds = TankConfig.GetCooldownSecondsAtLevel(playerTankData.CooldownLevel);
            player1.ConfigureSkill(selectedTankType);
            player2 = new PlayerTank(new Bitmap(tankImg1), new Bitmap(tankImg2), ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan, -50, -50, 2, config.BaseHealth, config.BaseAttack);
            player2.UseMouseAim = false;
            player2.ConfigureSkill(selectedTankType);

            string statsPath = AppPaths.GetDataPath("stats.dat");
            if(File.Exists(statsPath))
            {
                using (FileStream fs = new FileStream(statsPath, FileMode.OpenOrCreate))
                {
                    stats = (Statistics)formatter.Deserialize(fs);
                }
            }

            timer1.Interval = GameTiming.TimerIntervalMs;

            fontsProjects();
            fonts();
            InitializeNetworkUi();
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized || backgroundMain == null || grMemory == null)
            {
                return;
            }

            if (modeMenu == 3)
            {
                RefreshBattleBackground();
                Invalidate();
            }
            else
            {
                ConfigureChineseLayout();
                Invalidate();
            }
        }

        private void StartFromLaunchOptions()
        {
            if (launchOptions == null || !launchOptions.StartImmediately)
            {
                return;
            }

            HideOverlayControls();
            if (launchOptions.UseNetwork && launchOptions.NetworkConfig != null)
            {
                BeginNetworkConnect(launchOptions.NetworkConfig);
                return;
            }

            setting.twoPlayer = currentGameMode != GameMode.SinglePlayer;
            ApplyDifficultySettings(launchOptions.Difficulty);
            StartBattleDirectly();
        }

        private void StartBattleDirectly()
        {
            ResetMatchStats();
            HideOverlayControls();
            modeMenu = 3;
            RefreshBattleBackground();
            isReadyTo1Level();
            timer1.Interval = GameTiming.TimerIntervalMs;
            timer1.Enabled = true;
        }
    }
}
