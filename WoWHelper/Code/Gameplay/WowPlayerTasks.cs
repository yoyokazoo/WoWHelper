using InputManager;
using System;
using System.Numerics;
using System.Threading.Tasks;
using WindowsGameAutomationTools.Images;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;
using WoWHelper.Code.Gameplay;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        #region Management Tasks

        public async Task<bool> FocusOnWindowTask()
        {
            IntPtr h = ScreenCapture.GetWindowHandleByName("WowClassic");
            if (h == IntPtr.Zero) { return false; }

            ScreenCapture.SetForegroundWindow(h);

            await Task.Delay(750);
            return true;
        }

        public async Task<bool> SetLogoutVariablesTask()
        {
            if (WorldState.LowOnDynamite)
            {
                LogoutTriggered = true;
                LogoutReason = "Low on Dynamite";
            }
            else if (WorldState.LowOnHealthPotions)
            {
                LogoutTriggered = true;
                LogoutReason = "Low on Health Potions";
            }
            else if (WorldState.LowOnAmmo && FarmingConfig.EngageMethod == WowFarmingConfiguration.EngagementMethod.Shoot)
            {
                LogoutTriggered = true;
                LogoutReason = "Low on Ammo";
            }
            else if (!CurrentTimeInsideDuration(FarmStartTime, WowPlayerConstants.FARM_TIME_LIMIT_MILLIS))
            {
                LogoutTriggered = true;
                LogoutReason = "Farm Time Limit Reached";
            }

            await Task.Delay(0);

            return LogoutTriggered;
        }

        public async Task<bool> StartLogoutTask()
        {
            await WowInput.PressKeyWithShift(WowInput.SHIFT_LOGOUT_MACRO);
            return true;
        }

        public async Task<bool> CheckIfLoggedOutTask()
        {
            await Task.Delay(0);
            return WorldState.OnLoginScreen;
        }

        public async Task<bool> LootTask()
        {
            Mouse.Move(1720, 720);
            Mouse.PressButton(Mouse.MouseKeys.Right);
            await Task.Delay(1500);
            return true;
        }

        public async Task<bool> SkinTask()
        {
            Mouse.Move(1720, 720);
            Mouse.PressButton(Mouse.MouseKeys.Right);
            await Task.Delay(3000);
            return true;
        }

        /*
        public long LongWaitTimeStart { get; private set; }
        public long LongWaitTimeDuration { get; private set; }
        // Set a non-blocking long wait time
        public async Task<bool> SetLongWaitTask(long duration)
        {
            await Task.Delay(0);
            LongWaitTimeStart = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            LongWaitTimeDuration = duration;
            return true;
        }

        public async Task<bool> CheckLongWaitTask()
        {
            await Task.Delay(0);
            return CurrentTimeInsideDuration(LongWaitTimeStart, LongWaitTimeDuration);
        }
        */

        #endregion

        #region Combat Tasks

        public async Task<bool> StartBattleReadyRecoverTask()
        {
            await Task.Delay(0);
            if (WorldState.PlayerHpPercent < WowPlayerConstants.EAT_FOOD_HP_THRESHOLD)
            {
                Keyboard.KeyPress(WowInput.EAT_FOOD_KEY);
            }

            return true;
        }

        public async Task<bool> WaitUntilBattleReadyTask()
        {
            // For now, I don't care if dynamite is cooled down.  If we dynamited and didn't have to potion, we're probably safe enough to keep going
            // especially since the dynamite cooldown is so short it'll probably be up by the time we need it again.

            bool hpRecovered = WorldState.PlayerHpPercent >= WowPlayerConstants.STOP_RESTING_HP_THRESHOLD;
            bool potionIsCooledDown = !WowPlayer.CurrentTimeInsideDuration(HealthPotionTime, WowGameplayConstants.POTION_COOLDOWN_MILLIS);
            bool battleReady = hpRecovered && potionIsCooledDown;

            if (battleReady)
            {
                await ScootForwardsTask();
            }
            
            return battleReady;
        }

        public async Task<bool> TryToEngageTask()
        {
            if (FarmingConfig.EngageMethod == Code.Gameplay.WowFarmingConfiguration.EngagementMethod.Charge)
            {
                return await TryToChargeTask();
            }
            else if (FarmingConfig.EngageMethod != Code.Gameplay.WowFarmingConfiguration.EngagementMethod.Charge)
            {
                return await TryToShootTask();
            }

            return false;
        }

        public async Task<bool> TryToChargeTask()
        {
            Keyboard.KeyPress(WowInput.CHARGE_KEY);
            await Task.Delay(250);
            UpdateWorldState();

            // break this apart a bit?  Smaller discrete charge task and then all the "rotation, wait for charge to land" cruft around it
            int loopNum = 0;
            while (!WorldState.IsInCombat && WorldState.CanChargeTarget && loopNum < WowPlayerConstants.ENGAGE_ROTATION_ATTEMPTS)
            {
                await TurnABitToTheLeftTask();
                Keyboard.KeyPress(WowInput.CHARGE_KEY);

                await Task.Delay(250);
                UpdateWorldState();

                loopNum++;
            }

            // give some time for the charge to land
            await Task.Delay(500);

            return WorldState.IsInCombat;
        }

        public async Task<bool> TryToShootTask()
        {
            Console.WriteLine($"Clicked first shoot");
            Keyboard.KeyPress(WowInput.SHOOT_MACRO);
            await Task.Delay(250);
            UpdateWorldState();

            // break this apart a bit?  Smaller discrete charge task and then all the "rotation, wait for charge to land" cruft around it
            int loopNum = 0;
            do
            {
                if (!WorldState.WaitingToShoot)
                {
                    Console.WriteLine($"Not shooting, probably not facing");
                    await TurnABitToTheLeftTask();
                    Keyboard.KeyPress(WowInput.SHOOT_MACRO);
                }

                await Task.Delay(250);

                UpdateWorldState();

                loopNum++;
            } while (!WorldState.IsInCombat && WorldState.CanShootTarget && loopNum < WowPlayerConstants.ENGAGE_ROTATION_ATTEMPTS);

            // give some time for the shot to land
            await Task.Delay(500);

            return WorldState.IsInCombat;
        }

        public async Task<bool> StartOfCombatTask()
        {
            await StartOfCombatWiggle();

            // always kick things off with heroic strike macro to /startattack
            Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);

            return true;
        }

        public async Task<bool> MakeSureWeAreAttackingEnemyTask()
        {
            bool attackerJustDied = PreviousWorldState?.AttackerCount > WorldState.AttackerCount && WorldState.AttackerCount > 0;
            bool inCombatButNotAutoAttacking = WorldState.IsInCombat && !WorldState.IsAutoAttacking;
            bool tooFarAway = WorldState.TooFarAway;
            bool facingWrongWay = WorldState.FacingWrongWay; // potentially need to turn in case we're webbed and backing up wont work
            bool targetNeedsToBeInFront = WorldState.TargetNeedsToBeInFront;

            if (attackerJustDied || facingWrongWay || targetNeedsToBeInFront)
            {
                // one of the mobs just died, scoot back to make sure the next mob is in front of you
                await ScootBackwardsTask();
            }

            if (tooFarAway)
            {
                // we may have targeted something in the distance then got aggroed by something else, clear target so we pick them up
                Keyboard.KeyPress(WowInput.CLEAR_TARGET_MACRO);
            }

            if (attackerJustDied || inCombatButNotAutoAttacking || tooFarAway)
            {
                // /startattack
                Keyboard.KeyPress(WowInput.HEROIC_STRIKE_KEY);
            }

            return attackerJustDied || inCombatButNotAutoAttacking || tooFarAway || facingWrongWay || targetNeedsToBeInFront;
        }

        public async Task<bool> TooManyAttackersTask()
        {
            bool tooManyAttackers = WorldState.AttackerCount > 2;

            if (tooManyAttackers)
            {
                SlackHelper.SendMessageToChannel($"TOO MANY ATTACKERS HELP");
                // cast retaliation
                Keyboard.KeyPress(WowInput.RETALIATION_KEY);
                // until I get GCD tracking working, just wait a bit and click it again to make sure
                await Task.Delay(1500);
                Keyboard.KeyPress(WowInput.RETALIATION_KEY);
            }

            return tooManyAttackers;
        }

        public async Task<bool> ThrowDynamiteTask()
        {
            bool shouldThrowDynamite = WorldState.AttackerCount > 1;

            if (shouldThrowDynamite)
            {
                Mouse.Move(1770, 770);
                await Task.Delay(50);
                Keyboard.KeyPress(WowInput.DYNAMITE_KEY);
                await Task.Delay(50);
                Mouse.PressButton(Mouse.MouseKeys.Left);
                await Task.Delay(1000);
            }

            return shouldThrowDynamite;
        }

        public async Task<bool> UseHealingPotionTask()
        {
            bool shouldUseHealingPotion = WorldState.PlayerHpPercent <= WowGameplayConstants.HEALING_POTION_HP_THRESHOLD;

            if (shouldUseHealingPotion)
            {
                SlackHelper.SendMessageToChannel("Potion used!");
                Keyboard.KeyPress(WowInput.HEALING_POTION_KEY);
                await Task.Delay(0);
            }

            return shouldUseHealingPotion;
        }

        #endregion

        #region Movement Tasks

        // Returns true if we've reached the waypoint
        // Returns false if we haven't yet reached the waypoint
        // Rotates towards the waypoint or walks towards the waypoint, depending
        public async Task<bool> MoveTowardsWaypointTask()
        {
            float waypointDistance = Vector2.Distance(WorldState.PlayerLocation, FarmingConfig.WaypointDefinition.Waypoints[CurrentWaypointIndex]);
            float desiredDegrees = WowPathfinding.GetDesiredDirectionInDegrees(WorldState.PlayerLocation, FarmingConfig.WaypointDefinition.Waypoints[CurrentWaypointIndex]);
            float degreesDifference = WowPathfinding.GetDegreesToMove(WorldState.FacingDegrees, desiredDegrees);

            Console.WriteLine($"Heading towards waypoint {FarmingConfig.WaypointDefinition.Waypoints[CurrentWaypointIndex]}. At {WorldState.MapX},{WorldState.MapY}.  DesiredDegrees: {desiredDegrees}, facing degrees: {WorldState.FacingDegrees}.  DegreesDifference: {degreesDifference}");

            if (waypointDistance <= FarmingConfig.WaypointDefinition.DistanceTolerance)
            {
                //Console.WriteLine($"Arrived at {waypoint} ({worldState.MapX},{worldState.MapY})");
                await EndWalkForwardTask();
                return true;
            }

            if (Math.Abs(degreesDifference) > WowPathfinding.GetWaypointDegreesTolerance(waypointDistance))
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

        public async Task<bool> RotateToDirectionTask(float desiredDegrees, float distance)
        {
            bool mouseDown = false;

            try
            {
                while (true)
                {
                    UpdateWorldState();

                    float currentDegrees = WorldState.FacingDegrees;
                    float degreesToMove = WowPathfinding.GetDegreesToMove(currentDegrees, desiredDegrees);
                    float absDegreesToMove = Math.Abs(degreesToMove);

                    //Console.WriteLine($"Desired Degrees: {desiredDegrees} Facing Degrees: {worldState.FacingDegrees} Degrees to Move: {degreesToMove}");

                    if (absDegreesToMove <= WowPathfinding.GetWaypointDegreesTolerance(distance))
                        break;

                    if (mouseDown == false)
                    {
                        Mouse.ButtonDown(Mouse.MouseKeys.Right);
                        await Task.Delay(50);
                        mouseDown = true;
                    }

                    // Map angle difference -> [0,1] where 0 = on target, 1 = far away (>= maxSpeedAngle)
                    float t = absDegreesToMove / WowPathfinding.MAX_SPEED_ANGLE;

                    // Interpolate between minSpeed and maxSpeed
                    int speed = (int)Math.Round(WowPathfinding.MIN_ROTATION_SPEED + (WowPathfinding.MAX_ROTATION_SPEED - WowPathfinding.MIN_ROTATION_SPEED) * t);

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

        public async Task<bool> StartWalkForwardTask()
        {
            await Task.Delay(0);
            Keyboard.KeyDown(WowInput.MOVE_FORWARD);
            return true;
        }

        public async Task<bool> EndWalkForwardTask()
        {
            await Task.Delay(0);
            Keyboard.KeyUp(WowInput.MOVE_FORWARD);
            return true;
        }

        public async Task<bool> ScootForwardsTask()
        {
            Keyboard.KeyDown(WowInput.MOVE_FORWARD);
            await Task.Delay(100);
            Keyboard.KeyUp(WowInput.MOVE_FORWARD);
            return true;
        }

        public async Task<bool> ScootBackwardsTask()
        {
            Keyboard.KeyDown(WowInput.MOVE_BACK);
            await Task.Delay(1000);
            Keyboard.KeyUp(WowInput.MOVE_BACK);
            return true;
        }

        public async Task<bool> StartOfCombatWiggle()
        {
            // move back a bit to fix camera direction
            Keyboard.KeyDown(WowInput.MOVE_BACK);
            await Task.Delay(400);
            Keyboard.KeyUp(WowInput.MOVE_BACK);

            // scoot forward a tiny bit to get back in range
            Keyboard.KeyDown(WowInput.MOVE_FORWARD);
            await Task.Delay(20);
            Keyboard.KeyUp(WowInput.MOVE_FORWARD);

            return true;
        }

        public async Task<bool> TurnABitToTheLeftTask()
        {
            Keyboard.KeyDown(WowInput.TURN_LEFT);
            await Task.Delay(500);
            Keyboard.KeyUp(WowInput.TURN_LEFT);

            return true;
        }

        public async Task<bool> GetOutOfWater()
        {
            // Holding jump ascends
            Keyboard.KeyDown(WowInput.JUMP);
            await Task.Delay(1000);
            Keyboard.KeyUp(WowInput.JUMP);

            return true;
        }

        public async Task<bool> AvoidObstacle(bool left)
        {
            // stop walking forward
            await EndWalkForwardTask();

            // back off obstruction
            Keyboard.KeyDown(WowInput.MOVE_BACK);
            await Task.Delay(1000);
            Keyboard.KeyUp(WowInput.MOVE_BACK);

            // strafe
            var strafeKey = left ? WowInput.STRAFE_LEFT : WowInput.STRAFE_RIGHT;
            Keyboard.KeyDown(strafeKey);
            await Task.Delay(2000);
            Keyboard.KeyUp(strafeKey);

            return true;
        }

        public async Task<bool> AvoidObstacleByJumping()
        {
            Keyboard.KeyPress(WowInput.JUMP);
            await Task.Delay(1000);
            Keyboard.KeyPress(WowInput.JUMP);

            return true;
        }

        #endregion
    }
}
