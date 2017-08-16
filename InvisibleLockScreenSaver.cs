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
            this.Initialize += new EventHandler(Saver_Initialize);
            this.Update += new EventHandler(Saver_Update);

            this.SettingsText = @"website: https://github.com/wmhilton/lock-screen

Based on:
Screensaver.cs © Rei Miyasaka 2006 rei@thefraser.com";
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
        
        void Saver_Update(object sender, EventArgs e)
        {
        }      

        void Saver_Initialize(object sender, EventArgs e)
        {
        }
    }
}
