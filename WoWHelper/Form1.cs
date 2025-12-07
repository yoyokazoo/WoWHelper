using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsGameAutomationTools.Images;

namespace WoWHelper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /*
            WoWPlayer player = new WoWPlayer();

            //player.UpdateFromBitmap();
            //IntPtr wowHandle = ScreenCapture.GetWindowHandleByName("WowClassic");
            //Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromWindowHandle(wowHandle);
            //ScreenCapture.SaveBitmapToFile(wowBitmap, "TestCapture.bmp");

            Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(new Rectangle(0, 0, 400, 400));

            player.UpdateFromBitmap(wowBitmap);
            ScreenCapture.SaveBitmapToFile(wowBitmap, "TestCapture.bmp");

            Console.WriteLine($"Done ({player.WorldState.HpPercent}) ({player.WorldState.ResourcePercent})");
            */
            _ = CoreGameplayLoopTask();
        }



        async Task<bool> CoreGameplayLoopTask()
        {
            WoWPlayer player = new WoWPlayer();

            await FocusOnWindowTask();

            Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(new Rectangle(0, 0, 400, 400));
            player.UpdateFromBitmap(wowBitmap);

            Console.WriteLine($"Done ({player.WorldState.PlayerHpPercent}) ({player.WorldState.ResourcePercent})");

            return true;
        }

        public static Bitmap GetEQBitmap()
        {
            //ScreenCapture.
            //return ScreenCapture.CaptureWindowBM(GetEQWindowHandle());
            return null;
        }

        public static IntPtr GetWindowHandle()
        {
            //if (currentCharacterName != null) { return eqWindowHandles[currentCharacterName]; }
            //var processes = Process.GetProcesses();
            return Process.GetProcessesByName("WowClassic").FirstOrDefault().MainWindowHandle;
        }

        public static async Task<bool> FocusOnWindowTask()
        {
            IntPtr h = ScreenCapture.GetWindowHandleByName("WowClassic");
            if (h == IntPtr.Zero) { return false; }

            ScreenCapture.SetForegroundWindow(h);

            await Task.Delay(750);
            return true;
        }
    }
}
