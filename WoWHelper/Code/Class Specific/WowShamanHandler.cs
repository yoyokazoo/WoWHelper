using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    // TODO: Very old and needs love before actually being used
    public class WowShamanHandler : WowClassHandler<WowShamanClassState>
    {
        #region Input

        private static class Input
        {
            public const Keys LIGHTNING_BOLT = Keys.D2;
            public const Keys SHIFT_LIGHTNING_BOLT_RANK_1 = Keys.D2;

            public const Keys EARTH_SHOCK = Keys.D3;
            public const Keys SHIFT_FLAME_SHOCK = Keys.D3;

            public const Keys LIGHTNING_SHIELD = Keys.D4;
            public const Keys SHIFT_ROCKBITER_WEAPON = Keys.D4;

            public const Keys CURE_POISON = Keys.D5;
            public const Keys SHIFT_CURE_DISEASE = Keys.D5;

            public const Keys FROST_SHOCK = Keys.D6;
            public const Keys SHIFT_6 = Keys.D6;

            public const Keys KEY_7 = Keys.D7;
            public const Keys SHIFT_7 = Keys.D7;
        }

        #endregion

        #region Constants

        #endregion

        #region State

        public WowShamanHandler(WowPlayer player) : base(player) { }

        #endregion

        #region Tasks

        public override async Task SingleCombatTask()
        {
            Console.WriteLine("Kicking off core combat loop");
            bool emergencyActionTaken = false;
            bool isFacingLongRangeCaster = false;

            await Player.StartAttackTask();

            await Player.UpdateWorldStateAsync();

            await Player.EveryWorldStateUpdateTasks();

            // Needs Battle Shout treatment
            if (ClassState.ShouldCastRockbiterWeapon)
            {
                await WowInput.PressKeyWithShift(Input.SHIFT_ROCKBITER_WEAPON);
                return;
            }
            else if (ShouldCastLightningShield())
            {
                await WowInput.PressKey(Input.LIGHTNING_SHIELD);
                return;
            }

            // First do our "Make sure we're not standing around doing nothing" checks
            if (!WorldState.IsTargetLongRangeCaster && await Player.MeleeMakeSureWeAreAttackingEnemyTask())
            {
                return;
            }

            if ((WorldState.IsTargetLongRangeCaster && !isFacingLongRangeCaster) ||
                (WorldState.TargetNeedsToBeInFront && WorldState.IsTargetLongRangeCaster))
            {
                await Player.TurnToFaceTargetMarkerTask();
                isFacingLongRangeCaster = true;
                return;
            }

            if (WorldState.IsTargetLongRangeCaster && !ClassState.IsInEarthShockRange)
            {
                await RunIntoEarthShockRange();
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

            if (ShouldCastFlameShock())
            {
                Console.WriteLine($"Trying to Flame Shock!");
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKeyWithShift(Input.SHIFT_FLAME_SHOCK);
            }
            else if (ShouldCastEarthShock())
            {
                Console.WriteLine($"Trying to Earth Shock!");
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKey(Input.EARTH_SHOCK);
            }
            else if (ShouldCastFrostShock())
            {
                Console.WriteLine($"Trying to Frost Shock!");
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKey(Input.FROST_SHOCK);
            }
            else if (ShouldCastCureDisease())
            {
                Console.WriteLine($"Trying to Cure Disease!");
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKeyWithShift(Input.SHIFT_CURE_DISEASE);
            }
            else if (ShouldCastCurePoison())
            {
                Console.WriteLine($"Trying to Cure Poison!");
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKey(Input.CURE_POISON);
            }
            else// if (WorldState.AttackerCount <= 1)
            {
                // we should already be attacking
                //await WowInput.PressKey(WowInput.SHAMAN_ATTACK);
            }
        }

        private async Task<bool> RunIntoEarthShockRange()
        {
            await Player.TurnToFaceTargetMarkerTask();
            await Player.StartWalkForwardTask();
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(1000);
                Player.UpdateWorldState();
                if (ClassState.IsInEarthShockRange)
                {
                    break;
                }
            }
            await Player.EndWalkForwardTask();
            return ClassState.IsInEarthShockRange;
        }

        private bool ShouldCastLightningShield()
        {
            var location = Player.FarmingLocationConfiguration;
            if (location != null && location.AllMobsInZoneAreNatureImmune)
            {
                return false;
            }

            // Skip if the only mob we're fighting is nature immune
            // We still waste charges in the multi-attacker scenario, but we just want to dump mana to kill ASAP in those cases
            bool skipLightningShield = WorldState.IsTargetNatureImmune && WorldState.AttackerCount <= 1;

            if (skipLightningShield)
            {
                return false;
            }

            return ClassState.ShouldCastLightningShield;
        }

        private bool ShouldCastFlameShock()
        {
            // Skip if fire immune
            bool skipFlameShock = WorldState.IsTargetFireImmune;
            // Always shock runners, nature immune, and high hp mobs
            skipFlameShock |= !WorldState.IsTargetRunnerMob && !WorldState.IsTargetNatureImmune && WorldState.TargetHpPercent < 75;
            // Open question if we should flame shock casters at the start of fights. Probably worth waiting?
            skipFlameShock |= (WorldState.IsTargetCasterMob || WorldState.IsTargetLongRangeCaster) && !WorldState.IsTargetNatureImmune;

            if (skipFlameShock)
            {
                return false;
            }

            return ClassState.ShouldCastFlameShock;
        }

        private bool ShouldCastEarthShock()
        {
            // Skip if nature immune -- Frost Shock takes over entirely for those targets,
            // see ShouldCastFrostShock below.
            bool skipEarthShock = WorldState.IsTargetNatureImmune;
            // Always shock runners, if we're low hp, if there are multiple mobs, or if the mob is high hp
            skipEarthShock |= !WorldState.IsTargetRunnerMob && WorldState.PlayerHpPercent > 50 && WorldState.AttackerCount <= 1 && WorldState.TargetHpPercent < 20 && !WorldState.IsTargetCasterMob && !WorldState.IsTargetCasting;
            // Don't earth shock casters that aren't currently casting
            // TODO: Rename IsTargetCasterMob to something like "ShouldWaitToCounterspell" or something
            skipEarthShock |= (WorldState.IsTargetCasterMob || WorldState.IsTargetLongRangeCaster) && !WorldState.IsTargetCasting;

            if (skipEarthShock)
            {
                return false;
            }

            return ClassState.CanCastEarthShock;
        }

        private bool ShouldCastFrostShock()
        {
            if (!WorldState.IsTargetNatureImmune)
            {
                return false;
            }

            bool skipFrostShock = !WorldState.IsTargetRunnerMob && WorldState.PlayerHpPercent > 50 && WorldState.AttackerCount <= 1 && WorldState.TargetHpPercent < 20;

            if (skipFrostShock)
            {
                return false;
            }

            return ClassState.CanCastFrostShock;
        }

        private bool ShouldCastCurePoison()
        {
            return WorldState.PlayerIsPoisoned && ClassState.CanCurePoison;
        }

        private bool ShouldCastCureDisease()
        {
            return WorldState.PlayerIsDiseased && ClassState.CanCureDisease;
        }

        public override async Task<bool> PostBattleRecoveryTask()
        {
            if (ShouldCastCurePoison())
            {
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKey(Input.CURE_POISON);
            }

            if (ShouldCastCureDisease())
            {
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKeyWithShift(Input.SHIFT_CURE_DISEASE);
            }

            return true;
        }

        public override async Task<bool> PreBattleBuffingTask()
        {
            if (ClassState.ShouldCastRockbiterWeapon)
            {
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKeyWithShift(Input.SHIFT_ROCKBITER_WEAPON);
            }

            if (ShouldCastLightningShield())
            {
                await Player.WaitForGlobalCooldownTask();
                await WowInput.PressKey(Input.LIGHTNING_SHIELD);
            }

            return true;
        }

        public override async Task<bool> ExecuteEngageTask()
        {
            if (await Player.AbandonUnreachableEngageTarget())
            {
                return false;
            }

            if (WorldState.IsTargetLongRangeCaster && !ClassState.IsInEarthShockRange)
            {
                await RunIntoEarthShockRange();
            }

            Console.WriteLine($"Shaman ExecuteEngageTask, EngageAttempts {Player.EngageAttempts}, WorldState.IsCurrentlyCasting? {WorldState.IsCurrentlyCasting}, WorldState.IsInCombat? {WorldState.IsInCombat}");
            if (!WorldState.IsCurrentlyCasting && !WorldState.IsInCombat)
            {
                await Player.TurnToFaceTargetMarkerTask();

                if (WorldState.IsTargetNatureImmune)
                {
                    await WowInput.PressKeyWithShift(Input.SHIFT_LIGHTNING_BOLT_RANK_1);
                }
                else
                {
                    await WowInput.PressKey(Input.LIGHTNING_BOLT);
                }

                await Task.Delay(500); // IsCurrentlyCasting can take a little bit to update, give it a buffer
                await Player.UpdateWorldStateAsync();
            }

            return CanEngageTarget();
        }

        public override bool CanEngageTarget()
        {
            return ClassState.CanSpellcastPullTarget;
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
