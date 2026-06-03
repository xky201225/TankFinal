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

namespace Peace_of_Tanks
{
    public partial class MainWindow : Form
    {
        BinaryFormatter formatter = new BinaryFormatter();

        public static event TankEvent MovePlayer1, MovePlayer2;  // 写械谢械谐邪褌 写胁懈卸械薪懈械 懈谐褉芯泻芯胁
        public static event AnimateEvent DelayEvent;             // 写械谢械谐邪褌 蟹邪写械褉卸泻邪 邪薪懈屑邪褑懈懈 (芯褌褉懈褋芯胁泻邪 褋 锌邪褍蟹邪屑懈 屑械卸写褍 褉懈褋褍薪泻邪屑懈)
        public static event EnemyEvent MoveEnemy;                // 写械谢械谐邪褌 写胁懈卸械薪懈械 胁褉邪谐芯胁

        int menu = 0;  // 胁褘斜褉邪薪薪褘泄 锌褍薪泻褌 屑械薪褞
        int modeMenu = 0; // 褌械泻褍褖械械 屑械薪褞
        bool secondPlayer = false, firstPlayer = true; // 芯褌芯斜褉邪卸械薪懈械 锌械褉胁芯谐芯 懈 胁褌芯褉芯谐芯 懈谐褉芯泻邪
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
        private bool baseDestroyed;
        private int destroyedBaseOwner;
        private int activeEnemyLimit = 3;
        private int player1Damage;
        private int player2Damage;
        private int player1Deaths;
        private int player2Deaths;
        private bool settlementShown;
        private bool expRewarded;
        private int lastSettlementExpGain;
        private readonly List<Control> settlementControls = new List<Control>();

        public bool ReturnToLobby { get { return returnToLobby; } }

        List<BaseTank> tanks = new List<BaseTank>();
        List<EnemyTank> enemies = new List<EnemyTank>();
        List<Bullet> bullets = new List<Bullet>();
        List<Box> boxes = new List<Box>();
        List<Animation> animations = new List<Animation>();

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
            }
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

            Text = "和平坦克大战";

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
                grMain.DrawImage(BackgroundImage, 0, 0);
                timer1.Enabled = false;
                StatisticMenu();

