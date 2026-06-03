using System;
using System.Drawing;

namespace Peace_of_Tanks
{
    public class Bullet
    {
        private const int BulletSize = 10;
        private const int TankSize = 50;
        private const int TankCenterOffset = (TankSize - BulletSize) / 2;
        private static readonly double BaseSpeed = GameTiming.ScaleDistance(125);
        private const double CollisionStep = 5.0;
        private readonly Bitmap pictureBullet;
        private double preciseX;
        private double preciseY;
        private readonly int direction;
        private readonly double speed;

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
            get { return direction; }
        }

        public int MoveStepCount
        {
            get { return (int)Math.Ceiling(speed / CollisionStep); }
        }

        public Bullet(Bitmap picture, int x, int y, int direction, int who) : this(picture, x, y, direction, who, 1, 1.0)
        {
        }

        public Bullet(Bitmap picture, int x, int y, int direction, int who, int damage) : this(picture, x, y, direction, who, damage, 1.0)
        {
        }

        public Bullet(Bitmap picture, int x, int y, int direction, int who, int damage, double speedMultiplier)
        {
            if (ResourceManager.Bullet != null && ResourceManager.Bullet.Width > 1)
            {
                pictureBullet = new Bitmap(ResourceManager.Bullet);
            }
            else
            {
                pictureBullet = Peace_of_Tanks.Properties.Resources.PaoDan;
                pictureBullet.MakeTransparent(Color.Black);
            }

            preciseX = x;
            preciseY = y;
            speed = BaseSpeed * Math.Max(0.1, speedMultiplier);
            this.direction = direction;
            this.who = who;
            this.damage = damage;
            ChangePosition();
            RotatePicture(direction);
        }

        private void ChangePosition()
        {
            if (direction == 0)
            {
                preciseX -= BulletSize;
                preciseY += TankCenterOffset;
            }
            if (direction == 1)
            {
                preciseX += TankSize;
                preciseY += TankCenterOffset;
            }
            if (direction == 2)
            {
                preciseX += TankCenterOffset;
                preciseY -= BulletSize;
            }
            if (direction == 3)
            {
                preciseX += TankCenterOffset;
                preciseY += TankSize;
            }
        }

        private void RotatePicture(int direct)
        {
            if (direct == 0) pictureBullet.RotateFlip(RotateFlipType.Rotate90FlipX);
            if (direct == 3) pictureBullet.RotateFlip(RotateFlipType.Rotate180FlipNone);
            if (direct == 1) pictureBullet.RotateFlip(RotateFlipType.Rotate90FlipNone);
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
            if (direction == 0)
            {
                preciseX -= distance;
            }
            if (direction == 2)
            {
                preciseY -= distance;
            }
            if (direction == 1)
            {
                preciseX += distance;
            }
            if (direction == 3)
            {
                preciseY += distance;
            }
        }

        public void Draw(Graphics grMemory)
        {
            grMemory.DrawImage(pictureBullet, x, y, BulletSize, BulletSize);
        }
    }
}
