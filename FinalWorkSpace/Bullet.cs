using System;
using System.Drawing;

namespace Peace_of_Tanks
{
    public class Bullet
    {
        private const double Speed = 62.5;
        private readonly Bitmap pictureBullet;
        private double preciseX;
        private double preciseY;
        private readonly int direction;

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

        public Bullet(Bitmap picture, int x, int y, int direction, int who)
        {
            if (ResourceManager.Bullet != null && ResourceManager.Bullet.Width > 1)
            {
                pictureBullet = new Bitmap(ResourceManager.Bullet);
            }
            else
            {
                pictureBullet = Peace_of_Tanks.Properties.Resources.bullet1;
                pictureBullet.MakeTransparent(Color.Black);
            }

            preciseX = x;
            preciseY = y;
            this.direction = direction;
            this.who = who;
            ChangePosition();
            RotatePicture(direction);
        }

        public Bullet(Bitmap picture, int x, int y, int direction, int who, int damage) : this(picture, x, y, direction, who)
        {
            this.damage = damage;
        }

        private void ChangePosition()
        {
            if (direction == 0)
            {
                preciseX -= 20;
                preciseY += 20;
            }
            if (direction == 1)
            {
                preciseX += 60;
                preciseY += 20;
            }
            if (direction == 2)
            {
                preciseX += 20;
                preciseY -= 20;
            }
            if (direction == 3)
            {
                preciseX += 20;
                preciseY += 60;
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
            if (direction == 0)
            {
                preciseX -= Speed;
            }
            if (direction == 2)
            {
                preciseY -= Speed;
            }
            if (direction == 1)
            {
                preciseX += Speed;
            }
            if (direction == 3)
            {
                preciseY += Speed;
            }
        }

        public void Draw(Graphics grMemory)
        {
            grMemory.DrawImage(pictureBullet, x, y, 10, 10);
        }
    }
}
