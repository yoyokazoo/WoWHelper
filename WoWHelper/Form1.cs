using InputManager;
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


        WoWPlayer Player = null;
        async Task<bool> CoreGameplayLoopTask()
        {
            Player = new WoWPlayer();

            await FocusOnWindowTask();

            Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(new Rectangle(0, 0, 400, 800));
            Player.UpdateFromBitmap(wowBitmap);

            //var direction = Pathfinding.GetDirectionToDragMouse(Player.WorldState.FacingDegrees, 150f);

            await MoveThroughWaypointsTask(Player);

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

        public static async Task<bool> MoveThroughWaypointsTask(WoWPlayer player)
        {
            List<Vector2> waypoints = new List<Vector2>();
            //waypoints.Add(new Vector2(52f, 47f));
            waypoints.Add(new Vector2(53.13f, 46.48f));
            waypoints.Add(new Vector2(53.48f, 45.56f));
            waypoints.Add(new Vector2(52.01f, 45.12f));

            float waypointTolerance = 0.03f;

            // foreach
            do
            {
                Vector2 currentLocation = new Vector2(player.WorldState.MapX, player.WorldState.MapY);
                Vector2 nextWaypoint = waypoints[0];
                float desiredDegrees = Pathfinding.GetDirectionInDegrees(currentLocation, nextWaypoint);

                await MoveToHeadingTask(player, desiredDegrees);
                await WalkForwardTask(player);

                Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(new Rectangle(0, 0, 400, 800));
                player.UpdateFromBitmap(wowBitmap);
                wowBitmap.Dispose();
            } while (Math.Abs(waypoints[0].X - player.WorldState.MapX) > waypointTolerance || Math.Abs(waypoints[0].Y - player.WorldState.MapY) > waypointTolerance);

            return true;
        }

        public static async Task<bool> WalkForwardTask(WoWPlayer player)
        {
            Keyboard.KeyDown(Keys.W); await Task.Delay(5000);
            Keyboard.KeyUp(Keys.W); await Task.Delay(50);
            return true;
        }

        public static async Task<bool> MoveToHeadingTask(WoWPlayer player, float desiredDegrees)
        {
            float degreeTolerance = 5f;

            Mouse.ButtonDown(Mouse.MouseKeys.Right); await Task.Delay(50);

            while (Math.Abs(player.WorldState.FacingDegrees - desiredDegrees) > degreeTolerance)
            {
                float degreesToMove = Pathfinding.GetDegreesToMove(player.WorldState.FacingDegrees, desiredDegrees);

                int speed = Math.Abs(degreesToMove) > 40 ? 30 : 5;

                int direction = degreesToMove <= 0 ? 1 : -1;
                int verticalDirection = 0;// ((int)degreesToMove % 3) - 1;

                Mouse.MoveRelative(speed * direction, verticalDirection);

                Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(new Rectangle(0, 0, 400, 800));
                player.UpdateFromBitmap(wowBitmap);
                wowBitmap.Dispose();
            }

            Mouse.ButtonUp(Mouse.MouseKeys.Right); await Task.Delay(50);
            return true;
        }
    }
}
