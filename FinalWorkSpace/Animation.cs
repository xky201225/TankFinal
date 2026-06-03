using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace Peace_of_Tanks
{
    public class AnimateEventArgs : EventArgs
    {
        public int x, y;
        public int type; // 0 - анимация разрушения ящика, 1 - анимания уничтожения танка
        public int delay;

        public AnimateEventArgs() : base()
        { }
    }

    public delegate void AnimateEvent();

    public class Animation
    {
        public AnimateEventArgs message;
        public Bitmap picture;

        public Animation()
        {
            message = new AnimateEventArgs();
        }

        public void Delay()
        {
            if (message.type == 0)
            {
                if (message.delay >= 3) picture = ResourceManager.HitEffect1 ?? Peace_of_Tanks.Properties.Resources.Ji1;
                else if (message.delay == 2) picture = ResourceManager.HitEffect2 ?? Peace_of_Tanks.Properties.Resources.Ji2;
                else if (message.delay == 1) picture = ResourceManager.HitEffect3 ?? Peace_of_Tanks.Properties.Resources.Ji3;
                message.delay--;
            }
            else if (message.type == 1)
            {
                if (message.delay >= 5) picture = ResourceManager.ExplosionEffect1 ?? Peace_of_Tanks.Properties.Resources.Bao1;
                else if (message.delay == 4) picture = ResourceManager.ExplosionEffect2 ?? Peace_of_Tanks.Properties.Resources.Bao2;
                else if (message.delay == 3) picture = ResourceManager.ExplosionEffect3 ?? Peace_of_Tanks.Properties.Resources.Bao3;
                else if (message.delay == 2) picture = ResourceManager.ExplosionEffect4 ?? Peace_of_Tanks.Properties.Resources.Bao4;
                else if (message.delay == 1) picture = ResourceManager.ExplosionEffect5 ?? Peace_of_Tanks.Properties.Resources.Bao5;
                message.delay--;
            }
        }

        public void Draw(Graphics grMemory)
        {
            if (picture == null) return;
            if (message.type == 0)
                grMemory.DrawImage(picture, message.x - 10, message.y - 10, 60, 60);  // 击中效果
            else if (message.type == 1)
                grMemory.DrawImage(picture, message.x - 30, message.y - 30, 120, 120);  // 爆炸效果
        }

        public static Bitmap GetFrameImage(int type, int delay)
        {
            if (type == 0)
            {
                if (delay >= 3) return ResourceManager.HitEffect1 ?? Peace_of_Tanks.Properties.Resources.Ji1;
                if (delay == 2) return ResourceManager.HitEffect2 ?? Peace_of_Tanks.Properties.Resources.Ji2;
                return ResourceManager.HitEffect3 ?? Peace_of_Tanks.Properties.Resources.Ji3;
            }

            if (delay >= 5) return ResourceManager.ExplosionEffect1 ?? Peace_of_Tanks.Properties.Resources.Bao1;
            if (delay == 4) return ResourceManager.ExplosionEffect2 ?? Peace_of_Tanks.Properties.Resources.Bao2;
            if (delay == 3) return ResourceManager.ExplosionEffect3 ?? Peace_of_Tanks.Properties.Resources.Bao3;
            if (delay == 2) return ResourceManager.ExplosionEffect4 ?? Peace_of_Tanks.Properties.Resources.Bao4;
            return ResourceManager.ExplosionEffect5 ?? Peace_of_Tanks.Properties.Resources.Bao5;
        }
    }
}