                grMain.DrawImage(backgroundMemory, 0, 0);
            }
            else if (modeMenu == 5)
            {
                grMemory.DrawImage(BackgroundImage, 0, 0);
                grMain.DrawImage(BackgroundImage, 0, 0);

                PicButton1Player.Visible = PicButton2Player.Visible = PicButtonStat.Visible = PicTank.Visible = PicRules.Visible = PicAbout.Visible = PicBackToLobby.Visible = false;

                ShowStatistic();

                grMain.DrawImage(backgroundMemory, 0, 0);
            }
            else if (modeMenu == 6)
            {
                grMemory.DrawImage(BackgroundImage, 0, 0);
                grMain.DrawImage(BackgroundImage, 0, 0);

                PicButton1Player.Visible = PicButton2Player.Visible = PicButtonStat.Visible = PicTank.Visible = PicRules.Visible = PicAbout.Visible = PicBackToLobby.Visible = false;

                RichRules.Visible = true;
                RichRules.ReadOnly = true;

                grMain.DrawImage(backgroundMemory, 0, 0);
            }
            else if (modeMenu == 7)
            {
                grMemory.DrawImage(BackgroundImage, 0, 0);
                grMain.DrawImage(BackgroundImage, 0, 0);

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
            }

            grMemory.DrawImage(backgroundMain, 0, 0);
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

        private bool HandleBulletCollision(int bulletIndex)
        {
            Bullet bullet = bullets[bulletIndex];

            if (Collisions.check_touch(this, bullet))
            {
                AddHitEffect(bullet.x, bullet.y);
                bullets.RemoveAt(bulletIndex);
                return true;
            }

            for (int j = 0; j < boxes.Count; j++)
            {
                if (Collisions.check_touch(this, bullet, boxes[j]) == 2)
                {
                    boxes[j].hitPoint--;
                    AddHitEffect(boxes[j].x, boxes[j].y);
                    if (boxes[j].hitPoint == 0)
                    {
                        MarkBaseDestroyed(boxes[j]);

                        boxes.RemoveAt(j);
                    }

                    bullets.RemoveAt(bulletIndex);
                    return true;
                }
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
                        enemies[j].hitpoints -= bullet.damage;
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
                TrackDamage(bullet.who, bullet.damage);
                player1.hitpoints -= bullet.damage;
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
                TrackDamage(bullet.who, bullet.damage);
                player2.hitpoints -= bullet.damage;
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
            using (Font font = new Font("Microsoft YaHei UI", 11, FontStyle.Bold))
            using (SolidBrush selfBrush = new SolidBrush(Color.FromArgb(255, 231, 145)))
            using (SolidBrush otherBrush = new SolidBrush(Color.FromArgb(210, 230, 245)))
            {
                string selfText = IsNetworkGuest ? "己方击杀 " + player2KillCount : "己方击杀 " + player1KillCount;
                string otherText = IsNetworkGuest ? "对手击杀 " + player1KillCount : "对手击杀 " + player2KillCount;
                grMemory.DrawString(selfText, font, selfBrush, 310, 12);
                grMemory.DrawString(otherText, font, otherBrush, 470, 12);
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

            if (setting.twoPlayer || currentGameMode == GameMode.TwoPlayer || currentGameMode == GameMode.ThreePlayer || networkMode)
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

            if (launchOptions != null && launchOptions.BattleRule == BattleRuleMode.FreeBattle)
            {
                results = results
                    .OrderByDescending(r => r.Kills)
                    .ThenByDescending(r => r.Damage)
                    .ThenBy(r => r.Deaths)
                    .ToList();
            }
            else if (launchOptions != null && launchOptions.BattleRule == BattleRuleMode.BaseDefense && destroyedBaseOwner != 0)
            {
                results = results
                    .OrderByDescending(r => GetBaseDefenseWinnerSortValue(r))
                    .ThenByDescending(r => r.Kills)
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

        private int GetBaseDefenseWinnerSortValue(PlayerResultSnapshot result)
        {
            string playerName = destroyedBaseOwner == 1 ? "玩家2" : "玩家1";
            if (!networkMode && currentGameMode == GameMode.SinglePlayer && destroyedBaseOwner == 2)
            {
                playerName = "己方";
            }

            return result.Name == playerName ? 1 : 0;
        }

        private void MarkBaseDestroyed(Box box)
        {
            if (!(box is Box_Base))
            {
                return;
            }

            baseDestroyed = true;
            if (box is Box_PlayerBase)
            {
                destroyedBaseOwner = 1;
            }
            else if (box is Box_EnemyBase)
            {
                destroyedBaseOwner = 2;
            }
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
                Size = ClientSize
            };
            settlementControls.Add(shade);
            Controls.Add(shade);
            shade.BringToFront();

            Label title = new Label
            {
                Text = "战斗结算",
                Font = new Font("Microsoft YaHei UI", 28, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(310, 105)
            };
            shade.Controls.Add(title);

            Label expGain = new Label
            {
                Text = "本局经验 +" + lastSettlementExpGain,
                Font = new Font("Microsoft YaHei UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 224, 146),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(505, 118)
            };
            shade.Controls.Add(expGain);

            string[] headers = { "排名", "玩家", "伤害", "击杀", "死亡", "KD" };
            int[] xs = { 62, 148, 285, 402, 512, 612 };
            for (int i = 0; i < headers.Length; i++)
            {
                Label header = CreateSettlementLabel(headers[i], new Point(xs[i], 190), 13, Color.FromArgb(255, 224, 146), FontStyle.Bold);
                shade.Controls.Add(header);
            }

            for (int i = 0; i < results.Count; i++)
            {
                PlayerResultSnapshot result = results[i];
                int y = 238 + i * 56;
                Color rowColor = i == 0 ? Color.White : Color.FromArgb(210, 224, 236);
                string displayName = result.Name;
                if (networkMode && ((IsNetworkGuest && result.Name == "玩家2") || (IsNetworkHost && result.Name == "玩家1")))
                {
                    displayName += "（己方）";
                }
                shade.Controls.Add(CreateSettlementLabel(result.Rank.ToString(), new Point(xs[0], y), 13, rowColor, FontStyle.Bold));
                shade.Controls.Add(CreateSettlementLabel(displayName, new Point(xs[1], y), 13, rowColor, FontStyle.Bold));
                shade.Controls.Add(CreateSettlementLabel(result.Damage.ToString(), new Point(xs[2], y), 13, rowColor, FontStyle.Regular));
                shade.Controls.Add(CreateSettlementLabel(result.Kills.ToString(), new Point(xs[3], y), 13, rowColor, FontStyle.Regular));
                shade.Controls.Add(CreateSettlementLabel(result.Deaths.ToString(), new Point(xs[4], y), 13, rowColor, FontStyle.Regular));
                shade.Controls.Add(CreateSettlementLabel(CalculateKd(result.Kills, result.Deaths).ToString("0.00"), new Point(xs[5], y), 13, rowColor, FontStyle.Regular));
            }

            Button back = new Button
            {
                Text = "返回大厅",
                Font = new Font("Microsoft YaHei UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(190, 126, 84, 26),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(325, 610),
                Size = new Size(150, 42)
            };
            back.FlatAppearance.BorderColor = Color.FromArgb(255, 205, 99);
            back.Click += (s, e) =>
            {
                returnToLobby = true;
                Close();
            };
            shade.Controls.Add(back);
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
            int exp = DataManager.RewardExp(currentGameMode, rank);
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
            baseDestroyed = false;
            destroyedBaseOwner = 0;
            settlementShown = false;
            expRewarded = false;
            lastSettlementExpGain = 0;
            ClearSettlementControls();
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
            if (x < ClientRectangle.Left || y < ClientRectangle.Top || x + 50 > ClientRectangle.Right || y + 50 > ClientRectangle.Bottom)
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
                if (IsBlockingBox(boxes[i]) && RectsOverlap(safeX, safeY, 100, 100, boxes[i].x, boxes[i].y, 25, 25))
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
            int[] offsets = { 0, 60, -60, 120, -120, 180, -180 };
            foreach (int offsetY in offsets)
            {
                foreach (int offsetX in offsets)
                {
                    int x = Math.Max(ClientRectangle.Left + 25, Math.Min(ClientRectangle.Right - 75, desiredX + offsetX));
                    int y = Math.Max(ClientRectangle.Top + 25, Math.Min(ClientRectangle.Bottom - 75, desiredY + offsetY));
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
            foreach (BaseTank tank in tanks)
            {
                Point safePoint = FindSafeSpawnPoint(tank.x, tank.y, placed);
                tank.x = safePoint.X;
                tank.y = safePoint.Y;
                placed.Add(tank);
            }
        }

        private void SpawnEnemyAt(int spawnIndex)
        {
            int index = Math.Abs(spawnIndex) % 4;
            int layer = Math.Abs(spawnIndex) / 4;
            int offset = layer * 60;
            TankType enemyType = TankResourceLoader.GetRandomEnemyTankType();
            var images = TankResourceLoader.GetEnemyTankImages(enemyType);
            var config = TankConfigs.GetConfig(enemyType);
            int x = level.masPointsSpawnEnemy[index, 0];
            int y = level.masPointsSpawnEnemy[index, 1];

            if (index == 0 || index == 1)
            {
                y = Math.Max(50, Math.Min(ClientRectangle.Bottom - 100, y + offset));
            }
            else
            {
                x = Math.Max(50, Math.Min(ClientRectangle.Right - 100, x + offset));
            }
            Point spawnPoint = FindSafeSpawnPoint(x, y, tanks);

            EnemyTank enemy = new EnemyTank(images.Item1, images.Item2,
                ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan,
                spawnPoint.X,
                spawnPoint.Y,
                2,
                config.BaseHealth,
                config.BaseAttack);
            enemy.TankType = enemyType;

            enemies.Add(enemy);
            tanks.Add(enemy);
            MoveEnemy += enemy.Move;
        }

        private bool ShouldKeepEnemyCountStable()
        {
            return launchOptions != null
                && (launchOptions.BattleRule == BattleRuleMode.FreeBattle || launchOptions.BattleRule == BattleRuleMode.BaseDefense);
        }

        private void RegisterEnemyKill(int shooter)
        {
            if (!ShouldKeepEnemyCountStable())
            {
                setting.countEnemies--;
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

        private void RefillEnemiesIfNeeded()
        {
            if (!ShouldKeepEnemyCountStable())
            {
                return;
            }

            while (enemies.Count < activeEnemyLimit)
            {
                SpawnEnemyAt(enemies.Count);
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
            int countDegree = 0;
            int numBuf;
            int[] masNum = new int[10];
            int bufPoints;

            int y = 200, x = 320;

            labelStat1.Visible = labelStat2.Visible = labelStat3.Visible = labelStat4.Visible = labelStat5.Visible = labelStat6.Visible = true;
            labelStat7.Visible = labelStat8.Visible = labelStat9.Visible = labelStat9.Visible = labelStat10.Visible = true;

            for (int i = 0; i < 10; i++)
            {
                countDegree = 0;
                x = 320;

                WriteNameLabelStat(i, stats.statistic[i].name);

                bufPoints = stats.statistic[i].points;

                if (bufPoints == 0)
                    Numbers.Draw(grMemory, Numbers.ReturnNumPic(0), x += 30 , y);
                else
                    while (bufPoints != 0)
                    {
                        numBuf = bufPoints % 10;
                        masNum[countDegree] = numBuf;
                        bufPoints /= 10;
                        countDegree++;
                    }

                for (int j = countDegree - 1; j >= 0; j--)
                {
                    Numbers.Draw(grMemory, Numbers.ReturnNumPic(masNum[j]), x += 30, y);
                    x += 15;
                }

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

            int countDegree = 0;
            int numBuf;
            int[] masNum = new int[10];
            int bufPoints = setting.pointsPlayer1;

            int x = 300;

            if (bufPoints == 0)
                Numbers.Draw(grMemory, Numbers.ReturnNumPic(0), x, 180);
            else
                while (bufPoints != 0)
                {
                    numBuf = bufPoints % 10;
                    masNum[countDegree] = numBuf;
                    bufPoints /= 10;
                    countDegree++;
                }

            for (int i = countDegree - 1; i >= 0; i--)
            {
                Numbers.Draw(grMemory, Numbers.ReturnNumPic(masNum[i]), x, 180);
                x += 30;
            }

            if (setting.twoPlayer)
            {
                PictureStat2.Visible = true;
                PictureStat2.Image = CreateTextBitmap("二号玩家", PictureStat2.Width, PictureStat2.Height, 17);

                countDegree = 0;
                bufPoints = setting.pointsPlayer2;

                x = 300;

                if (bufPoints == 0)
                    Numbers.Draw(grMemory, Numbers.ReturnNumPic(0), x, 240);
                else
                    while (bufPoints != 0)
                    {
                        numBuf = bufPoints % 10;
                        masNum[countDegree] = numBuf;
                        bufPoints /= 10;
                        countDegree++;
                    }

                for (int i = countDegree - 1; i >= 0; i--)
                {
                    Numbers.Draw(grMemory, Numbers.ReturnNumPic(masNum[i]), x, 240);
                    x += 30;
                }
            }
        }

        private void isReadyTo1Level()
        {
            if (setting.level == 1)
            {
                gameoverX = 355; gameoverY = 10; pauseGameOver = GameTiming.ScaleLegacyTicks(GameOverSettlementDelayLegacyTicks);

                firstPlayer = true;
                tanks.Add(player1);
                tanks.Add(player2);

                player1.hitpoints = player1.maxHitpoints;

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

                for (int i = 0; i < Math.Min(3, activeEnemyLimit); i++)
                {
                    SpawnEnemyAt(i);
                }

                Point player1Spawn = FindSafeSpawnPoint(level.masPointsSpawnPlayers[0, 0], level.masPointsSpawnPlayers[0, 1], null);
                player1.x = player1Spawn.X;
                player1.y = player1Spawn.Y;
                MovePlayer1 += player1.Move;

                PicturePlayer1_.Visible = true;

                if (setting.twoPlayer)
                {
                    player2.hitpoints = player2.maxHitpoints;
                    Point player2Spawn = FindSafeSpawnPoint(level.masPointsSpawnPlayers[2, 0], level.masPointsSpawnPlayers[2, 1], tanks);
                    player2.x = player2Spawn.X;
                    player2.y = player2Spawn.Y;
                    MovePlayer2 += player2.Move;
                    picturePlayer2_.Visible = true;
                    secondPlayer = true;
                }

                for (int i = enemies.Count; i < activeEnemyLimit; i++)
                {
                    SpawnEnemyAt(i);
                }

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

            if (IsNetworkHost)
            {
                ApplyRemoteInputToSecondPlayer();
            }

            ///////////////////////////////////////////////芯褌褉懈褋芯胁泻邪 薪懈卸薪械泄 锌邪薪械谢褜

            int x = 150, y = 760;
            for (int i = 0; i < setting.lifesPlayer1; i++)
            {
                grMemory.DrawImage(heart, x, y, 25, 25);
                x += 30;
            }
            if(setting.twoPlayer)
            {
                x = 530;
                for (int i = 0; i < setting.lifesPlayer2; i++)
                {
                    grMemory.DrawImage(heart, x, y, 25, 25);
                    x += 30;
                }
            }

            int countDegree = 0;
            int numBuf;
            int[] masNum = new int[10];
            int bufPoints = setting.pointsPlayer1;

            x = 255;

            if (bufPoints == 0)
                Numbers.Draw(grMemory, Numbers.ReturnNumPic(0), x, 760);
            else
                while(bufPoints != 0)
                {
                    numBuf = bufPoints % 10;
                    masNum[countDegree] = numBuf;
                    bufPoints /= 10;
                    countDegree++;
                }

            for (int i = countDegree - 1; i >= 0; i--)
            {
                Numbers.Draw(grMemory, Numbers.ReturnNumPic(masNum[i]), x, 760);
                x += 30;
            }

            if(setting.twoPlayer)
            {
                countDegree = 0;
                bufPoints = setting.pointsPlayer2;

                x = 633;

                if (bufPoints == 0)
                    Numbers.Draw(grMemory, Numbers.ReturnNumPic(0), x, 760);
                else
                    while (bufPoints != 0)
                    {
                        numBuf = bufPoints % 10;
                        masNum[countDegree] = numBuf;
                        bufPoints /= 10;
                        countDegree++;
                    }

                for (int i = countDegree - 1; i >= 0; i--)
                {
                    Numbers.Draw(grMemory, Numbers.ReturnNumPic(masNum[i]), x, 760);
                    x += 30;
                }
            }

            /////////////////////////////////////////////////////褋锌邪胁薪 斜芯褌芯胁
            if (setting.twoPlayer || networkMode)
            {
                DrawKillHud(player1Kills, player2Kills);
            }

            if (!ShouldKeepEnemyCountStable() && timeSpawn == 0 && setting.countEnemies > 0 && enemies.Count < activeEnemyLimit)
            {
                Random random = new Random();
                bool[] mas = new bool[4] { false, false, false, false };
                int num = random.Next(0, 4);

                for (int i = 0; i < tanks.Count; i++)
                    for (int j = 0; j < 3; j++)
                        if (Collisions.check_touch(level.masPointsSpawnEnemy[j, 0], level.masPointsSpawnEnemy[j, 1], tanks[i].x, tanks[i].y))
                            mas[j] = true;

                num = random.Next(0, 4);
                if (mas[0] == true && mas[1] == true && mas[2] == true)
                {
                    timeSpawn = setting.timeSpawn;
                }
                else
                {
                    while (mas[num] == true)
                    {
                        num = random.Next(0, 4);
                    }

                    SpawnEnemyAt(num);

                    timeSpawn = setting.timeSpawn;
                }
            }
            else
                timeSpawn--;

            //////////////////////////////////////////////锌褉芯胁械褉泻邪 褋褌芯谢泻薪芯胁械薪懈泄
            for (int i = 0; i < bullets.Count; i++) //锌褉芯胁械褉泻邪 褋褌芯谢泻薪芯胁械薪懈褟 锌褍谢褜 懈 褟褖懈泻芯胁
            {
                bool bulletDeath = HandleBulletCollision(i);
                for (int j = 0; j < boxes.Count; j++)
                {
                    if (!bulletDeath && Collisions.check_touch(this, bullets[i], boxes[j]) == 2)
                    {
                        boxes[j].hitPoint--;
                        AddHitEffect(boxes[j].x, boxes[j].y);
                        if (boxes[j].hitPoint == 0)
                        {
                            MarkBaseDestroyed(boxes[j]);

                            boxes.RemoveAt(j);
                        }
                        bullets.RemoveAt(i);

                        bulletDeath = true;

                        break;
                    }
                    else if (!bulletDeath && Collisions.check_touch(this, bullets[i], boxes[j]) == 1)
                    {
                        AddHitEffect(boxes[j].x, boxes[j].y);
                        bullets.RemoveAt(i);
                        bulletDeath = true;

                        break;
                    }
                }

                //褋褌芯谢泻薪芯胁械薪懈械 锌褍谢褜 褋 斜芯褌邪屑懈
                for(int j = 0; j < enemies.Count; j++)
                {
                    if(!bulletDeath && Collisions.check_touch(bullets[i], enemies[j]) )
                    {
                        if (bullets[i].who == 1)
                            setting.pointsPlayer1 += setting.pointsForEnemy;
                        else if (bullets[i].who == 2)
                            setting.pointsPlayer2 += setting.pointsForEnemy;

                        if (bullets[i].who != 0)
                        {
                            AddHitEffect(enemies[j].x, enemies[j].y);
                            TrackDamage(bullets[i].who, bullets[i].damage);
                            enemies[j].hitpoints -= bullets[i].damage;  // 浣跨敤瀛愬脊浼ゅ
                            if (enemies[j].hitpoints <= 0)
                            {
                                RegisterEnemyKill(bullets[i].who);
                            }
                            bullets.RemoveAt(i);
                            bulletDeath = true;

                            break;
                        }
                    }
                }

                //褋褌芯谢泻薪芯胁械薪懈械 锌褍谢褜 褋 1 懈谐褉芯泻芯屑
                if (!bulletDeath && firstPlayer && bullets[i].who != 1 && Collisions.check_touch(bullets[i], player1))
                {
                    if(bullets[i].who == 2)
                    {
                        setting.pointsPlayer2 += setting.pointsForPlayers;
                    }

                    AddHitEffect(player1.x, player1.y);
                    TrackDamage(bullets[i].who, bullets[i].damage);
                    player1.hitpoints -= bullets[i].damage;  // 浣跨敤瀛愬脊浼ゅ
                    if (player1.hitpoints <= 0 && bullets[i].who == 2)
                    {
                        player2Kills++;
                    }
                    bullets.RemoveAt(i);
                    bulletDeath = true;
                }

                //褋褌芯谢泻薪芯胁械薪懈械 锌褍谢褜 褋芯 2 懈谐褉芯泻芯屑
                if (!bulletDeath && secondPlayer && bullets[i].who != 2 && Collisions.check_touch(bullets[i], player2))
                {
                    if (bullets[i].who == 1)
                    {
                        setting.pointsPlayer1 += setting.pointsForPlayers;
                    }

                    AddHitEffect(player2.x, player2.y);
                    TrackDamage(bullets[i].who, bullets[i].damage);
                    player2.hitpoints -= bullets[i].damage;  // 浣跨敤瀛愬脊浼ゅ
                    if (player2.hitpoints <= 0 && bullets[i].who == 1)
                    {
                        player1Kills++;
                    }
                    bullets.RemoveAt(i);
                    bulletDeath = true;
                }

                //写胁懈卸械薪懈械 锌褍谢懈
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

            //蟹邪写械褉卸泻邪 芯褌褉懈褋芯胁泻懈 邪薪懈屑邪褑懈懈  (写械谢械谐邪褌)
            if(DelayEvent != null)
                DelayEvent(); 

            //写胁懈卸械薪懈械 斜芯褌芯胁
            if (MoveEnemy != null)
            {
                for (int i = 0; i < setting.speedEnemy; i++)
                    MoveEnemy(this, boxes, bullets, tanks);
            };

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
                enemies[i].Draw(grMemory);
            }

            if (firstPlayer) player1.Draw(grMemory, Color.FromArgb(80, 235, 120));                    // 芯褌褉懈褋芯胁泻邪 褌邪薪泻芯胁 懈谐褉芯泻芯胁
            if (secondPlayer) player2.Draw(grMemory, networkMode ? Color.FromArgb(255, 82, 82) : Color.Empty);

            for (int i = 0; i < boxes.Count; i++) // 芯褌褉懈褋芯胁泻邪 谢械褋薪褘褏 褟褖懈泻芯胁, 锌芯蟹卸械 芯褌褉懈褋芯胁泻懈 褌邪薪泻芯胁, 褔褌芯斜褘 褌邪薪泻 屑芯谐 锌褉褟褌邪褌褜褋褟 胁 谢械褋褍
            {
                if (boxes[i] is Box_Green)
                    boxes[i].Draw(grMemory);
            }

            for(int i = 0; i < animations.Count; i++) // 芯褌褉懈褋芯胁泻邪 邪薪懈屑邪褑懈懈
                animations[i].Draw(grMemory);

            ////////////////////////////////////////////////////////////////////////锌褉芯胁械褉泻邪 泻芯薪褑邪 懈谐褉褘

            bool freeBattleFinished = launchOptions != null && launchOptions.BattleRule == BattleRuleMode.FreeBattle && (player1Kills >= 15 || player2Kills >= 15);
            bool baseDefenseFinished = launchOptions != null && launchOptions.BattleRule == BattleRuleMode.BaseDefense && baseDestroyed;
            bool singlePlayerCleared = currentGameMode == GameMode.SinglePlayer && setting.countEnemies <= 0 && enemies.Count == 0;
            bool networkLastPlayerStanding = networkMode && CountActiveNetworkPlayers() <= 1;

            bool lifeLimitFinished = !networkMode && ((setting.lifesPlayer1 == 0 && !setting.twoPlayer) || ((setting.lifesPlayer2 == 0  && setting.lifesPlayer1 == 0) && setting.twoPlayer));

            if (lifeLimitFinished
                || freeBattleFinished || baseDefenseFinished || networkLastPlayerStanding
                || singlePlayerCleared)
            {
                if (!singlePlayerCleared)
                {
                    gameoverY = Math.Min(140, gameoverY + 24);
                    grMemory.DrawImage(gameover, gameoverX, gameoverY);
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

                    MovePlayer1 -= player1.Move;
                    MovePlayer2 -= player2.Move;

                    List<PlayerResultSnapshot> settlementResults = BuildSettlementResults();
                    if (IsNetworkHost)
                    {
                        BroadcastSnapshot(true, settlementResults);
                    }

                    ShowSettlementPage(settlementResults);

                    return;
                }
            }

            grMain.DrawImage(backgroundMemory, 0, 0);
            grMemory.DrawImage(backgroundMain, 0, 0);

            ////////////////////////////////////////////////////////////// 谢芯谐懈泻邪

            ProcessPlayerFireInputs();

            if (player1.message.pauseSpace > 0) player1.message.pauseSpace--;                    // 锌邪褍蟹邪 屑械卸写褍 胁褘褋褌褉械谢邪屑懈 褍 懈谐褉芯泻芯胁
            if (secondPlayer && player2.message.pauseSpace > 0) player2.message.pauseSpace--;     //

            for (int i = 0; i < enemies.Count; i++) //锌邪褍蟹邪 屑械卸写褍 胁褘褋褌褉械谢邪屑懈 褍 斜芯褌芯胁
                enemies[i].message.pauseSpace--;

            for (int i = 0; i < 8; i++)                                                         // 写胁懈卸械薪懈械 褌邪薪泻芯胁(胁薪褍褌褉懈 锌褉芯胁械褉泻邪 薪邪 褋芯锌褉懈泻芯褋薪芯胁械薪懈褟 褌邪薪泻芯胁)
            {
                if (firstPlayer) MovePlayer1(this, null, player1.message, boxes, tanks);
                if (secondPlayer) MovePlayer2(this, null, player2.message, boxes, tanks);
            }

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

            for (int i = 0; i < enemies.Count; i++)                           //褍薪懈褔褌芯卸械薪懈械 胁褉邪谐芯胁 锌褉懈 褏锌 = 0
            {
                if(enemies[i].hitpoints <= 0)
                {
                    AddExplosionEffect(enemies[i].x, enemies[i].y);

                    MoveEnemy -= enemies[i].Move;
                    tanks.RemoveAt(i + 2);
                    enemies.RemoveAt(i);
                    i--;
                    RefillEnemiesIfNeeded();
                }
            }

            if (secondPlayer && player2.hitpoints <= 0)  // 褍薪懈褔褌芯卸械薪懈械 胁褌芯褉芯谐芯 懈谐褉芯泻邪
            {
                if (!networkMode)
                {
                    setting.lifesPlayer2--;
                }
                player2Deaths++;
                AddExplosionEffect(player2.x, player2.y);

                Random RanNum = new Random();
                int num = RanNum.Next(0, 4);
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

                Random RanNum = new Random();
                int num = RanNum.Next(0, 4);
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
        }

        //薪邪褔邪谢褜薪邪褟 懈薪懈褑懈邪谢懈蟹邪褑懈褟
        public MainWindow() : this(GameMode.SinglePlayer, TankType.Green)
        {
        }

        public MainWindow(GameMode mode, TankType tankType) : this(new GameLaunchOptions { Mode = mode, TankType = tankType, StartImmediately = false })
        {
        }

        public MainWindow(GameLaunchOptions options)
        {
            InitializeComponent();

            launchOptions = options ?? new GameLaunchOptions();
            currentGameMode = launchOptions.Mode;
            selectedTankType = launchOptions.TankType;
            if (launchOptions.MapConfig == null)
            {
                launchOptions.MapConfig = BattleMapFactory.CreateSystemMap(0);
            }

            backgroundMain = CreateCoverBackground(GetWarBackgroundImage(), ClientSize);
            backgroundMemory = new Bitmap(backgroundMain.Width, backgroundMain.Height);

            grMain = CreateGraphics();
            grMemory = Graphics.FromImage(backgroundMemory);
            grMemory.DrawImage(backgroundMain, 0, 0);

            // 浣跨敤閫変腑鐨勫潶鍏嬬被鍨嬪垱寤虹帺瀹跺潶鍏?
            var playerData = DataManager.CurrentPlayer;
            var playerTankData = playerData.Tanks[selectedTankType];
            var config = TankConfigs.GetConfig(selectedTankType);

            int attack = config.GetAttackAtLevel(playerTankData.Level);
            int health = config.GetHealthAtLevel(playerTankData.Level);

            // 加载对应颜色的坦克图片
            var (tankImg1, tankImg2) = TankResourceLoader.GetPlayerTankImages(selectedTankType);

            player1 = new PlayerTank(tankImg1, tankImg2, ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan, -100, -100, 2, health, attack);
            player2 = new PlayerTank(new Bitmap(tankImg1), new Bitmap(tankImg2), ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan, -50, -50, 2, config.BaseHealth, config.BaseAttack);

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

            setting.twoPlayer = currentGameMode == GameMode.TwoPlayer;
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
