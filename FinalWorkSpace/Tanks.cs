using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    public class TankEventArgs : EventArgs
    {
        public bool isUp, isDown, isLeft, isRight, isSpace, isSkill;
        public int pauseSpace = 0;
        public TankEventArgs() : base() { }

        public void clear()
        {
            isUp = isDown = isLeft = isRight = isSpace = isSkill = false;
        }
    }

    public class EnemyEventArgs : EventArgs
    {
        public int pauseSpace = GameTiming.ScaleLegacyTicks(50);
        public int pauseTurn = GameTiming.ScaleLegacyTicks(100);
        public int goToPlayer;
        public float patrolAngleDegrees;
        public int aggressionLevel = 1;
        public int aggroRange = 260;
        public int attackRange = 220;
        public bool isBoss;
        public static int player1X, player1Y, player2X, player2Y;
        public static int players; // 0 - no, 1 - 1 player, 2 - 2 player, 3 - both

        public EnemyEventArgs() : base() { }
    }

    public delegate void EnemyEvent(Form MainWindow, List<Box> boxes, List<Bullet> bullets, List<BaseTank> tanks);
    public delegate void TankEvent(Form MainWindow, KeyEventArgs e, TankEventArgs tank, List<Box> boxes, List<BaseTank> tanks);

    public class BaseTank
    {
        protected static readonly double BaseMoveSpeed = GameTiming.ScaleDistance(1.5);
        protected const int TankSize = 50;
        private static readonly Dictionary<int, SolidBrush[]> GlowBrushCache = new Dictionary<int, SolidBrush[]>();
        protected Bitmap pictureTank, pictureTank1, pictureTank2;
        private readonly Bitmap[] directionalPictureTank1 = new Bitmap[4];
        private readonly Bitmap[] directionalPictureTank2 = new Bitmap[4];
        private bool useFreeRotation;
        private float rotationDegrees;
        protected bool changePicture;
        public Bitmap pictureBullet;
        public int hitpoints = 1;
        public int maxHitpoints = 1;  // 最大生命值
        public int attack = 10;        // 攻击力
        public TankType TankType = TankType.Green;
        public bool IsBossEntity;
        public double speedMultiplier = 1.0;
        public double bulletSpeedMultiplier = 1.0;
        public double cooldownSeconds = 3.0;
        public DateTime nextFireAvailableAtUtc = DateTime.MinValue;
        protected double preciseX, preciseY;
        public int direction; //0 - left, 1 - right, 2 - up, 3 - down
        public int x
        {
            get { return (int)Math.Round(preciseX); }
            set { preciseX = value; }
        }
        public int y
        {
            get { return (int)Math.Round(preciseY); }
            set { preciseY = value; }
        }

        public float RotationDegrees
        {
            get { return rotationDegrees; }
        }

        public BaseTank(Bitmap pictureTank1, Bitmap pictureTank2, Bitmap pictureBullet, int x, int y, int direction)
        {
            pictureBullet.MakeTransparent(Color.Black);
            this.pictureBullet = pictureBullet;
            InitializeDirectionalImages(pictureTank1, pictureTank2);
            changePicture = false;
            preciseX = x;
            preciseY = y;
            SetDirection(direction);
        }

        // 带攻击力和生命力的构造函数
        public BaseTank(Bitmap pictureTank1, Bitmap pictureTank2, Bitmap pictureBullet, int x, int y, int direction, int health, int attack)
            : this(pictureTank1, pictureTank2, pictureBullet, x, y, direction)
        {
            this.maxHitpoints = health;
            this.hitpoints = health;
            this.attack = attack;
        }

        public void SetImages(Bitmap pictureTank1, Bitmap pictureTank2)
        {
            InitializeDirectionalImages(pictureTank1, pictureTank2);
            SetDirection(direction);
        }

        private void InitializeDirectionalImages(Bitmap basePictureTank1, Bitmap basePictureTank2)
        {
            for (int i = 0; i < 4; i++)
            {
                directionalPictureTank1[i]?.Dispose();
                directionalPictureTank2[i]?.Dispose();
                directionalPictureTank1[i] = CreateDirectionalBitmap(basePictureTank1, i);
                directionalPictureTank2[i] = CreateDirectionalBitmap(basePictureTank2, i);
            }
        }

        private static Bitmap CreateDirectionalBitmap(Bitmap source, int targetDirection)
        {
            Bitmap rotated = new Bitmap(source);
            rotated.MakeTransparent(Color.Black);

            switch (targetDirection)
            {
                case 0:
                    rotated.RotateFlip(RotateFlipType.Rotate90FlipX);
                    break;
                case 1:
                    rotated.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                case 3:
                    rotated.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    break;
            }

            return rotated;
        }

        protected void SetDirection(int newDirection)
        {
            direction = newDirection;
            rotationDegrees = DirectionToRotationDegrees(newDirection);
            pictureTank1 = directionalPictureTank1[newDirection];
            pictureTank2 = directionalPictureTank2[newDirection];
            pictureTank = changePicture ? pictureTank2 : pictureTank1;
        }

        protected void EnableFreeRotation()
        {
            useFreeRotation = true;
            pictureTank1 = directionalPictureTank1[2];
            pictureTank2 = directionalPictureTank2[2];
            pictureTank = changePicture ? pictureTank2 : pictureTank1;
        }

        public void SetRotationDegrees(float angleDegrees)
        {
            rotationDegrees = NormalizeAngle(angleDegrees);
        }

        protected void changePictureFun()
        {
            if (changePicture)
            {
                pictureTank = pictureTank2;
                changePicture = false;
            }
            else
            {
                changePicture = true;
                pictureTank = pictureTank1;
            }
        }

        public Bitmap CurrentPicture
        {
            get { return pictureTank; }
        }

        public void Draw(Graphics grMemory)
        {
            Draw(grMemory, Color.Empty);
        }

        public void Draw(Graphics grMemory, Color glowColor)
        {
            DrawGlow(grMemory, x, y, TankSize, TankSize, glowColor);
            if (useFreeRotation)
            {
                DrawRotated(grMemory, pictureTank, x, y, TankSize, TankSize, rotationDegrees);
                return;
            }

            grMemory.DrawImage(pictureTank, x, y, TankSize, TankSize);
        }

        public static void DrawGlow(Graphics graphics, int x, int y, int width, int height, Color glowColor)
        {
            if (glowColor == Color.Empty)
            {
                return;
            }

            int centerX = x + width / 2;
            int centerY = y + height / 2;
            SolidBrush[] brushes = GetGlowBrushes(glowColor);
            for (int i = 0; i < 4; i++)
            {
                int expand = 16 - i * 4;
                graphics.FillEllipse(
                    brushes[i],
                    centerX - width / 2 - expand,
                    centerY - height / 2 - expand,
                    width + expand * 2,
                    height + expand * 2);
            }
        }

        protected bool IsInsideBattlefield(Form mainWindow, int nextX, int nextY)
        {
            if (mainWindow == null)
            {
                return true;
            }

            Rectangle bounds = new Rectangle(
                mainWindow.ClientRectangle.Left,
                mainWindow.ClientRectangle.Top,
                Math.Min(mainWindow.ClientRectangle.Width, Collisions.BattlefieldWidth),
                Math.Min(mainWindow.ClientRectangle.Height, Collisions.BattlefieldHeight));
            return nextX >= bounds.Left
                && nextY >= bounds.Top
                && nextX + TankSize <= bounds.Right
                && nextY + TankSize <= bounds.Bottom;
        }

        protected double GetMoveSpeed()
        {
            return BaseMoveSpeed * Math.Max(0.1, speedMultiplier);
        }

        protected static float NormalizeAngle(float angleDegrees)
        {
            angleDegrees %= 360f;
            if (angleDegrees < 0f)
            {
                angleDegrees += 360f;
            }

            return angleDegrees;
        }

        protected static float DirectionToRotationDegrees(int direction)
        {
            switch (direction)
            {
                case 0: return 270f;
                case 1: return 90f;
                case 3: return 180f;
                default: return 0f;
            }
        }

        protected static float VectorToRotationDegrees(double moveX, double moveY)
        {
            if (Math.Abs(moveX) < double.Epsilon && Math.Abs(moveY) < double.Epsilon)
            {
                return 0f;
            }

            return NormalizeAngle((float)(Math.Atan2(moveY, moveX) * 180.0 / Math.PI + 90.0));
        }

        protected static void DrawRotated(Graphics graphics, Bitmap source, int x, int y, int width, int height, float angleDegrees)
        {
            var state = graphics.Save();
            graphics.TranslateTransform(x + width / 2f, y + height / 2f);
            graphics.RotateTransform(angleDegrees);
            graphics.DrawImage(source, -width / 2f, -height / 2f, width, height);
            graphics.Restore(state);
        }

        private static SolidBrush[] GetGlowBrushes(Color glowColor)
        {
            int key = glowColor.ToArgb();
            if (GlowBrushCache.TryGetValue(key, out SolidBrush[] brushes))
            {
                return brushes;
            }

            brushes = new SolidBrush[4];
            for (int i = 0; i < brushes.Length; i++)
            {
                int alpha = 34 + i * 18;
                brushes[i] = new SolidBrush(Color.FromArgb(alpha, glowColor));
            }

            GlowBrushCache[key] = brushes;
            return brushes;
        }
    }

    class PlayerTank : BaseTank
    {
        private const double CollisionProbeStep = 1.0;
        private const double DashDistance = 90.0;
        private const double DashProbeStep = 10.0;
        public TankEventArgs message;
        public bool UseMouseAim { get; set; }
        public TankSkillType SkillType { get; private set; }
        public double SkillCooldownSeconds { get; private set; }
        public DateTime nextSkillAvailableAtUtc = DateTime.MinValue;
        private DateTime overdriveEndsAtUtc = DateTime.MinValue;
        private DateTime shieldEndsAtUtc = DateTime.MinValue;
        private DateTime lifestealEndsAtUtc = DateTime.MinValue;
        private int shieldPoints;
        private int barrageShotsRemaining;

        public PlayerTank(Bitmap pictureTank1, Bitmap pictureTank2, Bitmap pictureBullet, int x, int y, int direction) : base(pictureTank1, pictureTank2, pictureBullet, x, y, direction)
        {
            message = new TankEventArgs();
            EnableFreeRotation();
        }

        public PlayerTank(Bitmap pictureTank1, Bitmap pictureTank2, Bitmap pictureBullet, int x, int y, int direction, int health, int attack)
            : base(pictureTank1, pictureTank2, pictureBullet, x, y, direction, health, attack)
        {
            message = new TankEventArgs();
            EnableFreeRotation();
        }

        public void ConfigureSkill(TankType tankType)
        {
            SkillType = TankSkillCatalog.GetSkillType(tankType);
            SkillCooldownSeconds = TankSkillCatalog.GetCooldownSeconds(SkillType);
            ResetSkillState();
        }

        public void ResetSkillState()
        {
            nextSkillAvailableAtUtc = DateTime.MinValue;
            overdriveEndsAtUtc = DateTime.MinValue;
            shieldEndsAtUtc = DateTime.MinValue;
            lifestealEndsAtUtc = DateTime.MinValue;
            shieldPoints = 0;
            barrageShotsRemaining = 0;
        }

        public int ShieldPoints
        {
            get
            {
                ExpireTimedEffects();
                return shieldPoints;
            }
        }

        public int BarrageShotsRemaining
        {
            get
            {
                return barrageShotsRemaining;
            }
        }

        public bool IsOverdriveActive
        {
            get
            {
                return DateTime.UtcNow < overdriveEndsAtUtc;
            }
        }

        public bool IsLifestealActive
        {
            get
            {
                return DateTime.UtcNow < lifestealEndsAtUtc;
            }
        }

        public bool IsSkillReady
        {
            get
            {
                return DateTime.UtcNow >= nextSkillAvailableAtUtc;
            }
        }

        public double GetRemainingSkillCooldownSeconds()
        {
            double seconds = (nextSkillAvailableAtUtc - DateTime.UtcNow).TotalSeconds;
            return Math.Max(0, seconds);
        }

        public void ExpireTimedEffects()
        {
            DateTime now = DateTime.UtcNow;
            if (shieldPoints > 0 && now >= shieldEndsAtUtc)
            {
                shieldPoints = 0;
            }
        }

        public int GetCurrentAttack()
        {
            return IsOverdriveActive ? (int)Math.Round(attack * 1.6) : attack;
        }

        public double GetCurrentCooldownSeconds()
        {
            return IsOverdriveActive ? Math.Max(0.2, cooldownSeconds * 0.55) : cooldownSeconds;
        }

        public bool ConsumeBarrageShot()
        {
            if (barrageShotsRemaining <= 0)
            {
                return false;
            }

            barrageShotsRemaining--;
            return true;
        }

        public int AbsorbDamage(int damage)
        {
            ExpireTimedEffects();
            if (shieldPoints <= 0 || damage <= 0)
            {
                return damage;
            }

            int absorbed = Math.Min(shieldPoints, damage);
            shieldPoints -= absorbed;
            return damage - absorbed;
        }

        public int ApplyLifesteal(int damage)
        {
            if (!IsLifestealActive || damage <= 0)
            {
                return 0;
            }

            int heal = Math.Max(1, (int)Math.Round(damage * 0.35));
            int before = hitpoints;
            hitpoints = Math.Min(maxHitpoints, hitpoints + heal);
            return Math.Max(0, hitpoints - before);
        }

        public bool TryActivateSkill(Form mainWindow, List<Box> boxes, List<BaseTank> tanks)
        {
            DateTime now = DateTime.UtcNow;
            if (now < nextSkillAvailableAtUtc)
            {
                return false;
            }

            bool activated;
            switch (SkillType)
            {
                case TankSkillType.Dash:
                    activated = TryDash(mainWindow, boxes, tanks);
                    break;
                case TankSkillType.Barrage:
                    barrageShotsRemaining = Math.Max(barrageShotsRemaining, 3);
                    activated = true;
                    break;
                case TankSkillType.Overdrive:
                    overdriveEndsAtUtc = now.AddSeconds(5);
                    activated = true;
                    break;
                case TankSkillType.Lifesteal:
                    lifestealEndsAtUtc = now.AddSeconds(8);
                    activated = true;
                    break;
                default:
                    hitpoints = Math.Min(maxHitpoints, hitpoints + Math.Max(18, maxHitpoints / 3));
                    activated = true;
                    break;
            }

            if (activated)
            {
                nextSkillAvailableAtUtc = now.AddSeconds(SkillCooldownSeconds);
            }

            return activated;
        }

        public void Move(Form MainWindow, KeyEventArgs e, TankEventArgs message, List<Box> boxes, List<BaseTank> tanks)
        {
            Move(MainWindow, e, message, boxes, tanks, 1);
        }

        public void Move(Form MainWindow, KeyEventArgs e, TankEventArgs message, List<Box> boxes, List<BaseTank> tanks, int movementStepCount)
        {
            double moveX = 0;
            double moveY = 0;

            if (message.isLeft) moveX -= 1;
            if (message.isRight) moveX += 1;
            if (message.isUp) moveY -= 1;
            if (message.isDown) moveY += 1;

            if (Math.Abs(moveX) < double.Epsilon && Math.Abs(moveY) < double.Epsilon)
            {
                return;
            }

            double length = Math.Sqrt(moveX * moveX + moveY * moveY);
            moveX /= length;
            moveY /= length;

            if (!UseMouseAim)
            {
                SetRotationDegrees(VectorToRotationDegrees(moveX, moveY));
            }

            double totalDistance = GetMoveSpeed() * Math.Max(1, movementStepCount);
            int probeCount = Math.Max(1, (int)Math.Ceiling(totalDistance / CollisionProbeStep));
            double startX = preciseX;
            double startY = preciseY;

            for (int probe = 1; probe <= probeCount; probe++)
            {
                double probeDistance = totalDistance * probe / probeCount;
                int nextX = (int)Math.Round(startX + moveX * probeDistance);
                int nextY = (int)Math.Round(startY + moveY * probeDistance);

                if (!CanMoveTo(MainWindow, boxes, tanks, nextX, nextY))
                {
                    return;
                }
            }

            preciseX = startX + moveX * totalDistance;
            preciseY = startY + moveY * totalDistance;
            changePictureFun();
        }

        private bool CanMoveTo(Form MainWindow, List<Box> boxes, List<BaseTank> tanks, int nextX, int nextY)
        {
            if (!IsInsideBattlefield(MainWindow, nextX, nextY))
            {
                return false;
            }

            for (int i = 0; i < boxes.Count; i++)
            {
                if (Collisions.check_touch(nextX, nextY, boxes[i], MainWindow))
                {
                    return false;
                }
            }

            int currentIndex = -1;
            for (int i = 0; i < tanks.Count; i++)
            {
                if (ReferenceEquals(tanks[i], this))
                {
                    currentIndex = i;
                    break;
                }
            }

            for (int i = 0; i < tanks.Count; i++)
            {
                if (i == currentIndex)
                {
                    continue;
                }

                if (Collisions.check_touch(nextX, nextY, tanks[i].x, tanks[i].y))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryDash(Form mainWindow, List<Box> boxes, List<BaseTank> tanks)
        {
            double radians = (RotationDegrees - 90.0) * Math.PI / 180.0;
            double moveX = Math.Cos(radians);
            double moveY = Math.Sin(radians);
            if (Math.Abs(moveX) < double.Epsilon && Math.Abs(moveY) < double.Epsilon)
            {
                return false;
            }

            double startX = preciseX;
            double startY = preciseY;
            int probeCount = Math.Max(1, (int)Math.Ceiling(DashDistance / DashProbeStep));
            double safeDistance = 0;
            for (int probe = 1; probe <= probeCount; probe++)
            {
                double probeDistance = DashDistance * probe / probeCount;
                int nextX = (int)Math.Round(startX + moveX * probeDistance);
                int nextY = (int)Math.Round(startY + moveY * probeDistance);
                if (!CanMoveTo(mainWindow, boxes, tanks, nextX, nextY))
                {
                    break;
                }

                safeDistance = probeDistance;
            }

            if (safeDistance <= 0)
            {
                return false;
            }

            preciseX = startX + moveX * safeDistance;
            preciseY = startY + moveY * safeDistance;
            changePictureFun();
            return true;
        }
    }

    class EnemyTank : BaseTank
    {
        private const double EnemyFireIntervalSeconds = 1.0;
        private static readonly Random ranNumber = new Random();

        public EnemyEventArgs message;
        private int avoidanceTurnDirection;

        public EnemyTank(Bitmap pictureTank1, Bitmap pictureTank2, Bitmap pictureBullet, int x, int y, int direction) : base(pictureTank1, pictureTank2, pictureBullet, x, y, direction)
        {
            EnableFreeRotation();
            message = new EnemyEventArgs();
            avoidanceTurnDirection = ranNumber.Next(0, 2) == 0 ? -1 : 1;
            cooldownSeconds = EnemyFireIntervalSeconds;
            nextFireAvailableAtUtc = DateTime.UtcNow.AddSeconds(EnemyFireIntervalSeconds);
            message.goToPlayer = ranNumber.Next(1, 3);
            message.patrolAngleDegrees = ranNumber.Next(0, 360);
        }

        public EnemyTank(Bitmap pictureTank1, Bitmap pictureTank2, Bitmap pictureBullet, int x, int y, int direction, int health, int attack)
            : base(pictureTank1, pictureTank2, pictureBullet, x, y, direction, health, attack)
        {
            EnableFreeRotation();
            message = new EnemyEventArgs();
            avoidanceTurnDirection = ranNumber.Next(0, 2) == 0 ? -1 : 1;
            cooldownSeconds = EnemyFireIntervalSeconds;
            nextFireAvailableAtUtc = DateTime.UtcNow.AddSeconds(EnemyFireIntervalSeconds);
            message.goToPlayer = ranNumber.Next(1, 3);
            message.patrolAngleDegrees = ranNumber.Next(0, 360);
        }

        private bool TryGetTargetPosition(out int targetX, out int targetY, out double distance)
        {
            targetX = x;
            targetY = y;
            distance = double.MaxValue;

            if (EnemyEventArgs.players == 0)
            {
                return false;
            }

            if (EnemyEventArgs.players == 3 && message.goToPlayer != 1 && message.goToPlayer != 2)
            {
                message.goToPlayer = ranNumber.Next(1, 3);
            }

            if (EnemyEventArgs.players == 1 || (EnemyEventArgs.players == 3 && message.goToPlayer == 1))
            {
                targetX = EnemyEventArgs.player1X;
                targetY = EnemyEventArgs.player1Y;
            }
            else if (EnemyEventArgs.players == 2 || (EnemyEventArgs.players == 3 && message.goToPlayer == 2))
            {
                targetX = EnemyEventArgs.player2X;
                targetY = EnemyEventArgs.player2Y;
            }

            double dx = (targetX + 25) - (x + 25);
            double dy = (targetY + 25) - (y + 25);
            distance = Math.Sqrt(dx * dx + dy * dy);
            return true;
        }

        private bool CanMoveTo(Form mainWindow, List<Box> boxes, List<BaseTank> tanks, int nextX, int nextY)
        {
            if (!IsInsideBattlefield(mainWindow, nextX, nextY))
            {
                return false;
            }

            for (int i = 0; i < boxes.Count; i++)
            {
                if (Collisions.check_touch(nextX, nextY, boxes[i], mainWindow))
                {
                    return false;
                }
            }

            int currentIndex = -1;
            for (int i = 0; i < tanks.Count; i++)
            {
                if (ReferenceEquals(tanks[i], this))
                {
                    currentIndex = i;
                    break;
                }
            }

            for (int i = 0; i < tanks.Count; i++)
            {
                if (i == currentIndex)
                {
                    continue;
                }

                if (Collisions.check_touch(nextX, nextY, tanks[i].x, tanks[i].y))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryMoveVector(Form mainWindow, List<Box> boxes, List<BaseTank> tanks, double moveX, double moveY, bool updateRotation)
        {
            if (Math.Abs(moveX) < double.Epsilon && Math.Abs(moveY) < double.Epsilon)
            {
                return false;
            }

            double length = Math.Sqrt(moveX * moveX + moveY * moveY);
            moveX /= length;
            moveY /= length;

            if (updateRotation)
            {
                SetRotationDegrees(VectorToRotationDegrees(moveX, moveY));
                pictureTank = changePicture ? pictureTank2 : pictureTank1;
            }

            double totalDistance = GetMoveSpeed();
            int probeCount = Math.Max(1, (int)Math.Ceiling(totalDistance / 1.5));
            double startX = preciseX;
            double startY = preciseY;

            for (int probe = 1; probe <= probeCount; probe++)
            {
                double probeDistance = totalDistance * probe / probeCount;
                int nextX = (int)Math.Round(startX + moveX * probeDistance);
                int nextY = (int)Math.Round(startY + moveY * probeDistance);
                if (!CanMoveTo(mainWindow, boxes, tanks, nextX, nextY))
                {
                    return false;
                }
            }

            preciseX = startX + moveX * totalDistance;
            preciseY = startY + moveY * totalDistance;
            changePictureFun();
            return true;
        }

        private bool TryMoveVectorAvoidingObstacles(Form mainWindow, List<Box> boxes, List<BaseTank> tanks, double moveX, double moveY, bool updateRotation)
        {
            if (TryMoveVector(mainWindow, boxes, tanks, moveX, moveY, updateRotation))
            {
                return true;
            }

            if (Math.Abs(moveX) < double.Epsilon && Math.Abs(moveY) < double.Epsilon)
            {
                return false;
            }

            int preferredDirection = avoidanceTurnDirection == 0 ? 1 : avoidanceTurnDirection;
            int[] angleOffsets = { 35, 55, 80, 110, 145, 180 };
            for (int i = 0; i < angleOffsets.Length; i++)
            {
                int offset = angleOffsets[i];
                if (TryMoveRotatedVector(mainWindow, boxes, tanks, moveX, moveY, preferredDirection * offset, updateRotation))
                {
                    return true;
                }

                if (TryMoveRotatedVector(mainWindow, boxes, tanks, moveX, moveY, -preferredDirection * offset, updateRotation))
                {
                    avoidanceTurnDirection = -preferredDirection;
                    return true;
                }
            }

            avoidanceTurnDirection = -preferredDirection;
            return false;
        }

        private bool TryMoveRotatedVector(Form mainWindow, List<Box> boxes, List<BaseTank> tanks, double moveX, double moveY, double angleDegrees, bool updateRotation)
        {
            double radians = angleDegrees * Math.PI / 180.0;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);
            double rotatedX = moveX * cos - moveY * sin;
            double rotatedY = moveX * sin + moveY * cos;
            return TryMoveVector(mainWindow, boxes, tanks, rotatedX, rotatedY, updateRotation);
        }

        private bool TryMoveTowards(Form mainWindow, List<Box> boxes, List<BaseTank> tanks, int targetX, int targetY)
        {
            double moveX = (targetX + 25) - (x + 25);
            double moveY = (targetY + 25) - (y + 25);
            return TryMoveVectorAvoidingObstacles(mainWindow, boxes, tanks, moveX, moveY, false);
        }

        private bool TryStrafe(Form mainWindow, List<Box> boxes, List<BaseTank> tanks, int targetX, int targetY)
        {
            double dx = (targetX + 25) - (x + 25);
            double dy = (targetY + 25) - (y + 25);
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length < double.Epsilon)
            {
                return false;
            }

            dx /= length;
            dy /= length;
            double sideX = -dy;
            double sideY = dx;
            if (message.pauseSpace <= 0)
            {
                message.pauseSpace = GameTiming.ScaleLegacyTicks(36 + ranNumber.Next(0, 24));
                if (ranNumber.Next(0, 2) == 0)
                {
                    sideX = -sideX;
                    sideY = -sideY;
                }
                message.goToPlayer = sideX >= 0 ? 1 : 2;
            }
            else
            {
                message.pauseSpace--;
                if (message.goToPlayer == 2)
                {
                    sideX = -sideX;
                    sideY = -sideY;
                }
            }

            return TryMoveVectorAvoidingObstacles(mainWindow, boxes, tanks, sideX, sideY, false);
        }

        private bool TryPatrol(Form mainWindow, List<Box> boxes, List<BaseTank> tanks)
        {
            if (message.pauseTurn <= 0)
            {
                message.pauseTurn = GameTiming.ScaleLegacyTicks(55 + ranNumber.Next(0, 35));
                message.patrolAngleDegrees += ranNumber.Next(-90, 91);
            }
            else
            {
                message.pauseTurn--;
            }

            double radians = (message.patrolAngleDegrees - 90.0) * Math.PI / 180.0;
            double moveX = Math.Cos(radians);
            double moveY = Math.Sin(radians);
            if (!TryMoveVectorAvoidingObstacles(mainWindow, boxes, tanks, moveX, moveY, true))
            {
                message.patrolAngleDegrees += 120f;
                return false;
            }

            message.patrolAngleDegrees = RotationDegrees;
            return true;
        }

        public void Move(Form MainWindow, List<Box> boxes, List<Bullet> bullets, List<BaseTank> tanks)
        {
            if (hitpoints <= 0 || bullets == null || tanks == null)
            {
                return;
            }

            bool activeOnField = false;
            for (int i = 0; i < tanks.Count; i++)
            {
                if (ReferenceEquals(tanks[i], this))
                {
                    activeOnField = true;
                    break;
                }
            }

            if (!activeOnField)
            {
                return;
            }

            int targetX;
            int targetY;
            double distance;
            bool hasTarget = TryGetTargetPosition(out targetX, out targetY, out distance);
            bool inAggroRange = hasTarget && distance <= message.aggroRange;
            bool inAttackRange = hasTarget && distance <= message.attackRange;

            float aimRotation = RotationDegrees;
            if (hasTarget)
            {
                aimRotation = (float)(Math.Atan2((targetY + 25) - (y + 25), (targetX + 25) - (x + 25)) * 180.0 / Math.PI + 90.0);
            }

            bool moved = false;
            if (inAttackRange)
            {
                moved = TryStrafe(MainWindow, boxes, tanks, targetX, targetY);
                SetRotationDegrees(aimRotation);
                pictureTank = changePicture ? pictureTank2 : pictureTank1;
            }
            else if (inAggroRange)
            {
                moved = TryMoveTowards(MainWindow, boxes, tanks, targetX, targetY);
                SetRotationDegrees(aimRotation);
                pictureTank = changePicture ? pictureTank2 : pictureTank1;
            }
            else
            {
                moved = TryPatrol(MainWindow, boxes, tanks);
            }

            if (!moved && hasTarget && inAggroRange && ranNumber.Next(0, 100) < 18 + message.aggressionLevel * 8)
            {
                moved = TryMoveTowards(MainWindow, boxes, tanks, targetX, targetY);
                SetRotationDegrees(aimRotation);
                pictureTank = changePicture ? pictureTank2 : pictureTank1;
            }

            DateTime now = DateTime.UtcNow;
            double fireChance = 0.20 + message.aggressionLevel * 0.18;
            if (inAttackRange)
            {
                fireChance += 0.22;
            }
            if (message.isBoss)
            {
                fireChance += 0.20;
            }
            fireChance = Math.Min(0.95, fireChance);

            if (hasTarget && inAttackRange && now >= nextFireAvailableAtUtc && ranNumber.NextDouble() < fireChance)
            {
                SetRotationDegrees(aimRotation);
                pictureTank = changePicture ? pictureTank2 : pictureTank1;
                float fireRotation = RotationDegrees;
                if (message.isBoss)
                {
                    bool phaseTwo = maxHitpoints > 0 && hitpoints <= maxHitpoints / 2;
                    int bulletSize = phaseTwo ? 22 : 18;
                    float spread = phaseTwo ? 28f : 15f;
                    bullets.Add(new Bullet(ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan, x, y, fireRotation - spread, 0, attack, bulletSpeedMultiplier * 0.92, bulletSize));
                    bullets.Add(new Bullet(ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan, x, y, fireRotation, 0, attack, bulletSpeedMultiplier * 0.96, bulletSize));
                    bullets.Add(new Bullet(ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan, x, y, fireRotation + spread, 0, attack, bulletSpeedMultiplier * 0.92, bulletSize));
                    if (phaseTwo)
                    {
                        bullets.Add(new Bullet(ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan, x, y, fireRotation - 8f, 0, attack, bulletSpeedMultiplier * 1.0, 20));
                        bullets.Add(new Bullet(ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan, x, y, fireRotation + 8f, 0, attack, bulletSpeedMultiplier * 1.0, 20));
                    }
                    nextFireAvailableAtUtc = now.AddSeconds(phaseTwo ? Math.Max(0.28, cooldownSeconds * 0.62) : Math.Max(0.36, cooldownSeconds * 0.78));
                }
                else
                {
                    bullets.Add(new Bullet(ResourceManager.Bullet ?? Peace_of_Tanks.Properties.Resources.PaoDan, x, y, fireRotation, 0, attack, bulletSpeedMultiplier * 0.78, 14));
                    nextFireAvailableAtUtc = now.AddSeconds(cooldownSeconds * 0.9);
                }
            }
        }
    }
}
