using InputManager;
using System;
using System.Numerics;
using System.Threading.Tasks;
using WindowsGameAutomationTools.Images;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;
using WoWHelper.Code.Gameplay;
using System.Windows.Forms;

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
            if (FarmingConfig.LogoutOnLowDynamite && WorldState.LowOnDynamite)
            {
                LogoutTriggered = true;
                LogoutReason = $"Low on Dynamite";
            }
            else if (WorldState.LowOnHealthPotions)
            {
                LogoutTriggered = true;
                LogoutReason = $"Low on Health Potions";
            }
            else if (WorldState.LowOnAmmo && FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Shoot)
            {
                LogoutTriggered = true;
                LogoutReason = $"Low on Ammo";
            }
            else if (!CurrentTimeInsideDuration(FarmStartTime, WowPlayerConstants.FARM_TIME_LIMIT_MILLIS))
            {
                LogoutTriggered = true;
                LogoutReason = $"Farm Time Limit Reached";
            }
            else if (EngageAttempts >= WowPlayerConstants.ENGAGE_ROTATION_ATTEMPTS)
            {
                LogoutTriggered = true;
                LogoutReason = $"Failed to engage target after {WowPlayerConstants.ENGAGE_ROTATION_ATTEMPTS} loops.  Something wrong?";
            }
            else if (FarmingConfig.LogoutOnFullBags && WorldState.BagsAreFull)
            {
                LogoutTriggered = true;
                LogoutReason = $"Bags are full!";
            }

            // also send once-per-session alerts here
            if (FarmingConfig.AlertOnFullBags && !FullBagsAlertSent && WorldState.BagsAreFull)
            {
                SlackHelper.SendMessageToChannel($"Bags are full!");
                FullBagsAlertSent = true;
            }

            await Task.Delay(0);

            return LogoutTriggered;
        }

        public async Task<bool> StartLogoutTask()
        {
            Console.WriteLine($"Starting logout: {LogoutReason}");
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

        public async Task<bool> PetriAltF4Task()
        {
            while (!WorldState.GCDCooledDown)
            {
                await UpdateWorldStateAsync();
            }

            await WowInput.PressKeyWithShift(WowInput.SHIFT_PETRIFICATION_FLASK);
            await Task.Delay(750);
            await WowInput.PressKeyWithAlt(WowInput.ALT_FORCE_QUIT_KEY);
            return true;
        }

        public async Task<bool> ThrowTargetDummyTask()
        {
            Mouse.ButtonDown(Mouse.MouseKeys.Left);
            await Task.Delay(30);
            Mouse.MoveRelative(0, 200);
            await Task.Delay(30);
            Mouse.ButtonUp(Mouse.MouseKeys.Left);

            await Task.Delay(250);

            await WowInput.PressKeyWithShift(WowInput.SHIFT_TARGET_DUMMY);
            await Task.Delay(250);
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
            if (WorldState.PlayerHpPercent < WowPlayerConstants.EAT_FOOD_HP_THRESHOLD)
            {
                await WowInput.PressKeyWithShift(WowInput.SHIFT_EAT_FOOD_KEY);
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

        public async Task<bool> KickOffEngageTask()
        {
            await Task.Delay(0);
            EngageAttempts = 1;

            if (FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Charge)
            {
                Keyboard.KeyPress(WowInput.CHARGE_KEY);
                return true;
            }
            else if (FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Shoot)
            {
                Keyboard.KeyPress(WowInput.SHOOT_MACRO);
                return true;
            }

            return false;
        }

        public async Task<bool> FaceCorrectDirectionToEngageTask()
        {
            EngageAttempts++;

            if (FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Charge)
            {
                await TurnABitToTheLeftTask();
                Keyboard.KeyPress(WowInput.CHARGE_KEY);
            }
            else if (FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Shoot)
            {
                if (!WorldState.WaitingToShoot)
                {
                    await TurnABitToTheLeftTask();
                    Keyboard.KeyPress(WowInput.SHOOT_MACRO);
                }
            }

            return CanEngageTarget();
        }

        public async Task<bool> StartAttackTask()
        {
            await Task.Delay(0);

            // always kick things off with heroic strike macro to /startattack
            Keyboard.KeyPress(WowInput.MORTALSTRIKE_BLOODTHIRST_MACRO);

            return true;
        }

        public async Task<bool> MakeSureWeAreAttackingEnemyTask()
        {
            bool attackerJustDied = PreviousWorldState?.AttackerCount > WorldState.AttackerCount && WorldState.AttackerCount > 0;
            bool inCombatButNotAutoAttacking = WorldState.IsInCombat && !WorldState.IsAutoAttacking;
            bool tooFarAway = WorldState.TooFarAway;
            bool facingWrongWay = WorldState.FacingWrongWay; // potentially need to turn in case we're webbed and backing up wont work
            bool targetNeedsToBeInFront = WorldState.TargetNeedsToBeInFront;

            // now since we have more accurate "are they in front" checking, try not backing up if attacker just died
            if (/*attackerJustDied || */facingWrongWay || targetNeedsToBeInFront)
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
                Keyboard.KeyPress(WowInput.MORTALSTRIKE_BLOODTHIRST_MACRO);
            }

            return attackerJustDied || inCombatButNotAutoAttacking || tooFarAway || facingWrongWay || targetNeedsToBeInFront;
        }

        public async Task<bool> TooManyAttackersTask()
        {
            bool tooManyAttackers = WorldState.AttackerCount >= FarmingConfig.TooManyAttackersThreshold;

            if (tooManyAttackers)
            {
                SlackHelper.SendMessageToChannel($"TOO MANY ATTACKERS HELP");

                // cast retaliation once GCD is cooled down
                while (!WorldState.GCDCooledDown)
                {
                    await UpdateWorldStateAsync();
                }
                await WowInput.PressKeyWithShift(WowInput.SHIFT_RETALIATION_KEY);

                LogoutReason = "Got into a Retaliation situation, logging off for safety";
                LogoutTriggered = true;
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
                if (FarmingConfig.AlertOnPotionUsed)
                {
                    SlackHelper.SendMessageToChannel("Potion used!");
                }
                Keyboard.KeyPress(WowInput.HEALING_POTION_KEY);
                await Task.Delay(0);
            }

            return shouldUseHealingPotion;
        }

        public async Task<bool> UseDiamondFlaskTask()
        {
            bool shouldUseDiamondFlask = WorldState.AttackerCount > 1 &&
                !CurrentTimeInsideDuration(HealingTrinketTime, WowGameplayConstants.DIAMOND_FLASK_COOLDOWN_MILLIS);

            if (shouldUseDiamondFlask)
            {
                HealingTrinketTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                await WowInput.PressKeyWithShift(WowInput.SHIFT_HEALING_TRINKET);
            }

            return shouldUseDiamondFlask;
        }

        public async Task<bool> UseHealingTrinketTask()
        {
            bool shouldUseHealingTrinket = WorldState.PlayerHpPercent <= WowGameplayConstants.HEALING_TRINKET_HP_THRESHOLD &&
                !CurrentTimeInsideDuration(HealingTrinketTime, WowGameplayConstants.HEALING_TRINKET_COOLDOWN_MILLIS);

            if (shouldUseHealingTrinket)
            {
                HealingTrinketTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                await WowInput.PressKeyWithShift(WowInput.SHIFT_HEALING_TRINKET);
            }

            return shouldUseHealingTrinket;
        }

        public async Task<bool> StartOfCombatBerserkerRage()
        {
            await WowInput.PressKeyWithShift(WowInput.SHIFT_BERSERKER_RAGE_MACRO);
            await Task.Delay(150);
            Keyboard.KeyPress(WowInput.MORTALSTRIKE_BLOODTHIRST_MACRO);
            await Task.Delay(150);
            Keyboard.KeyPress(WowInput.MORTALSTRIKE_BLOODTHIRST_MACRO);
            await Task.Delay(150);
            Keyboard.KeyPress(WowInput.MORTALSTRIKE_BLOODTHIRST_MACRO);
            await Task.Delay(150);
            Keyboard.KeyPress(WowInput.MORTALSTRIKE_BLOODTHIRST_MACRO);

            return true;
        }

        #endregion

        #region Movement Tasks

        // Returns true if we've reached the waypoint
        // Returns false if we haven't yet reached the waypoint
        // Rotates towards the waypoint or walks towards the waypoint, depending
        public async Task<bool> MoveTowardsWaypointTask()
        {
            var waypoint = FarmingConfig.LocationConfiguration.Waypoints[CurrentWaypointIndex];
            float waypointDistance = Vector2.Distance(WorldState.PlayerLocation, waypoint);
            float desiredDegrees = WowPathfinding.GetDesiredDirectionInDegrees(WorldState.PlayerLocation, waypoint);
            float degreesDifference = WowPathfinding.GetDegreesToMove(WorldState.FacingDegrees, desiredDegrees);

            Console.WriteLine($"Heading towards waypoint {waypoint}. At {WorldState.MapX},{WorldState.MapY}.  DesiredDegrees: {desiredDegrees}, facing degrees: {WorldState.FacingDegrees}.  DegreesDifference: {degreesDifference}");

            if (waypointDistance <= FarmingConfig.LocationConfiguration.DistanceTolerance)
            {
                Console.WriteLine($"Arrived at {waypoint} ({WorldState.MapX},{WorldState.MapY})");
                await EndWalkForwardTask();
                return true;
            }

            if (Math.Abs(degreesDifference) > WowPathfinding.GetWaypointDegreesTolerance(waypointDistance))
            {
                Console.WriteLine($"degreesDifference too large, rotating to heading");
                //await EndWalkForwardTask();
                await RotateToDirectionTask(desiredDegrees, waypointDistance);
                return false;
            }
            else
            {
                // if we're already walking, ignore this
                Console.WriteLine($"Start walking forward");
                await StartWalkForwardTask();

                var lateralDistance = WowPathfinding.GetLateralDistance(WorldState.FacingDegrees, WorldState.PlayerLocation, waypoint);
                if (Math.Abs(lateralDistance) > WowPathfinding.STRAFE_LATERAL_DISTANCE_TOLERANCE)
                {
                    if (lateralDistance > 0)
                    {
                        Keyboard.KeyDown(WowInput.STRAFE_RIGHT);
                    }
                    else
                    {
                        Keyboard.KeyDown(WowInput.STRAFE_LEFT);
                    }
                }
                else
                {
                    Keyboard.KeyUp(WowInput.STRAFE_LEFT);
                    Keyboard.KeyUp(WowInput.STRAFE_RIGHT);
                }

                return false;
            }
        }

        public async Task<bool> RotateToDirectionTask(float desiredDegrees, float distance)
        {
            await Task.Delay(0);
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

                    Keys directionKey = degreesToMove <= 0 ? WowInput.TURN_RIGHT : WowInput.TURN_LEFT;

                    if (directionKey == WowInput.TURN_RIGHT)
                    {
                        Keyboard.KeyUp(WowInput.TURN_LEFT);
                    }
                    else
                    {
                        Keyboard.KeyUp(WowInput.TURN_RIGHT);
                    }

                    Keyboard.KeyDown(directionKey);
                }
            }
            finally
            {
                Keyboard.KeyUp(WowInput.TURN_LEFT);
                Keyboard.KeyUp(WowInput.TURN_RIGHT);
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
            Keyboard.KeyUp(WowInput.STRAFE_LEFT);
            Keyboard.KeyUp(WowInput.STRAFE_RIGHT);
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
