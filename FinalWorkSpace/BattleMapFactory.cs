using System;
using System.Collections.Generic;

namespace Peace_of_Tanks
{
    internal static class BattleMapFactory
    {
        public const int SystemMapCount = 6;
        public const int StageMapCount = 6;
        public const int GridWidth = 32;
        public const int GridHeight = 32;
        public const int TileSize = 25;

        public static string GetSystemMapName(int index)
        {
            return "系统地图 " + (index + 1);
        }

        public static string GetStageMapName(int stage)
        {
            switch (stage)
            {
                case 1: return "废墟前线";
                case 2: return "钢铁水坝";
                case 3: return "要塞核心";
                case 4: return "林地迷洲";
                case 5: return "烈火峡口";
                case 6: return "终极地堡";
                default: return "废墟前线";
            }
        }

        public static GameMapConfig CreateStageMap(int stage)
        {
            GameMapConfig map = CreateEmpty(GetStageMapName(stage));
            switch (stage)
            {
                case 2:
                    AddRect(map, MapTileType.Wall, 3, 4, 10, 2);
                    AddRect(map, MapTileType.Wall, 19, 4, 10, 2);
                    AddRect(map, MapTileType.Water, 12, 7, 8, 6);
                    AddRect(map, MapTileType.Iron, 4, 16, 3, 10);
                    AddRect(map, MapTileType.Iron, 25, 16, 3, 10);
                    AddRect(map, MapTileType.Grass, 10, 18, 4, 8);
                    AddRect(map, MapTileType.Grass, 18, 18, 4, 8);
                    AddRect(map, MapTileType.Wall, 11, 27, 10, 2);
                    break;
                case 3:
                    AddRect(map, MapTileType.Iron, 4, 4, 24, 2);
                    AddRect(map, MapTileType.Iron, 4, 26, 24, 2);
                    AddRect(map, MapTileType.Wall, 4, 8, 2, 16);
                    AddRect(map, MapTileType.Wall, 26, 8, 2, 16);
                    AddRect(map, MapTileType.Water, 12, 10, 8, 3);
                    AddRect(map, MapTileType.Water, 12, 19, 8, 3);
                    AddRect(map, MapTileType.Wall, 10, 14, 12, 4);
                    AddRect(map, MapTileType.Grass, 7, 11, 3, 10);
                    AddRect(map, MapTileType.Grass, 22, 11, 3, 10);
                    AddRect(map, MapTileType.Base, 15, 15, 2, 2);
                    break;
                case 4:
                    AddRect(map, MapTileType.Grass, 2, 3, 8, 7);
                    AddRect(map, MapTileType.Grass, 22, 3, 8, 7);
                    AddRect(map, MapTileType.Grass, 2, 22, 8, 7);
                    AddRect(map, MapTileType.Grass, 22, 22, 8, 7);
                    AddRect(map, MapTileType.Water, 13, 3, 6, 8);
                    AddRect(map, MapTileType.Water, 13, 21, 6, 8);
                    AddRect(map, MapTileType.Wall, 7, 13, 18, 2);
                    AddRect(map, MapTileType.Wall, 7, 17, 18, 2);
                    AddRect(map, MapTileType.Iron, 14, 14, 4, 4);
                    break;
                case 5:
                    AddRect(map, MapTileType.Wall, 3, 3, 26, 1);
                    AddRect(map, MapTileType.Wall, 3, 28, 26, 1);
                    AddRect(map, MapTileType.Wall, 3, 3, 1, 26);
                    AddRect(map, MapTileType.Wall, 28, 3, 1, 26);
                    AddRect(map, MapTileType.Water, 6, 6, 5, 5);
                    AddRect(map, MapTileType.Water, 21, 21, 5, 5);
                    AddRect(map, MapTileType.Iron, 13, 7, 2, 18);
                    AddRect(map, MapTileType.Iron, 17, 7, 2, 18);
                    AddRect(map, MapTileType.Grass, 7, 16, 6, 6);
                    AddRect(map, MapTileType.Grass, 19, 10, 6, 6);
                    AddRect(map, MapTileType.Base, 15, 15, 2, 2);
                    break;
                case 6:
                    AddRect(map, MapTileType.Iron, 5, 5, 22, 1);
                    AddRect(map, MapTileType.Iron, 5, 26, 22, 1);
                    AddRect(map, MapTileType.Iron, 5, 5, 1, 22);
                    AddRect(map, MapTileType.Iron, 26, 5, 1, 22);
                    AddRect(map, MapTileType.Wall, 9, 9, 14, 2);
                    AddRect(map, MapTileType.Wall, 9, 21, 14, 2);
                    AddRect(map, MapTileType.Wall, 9, 11, 2, 10);
                    AddRect(map, MapTileType.Wall, 21, 11, 2, 10);
                    AddRect(map, MapTileType.Water, 13, 13, 6, 2);
                    AddRect(map, MapTileType.Water, 13, 18, 6, 2);
                    AddRect(map, MapTileType.Grass, 3, 13, 5, 6);
                    AddRect(map, MapTileType.Grass, 24, 13, 5, 6);
                    AddRect(map, MapTileType.Base, 15, 15, 2, 2);
                    break;
                default:
                    AddRect(map, MapTileType.Wall, 3, 5, 8, 2);
                    AddRect(map, MapTileType.Wall, 21, 5, 8, 2);
                    AddRect(map, MapTileType.Grass, 5, 12, 4, 8);
                    AddRect(map, MapTileType.Grass, 23, 12, 4, 8);
                    AddRect(map, MapTileType.Water, 12, 13, 8, 4);
                    AddRect(map, MapTileType.Wall, 12, 22, 8, 2);
                    AddRect(map, MapTileType.Iron, 14, 7, 4, 3);
                    break;
            }

            return map;
        }

