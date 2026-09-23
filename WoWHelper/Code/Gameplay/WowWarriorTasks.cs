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
        public async Task<bool> WarriorCombatLoopTask(WowWarriorClassState classState)
        {
            Console.WriteLine("Kicking off core combat loop");
            bool thrownDynamite = false;
            bool potionUsed = false;
            bool healingTrinketUsed = false;
            bool tooManyAttackersActionsTaken = false;
            bool startOfCombatWiggled = false;

            await StartAttackTask();

            do
            {
                await UpdateWorldStateAsync();

                await EveryWorldStateUpdateTasks();

                // First do our "Make sure we're not standing around doing nothing" checks
                if (await MeleeMakeSureWeAreAttackingEnemyTask())
                {
                    continue;
                }

                // Next, check if we need to pop any big cooldowns
                if (!tooManyAttackersActionsTaken && await WarriorTooManyAttackersTask())
                {
                    tooManyAttackersActionsTaken = true;
                    continue;
                }

                // Just in case, if for some reason things are going really poorly, try to pop retal regardless
                if (!tooManyAttackersActionsTaken && WarriorShouldEmergencyRetaliate())
                {
                    SlackHelper.SendMessageToChannel($"{WowPlayerConstants.EMERGENCY_HP_THRESHOLD}% Retal popped, not sure what went wrong!");

                    // cast retaliation once GCD is cooled down
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_RETALIATION);

                    tooManyAttackersActionsTaken = true;

                    LogoutReason = $"Got down to {WowPlayerConstants.EMERGENCY_HP_THRESHOLD}% somehow";
                    LogoutTriggered = true;

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

                /*
                if (!healingTrinketUsed && await WarriorUseDiamondFlaskTask())
                {
                    healingTrinketUsed = true;
                    continue;
                }
                */

                if (!startOfCombatWiggled && PreviousWorldState.TargetHpPercent == 100 && WorldState.TargetHpPercent < 100)
                {
                    await StartOfCombatWiggle();
                    startOfCombatWiggled = true; // maybe not necessary? if they keep going to 100 maybe they're evading and it's good to keep backing up?
                }

                // TODO: so we don't spam, something like this?
                //if (!WorldState.GCDCooledDown)
                //{
                //    continue;
                //}

                if (WarriorShouldOpenWithBerserkerRage())
                {
                    await WarriorStartOfCombatBerserkerRage();
                    BerserkerRageTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                // Finally, if we've made it this far, do standard combat actions. This chain
                // picks ONE ability by priority based purely on game state (is the buff up,
                // is the proc available, does the target already have the debuff, ...) --
                // the WarriorShouldCastX checks deliberately don't look at rage. The rage
                // check happens inside the chosen branch: if we can't afford the ability
                // we've picked, we do nothing this tick and wait for rage to build, rather
                // than falling through to something cheaper.
                // Otherwise a 30-rage Mortal Strike/Bloodthirst coming off cooldown would
                // keep getting starved by 15-rage fillers spent the moment they're affordable.
                if (WarriorShouldCastBattleShout(classState))
                {
                    if (WorldState.ResourcePercent >= WowGameplayConstants.BATTLE_SHOUT_RAGE_COST)
                    {
                        await WowInput.PressKey(WowInput.WARRIOR_BATTLE_SHOUT);
                    }
                }
                else if (WarriorShouldCastSweepingStrikes(classState))
                {
                    if (WorldState.ResourcePercent >= WowGameplayConstants.SWEEPING_STRIKES_RAGE_COST)
                    {
                        await WowInput.PressKeyWithControl(WowInput.WARRIOR_CTRL_SWEEPING_STRIKES);
                    }
                }
                else if (WarriorShouldCastOverpower(classState))
                {
                    if (WorldState.ResourcePercent >= WowGameplayConstants.OVERPOWER_RAGE_COST)
                    {
                        await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_OVERPOWER);
                    }
                }
                else if (WarriorShouldCastExecute(classState))
                {
                    if (WorldState.ResourcePercent >= WowGameplayConstants.EXECUTE_RAGE_COST)
                    {
                        await WowInput.PressKey(WowInput.WARRIOR_EXECUTE);
                    }
                }
                else if (WarriorShouldCastSunderArmor(classState))
                {
                    if (WorldState.ResourcePercent >= WowGameplayConstants.SUNDER_ARMOR_RAGE_COST)
                    {
                        await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_SUNDER_ARMOR);
                    }
                }
                else if (WarriorShouldCastRend(classState))
                {
                    if (WorldState.ResourcePercent >= WowGameplayConstants.REND_RAGE_COST)
                    {
                        await WowInput.PressKey(WowInput.WARRIOR_REND);
                    }
                }
                else if (WarriorShouldCastMortalStrikeOrBloodthirst(classState))
                {
                    if (WorldState.ResourcePercent >= WowGameplayConstants.MORTAL_STRIKE_BLOODTHIRST_RAGE_COST)
                    {
                        await WowInput.PressKey(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST);
                    }
                }
                else if (WorldState.AttackerCount > 1)
                {
                    if (WarriorShouldCastCleave(classState))
                    {
                        if (WorldState.ResourcePercent >= WarriorCleaveRageRequired(classState))
                        {
                            await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_CLEAVE);
                        }
                    }
                }
                else // TODO: 0 attackers can happen if I forget to turn enemy nameplates on
                {
                    if (WarriorShouldCastHeroicStrike(classState))
                    {
                        if (WorldState.ResourcePercent >= WarriorHeroicStrikeRageRequired(classState))
                        {
                            await WowInput.PressKey(WowInput.WARRIOR_HEROIC_STRIKE);
                        }
                    }
                    // TODO: Actually split out Heroic Strike and cast if we have really surplus rage
                }
            } while (WorldState.IsInCombat);

            return true;
        }

        // Just in case, if for some reason things are going really poorly, try to pop retal regardless.
        public bool WarriorShouldEmergencyRetaliate()
        {
            return WorldState.PlayerHpPercent <= WowPlayerConstants.EMERGENCY_HP_THRESHOLD;
        }

        // Fear-casters are worth opening on preemptively, rather than reacting once feared.
        public bool WarriorShouldOpenWithBerserkerRage()
        {
            return WorldState.IsTargetFearCaster && !CurrentTimeInsideDuration(BerserkerRageTime, WowGameplayConstants.BERSERKER_RAGE_COOLDOWN_MILLIS);
        }

        // None of the WarriorShouldCastX checks below look at rage -- they only decide
        // whether the ability is the right thing to be doing given the game state. Rage is
        // checked afterwards, inside the chosen branch of the rotation chain.

        public bool WarriorShouldCastBattleShout(WowWarriorClassState classState)
        {
            return !classState.BattleShoutActive;
        }

        // Only below Battle Shout in priority. Sweeping Strikes makes the next
        // few melee swings cleave to a second target, so it's only worth popping
        // against multiple attackers, and only once it's actually trained and off
        // cooldown.
        public bool WarriorShouldCastSweepingStrikes(WowWarriorClassState classState)
        {
            return classState.KnowsSweepingStrikes &&
                classState.SweepingStrikesCooledDown &&
                WorldState.AttackerCount > 1;
        }

        public bool WarriorShouldCastOverpower(WowWarriorClassState classState)
        {
            return classState.OverpowerUsable;
        }

        public bool WarriorShouldCastExecute(WowWarriorClassState classState)
        {
            return classState.KnowsExecute &&
                WorldState.TargetHpPercent <= WowGameplayConstants.EXECUTE_HP_THRESHOLD;
        }

        // One Sunder Armor per target, and only while it's still worth it: a single stack's
        // armor reduction pays for its rage over the rest of a full-HP fight, but not on a mob
        // that's already mostly dead. TargetHasSunderArmor is "any stack at all", so this
        // deliberately never builds past the first one -- that rage is better spent on
        // Mortal Strike/Bloodthirst/Heroic Strike further down the priority list.
        public bool WarriorShouldCastSunderArmor(WowWarriorClassState classState)
        {
            return classState.KnowsSunderArmor &&
                !classState.TargetHasSunderArmor &&
                WorldState.TargetHpPercent >= WowPlayerConstants.SUNDER_ARMOR_HP_THRESHOLD;
        }

        // Bleed-immune targets never get Rend, regardless of anything else. Otherwise, Rend
        // once it's actually trained and the target isn't already bled, but only if it's
        // worth the rage: either high enough HP that Rend's DoT will have time to tick, or a
        // runner mob -- those flee at low HP, so Rend's damage-over-time keeps ticking (and
        // helps finish it off) even after it breaks line of sight/melee range.
        public bool WarriorShouldCastRend(WowWarriorClassState classState)
        {
            if (WorldState.IsTargetBleedImmune)
            {
                return false;
            }

            return classState.KnowsRend &&
                !classState.TargetHasRend &&
                (WorldState.TargetHpPercent > WowPlayerConstants.REND_HP_THRESHOLD || WorldState.IsTargetRunnerMob);
        }

        // classState.MortalStrikeOrBloodThirstCooledDown alone isn't a safe usability check --
        // it's decoded from a Lua cooldown query that reads ready for a spell the player hasn't
        // even trained yet (see KnowsMortalStrikeOrBloodthirst()'s comment in
        // WarriorFunctions.lua) -- so KnowsMortalStrikeOrBloodthirst has to gate it too.
        // Sits above the attacker-count split in the rotation since it's the same call
        // either way (the two branches used to duplicate it).
        public bool WarriorShouldCastMortalStrikeOrBloodthirst(WowWarriorClassState classState)
        {
            return classState.KnowsMortalStrikeOrBloodthirst &&
                classState.MortalStrikeOrBloodThirstCooledDown;
        }

        // Cleave only if nothing's already queued for the next swing (pressing it again
        // would just be a wasted keypress until the swing lands).
        public bool WarriorShouldCastCleave(WowWarriorClassState classState)
        {
            return !classState.NextSwingSpellQueued;
        }

        // Heroic only if nothing's already queued for the next swing (see Cleave above).
        public bool WarriorShouldCastHeroicStrike(WowWarriorClassState classState)
        {
            return !classState.NextSwingSpellQueued;
        }

        // Cleave/Heroic Strike are fillers for when Mortal Strike/Bloodthirst is on cooldown,
        // so their rage requirement is their own cost PLUS a reserve for the next Mortal
        // Strike/Bloodthirst -- otherwise the filler eats the rage MS/BT needs the moment it
        // comes back up. No reserve if we haven't trained MS/BT yet and can't cast it at all.
        public int WarriorCleaveRageRequired(WowWarriorClassState classState)
        {
            return WowGameplayConstants.CLEAVE_RAGE_COST + WarriorMortalStrikeOrBloodthirstRageReserve(classState);
        }

        public int WarriorHeroicStrikeRageRequired(WowWarriorClassState classState)
        {
            return WowGameplayConstants.HEROIC_STRIKE_RAGE_COST + WarriorMortalStrikeOrBloodthirstRageReserve(classState);
        }

        private int WarriorMortalStrikeOrBloodthirstRageReserve(WowWarriorClassState classState)
        {
            return classState.KnowsMortalStrikeOrBloodthirst ? WowGameplayConstants.MORTAL_STRIKE_BLOODTHIRST_RAGE_COST : 0;
        }

        public async Task<bool> WarriorStartBattleReadyRecoverTask(WowWarriorClassState classState)
        {
            // clams
            //if (WorldState.PlayerHpPercent < WowPlayerConstants.EAT_FOOD_HP_THRESHOLD)
            //{
                await WowInput.PressKey(WowInput.EAT_FOOD);
            //}

            return true;
        }

        public async Task<bool> WarriorWaitUntilBattleReadyTask(WowWarriorClassState classState)
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

        public async Task<bool> WarriorKickOffEngageTask(WowWarriorClassState classState)
        {
            await Task.Delay(0);
            EngageAttempts = 1;
            await TurnToFaceTargetMarkerTask();
            return true;
        }

        public async Task<bool> WarriorFaceCorrectDirectionToEngageTask(WowWarriorClassState classState)
        {
            EngageAttempts++;

            if (await AbandonUnreachableEngageTarget())
            {
                return false;
            }

            if (FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Charge && !classState.KnowsCharge) // no charge yet
            {
                await WowInput.PressKey(WowInput.START_ATTACK);
                await WalkIntoMeleeRangeTask(classState);
                //await WowInput.PressKey(WowInput.START_ATTACK);
            }
            else if (FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Charge)
            {
                await FaceAndChargeTarget();
            }
            else if (FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Pull)
            {
                if (!classState.WaitingToShoot)
                {
                    await TurnToFaceTargetMarkerTask();
                    await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_SHOOT);
                }
            }

            return WarriorCanEngageTarget(classState);
        }

        public async Task<bool> FaceAndChargeTarget()
        {
            await TurnToFaceTargetMarkerTask();
            await WowInput.PressKey(WowInput.WARRIOR_CHARGE);
            return true;
        }

        // Replaces the old shared CanEngageTarget() for the Warrior case --
        // both fields it needs (CanChargeTarget, CanShootTarget) live only on
        // WowWarriorClassState.
        public bool WarriorCanEngageTarget(WowWarriorClassState classState)
        {
            switch (FarmingConfig.EngageMethod)
            {
                case WowLocationConfiguration.EngagementMethod.Charge:
                    {
                        if (!classState.KnowsCharge && classState.CanChargeTarget)
                        {
                            return FindTargetMarkerOnScreen() != null;
                        }
                        return classState.CanChargeTarget;
                    }
                case WowLocationConfiguration.EngagementMethod.Pull: return classState.CanShootTarget;
                default: throw new System.NotImplementedException(
                    $"{nameof(WarriorCanEngageTarget)}: EngageMethod \"{FarmingConfig.EngageMethod}\" (from location " +
                    $"\"{FarmingConfig.LocationConfiguration?.Title}\") isn't supported for Warrior -- only Charge/Pull " +
                    $"are. This route's EngageMethod likely wasn't set up for this class -- see " +
                    $"WowConfigResolutionTasks.ResolveFarmingConfigurationTask.");
            }
        }

        public async Task<bool> WarriorTooManyAttackersTask()
        {
            bool tooManyAttackers = WorldState.AttackerCount >= WowPlayerConstants.TOO_MANY_ATTACKERS_THRESHOLD;

            if (tooManyAttackers)
            {
                SlackHelper.SendMessageToChannel($"TOO MANY ATTACKERS HELP");

                // cast retaliation once GCD is cooled down
                await WaitForGlobalCooldownTask();
                await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_RETALIATION);

                LogoutReason = "Got into a Retaliation situation, logging off for safety";
                LogoutTriggered = true;
            }

            return tooManyAttackers;
        }

        /*
        public async Task<bool> WarriorUseDiamondFlaskTask()
        {
            bool shouldUseDiamondFlask = WorldState.AttackerCount > 1 &&
                !CurrentTimeInsideDuration(HealingTrinketTime, WowGameplayConstants.DIAMOND_FLASK_COOLDOWN_MILLIS);

            if (shouldUseDiamondFlask)
            {
                HealingTrinketTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_HEALING_TRINKET);
            }

            return shouldUseDiamondFlask;
        }
        */

        public async Task<bool> WarriorStartOfCombatBerserkerRage()
        {
            await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_BERSERKER_RAGE);
            await Task.Delay(150);
            await WowInput.PressKey(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST);
            await Task.Delay(150);
            await WowInput.PressKey(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST);
            await Task.Delay(150);
            await WowInput.PressKey(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST);
            await Task.Delay(150);
            await WowInput.PressKey(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST);

            return true;
        }
    }
}
