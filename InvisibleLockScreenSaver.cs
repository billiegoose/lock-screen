using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using Screensavers;

namespace InvisibleLockscreen
{
    class TransparentLockSaver : Screensaver
    {
        public TransparentLockSaver()
            : base(FullscreenMode.SingleWindow)
        {
            this.Initialize += new EventHandler(PixieSaver_Initialize);
            this.Update += new EventHandler(PixieSaver_Update);

            this.SettingsText = "rei@thefraser.com";
        }

        [STAThread]
        static void Main()
        {
            TransparentLockSaver ps = new TransparentLockSaver();
            ps.Run();
        }

        Random rand = new Random();
        public Random Rand
        {
            get { return rand; }
        }

        public void AddPixie(Sprite pixie)
        {
            pixies.Add(pixie);
        }

        List<Sprite> pixies = new List<Sprite>();

        void PixieSaver_Update(object sender, EventArgs e)
        {
            DoUpdate();
            DoRender();
        }

        int interval;

        void DoUpdate()
        {
            if (interval == 5)
            {
                pixies.Add(new Sprite(this));
                interval = 0;
            }
            interval++;

            for (int i = 0; i < pixies.Count; i++)
                if (pixies[i].Update())
                {
                    pixies.RemoveAt(i);
                    i--;
                }
        }

        void DoRender()
        {
            Graphics0.Clear(Color.LightGray);

            foreach (Sprite pixie in pixies)
                pixie.Draw();
        }

        void PixieSaver_Initialize(object sender, EventArgs e)
        {
            //Update enough times to fill the screen with pixies
            //for (int i = 0; i < Window0.Size.Height; i++)
            //	DoUpdate();
        }
    }
}