        public static GameMapConfig CreateSystemMap(int index)
        {
            GameMapConfig map = CreateEmpty(GetSystemMapName(index));
            switch (index)
            {
                case 1:
                    AddRect(map, MapTileType.Wall, 4, 4, 24, 1);
                    AddRect(map, MapTileType.Wall, 4, 27, 24, 1);
                    AddRect(map, MapTileType.Grass, 7, 8, 1, 14);
                    AddRect(map, MapTileType.Iron, 24, 8, 1, 14);
                    AddRect(map, MapTileType.Water, 13, 11, 6, 8);
                    break;
                case 2:
                    AddRect(map, MapTileType.Wall, 6, 7, 3, 15);
                    AddRect(map, MapTileType.Wall, 23, 10, 3, 15);
                    AddRect(map, MapTileType.Iron, 13, 13, 7, 2);
                    AddRect(map, MapTileType.Water, 14, 17, 4, 4);
                    AddRect(map, MapTileType.Grass, 10, 6, 12, 2);
                    break;
                case 3:
                    AddRect(map, MapTileType.Water, 3, 3, 7, 7);
                    AddRect(map, MapTileType.Water, 22, 22, 7, 7);
                    AddRect(map, MapTileType.Wall, 11, 11, 10, 2);
                    AddRect(map, MapTileType.Wall, 11, 20, 10, 2);
                    AddRect(map, MapTileType.Grass, 15, 4, 3, 24);
                    break;
                case 4:
                    AddRect(map, MapTileType.Iron, 6, 6, 2, 20);
                    AddRect(map, MapTileType.Iron, 24, 6, 2, 20);
                    AddRect(map, MapTileType.Wall, 11, 9, 10, 3);
                    AddRect(map, MapTileType.Wall, 11, 20, 10, 3);
                    AddRect(map, MapTileType.Base, 15, 15, 2, 2);
                    break;
                case 5:
                    AddRect(map, MapTileType.Wall, 3, 3, 10, 1);
                    AddRect(map, MapTileType.Wall, 19, 28, 10, 1);
                    AddRect(map, MapTileType.Wall, 3, 19, 1, 10);
                    AddRect(map, MapTileType.Wall, 28, 3, 1, 10);
                    AddRect(map, MapTileType.Iron, 15, 6, 2, 20);
                    AddRect(map, MapTileType.Iron, 6, 15, 20, 2);
                    AddRect(map, MapTileType.Water, 8, 8, 4, 4);
                    AddRect(map, MapTileType.Water, 20, 20, 4, 4);
                    AddRect(map, MapTileType.Grass, 6, 21, 6, 5);
                    AddRect(map, MapTileType.Grass, 21, 6, 5, 6);
                    AddRect(map, MapTileType.Base, 15, 15, 2, 2);
                    break;
                default:
                    AddRect(map, MapTileType.Wall, 4, 4, 24, 1);
                    AddRect(map, MapTileType.Wall, 4, 27, 24, 1);
                    AddRect(map, MapTileType.Wall, 4, 6, 1, 21);
                    AddRect(map, MapTileType.Wall, 27, 6, 1, 21);
                    AddRect(map, MapTileType.Water, 12, 10, 8, 10);
                    AddRect(map, MapTileType.Grass, 8, 8, 1, 16);
                    AddRect(map, MapTileType.Iron, 23, 8, 1, 16);
                    break;
            }

            return map;
        }

