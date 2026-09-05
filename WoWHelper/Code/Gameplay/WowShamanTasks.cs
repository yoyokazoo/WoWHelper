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
        public async Task<bool> ShamanCombatLoopTask(WowShamanClassState classState)
        {
            Console.WriteLine("Kicking off core combat loop");
            bool thrownDynamite = false;
            bool potionUsed = false;
            bool emergencyActionTaken = false;
            bool startOfCombatWiggled = false;

            await StartAttackTask();

            do
            {
                await UpdateWorldStateAsync();

                await EveryWorldStateUpdateTasks();

                // Make sure to buff
                // TODO: this needs to be changed to raw resource
                // TODO: move this to a helper too since it's used in a couple places (see
                // ShamanShouldCastLightningShield/EarthShock/FlameShock below)?
                if (classState.ShouldCastRockbiterWeapon)
                {
                    await WowInput.PressKeyWithShift(WowInput.SHAMAN_SHIFT_ROCKBITER_WEAPON);
                    continue;
                }
                else if (ShamanShouldCastLightningShield(classState))
                {
                    Keyboard.KeyPress(WowInput.SHAMAN_LIGHTNING_SHIELD);
                    continue;
                }

                // First do our "Make sure we're not standing around doing nothing" checks
                if (await MeleeMakeSureWeAreAttackingEnemyTask())
                {
                    continue;
                }

                // Next, check if we need to pop any big cooldowns
                if (!emergencyActionTaken && await ShamanEmergencyTask())
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

                if (!startOfCombatWiggled && PreviousWorldState.TargetHpPercent == 100 && WorldState.TargetHpPercent < 100)
                {
                    await StartOfCombatWiggle();
                    startOfCombatWiggled = true; // maybe not necessary? if they keep going to 100 maybe they're evading and it's good to keep backing up?
                }

                if (ShamanShouldCastFlameShock(classState))
                {
                    Console.WriteLine($"Trying to Flame Shock!");
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(WowInput.SHAMAN_SHIFT_FLAME_SHOCK);
                }
                else if (ShamanShouldCastEarthShock(classState))
                {
                    Console.WriteLine($"Trying to Earth Shock!");
                    await WaitForGlobalCooldownTask();
                    Keyboard.KeyPress(WowInput.SHAMAN_EARTH_SHOCK);
                }
                else if (ShamanShouldCastCureDisease(classState))
                {
                    Console.WriteLine($"Trying to Cure Disease!");
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(WowInput.SHAMAN_SHIFT_CURE_DISEASE);
                }
                else if (ShamanShouldCastCurePoison(classState))
                {
                    Console.WriteLine($"Trying to Cure Poison!");
                    await WaitForGlobalCooldownTask();
                    Keyboard.KeyPress(WowInput.SHAMAN_CURE_POISON);
                }
                else// if (WorldState.AttackerCount <= 1)
                {
                    // we should already be attacking
                    //Keyboard.KeyPress(WowInput.SHAMAN_ATTACK);
                }
            } while (WorldState.IsInCombat);

            return true;
        }

        public bool ShamanShouldCastLightningShield(WowShamanClassState classState)
        {
            // Skip if the only mob we're fighting is nature immune
            // We still waste charges in the multi-attacker scenario, but we just want to dump mana to kill ASAP in those cases
            bool skipLightningShield = WorldState.IsTargetNatureImmune && WorldState.AttackerCount <= 1;

            if (skipLightningShield)
            {
                return false;
            }

            return classState.ShouldCastLightningShield;
        }

        public bool ShamanShouldCastFlameShock(WowShamanClassState classState)
        {
            // Skip if fire immune
            bool skipFlameShock = WorldState.IsTargetFireImmune;
            // Always shock runners, nature immune, and high hp mobs
            skipFlameShock |= !WorldState.IsTargetRunnerMob && !WorldState.IsTargetNatureImmune && WorldState.TargetHpPercent < 75;
            // Open question if we should flame shock casters at the start of fights. Probably worth waiting?
            skipFlameShock |= WorldState.IsTargetCasterMob && !WorldState.IsTargetNatureImmune;

            if (skipFlameShock)
            {
                return false;
            }

            return classState.ShouldCastFlameShock;
        }

        public bool ShamanShouldCastEarthShock(WowShamanClassState classState)
        {
            // Skip if nature immune
            bool skipEarthShock = WorldState.IsTargetNatureImmune;
            // Always shock runners, if we're low hp, if there are multiple mobs, or if the mob is high hp
            skipEarthShock |= !WorldState.IsTargetRunnerMob && WorldState.PlayerHpPercent > 50 && WorldState.AttackerCount <= 1 && WorldState.TargetHpPercent < 20 && !WorldState.IsTargetCasterMob && !WorldState.IsTargetCasting;
            // Don't earth shock casters that aren't currently casting
            // TODO: Rename IsTargetCasterMob to something like "ShouldWaitToCounterspell" or something
            skipEarthShock |= WorldState.IsTargetCasterMob && !WorldState.IsTargetCasting;

            if (skipEarthShock)
            {
                return false;
            }

            return classState.CanCastEarthShock;
        }

        public bool ShamanShouldCastCurePoison(WowShamanClassState classState)
        {
            return WorldState.PlayerIsPoisoned && classState.CanCurePoison;
        }

        public bool ShamanShouldCastCureDisease(WowShamanClassState classState)
        {
            return WorldState.PlayerIsDiseased && classState.CanCureDisease;
        }

        public async Task<bool> ShamanStartBattleReadyRecoverTask(WowShamanClassState classState)
        {
            if (WorldState.PlayerHpPercent < WowPlayerConstants.EAT_FOOD_HP_THRESHOLD)
            {
                Keyboard.KeyPress(WowInput.EAT_FOOD);
            }

            // water??
            await Task.Delay(0);
            /*
            if (WorldState.PlayerHpPercent < WowPlayerConstants.DRINK_WATER_MP_THRESHOLD)
            {
                await WowInput.PressKeyWithShift(WowInput.SHIFT_DRINK_WATER);
            }
            */

            return true;
        }

        public async Task<bool> ShamanWaitUntilBattleReadyTask(WowShamanClassState classState)
        {
            // For now, I don't care if dynamite is cooled down.  If we dynamited and didn't have to potion, we're probably safe enough to keep going
            // especially since the dynamite cooldown is so short it'll probably be up by the time we need it again.

            bool hpRecovered = WorldState.PlayerHpPercent >= WowPlayerConstants.STOP_RESTING_HP_THRESHOLD;
            bool mpRecovered = WorldState.ResourcePercent >= WowPlayerConstants.STOP_RESTING_MP_THRESHOLD;
            bool potionIsCooledDown = !WowPlayer.CurrentTimeInsideDuration(HealthPotionTime, WowGameplayConstants.POTION_COOLDOWN_MILLIS);
            bool battleReady = hpRecovered && mpRecovered && potionIsCooledDown;

            if (ShamanShouldCastCurePoison(classState))
            {
                await WaitForGlobalCooldownTask();
                Keyboard.KeyPress(WowInput.SHAMAN_CURE_POISON);
            }

            if (ShamanShouldCastCureDisease(classState))
            {
                await WaitForGlobalCooldownTask();
                await WowInput.PressKeyWithShift(WowInput.SHAMAN_SHIFT_CURE_DISEASE);
            }

            if (battleReady)
            {
                bool buffed = false;
                if (classState.ShouldCastRockbiterWeapon)
                {
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(WowInput.SHAMAN_SHIFT_ROCKBITER_WEAPON);
                    buffed = true;
                }

                if (ShamanShouldCastLightningShield(classState))
                {
                    await WaitForGlobalCooldownTask();
                    Keyboard.KeyPress(WowInput.SHAMAN_LIGHTNING_SHIELD);
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

        // Simplified while the engage flow is being reworked around the new target-marker
        // bearing detection (see WowMovementTasks.TurnToFaceTargetMarkerTask) -- no longer
        // presses the pull cast here at all; that's moving into
        // ShamanFaceCorrectDirectionToEngageTask below, after facing is actually corrected.
        // Not yet complete.
        public async Task<bool> ShamanKickOffEngageTask(WowShamanClassState classState)
        {
            await Task.Delay(0);
            EngageAttempts = 1;
            return true;
        }

        public async Task<bool> ShamanFaceCorrectDirectionToEngageTask(WowShamanClassState classState)
        {
            EngageAttempts++;

            if (AbandonUnreachableEngageTarget())
            {
                return false;
            }

            Console.WriteLine($"ShamanFaceCorrectDirectionToEngageTask, EngageAttempts {EngageAttempts}, WorldState.IsCurrentlyCasting? {WorldState.IsCurrentlyCasting}, WorldState.IsInCombat? {WorldState.IsInCombat}");
            if (!WorldState.IsCurrentlyCasting && !WorldState.IsInCombat)
            {
                // Replaces the old blind TurnABitToTheLeftTask() -- see
                // WowMovementTasks.TurnToFaceTargetMarkerTask for the target-marker-based
                // bearing detection this now uses instead.
                await TurnToFaceTargetMarkerTask();
                Keyboard.KeyPress(WowInput.SHAMAN_LIGHTNING_BOLT);
                await Task.Delay(500); // IsCurrentlyCasting can take a little bit to update, give it a buffer
                await UpdateWorldStateAsync();
            }

            return ShamanCanEngageTarget(classState);
        }

        // Replaces the old shared CanEngageTarget() for the Shaman case --
        // CanSpellcastPullTarget is shared with Mage under the same name but
        // each class gets its own ClassState type, so each also gets its own
        // thin CanEngageTarget wrapper. Shaman always pulls with a spell
        // regardless of FarmingConfig.EngageMethod (Charge/Pull only matters for
        // Warrior -- see the enum's own comment on WowLocationConfiguration.cs),
        // so unlike WarriorCanEngageTarget this doesn't need to dispatch on it at all.
        public bool ShamanCanEngageTarget(WowShamanClassState classState)
        {
            return classState.CanSpellcastPullTarget;
        }

        public async Task<bool> ShamanEmergencyTask()
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
