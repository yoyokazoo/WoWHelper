using InputManager;
using SlackAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsGameAutomationTools.Images;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;

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
            WoWPlayer player = new WoWPlayer();
            player.KickOffCoreLoop();


            //player.UpdateFromBitmap();
            //IntPtr wowHandle = ScreenCapture.GetWindowHandleByName("WowClassic");
            //Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromWindowHandle(wowHandle);
            //ScreenCapture.SaveBitmapToFile(wowBitmap, "TestCapture.bmp");
            /*
            Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(new Rectangle(0, 0, 400, 400));

            player.UpdateFromBitmap(wowBitmap);
            ScreenCapture.SaveBitmapToFile(wowBitmap, "TestCapture.bmp");

            Console.WriteLine($"Done ({player.WorldState.HpPercent}) ({player.WorldState.ResourcePercent})");
            */
            //_ = CoreGameplayLoopTask();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SlackHelper.SendMessageToChannel("Potion used!", OnPostMessageCompleted);
            //SlackHelper.SendScreenshotToChannel();
        }

        private void OnPostMessageCompleted(PostMessageResponse response)
        {
            // TODO: handle the response here
            Console.WriteLine(response);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            CopyLuaAddonToWoW(textBox1.Text);
        }

        public static void CopyLuaAddonToWoW(string destinationDir)
        {
            // Base directory = bin\Debug\
            string baseDir = AppContext.BaseDirectory;

            // Relative source folder under bin\Debug
            string sourceDir = Path.Combine(baseDir, "Lua Addon");

            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException(
                    $"Source addon directory does not exist: {sourceDir}");
            }

            CopyDirectoryRecursive(sourceDir, destinationDir);
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var filePath in Directory.GetFiles(sourceDir))
            {
                string destFilePath = Path.Combine(
                    destinationDir,
                    Path.GetFileName(filePath));

                System.IO.File.Copy(filePath, destFilePath, overwrite: true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(
                    destinationDir,
                    Path.GetFileName(subDir));

                CopyDirectoryRecursive(subDir, destSubDir);
            }
        }
    }
}
