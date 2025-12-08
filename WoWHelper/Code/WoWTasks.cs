using InputManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsGameAutomationTools.Images;

namespace WoWHelper.Code
{
    public class WoWTasks
    {
        async Task<bool> CoreGameplayLoopTaskASDF()
        {
            /*
            WoWPlayer player = new WoWPlayer();

            await FocusOnWindowTask();

            Bitmap wowBitmap = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(new Rectangle(0, 0, 400, 800));
            player.UpdateFromBitmap(wowBitmap);

            //var direction = Pathfinding.GetDirectionToDragMouse(Player.WorldState.FacingDegrees, 150f);

            await MoveThroughWaypointsTask(player);

            return true;
            */
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

        public static async Task<bool> CheckForValidTargetTask()
        {
            Keyboard.KeyPress(Keys.Tab);

            WoWWorldState worldState = WoWWorldState.GetWoWWorldState();

            return true;
        }

        public static async Task<bool> MoveThroughWaypointsTask()
        {
            WoWWorldState worldState = WoWWorldState.GetWoWWorldState();

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

                while (Math.Abs(waypoint.X - worldState.MapX) > waypointTolerance || Math.Abs(waypoint.Y - worldState.MapY) > waypointTolerance)
                {
                    Vector2 currentLocation = new Vector2(worldState.MapX, worldState.MapY);
                    float desiredDegrees = Pathfinding.GetDirectionInDegrees(currentLocation, waypoint);

                    await MoveToHeadingTask(desiredDegrees);

                    if (startedWalking == false)
                    {
                        await StartWalkForwardTask();
                        startedWalking = true;
                    }

                    int wait = Vector2.Distance(currentLocation, waypoint) > 0.2 ? maxWait : minWait;
                    await Task.Delay(wait);

                    worldState = WoWWorldState.GetWoWWorldState();
                }

                if (startedWalking == true)
                {
                    await EndWalkForwardTask();
                }
            }

            return true;
        }

        public static async Task<bool> StartWalkForwardTask()
        {
            await Task.Delay(0);
            Keyboard.KeyDown(Keys.W);
            return true;
        }

        public static async Task<bool> EndWalkForwardTask()
        {
            await Task.Delay(0);
            Keyboard.KeyUp(Keys.W);
            return true;
        }

        public static async Task<bool> MoveToHeadingTask(float desiredDegrees)
        {
            WoWWorldState worldState = WoWWorldState.GetWoWWorldState();

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
                    float currentDegrees = worldState.FacingDegrees;
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

                    worldState = WoWWorldState.GetWoWWorldState();

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
