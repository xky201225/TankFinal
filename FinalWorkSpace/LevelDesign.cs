using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    class LevelDesign
    {
        public int[,] masPointsSpawnEnemy = new int[4, 2];
        public int[,] masPointsSpawnPlayers = new int[4, 2];

        public LevelDesign()
        {
            for (int i = 0; i < 4; i++)
            {
                masPointsSpawnEnemy[i, 0] = new int();
                masPointsSpawnEnemy[i, 1] = new int();
            }
            for (int i = 0; i < 2; i++)
            {
                masPointsSpawnPlayers[i, 0] = new int();
                masPointsSpawnPlayers[i, 1] = new int();
            }
        }

        public void SetDefaultSpawnPoints()
        {
            masPointsSpawnEnemy[0, 0] = 50;
            masPointsSpawnEnemy[0, 1] = 380;
            masPointsSpawnEnemy[1, 0] = 700;
            masPointsSpawnEnemy[1, 1] = 380;
            masPointsSpawnEnemy[2, 0] = 380;
            masPointsSpawnEnemy[2, 1] = 50;
            masPointsSpawnEnemy[3, 0] = 380;
            masPointsSpawnEnemy[3, 1] = 700;

            masPointsSpawnPlayers[0, 0] = 50;
            masPointsSpawnPlayers[0, 1] = 50;
            masPointsSpawnPlayers[1, 0] = 700;
            masPointsSpawnPlayers[1, 1] = 50;
            masPointsSpawnPlayers[2, 0] = 50;
            masPointsSpawnPlayers[2, 1] = 700;
            masPointsSpawnPlayers[3, 0] = 700;
            masPointsSpawnPlayers[3, 1] = 700;
        }

        public void Level1(Form MW, List<Box> boxes)
        {
            // 战场规模扩大到原来的3倍
            SetDefaultSpawnPoints();

            boxes.Clear();

            int y = 225;  // 75 * 3
            int x = 300;  // 100 * 3

            Box box = new Box_Wall(x, y);
            for (x = 300; x <= 2100; x += 25)
            {
                if(x < 1050 || x > 1350)
                {
                    box = new Box_Wall(x, y);
                    boxes.Add(box);
                    box = new Box_Wall(x, y + 25);
                    boxes.Add(box);
                }
            }
            y = 1950;  // 650 * 3
            for (x = 300; x <= 2100; x += 25)
            {
                if (x < 1050 || x > 1350)
                {
                    box = new Box_Wall(x, y);
                    boxes.Add(box);
                    box = new Box_Wall(x, y + 25);
                    boxes.Add(box);
                }
            }
            x = 300;  // 100 * 3
            int x2 = 2025;  // 675 * 3
            for (y = 375; y < 2025; y += 25)  // 125 * 3 到 675 * 3
            {
                if (y < 1050 || y > 1350)
                {
                    box = new Box_Wall(x, y);
                    boxes.Add(box);
                    box = new Box_Wall(x+25, y);
                    boxes.Add(box);
                    box = new Box_Wall(x2, y);
                    boxes.Add(box);
                    box = new Box_Wall(x2 + 25, y);
                    boxes.Add(box);
                }
            }

            for (x = 900; x <= 1500; x += 25)  // 300 * 3 到 500 * 3
            {
                for(y = 750; y <= 1500; y += 25)  // 250 * 3 到 500 * 3
                {
                    box = new Box_Water(x, y);
                    boxes.Add(box);
                }
            }

            x = 600;  // 200 * 3
            for(y = 600; y < 1500; y += 25)  // 200 * 3 到 500 * 3
            {
                box = new Box_Green(x, y);
                boxes.Add(box);
                box = new Box_Green(x + 25, y);
                boxes.Add(box);
            }

            x = 1725;  // 575 * 3
            for (y = 600; y < 1500; y += 25)  // 200 * 3 到 500 * 3
            {
                box = new Box_Iron(x, y);
                boxes.Add(box);
                box = new Box_Iron(x + 25, y);
                boxes.Add(box);
            }
        }
    }
}
