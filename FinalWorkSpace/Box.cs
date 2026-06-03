using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Peace_of_Tanks
{
    public class Box
    {
        public int hitPoint;
        public int x, y;
        public Bitmap pictureBox;

        public Box(int x , int y)
        {
            this.x = x;
            this.y = y;
        }

        public virtual void Draw(Graphics grMemory)
        {
            grMemory.DrawImage(pictureBox, x, y, 25, 25);
        }
    }

    class Box_Wall : Box
    {
        public Box_Wall (int x, int y) : base(x, y)
        {
            hitPoint = 1;
            pictureBox = ResourceManager.TerrainWall ?? Peace_of_Tanks.Properties.Resources.Qiang;
        }
    }

    class Box_Water : Box
    {
        public Box_Water(int x, int y) : base(x, y)
        {
            hitPoint = -1;
            pictureBox = ResourceManager.TerrainWater ?? Peace_of_Tanks.Properties.Resources.Shui;
        }
    }

    class Box_Green : Box
    {
        public Box_Green(int x, int y) : base(x, y)
        {
            hitPoint = -1;
            pictureBox = ResourceManager.TerrainGrass ?? Peace_of_Tanks.Properties.Resources.Cao;
        }
    }

    class Box_Iron : Box
    {
        public Box_Iron(int x, int y) : base(x, y)
        {
            hitPoint = -1;
            pictureBox = ResourceManager.TerrainIron ?? Peace_of_Tanks.Properties.Resources.Tie;
        }
    }

    class Box_Base : Box
    {
        public Box_Base(int x, int y) : base(x, y)
        {
            hitPoint = 5;
            pictureBox = ResourceManager.TerrainBase ?? Peace_of_Tanks.Properties.Resources.base_tile;
        }

        protected void DrawBaseImage(Graphics grMemory)
        {
            grMemory.DrawImage(pictureBox, x, y, 25, 25);
        }

        protected void DrawBaseGlow(Graphics grMemory, Color color)
        {
            using (Pen outer = new Pen(color, 3))
            using (Pen inner = new Pen(Color.FromArgb(210, color.R, color.G, color.B), 1))
            {
                grMemory.DrawRectangle(outer, x - 2, y - 2, 28, 28);
                grMemory.DrawRectangle(inner, x, y, 24, 24);
            }
        }
    }

    class Box_PlayerBase : Box_Base
    {
        public Box_PlayerBase(int x, int y) : base(x, y)
        {
        }

        public override void Draw(Graphics grMemory)
        {
            DrawBaseGlow(grMemory, Color.FromArgb(120, 80, 235, 120));
            base.Draw(grMemory);
        }

    }

    class Box_EnemyBase : Box_Base
    {
        public Box_EnemyBase(int x, int y) : base(x, y)
        {
        }

        public override void Draw(Graphics grMemory)
        {
            DrawBaseGlow(grMemory, Color.FromArgb(135, 255, 82, 82));
            DrawBaseImage(grMemory);
        }
    }
}
