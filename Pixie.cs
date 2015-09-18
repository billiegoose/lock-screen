using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Screensavers;

namespace InvisibleLockscreen
{
    class Sprite
    {
        public Sprite(TransparentLockSaver screensaver)
        {
            this.screensaver = screensaver;
            y = 0;
            x = screensaver.Rand.Next(screensaver.Window0.Size.Width);
            tendency = screensaver.Rand.Next(25, 75);
        }

        readonly static Brush brush = new Pen(Color.Green).Brush;
        int tendency;
        int x, y;
        private TransparentLockSaver screensaver;

        public virtual bool Update()
        {
            y++;
            int num = screensaver.Rand.Next(100);
            if (num < tendency)
                x++;
            else
                x--;

            return y >= screensaver.Window0.Size.Height;
        }

        public virtual void Draw()
        {
            //screensaver.Graphics0.DrawString("Will is Amazing", new Font("OCR A Extended", 8), brush, new PointF(x, y));
        }
    }
}
