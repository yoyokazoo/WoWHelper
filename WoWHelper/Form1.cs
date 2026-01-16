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

            StartPosition = FormStartPosition.Manual;
            Location = new Point(600, 600);

            // always stay up to date, for now since I'm the only one using it we know the path is right
            CopyLuaAddonToWoW(textBox1.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            WowPlayer player = new WowPlayer();
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
            //SlackHelper.SendMessageToChannel("Test slack message!", OnPostMessageCompleted);
            //SlackHelper.SendScreenshotToChannel();
            WowPlayer player = new WowPlayer();
            player.AdHocTest();
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

        /*
         * Waypoints = new List<Vector2>
            {
                new Vector2(53.60f, 39.54f),
                new Vector2(53.58f, 40.99f),
                new Vector2(52.96f, 41.78f),
                new Vector2(52.07f, 42.04f),
                new Vector2(51.51f, 41.22f),
                new Vector2(50.84f, 41.76f),
                new Vector2(51.25f, 42.36f),
                new Vector2(50.71f, 43.01f),
                new Vector2(52.22f, 43.96f),
                new Vector2(52.97f, 43.92f),
                new Vector2(53.44f, 43.40f),
                new Vector2(53.54f, 42.67f),
                new Vector2(54.59f, 43.87f),
                new Vector2(55.23f, 42.98f),
                new Vector2(54.28f, 42.10f),
                new Vector2(53.96f, 40.54f),
            },
        */
        private void button4_Click(object sender, EventArgs e)
        {
            List<Vector2> points = new List<Vector2>();
            points.Add(new Vector2(45.96f, 56.01f));
            points.Add(new Vector2(44.58f, 53.73f));
            points.Add(new Vector2(47.67f, 53.31f));
            points.Add(new Vector2(45.96f, 56.01f));
            float maxDistance = 1.0f;
            var dividedPoints = PathSubdivision.Subdivide(points, maxDistance);
            Console.WriteLine($"Waypoints = new List<Vector2>");
            Console.WriteLine($"{{");
            foreach (var point in dividedPoints)
            {
                Console.WriteLine($"new Vector2({point.X}f, {point.Y}f),");
            }
            Console.WriteLine($"}},");
        }
    }
}
