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
            waypoints.Add(new Vector2(52f, 47f));
            waypoints.Add(new Vector2(53.13f, 46.48f));
            waypoints.Add(new Vector2(53.48f, 45.56f));
            waypoints.Add(new Vector2(52.01f, 45.12f));
            waypoints.Add(new Vector2(52f, 47f));

            float waypointTolerance = 0.07f;

            int maxWait = 1000;
            int minWait = 110;

            foreach (var waypoint in waypoints)
            {
                bool startedWalking = false;

                while (Math.Abs(waypoint.X - player.WorldState.MapX) > waypointTolerance || Math.Abs(waypoint.Y - player.WorldState.MapY) > waypointTolerance)
                {
                    Vector2 currentLocation = new Vector2(player.WorldState.MapX, player.WorldState.MapY);
                    float desiredDegrees = Pathfinding.GetDirectionInDegrees(currentLocation, waypoint);

                    await MoveToHeadingTask(player, desiredDegrees);

                    if (startedWalking == false)
                    {
                        await StartWalkForwardTask(player);
                        startedWalking = true;
                    }

                    int wait = Vector2.Distance(currentLocation, waypoint) > 0.2 ? maxWait : minWait;
                    await Task.Delay(wait);

                    Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(new Rectangle(0, 0, 400, 800));
                    player.UpdateFromBitmap(wowBitmap);
                    wowBitmap.Dispose();
                }

                if (startedWalking == true)
                {
                    await EndWalkForwardTask(player);
                }
            }

            return true;
        }

        public static async Task<bool> StartWalkForwardTask(WoWPlayer player)
        {
            await Task.Delay(0);
            Keyboard.KeyDown(Keys.W);
            return true;
        }

        public static async Task<bool> EndWalkForwardTask(WoWPlayer player)
        {
            await Task.Delay(0);
            Keyboard.KeyUp(Keys.W);
            return true;
        }

        public static async Task<bool> MoveToHeadingTask(WoWPlayer player, float desiredDegrees)
        {
            int minSpeed = 3;
            int maxSpeed = 100;

            if (minSpeed <= 0) throw new ArgumentOutOfRangeException(nameof(minSpeed));
            if (maxSpeed < minSpeed) throw new ArgumentOutOfRangeException(nameof(maxSpeed));

            const float degreeTolerance = 5f;
            // Angle at which you’re already at max speed; tweak if desired
            const float maxSpeedAngle = 180f;

            bool mouseDown = false;

            try
            {
                while (true)
                {
                    float currentDegrees = player.WorldState.FacingDegrees;
                    float degreesToMove = Pathfinding.GetDegreesToMove(currentDegrees, desiredDegrees);
                    float absDegreesToMove = Math.Abs(degreesToMove);

                    // Exit condition
                    if (absDegreesToMove <= degreeTolerance)
                        break;

                    if (mouseDown == false)
                    {
                        Mouse.ButtonDown(Mouse.MouseKeys.Right);
                        await Task.Delay(50);
                        mouseDown = true;
                    }

                    // Map angle difference -> [0,1] where 0 = on target, 1 = far away (>= maxSpeedAngle)
                    float t = absDegreesToMove / maxSpeedAngle;
                    if (t > 1f) t = 1f;
                    if (t < 0f) t = 0f;

                    // Interpolate between minSpeed and maxSpeed
                    int speed = (int)Math.Round(minSpeed + (maxSpeed - minSpeed) * t);

                    // Ensure at least minSpeed in case of rounding
                    if (speed < minSpeed) speed = minSpeed;
                    if (speed > maxSpeed) speed = maxSpeed;

                    int direction = degreesToMove <= 0 ? 1 : -1;
                    int verticalDirection = 0;

                    Mouse.MoveRelative(speed * direction, verticalDirection);

                    using (Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(new Rectangle(0, 0, 400, 800)))
                    {
                        player.UpdateFromBitmap(wowBitmap);
                    }

                    //await Task.Delay(110);
                }
            }
            finally
            {
                if (mouseDown == true)
                {
                    Mouse.ButtonUp(Mouse.MouseKeys.Right);
                    await Task.Delay(50);
                }
            }

            return true;
        }
    }
}
