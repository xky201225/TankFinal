using System;
using System.Collections.Generic;

namespace Peace_of_Tanks
{
    internal static class BattleMapFactory
    {
        public const int SystemMapCount = 6;
        public const int GridWidth = 32;
        public const int GridHeight = 32;
        public const int TileSize = 25;

        public static string GetSystemMapName(int index)
        {
            return "系统地图 " + (index + 1);
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

            AddFortifiedBaseTiles(map, 2, 15, true);
            AddFortifiedBaseTiles(map, GridWidth - 4, 15, false);
        }

        public static void ApplyMap(GameMapConfig map, List<Box> boxes)
        {
            ApplyMap(map, boxes, BattleRuleMode.FreeBattle);
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

            if (battleRule == BattleRuleMode.BaseDefense)
            {
                AddBaseDefenseSites(boxes);
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
            AddFortifiedBase(boxes, 2, 15, true);
            AddFortifiedBase(boxes, GridWidth - 4, 15, false);
        }

        private static void AddFortifiedBase(List<Box> boxes, int gridX, int gridY, bool playerBase)
        {
            RemoveArea(boxes, gridX - 2, gridY - 2, 5, 5);
            AddBox(boxes, playerBase ? (Box)new Box_PlayerBase(gridX * TileSize, gridY * TileSize) : new Box_EnemyBase(gridX * TileSize, gridY * TileSize));

            for (int yy = gridY - 1; yy <= gridY + 1; yy++)
            {
                for (int xx = gridX - 1; xx <= gridX + 1; xx++)
                {
                    if (xx == gridX && yy == gridY)
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
            ClearTiles(map, gridX - 2, gridY - 2, 5, 5);
            SetTile(map, gridX, gridY, MapTileType.Base);

            for (int yy = gridY - 1; yy <= gridY + 1; yy++)
            {
                for (int xx = gridX - 1; xx <= gridX + 1; xx++)
                {
                    if (xx == gridX && yy == gridY)
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
