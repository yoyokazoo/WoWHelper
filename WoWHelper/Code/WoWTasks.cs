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
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code.WorldState;

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

        public static async Task<bool> SetLogoutVariablesTask(WoWPlayer wowPlayer)
        {
            WoWWorldState worldState = WoWWorldState.GetWoWWorldState();
            if (worldState.LowOnDynamite)
            {
                wowPlayer.LogoutTriggered = true;
                wowPlayer.LogoutReason = "Low on Dynamite";
            }
            else if (worldState.LowOnHealthPotions)
            {
                wowPlayer.LogoutTriggered = true;
                wowPlayer.LogoutReason = "Low on Health Potions";
            }

            await Task.Delay(0);

            return wowPlayer.LogoutTriggered;
        }

        public static async Task<bool> LogoutTask()
        {
            WoWWorldState worldState;

            Keyboard.KeyPress(WoWInput.LOGOUT_MACRO);

            do
            {
                worldState = WoWWorldState.GetWoWWorldState();
                await Task.Delay(250);
            } while (!worldState.IsInCombat && !worldState.OnLoginScreen);

            return worldState.OnLoginScreen;
        }

        #endregion

        #region Combat Tasks

        public static async Task<bool> RecoverAfterFightTask(WoWPlayer wowPlayer)
        {
            WoWWorldState worldState;
            bool startedEatingFood = false;
            bool dynamiteOrPotionIsCooledDown = false;

            while(true)
            {
                await Task.Delay(200);
                worldState = WoWWorldState.GetWoWWorldState();

                if (worldState.IsInCombat)
                {
                    return false;
                }

                if (worldState.PlayerHpPercent < 85 && !startedEatingFood)
                {
                    Keyboard.KeyPress(WoWInput.EAT_FOOD_KEY);
                    startedEatingFood = true;
                }

                //bool dynamiteIsCooledDown = !WoWPlayer.CurrentTimeInsideDuration(wowPlayer.LastDynamiteTime, WoWGameplayConstants.DYNAMITE_COOLDOWN_MILLIS);
                bool potionIsCooledDown = !WoWPlayer.CurrentTimeInsideDuration(wowPlayer.LastHealthPotionTime, WoWGameplayConstants.POTION_COOLDOWN_MILLIS);

                // For now, I don't care if dynamite is cooled down.  If we dynamited and didn't have to potion, we're probably safe enough to keep going
                // especially since the dynamite cooldown is so short it'll probably be up by the time we need it again.
                if (worldState.PlayerHpPercent >= 100 && potionIsCooledDown)
                {
                    break;
                }
            }

            await ScootForwardsTask();
            return true;
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

        public static async Task<bool> StartOfCombatTask()
        {
            // combat wiggle in case camera is pointed wrong direction
            Keyboard.KeyDown(Keys.S);
            await Task.Delay(400);
            Keyboard.KeyUp(Keys.S);

            Keyboard.KeyDown(Keys.W);
            await Task.Delay(20);
            Keyboard.KeyUp(Keys.W);

            // always kick things off with heroic strike macro to /startattack
            Keyboard.KeyPress(WoWInput.HEROIC_STRIKE_KEY);

            return true;
        }

        public static async Task<bool> MakeSureWeAreAttackingEnemyTask(WoWWorldState worldState, WoWWorldState previousWorldState)
        {
            bool attackerJustDied = previousWorldState?.AttackerCount > worldState.AttackerCount && worldState.AttackerCount > 0;
            bool inCombatButNotAutoAttacking = worldState.IsInCombat && !worldState.IsAutoAttacking;
            bool tooFarAway = worldState.TooFarAway;
            bool facingWrongWay = worldState.FacingWrongWay; // potentially need to turn in case we're webbed and backing up wont work

            if (attackerJustDied || facingWrongWay)
            {
                // one of the mobs just died, scoot back to make sure the next mob is in front of you
                await WoWTasks.ScootBackwardsTask();
            }

            if (tooFarAway)
            {
                // we may have targeted something in the distance then got aggroed by something else, clear target so we pick them up
                Keyboard.KeyPress(WoWInput.CLEAR_TARGET_MACRO);
            }

            if (attackerJustDied || inCombatButNotAutoAttacking || tooFarAway)
            {
                // /startattack
                Keyboard.KeyPress(WoWInput.HEROIC_STRIKE_KEY);
            }

            return attackerJustDied || inCombatButNotAutoAttacking || tooFarAway || facingWrongWay;
        }

        public static async Task<bool> TooManyAttackersTask(WoWWorldState worldState)
        {
            bool tooManyAttackers = worldState.AttackerCount > 2;

            if (tooManyAttackers)
            {
                SlackHelper.SendMessageToChannel($"TOO MANY ATTACKERS HELP");
                // cast retaliation
                Keyboard.KeyPress(WoWInput.RETALIATION_KEY);
                // until I get GCD tracking working, just wait a bit and click it again to make sure
                await Task.Delay(1500);
                Keyboard.KeyPress(WoWInput.RETALIATION_KEY);
            }

            return tooManyAttackers;
        }

        public static async Task<bool> ThrowDynamiteTask(WoWWorldState worldState)
        {
            bool shouldThrowDynamite = worldState.AttackerCount > 1;

            if (shouldThrowDynamite)
            {
                Mouse.Move(1770, 770);
                await Task.Delay(50);
                Keyboard.KeyPress(WoWInput.DYNAMITE_KEY);
                await Task.Delay(50);
                Mouse.PressButton(Mouse.MouseKeys.Left);
                await Task.Delay(1000);
            }

            return shouldThrowDynamite;
        }

        public static async Task<bool> UseHealingPotionTask(WoWWorldState worldState)
        {
            bool shouldUseHealingPotion = worldState.PlayerHpPercent <= WoWGameplayConstants.HEALING_POTION_HP_THRESHOLD;

            if (shouldUseHealingPotion)
            {
                SlackHelper.SendMessageToChannel("Potion used!");
                Keyboard.KeyPress(WoWInput.HEALING_POTION_KEY);
                await Task.Delay(0);
            }

            return shouldUseHealingPotion;
        }

        #endregion

        #region Movement Tasks

        // Returns true if we've reached the waypoint
        // Returns false if we haven't yet reached the waypoint
        // Rotates towards the waypoint or walks towards the waypoint, depending
        public static async Task<bool> MoveTowardsWaypointTask(WoWWorldState worldState, WoWWaypointDefinition waypoint, int waypointIndex)
        {
            float waypointDistance = Vector2.Distance(worldState.PlayerLocation, waypoint.Waypoints[waypointIndex]);
            float desiredDegrees = WoWPathfinding.GetDesiredDirectionInDegrees(worldState.PlayerLocation, waypoint.Waypoints[waypointIndex]);
            float degreesDifference = WoWPathfinding.GetDegreesToMove(worldState.FacingDegrees, desiredDegrees);

            //Console.WriteLine($"Heading towards waypoint {waypoint}. At {worldState.MapX},{worldState.MapY}.  DesiredDegrees: {desiredDegrees}, facing degrees: {worldState.FacingDegrees}.  DegreesDifference: {degreesDifference}");

            if (waypointDistance <= waypoint.DistanceTolerance)
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
