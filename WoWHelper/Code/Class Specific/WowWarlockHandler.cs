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
            public const Keys SHADOW_BOLT = Keys.D2;
            public const Keys SHIFT_2 = Keys.D2;

            public const Keys CORRUPTION = Keys.D3;
            public const Keys SHIFT_IMMOLATE = Keys.D3;

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

        public override async Task<bool> PostBattleRecoveryTask()
        {
            if (ClassState.ShouldSummonPet)
            {
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKey(Input.SUMMON_PET);
            }

            return true;
        }

        public override async Task<bool> PreBattleBuffingTask()
        {
            if (ClassState.ShouldCastDemonArmor)
            {
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKeyWithShift(Input.SHIFT_DEMON_ARMOR);
            }

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

        public override async Task SingleCombatTask()
        {
            Console.WriteLine("Kicking off core combat loop");
            bool emergencyActionTaken = false;

            bool isFacingLongRangeCaster = false;

            bool startedWanding = false;

            await Player.UpdateWorldStateAsync();

            await Player.EveryWorldStateUpdateTasks();

            if (ClassState.ShouldCastDemonArmor)
            {
                await WowInput.PressKeyWithShift(Input.SHIFT_DEMON_ARMOR);
                return;
            }

            if (ShouldCastImmolate())
            {
                await WowInput.PressKeyWithShift(Input.SHIFT_IMMOLATE);
                ImmolateCastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                return;
            }

            if (ShouldCastCorruption())
            {
                await WowInput.PressKey(Input.CORRUPTION);
                CorruptionCastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                return;
            }

            // First do our "Make sure we're not standing around doing nothing" checks
            if (!WorldState.IsTargetLongRangeCaster && await Player.MeleeMakeSureWeAreAttackingEnemyTask())
            {
                return;
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
                return;
            }

            // Next, check if we need to pop any big cooldowns
            if (!emergencyActionTaken && await EmergencyTask())
            {
                emergencyActionTaken = true;
                return;
            }

            if (await Player.ThrowDynamiteTask())
            {
                return;
            }

            if (await Player.UseHealingPotionTask())
            {
                return;
            }
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