        public static GameMapConfig CreateEmpty(string name)
        {
            return new GameMapConfig
            {
                Name = name,
                Width = GridWidth,
                Height = GridHeight,
                Tiles = new MapTileType[GridWidth * GridHeight]
            };
        }

        public static GameMapConfig CloneMap(GameMapConfig source)
        {
            GameMapConfig clone = CreateEmpty(source != null ? source.Name : string.Empty);
            if (source == null || source.Tiles == null)
            {
                return clone;
            }

            clone.Width = source.Width;
            clone.Height = source.Height;
            clone.Tiles = new MapTileType[source.Tiles.Length];
            Array.Copy(source.Tiles, clone.Tiles, source.Tiles.Length);
            return clone;
        }

        public static void AddBaseDefenseTiles(GameMapConfig map)
        {
            if (map == null || map.Tiles == null)
            {
                return;
            }

            AddFortifiedBaseTiles(map, 2, 8, true);
            AddFortifiedBaseTiles(map, GridWidth - 4, GridHeight - 10, false);
        }

        public static void ApplyMap(GameMapConfig map, List<Box> boxes)
        {
            ApplyMap(map, boxes, BattleRuleMode.Versus);
        }

        public static void ApplyMap(GameMapConfig map, List<Box> boxes, BattleRuleMode battleRule)
        {
            boxes.Clear();
            if (map == null || map.Tiles == null)
            {
                return;
            }

            int width = Math.Min(map.Width, GridWidth);
            int height = Math.Min(map.Height, GridHeight);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * map.Width + x;
                    if (index < 0 || index >= map.Tiles.Length)
                    {
                        continue;
                    }

                    Box box = CreateBox(map.Tiles[index], x * TileSize, y * TileSize);
                    if (box != null)
                    {
                        boxes.Add(box);
                    }
                }
            }

        }

        public static MapTileType GetTile(GameMapConfig map, int x, int y)
        {
            if (map == null || map.Tiles == null || x < 0 || y < 0 || x >= map.Width || y >= map.Height)
            {
                return MapTileType.Empty;
            }

            return map.Tiles[y * map.Width + x];
        }

        public static void SetTile(GameMapConfig map, int x, int y, MapTileType tile)
        {
            if (map == null || map.Tiles == null || x < 0 || y < 0 || x >= map.Width || y >= map.Height)
            {
                return;
            }

            map.Tiles[y * map.Width + x] = tile;
        }

        private static Box CreateBox(MapTileType type, int x, int y)
        {
            switch (type)
            {
                case MapTileType.Wall: return new Box_Wall(x, y);
                case MapTileType.Water: return new Box_Water(x, y);
                case MapTileType.Grass: return new Box_Green(x, y);
                case MapTileType.Iron: return new Box_Iron(x, y);
                case MapTileType.Base: return new Box_Base(x, y);
                default: return null;
            }
        }

        private static void AddBaseDefenseSites(List<Box> boxes)
        {
            AddFortifiedBase(boxes, 2, 8, true);
            AddFortifiedBase(boxes, GridWidth - 4, GridHeight - 10, false);
        }

        private static void AddFortifiedBase(List<Box> boxes, int gridX, int gridY, bool playerBase)
        {
            RemoveArea(boxes, gridX - 2, gridY - 2, 6, 6);
            AddBaseCluster(boxes, gridX, gridY, playerBase);

            for (int yy = gridY - 1; yy <= gridY + 2; yy++)
            {
                for (int xx = gridX - 1; xx <= gridX + 2; xx++)
                {
                    if (xx >= gridX && xx <= gridX + 1 && yy >= gridY && yy <= gridY + 1)
                    {
                        continue;
                    }

                    if (playerBase && xx < gridX)
                    {
                        continue;
                    }

                    if (!playerBase && xx > gridX)
                    {
                        continue;
                    }

                    AddBox(boxes, new Box_Wall(xx * TileSize, yy * TileSize));
                }
            }
        }

        private static void AddFortifiedBaseTiles(GameMapConfig map, int gridX, int gridY, bool playerBase)
        {
            ClearTiles(map, gridX - 2, gridY - 2, 6, 6);
            for (int yy = gridY; yy <= gridY + 1; yy++)
            {
                for (int xx = gridX; xx <= gridX + 1; xx++)
                {
                    SetTile(map, xx, yy, MapTileType.Base);
                }
            }

            for (int yy = gridY - 1; yy <= gridY + 2; yy++)
            {
                for (int xx = gridX - 1; xx <= gridX + 2; xx++)
                {
                    if (xx >= gridX && xx <= gridX + 1 && yy >= gridY && yy <= gridY + 1)
                    {
                        continue;
                    }

                    if (playerBase && xx < gridX)
                    {
                        continue;
                    }

                    if (!playerBase && xx > gridX)
                    {
                        continue;
                    }

                    SetTile(map, xx, yy, MapTileType.Wall);
                }
            }
        }

        private static void AddBaseCluster(List<Box> boxes, int gridX, int gridY, bool playerBase)
        {
            for (int yy = gridY; yy <= gridY + 1; yy++)
            {
                for (int xx = gridX; xx <= gridX + 1; xx++)
                {
                    AddBox(boxes, playerBase ? (Box)new Box_PlayerBase(xx * TileSize, yy * TileSize) : new Box_EnemyBase(xx * TileSize, yy * TileSize));
                }
            }
        }

        private static void ClearTiles(GameMapConfig map, int gridX, int gridY, int width, int height)
        {
            for (int yy = gridY; yy < gridY + height; yy++)
            {
                for (int xx = gridX; xx < gridX + width; xx++)
                {
                    SetTile(map, xx, yy, MapTileType.Empty);
                }
            }
        }

        private static void RemoveArea(List<Box> boxes, int gridX, int gridY, int width, int height)
        {
            int left = gridX * TileSize;
            int top = gridY * TileSize;
            int right = (gridX + width) * TileSize;
            int bottom = (gridY + height) * TileSize;
            for (int i = boxes.Count - 1; i >= 0; i--)
            {
                if (boxes[i].x >= left && boxes[i].x < right && boxes[i].y >= top && boxes[i].y < bottom)
                {
                    boxes.RemoveAt(i);
                }
            }
        }

        private static void AddBox(List<Box> boxes, Box box)
        {
            if (box != null)
            {
                boxes.Add(box);
            }
        }

        private static void AddRect(GameMapConfig map, MapTileType type, int x, int y, int width, int height)
        {
            for (int yy = y; yy < y + height; yy++)
            {
                for (int xx = x; xx < x + width; xx++)
                {
                    SetTile(map, xx, yy, type);
                }
            }
        }
    }
}
