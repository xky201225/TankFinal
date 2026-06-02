using System.Drawing;

namespace Peace_of_Tanks
{
    public static class ResourceManager
    {
        public static Bitmap LobbyBackground { get; private set; }
        public static Bitmap WarBackground { get; private set; }
        public static Bitmap MenuBackground { get; private set; }

        public static Bitmap TerrainGrass { get; private set; }
        public static Bitmap TerrainWall { get; private set; }
        public static Bitmap TerrainIron { get; private set; }
        public static Bitmap TerrainWater { get; private set; }

        public static Bitmap Bullet { get; private set; }

        public static Bitmap HitEffect1 { get; private set; }
        public static Bitmap HitEffect2 { get; private set; }
        public static Bitmap HitEffect3 { get; private set; }

        public static Bitmap ExplosionEffect1 { get; private set; }
        public static Bitmap ExplosionEffect2 { get; private set; }
        public static Bitmap ExplosionEffect3 { get; private set; }
        public static Bitmap ExplosionEffect4 { get; private set; }
        public static Bitmap ExplosionEffect5 { get; private set; }

        public static void Initialize()
        {
            LobbyBackground = Peace_of_Tanks.Properties.Resources.back;
            WarBackground = Peace_of_Tanks.Properties.Resources.war;
            MenuBackground = Peace_of_Tanks.Properties.Resources.jiazai;

            TerrainGrass = Peace_of_Tanks.Properties.Resources.Cao;
            TerrainWall = Peace_of_Tanks.Properties.Resources.Qiang;
            TerrainIron = Peace_of_Tanks.Properties.Resources.Tie;
            TerrainWater = Peace_of_Tanks.Properties.Resources.Shui;

            Bullet = Peace_of_Tanks.Properties.Resources.PaoDan;

            HitEffect1 = Peace_of_Tanks.Properties.Resources.Ji1;
            HitEffect2 = Peace_of_Tanks.Properties.Resources.Ji2;
            HitEffect3 = Peace_of_Tanks.Properties.Resources.Ji3;

            ExplosionEffect1 = Peace_of_Tanks.Properties.Resources.Bao1;
            ExplosionEffect2 = Peace_of_Tanks.Properties.Resources.Bao2;
            ExplosionEffect3 = Peace_of_Tanks.Properties.Resources.Bao3;
            ExplosionEffect4 = Peace_of_Tanks.Properties.Resources.Bao4;
            ExplosionEffect5 = Peace_of_Tanks.Properties.Resources.Bao5;
        }

        public static Bitmap GetHitEffectFrame(int frameIndex)
        {
            switch (frameIndex)
            {
                case 0: return HitEffect1;
                case 1: return HitEffect2;
                case 2: return HitEffect3;
                default: return HitEffect1;
            }
        }

        public static Bitmap GetExplosionEffectFrame(int frameIndex)
        {
            switch (frameIndex)
            {
                case 0: return ExplosionEffect1;
                case 1: return ExplosionEffect2;
                case 2: return ExplosionEffect3;
                case 3: return ExplosionEffect4;
                case 4: return ExplosionEffect5;
                default: return ExplosionEffect1;
            }
        }
    }
}
