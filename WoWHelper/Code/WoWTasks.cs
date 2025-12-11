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

        public static async Task<bool> RecoverAfterFightTask()
        {
            WoWWorldState worldState;
            bool startedEatingFood = false;

            do
            {
                worldState = WoWWorldState.GetWoWWorldState();

                if (worldState.PlayerHpPercent < 85 && !startedEatingFood)
                {
                    //Console.WriteLine($"PlayerHpPercent = {worldState.PlayerHpPercent} ({startedEatingFood}), eating.");
                    //if (worldState.PlayerHpPercent == 0) ScreenCapture.SaveTestDesktopScreenshot("hp percent zero.bmp");
                    Keyboard.KeyPress(WoWInput.EAT_FOOD_KEY);
                    startedEatingFood = true;

                }
                await Task.Delay(200);
            } while (worldState.PlayerHpPercent < 100 && !worldState.IsInCombat); // and charge is cooling down

            if (!worldState.IsInCombat)
            {
                await ScootForwardsTask();
            }

            return !worldState.IsInCombat;
        }

        public static async Task<bool> TryToChargeTask()
        {
            WoWWorldState worldState;

            // break this apart a bit?  Smaller discrete charge task and then all the "rotation, wait for charge to land" cruft around it
            int loopNum = 0;
            do
            {
                if (loopNum > 0)
                {
                    await TurnABitToTheLeftTask();
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

        #endregion

        #region Movement Tasks

        // Returns true if we've reached the waypoint
        // Returns false if we haven't yet reached the waypoint
        // Rotates towards the waypoint or walks towards the waypoint, depending
        public static async Task<bool> MoveTowardsWaypointTask(WoWWorldState worldState, Vector2 waypoint)
        {
            float waypointDistance = Vector2.Distance(worldState.PlayerLocation, waypoint);
            float desiredDegrees = WoWPathfinding.GetDesiredDirectionInDegrees(worldState.PlayerLocation, waypoint);
            float degreesDifference = WoWPathfinding.GetDegreesToMove(worldState.FacingDegrees, desiredDegrees);

            //Console.WriteLine($"Heading towards waypoint {waypoint}. At {worldState.MapX},{worldState.MapY}.  DesiredDegrees: {desiredDegrees}, facing degrees: {worldState.FacingDegrees}.  DegreesDifference: {degreesDifference}");

            if (waypointDistance <= WoWPathfinding.WAYPOINT_DISTANCE_TOLERANCE)
            {
                //Console.WriteLine($"Arrived at {waypoint} ({worldState.MapX},{worldState.MapY})");
                await EndWalkForwardTask();
                return true;
            }

            if (Math.Abs(degreesDifference) > WoWPathfinding.GetWaypointDegreesTolerance(waypointDistance))
            {
                //Console.WriteLine($"degreesDifference too large, rotating to heading");
                //await EndWalkForwardTask();
                await RotateToDirectionTask(desiredDegrees, waypointDistance);
                return false;
            }
            else
            {
                // if we're already walking, ignore this
                //Console.WriteLine($"Start walking forward");
                await StartWalkForwardTask();
                return false;
            }
        }

        public static async Task<bool> RotateToDirectionTask(float desiredDegrees, float distance)
        {
            bool mouseDown = false;

            try
            {
                while (true)
                {
                    WoWWorldState worldState = WoWWorldState.GetWoWWorldState();

                    float currentDegrees = worldState.FacingDegrees;
                    float degreesToMove = WoWPathfinding.GetDegreesToMove(currentDegrees, desiredDegrees);
                    float absDegreesToMove = Math.Abs(degreesToMove);

                    //Console.WriteLine($"Desired Degrees: {desiredDegrees} Facing Degrees: {worldState.FacingDegrees} Degrees to Move: {degreesToMove}");

                    if (absDegreesToMove <= WoWPathfinding.GetWaypointDegreesTolerance(distance))
                        break;

                    if (mouseDown == false)
                    {
                        Mouse.ButtonDown(Mouse.MouseKeys.Right);
                        await Task.Delay(50);
                        mouseDown = true;
                    }

                    // Map angle difference -> [0,1] where 0 = on target, 1 = far away (>= maxSpeedAngle)
                    float t = absDegreesToMove / WoWPathfinding.MAX_SPEED_ANGLE;

                    // Interpolate between minSpeed and maxSpeed
                    int speed = (int)Math.Round(WoWPathfinding.MIN_ROTATION_SPEED + (WoWPathfinding.MAX_ROTATION_SPEED - WoWPathfinding.MIN_ROTATION_SPEED) * t);

                    int direction = degreesToMove <= 0 ? 1 : -1;
                    int verticalDirection = 0;

                    Mouse.MoveRelative(speed * direction, verticalDirection);
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

        public static async Task<bool> StartWalkForwardTask()
        {
            await Task.Delay(0);
            Keyboard.KeyDown(WoWInput.MOVE_FORWARD);
            return true;
        }

        public static async Task<bool> EndWalkForwardTask()
        {
            await Task.Delay(0);
            Keyboard.KeyUp(WoWInput.MOVE_FORWARD);
            return true;
        }

        public static async Task<bool> ScootForwardsTask()
        {
            Keyboard.KeyDown(WoWInput.MOVE_FORWARD);
            await Task.Delay(100);
            Keyboard.KeyUp(WoWInput.MOVE_FORWARD);
            return true;
        }

        public static async Task<bool> ScootBackwardsTask()
        {
            Keyboard.KeyDown(WoWInput.MOVE_BACK);
            await Task.Delay(1000);
            Keyboard.KeyUp(WoWInput.MOVE_BACK);
            return true;
        }

        public static async Task<bool> TurnABitToTheLeftTask()
        {
            Keyboard.KeyDown(WoWInput.TURN_LEFT);
            await Task.Delay(500);
            Keyboard.KeyUp(WoWInput.TURN_LEFT);

            return true;
        }

        #endregion
    }
}
