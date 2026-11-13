using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    // TODO: Very old and needs love before actually being used
    public class WowWarlockHandler : WowClassHandler<WowWarlockClassState>
    {
        #region Input

        private static class Input
        {
            /*
             * 2 Bolt
#showtooltip Shadow Bolt
/cast Shadow Bolt
            */
            public const Keys SHADOW_BOLT = Keys.D2;
            public const Keys SHIFT_2 = Keys.D2;

            /*
             * 3 Dot
#showtooltip [mod:shift] Immolate; Corruption
/use [nomod] Corruption
/use [mod:shift] Immolate
             */
            public const Keys CORRUPTION = Keys.D3;
            public const Keys SHIFT_IMMOLATE = Keys.D3;

            /*
             * 4 Buff
#showtooltip [mod:shift] Demon Skin; Summon Imp
/use [nomod] Summon Imp
/use [mod:shift] Demon Skin
            */
            public const Keys SUMMON_PET = Keys.D4;
            public const Keys SHIFT_DEMON_ARMOR = Keys.D4;
        }

        #endregion

        #region Constants

        private const int DOT_TARGET_HP_THRESHOLD = 25;
        // was having issues where it would recast the dot immediately before seeing the debuff on the mob
        // probably just needs a bug fix instead of this
        private const int DOT_RECAST_SUPPRESS_MILLIS = 4000;

        #endregion

        #region State

        private long ImmolateCastTime;
        private long CorruptionCastTime;

        public WowWarlockHandler(WowPlayer player) : base(player) { }

        #endregion

        #region Tasks

        public override async Task<bool> StartBattleReadyRecoverTask()
        {
            if (WorldState.PlayerHpPercent < WowConstants.EAT_FOOD_HP_THRESHOLD)
            {
                await WowInput.PressKey(WowInput.EAT_FOOD);
                await Task.Delay(200);
            }

            if (WorldState.ResourcePercent < WowConstants.DRINK_WATER_MP_THRESHOLD)
            {
                await WowInput.PressKeyWithShift(WowInput.SHIFT_DRINK_WATER);
            }

            return true;
        }

        public override async Task<bool> WaitUntilBattleReadyTask()
        {
            bool hpRecovered = WorldState.PlayerHpPercent >= WowConstants.STOP_RESTING_HP_THRESHOLD;
            bool mpRecovered = WorldState.ResourcePercent >= WowConstants.STOP_RESTING_MP_THRESHOLD;
            bool potionIsCooledDown = !GeneralHelpers.CurrentTimeInsideDuration(Player.HealthPotionTime, WowConstants.POTION_COOLDOWN_MILLIS);
            bool battleReady = hpRecovered && mpRecovered && potionIsCooledDown;

            if (battleReady)
            {
                bool buffed = false;
                if (ClassState.ShouldCastDemonArmor)
                {
                    await Player.WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(Input.SHIFT_DEMON_ARMOR);
                    buffed = true;
                }

                if (ClassState.ShouldSummonPet)
                {
                    await Player.WaitForGlobalCooldownTask();
                    await WowInput.PressKey(Input.SUMMON_PET);
                    buffed = true;
                }

                if (buffed)
                {
                    return false;
                }

                await Player.ScootForwardsTask();
            }

            return battleReady;
        }

        public override bool IsBattleReady()
        {
            throw new System.NotImplementedException($"{nameof(WowWarlockHandler)}.{nameof(IsBattleReady)} isn't implemented yet.");
        }

        public override async Task<bool> KickOffEngageTask()
        {
            await Player.TurnToFaceTargetMarkerTask();
            return true;
        }

        public override async Task<bool> ExecuteEngageTask()
        {
            if (await Player.AbandonUnreachableEngageTarget())
            {
                return false;
            }

            if (!WorldState.IsCurrentlyCasting && !WorldState.IsInCombat)
            {
                await Player.TurnToFaceTargetMarkerTask();
                await WowInput.PressKey(Input.SHADOW_BOLT);
                await Task.Delay(500); // IsCurrentlyCasting can take a little bit to update, give it a buffer
                await Player.UpdateWorldStateAsync();
            }

            return CanEngageTarget();
        }

        public override async Task<bool> CombatLoopTask()
        {
            Console.WriteLine("Kicking off core combat loop");
            bool emergencyActionTaken = false;

            bool isFacingLongRangeCaster = false;

            bool startedWanding = false;

            do
            {
                await Player.UpdateWorldStateAsync();

                await Player.EveryWorldStateUpdateTasks();

                if (ClassState.ShouldCastDemonArmor)
                {
                    await WowInput.PressKeyWithShift(Input.SHIFT_DEMON_ARMOR);
                    continue;
                }

                if (ShouldCastImmolate())
                {
                    await WowInput.PressKeyWithShift(Input.SHIFT_IMMOLATE);
                    ImmolateCastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    continue;
                }

                if (ShouldCastCorruption())
                {
                    await WowInput.PressKey(Input.CORRUPTION);
                    CorruptionCastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    continue;
                }

                // First do our "Make sure we're not standing around doing nothing" checks
                if (!WorldState.IsTargetLongRangeCaster && await Player.MeleeMakeSureWeAreAttackingEnemyTask())
                {
                    continue;
                }

                if (!startedWanding)
                {
                    await Player.StartAttackTask();
                }

                if ((WorldState.IsTargetLongRangeCaster && !isFacingLongRangeCaster) ||
                    (WorldState.TargetNeedsToBeInFront && WorldState.IsTargetLongRangeCaster))
                {
                    await Player.TurnToFaceTargetMarkerTask();
                    isFacingLongRangeCaster = true;
                    continue;
                }

                // Next, check if we need to pop any big cooldowns
                if (!emergencyActionTaken && await EmergencyTask())
                {
                    emergencyActionTaken = true;
                    continue;
                }

                if (await Player.ThrowDynamiteTask())
                {
                    continue;
                }

                if (await Player.UseHealingPotionTask())
                {
                    continue;
                }
            } while (WorldState.IsInCombat);

            return true;
        }

        public override async Task SingleCombatTask()
        {
            await CombatLoopTask();
        }

        public override bool CanEngageTarget()
        {
            return ClassState.CanSpellcastPullTarget;
        }

        private bool ShouldConsiderCastingDot()
        {
            return WorldState.TargetHpPercent > DOT_TARGET_HP_THRESHOLD
                //|| WorldState.AttackerCount > 1
                || WorldState.PlayerHpPercent <= WowConstants.HEALING_POTION_HP_THRESHOLD;
        }

        private bool ShouldCastImmolate()
        {
            if (!ShouldConsiderCastingDot())
            {
                return false;
            }

            if (GeneralHelpers.CurrentTimeInsideDuration(ImmolateCastTime, DOT_RECAST_SUPPRESS_MILLIS))
            {
                return false;
            }

            return ClassState.ShouldCastImmolate;
        }

        private bool ShouldCastCorruption()
        {
            if (!ShouldConsiderCastingDot())
            {
                return false;
            }

            if (GeneralHelpers.CurrentTimeInsideDuration(CorruptionCastTime, DOT_RECAST_SUPPRESS_MILLIS))
            {
                return false;
            }

            return ClassState.ShouldCastCorruption;
        }

        private async Task<bool> EmergencyTask()
        {
            await Task.Delay(0);
            bool tooManyAttackers = WorldState.AttackerCount >= WowConstants.TOO_MANY_ATTACKERS_THRESHOLD;
            bool emergencyHpThreshold = WorldState.PlayerHpPercent <= WowConstants.EMERGENCY_HP_THRESHOLD;

            if (tooManyAttackers || emergencyHpThreshold)
            {
                string warningMessage = tooManyAttackers ? "TOO MANY ATTACKERS HELP" : $"Emergency HP Threshold hit ({WorldState.PlayerHpPercent})";
                Player.SetLogoutVariablesAndSlack($"Got into an emergency situation ({warningMessage}), logging off for safety");

                await Player.WaitForGlobalCooldownTask();
                await Player.ThrowDynamiteTask(forceThrow: true);

                await Player.WaitForGlobalCooldownTask();
                await Player.ThrowTargetDummyTask();

                return true;
            }

            return false;
        }

        #endregion
    }
}
