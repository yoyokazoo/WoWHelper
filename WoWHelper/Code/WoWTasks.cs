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
        #region Windows Management Tasks

        public static async Task<bool> FocusOnWindowTask()
        {
            IntPtr h = ScreenCapture.GetWindowHandleByName("WowClassic");
            if (h == IntPtr.Zero) { return false; }

            ScreenCapture.SetForegroundWindow(h);

            await Task.Delay(750);
            return true;
        }

        #endregion

        #region Combat Tasks



        #endregion

        #region Movement Tasks



        #endregion

        public static async Task<bool> CheckForValidTargetTask()
        {
            for (int i = 0; i < 3; i++)
            {
                Keyboard.KeyPress(Keys.Tab);

                await Task.Delay(250);

                WoWWorldState worldState = WoWWorldState.GetWoWWorldState();

                if(worldState.CanChargeTarget)
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> TryToChargeTask()
        {
            WoWWorldState worldState;

            int loopNum = 0;
            do
            {
                if (loopNum > 0)
                {
                    Keyboard.KeyDown(Keys.A);
                    await Task.Delay(250);
                    Keyboard.KeyUp(Keys.A);
                }

                Keyboard.KeyPress(WoWInput.CHARGE_KEY);

                await Task.Delay(250);

                worldState = WoWWorldState.GetWoWWorldState();

                loopNum++;
            } while (!worldState.IsInCombat && worldState.CanChargeTarget);

            // give some time for the charge to land
            await Task.Delay(500);

            return worldState.IsInCombat;
        }

        public static async Task<bool> MoveTowardsWaypointTask(WoWWorldState worldState, Vector2 waypoint)
        {
            // return false if you haven't arrived at the waypoint yet, return true if you've arrived at the waypoint?

            Vector2 currentLocation = new Vector2(worldState.MapX, worldState.MapY);
            float desiredDegrees = Pathfinding.GetDirectionInDegrees(currentLocation, waypoint);
            float degreesDifference = Pathfinding.GetDegreesToMove(worldState.FacingDegrees, desiredDegrees);

            float incorrectDirectionDegreesTolerance = 20f;
            float waypointDistanceTolerance = 0.07f;

            Console.WriteLine($"Heading towards waypoint {waypoint}. At {worldState.MapX},{worldState.MapY}.  DesiredDegrees: {desiredDegrees}, facing degrees: {worldState.FacingDegrees}.  DegreesDifference: {degreesDifference}");

            if (Math.Abs(waypoint.X - worldState.MapX) <= waypointDistanceTolerance && Math.Abs(waypoint.Y - worldState.MapY) <= waypointDistanceTolerance)
            {
                Console.WriteLine($"Arrived at {waypoint} ({worldState.MapX},{worldState.MapY})");
                await EndWalkForwardTask();
                return true;
            }

            if (Math.Abs(degreesDifference) > incorrectDirectionDegreesTolerance)
            {
                Console.WriteLine($"degreesDifference too large, rotating to heading");
                await EndWalkForwardTask();
                await MoveToHeadingTask(desiredDegrees);
                return false;
            }
            else
            {
                // if we're already walking, ignore this
                Console.WriteLine($"Start walking forward");
                await StartWalkForwardTask();
                return false;
            }
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
            int maxSpeed = 10;

            if (minSpeed <= 0) throw new ArgumentOutOfRangeException(nameof(minSpeed));
            if (maxSpeed < minSpeed) throw new ArgumentOutOfRangeException(nameof(maxSpeed));

            const float degreeTolerance = 20f;
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

                    Console.WriteLine($"Desired Degrees: {desiredDegrees} Facing Degrees: {worldState.FacingDegrees} Degrees to Move: {degreesToMove}");

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
                    //await Task.Delay(200);

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
