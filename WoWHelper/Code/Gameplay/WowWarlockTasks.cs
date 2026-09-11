using InputManager;
using System;
using System.Threading.Tasks;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        // Warlock rotation -- not implemented yet. Mirrors the six dispatch
        // entry points WowWarriorTasks/WowMageTasks/WowShamanTasks each
        // provide (see WowPlayerCombatConfig.cs's six switches), stubbed out
        // so WowCombatConfiguration.Warlock dispatches cleanly to here
        // instead of hitting the "no dispatch implemented" NotImplementedException
        // in WowPlayerCombatConfig.cs. Each throws its own NotImplementedException
        // for now -- fill these in the same way WowShamanTasks.cs does for
        // Shaman (that file is the most recently-added class and the closest
        // model to follow).
        public async Task<bool> WarlockStartBattleReadyRecoverTask(WowWarlockClassState classState)
        {
            if (WorldState.PlayerHpPercent < WowPlayerConstants.EAT_FOOD_HP_THRESHOLD)
            {
                Keyboard.KeyPress(WowInput.EAT_FOOD);
                await Task.Delay(200);
            }

            if (WorldState.ResourcePercent < WowPlayerConstants.DRINK_WATER_MP_THRESHOLD)
            {
                await WowInput.PressKeyWithShift(WowInput.SHIFT_DRINK_WATER);
            }

            return true;
        }

        public async Task<bool> WarlockWaitUntilBattleReadyTask(WowWarlockClassState classState)
        {
            // For now, I don't care if dynamite is cooled down.  If we dynamited and didn't have to potion, we're probably safe enough to keep going
            // especially since the dynamite cooldown is so short it'll probably be up by the time we need it again.

            bool hpRecovered = WorldState.PlayerHpPercent >= WowPlayerConstants.STOP_RESTING_HP_THRESHOLD;
            bool mpRecovered = WorldState.ResourcePercent >= WowPlayerConstants.STOP_RESTING_MP_THRESHOLD;
            bool potionIsCooledDown = !WowPlayer.CurrentTimeInsideDuration(HealthPotionTime, WowGameplayConstants.POTION_COOLDOWN_MILLIS);
            bool battleReady = hpRecovered && mpRecovered && potionIsCooledDown;

            if (battleReady)
            {
                bool buffed = false;
                if (classState.ShouldCastRockbiterWeapon)
                {
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(WowInput.WARLOCK_SHIFT_DEMON_ARMOR);
                    buffed = true;
                }

                if (buffed)
                {
                    return false;
                }

                await ScootForwardsTask();
            }

            return battleReady;
        }

        public async Task<bool> WarlockKickOffEngageTask(WowWarlockClassState classState)
        {
            await Task.Delay(0);
            EngageAttempts = 1;
            await TurnToFaceTargetMarkerTask();
            return true;
        }

        public async Task<bool> WarlockFaceCorrectDirectionToEngageTask(WowWarlockClassState classState)
        {
            EngageAttempts++;

            if (AbandonUnreachableEngageTarget())
            {
                return false;
            }

            //Console.WriteLine($"WarlockFaceCorrectDirectionToEngageTask, EngageAttempts {EngageAttempts}, WorldState.IsCurrentlyCasting? {WorldState.IsCurrentlyCasting}, WorldState.IsInCombat? {WorldState.IsInCombat}");
            if (!WorldState.IsCurrentlyCasting && !WorldState.IsInCombat)
            {
                await TurnToFaceTargetMarkerTask();
                Keyboard.KeyPress(WowInput.WARLOCK_SHADOW_BOLT);
                await Task.Delay(500); // IsCurrentlyCasting can take a little bit to update, give it a buffer
                await UpdateWorldStateAsync();
            }

            return WarlockCanEngageTarget(classState);
        }

        public async Task<bool> WarlockCombatLoopTask(WowWarlockClassState classState)
        {
            Console.WriteLine("Kicking off core combat loop");
            bool thrownDynamite = false;
            bool potionUsed = false;
            bool emergencyActionTaken = false;

            bool isFacingLongRangeCaster = false;
            bool hasWalkedTowardsLongRangeCaster = false;

            await StartAttackTask();

            do
            {
                await UpdateWorldStateAsync();

                await EveryWorldStateUpdateTasks();

                if (classState.ShouldCastRockbiterWeapon)
                {
                    await WowInput.PressKeyWithShift(WowInput.WARLOCK_SHIFT_DEMON_ARMOR);
                    continue;
                }

                // First do our "Make sure we're not standing around doing nothing" checks
                if (!WorldState.IsTargetLongRangeCaster && await MeleeMakeSureWeAreAttackingEnemyTask())
                {
                    continue;
                }

                if ((WorldState.IsTargetLongRangeCaster && !isFacingLongRangeCaster) ||
                    (WorldState.TargetNeedsToBeInFront && WorldState.IsTargetLongRangeCaster))
                {
                    await TurnToFaceTargetMarkerTask();
                    isFacingLongRangeCaster = true;
                    continue;
                }

                // Next, check if we need to pop any big cooldowns
                if (!emergencyActionTaken && await WarlockEmergencyTask())
                {
                    emergencyActionTaken = true;
                    continue;
                }

                if (!thrownDynamite && await ThrowDynamiteTask())
                {
                    DynamiteTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    thrownDynamite = true;
                    continue;
                }

                if (!potionUsed && await UseHealingPotionTask())
                {
                    HealthPotionTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    potionUsed = true;
                    continue;
                }

                if (WorldState.IsTargetLongRangeCaster && WorldState.TooFarAway && !hasWalkedTowardsLongRangeCaster)
                {
                    await StartWalkForwardTask();
                    await Task.Delay(500);
                    await EndWalkForwardTask();
                    hasWalkedTowardsLongRangeCaster = true;
                    continue;
                }

            } while (WorldState.IsInCombat);

            return true;
        }

        // Real per-class logic goes directly here (not a dispatch) -- see the
        // comment above WowPlayerCombatConfig.CanEngageTarget for why each
        // class gets its own thin CanEngageTarget wrapper instead of sharing
        // one method.
        public bool WarlockCanEngageTarget(WowWarlockClassState classState)
        {
            return classState.CanSpellcastPullTarget;
        }

        public async Task<bool> WarlockEmergencyTask()
        {
            await Task.Delay(0);
            bool tooManyAttackers = WorldState.AttackerCount >= FarmingConfig.TooManyAttackersThreshold;
            bool emergencyHpThreshold = WorldState.PlayerHpPercent <= WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD;

            if (tooManyAttackers || emergencyHpThreshold)
            //if(true)
            {
                string warningMessage = tooManyAttackers ? "TOO MANY ATTACKERS HELP" : $"Emergency HP Threshold hit ({WorldState.PlayerHpPercent})";
                SlackHelper.SendMessageToChannel(warningMessage);

                // Figure out what to do here.  War stomp? Magma Totem? War stomp -> ghost wolf -> run to safety?
                // Stoneskin totem for now
                //Keyboard.KeyPress(WowInput.THROW_DYNAMITE);
                await WaitForGlobalCooldownTask();
                await ThrowDynamiteTask(forceThrow: true);

                await WaitForGlobalCooldownTask();
                await ThrowTargetDummyTask();

                LogoutReason = $"Got into an emergency situation ({warningMessage}), logging off for safety";
                LogoutTriggered = true;
                return true;
            }

            return false;
        }
    }
}
