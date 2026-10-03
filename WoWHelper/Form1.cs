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
using WoWHelper.Shared;

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
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            WowPlayer player = new WowPlayer();
            player.KickOffAdHocTest();
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
