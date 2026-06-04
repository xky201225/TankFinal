using System;
using System.Drawing;

namespace Peace_of_Tanks
{
    public class Bullet
    {
        private const int BulletSize = 10;
        private const int TankSize = 50;
        private const double SpawnDistance = (TankSize + BulletSize) / 2.0;
        private static readonly double BaseSpeed = GameTiming.ScaleDistance(125);
        private const double CollisionStep = 5.0;
        private static bool fallbackBulletTransparencyApplied;
        private readonly Bitmap pictureBullet;
        private double preciseX;
        private double preciseY;
        private readonly float rotationDegrees;
        private readonly double speed;
        private readonly double directionX;
        private readonly double directionY;
        private readonly int drawSize;

        public int who;
        public int damage = 1;

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

        public int Direction
        {
            get { return RotationDegreesToLegacyDirection(rotationDegrees); }
        }

        public float RotationDegrees
        {
            get { return rotationDegrees; }
        }

        public int MoveStepCount
        {
            get { return (int)Math.Ceiling(speed / CollisionStep); }
        }

        public int DrawSize
        {
            get { return drawSize; }
        }

        public Bullet(Bitmap picture, int x, int y, int direction, int who) : this(picture, x, y, direction, who, 1, 1.0)
        {
        }

        public Bullet(Bitmap picture, int x, int y, int direction, int who, int damage) : this(picture, x, y, direction, who, damage, 1.0)
        {
        }

        public Bullet(Bitmap picture, int x, int y, int direction, int who, int damage, double speedMultiplier)
            : this(picture, x, y, DirectionToRotationDegrees(direction), who, damage, speedMultiplier, BulletSize)
        {
        }

        public Bullet(Bitmap picture, int x, int y, float rotationDegrees, int who, int damage, double speedMultiplier)
            : this(picture, x, y, rotationDegrees, who, damage, speedMultiplier, BulletSize)
        {
        }

        public Bullet(Bitmap picture, int x, int y, float rotationDegrees, int who, int damage, double speedMultiplier, int drawSize)
        {
            if (ResourceManager.Bullet != null && ResourceManager.Bullet.Width > 1)
            {
                pictureBullet = ResourceManager.Bullet;
            }
            else
            {
                pictureBullet = picture ?? Peace_of_Tanks.Properties.Resources.PaoDan;
                if (!fallbackBulletTransparencyApplied)
                {
                    pictureBullet.MakeTransparent(Color.Black);
                    fallbackBulletTransparencyApplied = true;
                }
            }

            preciseX = x;
            preciseY = y;
            this.drawSize = Math.Max(BulletSize, drawSize);
            speed = BaseSpeed * Math.Max(0.1, speedMultiplier);
            this.rotationDegrees = NormalizeAngle(rotationDegrees);
            GetDirectionVector(this.rotationDegrees, out directionX, out directionY);
            this.who = who;
            this.damage = damage;
            ChangePosition();
        }

        private void ChangePosition()
        {
            double centerX = preciseX + TankSize / 2.0 - drawSize / 2.0;
            double centerY = preciseY + TankSize / 2.0 - drawSize / 2.0;
            preciseX = centerX + directionX * SpawnDistance;
            preciseY = centerY + directionY * SpawnDistance;
        }

        public void Move()
        {
            for (int i = 0; i < MoveStepCount; i++)
            {
                MoveStep(i);
            }
        }

        public void MoveStep(int stepIndex)
        {
            double distance = Math.Min(CollisionStep, speed - CollisionStep * stepIndex);
            if (distance <= 0)
            {
                return;
            }

            MoveBy(distance);
        }

        private void MoveBy(double distance)
        {
            preciseX += directionX * distance;
            preciseY += directionY * distance;
        }

        public void Draw(Graphics grMemory)
        {
            var state = grMemory.Save();
            grMemory.TranslateTransform(x + drawSize / 2f, y + drawSize / 2f);
            grMemory.RotateTransform(rotationDegrees);
            grMemory.DrawImage(pictureBullet, -drawSize / 2f, -drawSize / 2f, drawSize, drawSize);
            grMemory.Restore(state);
        }

        private static float DirectionToRotationDegrees(int direction)
        {
            switch (direction)
            {
                case 0: return 270f;
                case 1: return 90f;
                case 3: return 180f;
                default: return 0f;
            }
        }

        private static int RotationDegreesToLegacyDirection(float angleDegrees)
        {
            angleDegrees = NormalizeAngle(angleDegrees);
            if (angleDegrees >= 315f || angleDegrees < 45f) return 2;
            if (angleDegrees < 135f) return 1;
            if (angleDegrees < 225f) return 3;
            return 0;
        }

        private static float NormalizeAngle(float angleDegrees)
        {
            angleDegrees %= 360f;
            if (angleDegrees < 0f)
            {
                angleDegrees += 360f;
            }

            return angleDegrees;
        }

        private static void GetDirectionVector(float angleDegrees, out double dx, out double dy)
        {
            double radians = (angleDegrees - 90.0) * Math.PI / 180.0;
            dx = Math.Cos(radians);
            dy = Math.Sin(radians);
        }
    }
}
