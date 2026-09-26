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
        // Warlock rotation -- in progress. Mirrors the six dispatch entry
        // points WowWarriorTasks/WowShamanTasks each provide
        // (see WowPlayerCombatConfig.cs's six switches). Modeled closely on
        // WowShamanTasks.cs since Shaman is the closest existing
        // ranged-pull/melee-sustain pattern to follow.
        public async Task<bool> WarlockStartBattleReadyRecoverTask(WowWarlockClassState classState)
        {
            if (WorldState.PlayerHpPercent < WowPlayerConstants.EAT_FOOD_HP_THRESHOLD)
            {
                await WowInput.PressKey(WowInput.EAT_FOOD);
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
            bool potionIsCooledDown = !GeneralHelpers.CurrentTimeInsideDuration(HealthPotionTime, WowGameplayConstants.POTION_COOLDOWN_MILLIS);
            bool battleReady = hpRecovered && mpRecovered && potionIsCooledDown;

            if (battleReady)
            {
                bool buffed = false;
                if (classState.ShouldCastDemonArmor)
                {
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(WowInput.WARLOCK_SHIFT_DEMON_ARMOR);
                    buffed = true;
                }

                if (classState.ShouldSummonPet)
                {
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKey(WowInput.WARLOCK_SUMMON_PET);
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

            if (await AbandonUnreachableEngageTarget())
            {
                return false;
            }

            //Console.WriteLine($"WarlockFaceCorrectDirectionToEngageTask, EngageAttempts {EngageAttempts}, WorldState.IsCurrentlyCasting? {WorldState.IsCurrentlyCasting}, WorldState.IsInCombat? {WorldState.IsInCombat}");
            if (!WorldState.IsCurrentlyCasting && !WorldState.IsInCombat)
            {
                await TurnToFaceTargetMarkerTask();
                await WowInput.PressKey(WowInput.WARLOCK_SHADOW_BOLT);
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

            bool startedWanding = false;

            do
            {
                await UpdateWorldStateAsync();

                await EveryWorldStateUpdateTasks();

                if (classState.ShouldCastDemonArmor)
                {
                    await WowInput.PressKeyWithShift(WowInput.WARLOCK_SHIFT_DEMON_ARMOR);
                    continue;
                }

                if (WarlockShouldCastImmolate(classState))
                {
                    await WowInput.PressKeyWithShift(WowInput.WARLOCK_SHIFT_IMMOLATE);
                    ImmolateCastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    continue;
                }

                if (WarlockShouldCastCorruption(classState))
                {
                    await WowInput.PressKey(WowInput.WARLOCK_CORRUPTION);
                    CorruptionCastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    continue;
                }

                // First do our "Make sure we're not standing around doing nothing" checks
                if (!WorldState.IsTargetLongRangeCaster && await MeleeMakeSureWeAreAttackingEnemyTask())
                {
                    continue;
                }

                if (!startedWanding)
                {
                    await StartAttackTask();
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

        // Shared gate for both DoTs below: only worth spending a GCD on a fresh DoT if the
        // mob isn't already about to die from direct damage
        // (WARLOCK_DOT_TARGET_HP_THRESHOLD) -- unless we're fighting more than one
        // attacker (the extra sustained damage helps) or we're low enough on HP that
        // ending the fight by any means available is the priority.
        public bool WarlockShouldConsiderCastingDot()
        {
            return WorldState.TargetHpPercent > WowGameplayConstants.WARLOCK_DOT_TARGET_HP_THRESHOLD
                //|| WorldState.AttackerCount > 1
                || WorldState.PlayerHpPercent <= WowGameplayConstants.HEALING_POTION_HP_THRESHOLD;
        }

        // classState.ShouldCastImmolate goes true the instant Immolate is cast (it just
        // checks TargetHasDebuffSpellName("Immolate")), but the debuff icon/pixel takes a
        // little while to actually show up, so without a cooldown of our own here the combat
        // loop re-presses Immolate several times before the first cast is ever reflected.
        // ImmolateCastTime is set right after we press the key (see WarlockCombatLoopTask).
        public bool WarlockShouldCastImmolate(WowWarlockClassState classState)
        {
            if (!WarlockShouldConsiderCastingDot())
            {
                return false;
            }

            if (GeneralHelpers.CurrentTimeInsideDuration(ImmolateCastTime, WowGameplayConstants.WARLOCK_DOT_RECAST_SUPPRESS_MILLIS))
            {
                return false;
            }

            return classState.ShouldCastImmolate;
        }

        // Same double-cast problem/fix as WarlockShouldCastImmolate above, for Corruption.
        public bool WarlockShouldCastCorruption(WowWarlockClassState classState)
        {
            if (!WarlockShouldConsiderCastingDot())
            {
                return false;
            }

            if (GeneralHelpers.CurrentTimeInsideDuration(CorruptionCastTime, WowGameplayConstants.WARLOCK_DOT_RECAST_SUPPRESS_MILLIS))
            {
                return false;
            }

            return classState.ShouldCastCorruption;
        }

        public async Task<bool> WarlockEmergencyTask()
        {
            await Task.Delay(0);
            bool tooManyAttackers = WorldState.AttackerCount >= WowPlayerConstants.TOO_MANY_ATTACKERS_THRESHOLD;
            bool emergencyHpThreshold = WorldState.PlayerHpPercent <= WowPlayerConstants.EMERGENCY_HP_THRESHOLD;

            if (tooManyAttackers || emergencyHpThreshold)
            //if(true)
            {
                string warningMessage = tooManyAttackers ? "TOO MANY ATTACKERS HELP" : $"Emergency HP Threshold hit ({WorldState.PlayerHpPercent})";
                SlackHelper.SendMessageToChannel(warningMessage);

                // Figure out what to do here.  War stomp? Magma Totem? War stomp -> ghost wolf -> run to safety?
                // Stoneskin totem for now
                //await WowInput.PressKey(WowInput.THROW_DYNAMITE);
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
