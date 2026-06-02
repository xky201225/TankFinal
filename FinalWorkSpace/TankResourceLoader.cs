using System;
using System.Drawing;

namespace Peace_of_Tanks
{
    public static class TankResourceLoader
    {
        public static (Bitmap, Bitmap) GetPlayerTankImages(TankType type)
        {
            return CreateAnimationFrames(GetTankResource(type));
        }

        public static (Bitmap, Bitmap) GetEnemyTankImages(TankType type)
        {
            if (type > TankType.Blue)
            {
                type = TankType.Green;
            }

            return CreateAnimationFrames(GetTankResource(type));
        }

        public static TankType GetRandomEnemyTankType()
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            int type = rand.Next(0, 3);
            return (TankType)type;
        }

        private static (Bitmap, Bitmap) CreateAnimationFrames(Bitmap source)
        {
            Bitmap frame1 = new Bitmap(source);
            Bitmap frame2 = new Bitmap(source);
            AdjustBrightness(frame2, 1.05f);
            return (frame1, frame2);
        }

        private static Bitmap GetTankResource(TankType type)
        {
            switch (type)
            {
                case TankType.Green:
                    return Peace_of_Tanks.Properties.Resources.Lv;
                case TankType.Yellow:
                    return Peace_of_Tanks.Properties.Resources.Huang;
                case TankType.Blue:
                    return Peace_of_Tanks.Properties.Resources.Lan;
                case TankType.Red:
                    return Peace_of_Tanks.Properties.Resources.Hong;
                case TankType.Purple:
                    return Peace_of_Tanks.Properties.Resources.Zi;
                default:
                    return Peace_of_Tanks.Properties.Resources.Lv;
            }
        }

        private static void AdjustBrightness(Bitmap bitmap, float factor)
        {
            try
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(
                        new float[][]
                        {
                            new float[] { factor, 0, 0, 0, 0 },
                            new float[] { 0, factor, 0, 0, 0 },
                            new float[] { 0, 0, factor, 0, 0 },
                            new float[] { 0, 0, 0, 1, 0 },
                            new float[] { 0, 0, 0, 0, 1 }
                        });

                    using (System.Drawing.Imaging.ImageAttributes attributes = new System.Drawing.Imaging.ImageAttributes())
                    {
                        attributes.SetColorMatrix(colorMatrix);
                        g.DrawImage(
                            bitmap,
                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            0,
                            0,
                            bitmap.Width,
                            bitmap.Height,
                            GraphicsUnit.Pixel,
                            attributes);
                    }
                }
            }
            catch
            {
            }
        }
    }
}
