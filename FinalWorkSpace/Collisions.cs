using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    class Collisions
    {
        public const int BattlefieldWidth = 1000;
        public const int BattlefieldHeight = 740;

        private static Rectangle GetBattlefieldBounds(Form mainWindow)
        {
            if (mainWindow == null)
            {
                return new Rectangle(0, 0, BattlefieldWidth, BattlefieldHeight);
            }

            return new Rectangle(
                mainWindow.ClientRectangle.Left,
                mainWindow.ClientRectangle.Top,
                Math.Min(mainWindow.ClientRectangle.Width, BattlefieldWidth),
                Math.Min(mainWindow.ClientRectangle.Height, BattlefieldHeight));
        }

        public static bool check_touch(Form mainWindow, Bullet bullet)
        {
            int s_bullet = 10;
            Rectangle bounds = GetBattlefieldBounds(mainWindow);
            return (bullet.x <= bounds.Left)
                || (bullet.y <= bounds.Top)
                || (bullet.x + s_bullet >= bounds.Right)
                || (bullet.y + s_bullet >= bounds.Bottom);
        }

        public static int check_touch(Form mainWindow, Bullet bullet, Box box)
        {
            int s_bullet = 10, s_box = 25;
            if (check_touch(mainWindow, bullet))
            {
                return 1;//касание  границ
            }
            else
                if (((bullet.x < box.x && bullet.x + s_bullet > box.x) || (bullet.x >= box.x && box.x + s_box > bullet.x))
                && ((bullet.y < box.y && bullet.y + s_bullet > box.y) || (bullet.y >= box.y && box.y + s_box > bullet.y))
                && !(box is Box_Water) && !(box is Box_Green))
                return 2; //касание бокса
            else
                return 0;
        }
        public static bool check_touch(Bullet bullet, BaseTank tank)
        {
            int s_bullet = 10, s_tank = 50;
            if (((bullet.x < tank.x && bullet.x + s_bullet > tank.x) || (bullet.x > tank.x && tank.x + s_tank > bullet.x)) &&
                ((bullet.y < tank.y && bullet.y + s_bullet > tank.y) || (bullet.y > tank.y && tank.y + s_tank > bullet.y)))
            {
                return true;
            }
            else return false;
        }
        public static bool check_touch(int x, int y, Box box, Form MainWindow)
        {
            int s_box = 25, s_tank = 50;
            Rectangle bounds = GetBattlefieldBounds(MainWindow);
            if ( ((x < box.x && x + s_tank > box.x)||(x >= box.x && box.x + s_box > x)) && 
                ((y < box.y && y + s_tank > box.y)||( y >= box.y && box.y + s_box > y)) && 
                !(box is Box_Green)
                
                || (x < bounds.Left) || (y < bounds.Top) || (x + s_tank > bounds.Right) || 
                (y + s_tank > bounds.Bottom))
                {
                    return true;
                }
            else return false;
        }
        public static bool check_touch(int x1, int y1, int x2, int y2)
        {
            int s_tank = 50;
            if (((x1 < x2 && x1 + s_tank > x2) || (x1 >= x2 && x2 + s_tank > x1)) &&
                ((y1 < y2 && y1 + s_tank > y2) || (y1 >= y2 && y2 + s_tank > y1)))
            {
                return true;
            }
            else return false;
        }
    }
}
