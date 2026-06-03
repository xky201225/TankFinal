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
        // 原有资源（保留兼容）
        public static Bitmap boom1;
        public static Bitmap boom2;
        public static Bitmap boom3;
        public static Bitmap megaBoom1;
        public static Bitmap megaBoom2;

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
                if (message.delay >= 3) picture = ResourceManager.HitEffect1 ?? boom1;
                else if (message.delay == 2) picture = ResourceManager.HitEffect2 ?? boom2;
                else if (message.delay == 1) picture = ResourceManager.HitEffect3 ?? boom3;
                message.delay--;
            }
            else if (message.type == 1)
            {
                if (message.delay >= 5) picture = ResourceManager.ExplosionEffect1 ?? megaBoom1;
                else if (message.delay == 4) picture = ResourceManager.ExplosionEffect2 ?? megaBoom1;
                else if (message.delay == 3) picture = ResourceManager.ExplosionEffect3 ?? megaBoom2;
                else if (message.delay == 2) picture = ResourceManager.ExplosionEffect4 ?? megaBoom2;
                else if (message.delay == 1) picture = ResourceManager.ExplosionEffect5 ?? megaBoom2;
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
                if (delay >= 3) return ResourceManager.HitEffect1 ?? boom1;
                if (delay == 2) return ResourceManager.HitEffect2 ?? boom2;
                return ResourceManager.HitEffect3 ?? boom3;
            }

            if (delay >= 5) return ResourceManager.ExplosionEffect1 ?? megaBoom1;
            if (delay == 4) return ResourceManager.ExplosionEffect2 ?? megaBoom1;
            if (delay == 3) return ResourceManager.ExplosionEffect3 ?? megaBoom2;
            if (delay == 2) return ResourceManager.ExplosionEffect4 ?? megaBoom2;
            return ResourceManager.ExplosionEffect5 ?? megaBoom2;
        }
    }
}
