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
                    SlackHelper.SendMessageToChannel($"{WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD}% Retal popped, not sure what went wrong!");

                    // cast retaliation once GCD is cooled down
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_RETALIATION_KEY);

                    tooManyAttackersActionsTaken = true;

                    LogoutReason = $"Got down to {WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD}% somehow";
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

                if (WarriorShouldOpenWithBerserkerRage())
                {
                    await WarriorStartOfCombatBerserkerRage();
                    BerserkerRageTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                // Finally, if we've made it this far, do standard combat actions
                if (WarriorShouldCastBattleShout(classState))
                {
                    await WowInput.PressKey(WowInput.WARRIOR_BATTLE_SHOUT);
                }
                else if (WarriorShouldCastOverpower(classState))
                {
                    await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_OVERPOWER);
                }
                else if (WarriorShouldCastExecute(classState))
                {
                    await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_EXECUTE);
                }
                else if (WarriorShouldCastRend(classState))
                {
                    await WowInput.PressKey(WowInput.WARRIOR_REND);
                }
                else if (WorldState.AttackerCount > 1)
                {
                    if (WarriorShouldCastMortalStrikeOrBloodthirst(classState))
                    {
                        await WowInput.PressKey(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST);
                    }
                    else if (WarriorShouldCastCleave())
                    {
                        await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_CLEAVE);
                    }
                }
                else if (WorldState.AttackerCount <= 1) // TODO: 0 attackers can happen if I forget to turn enemy nameplates on
                {
                    if (WarriorShouldCastMortalStrikeOrBloodthirst(classState))
                    {
                        await WowInput.PressKey(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST);
                    }
                    else if (WarriorShouldCastHeroicStrike(classState))
                    {
                        await WowInput.PressKey(WowInput.WARRIOR_HEROIC_STRIKE);
                    }
                    // TODO: Actually split out Heroic Strike and cast if we have really surplus rage
                }
            } while (WorldState.IsInCombat);

            return true;
        }

        // Just in case, if for some reason things are going really poorly, try to pop retal regardless.
        public bool WarriorShouldEmergencyRetaliate()
        {
            return WorldState.PlayerHpPercent <= WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD;
        }

        // Fear-casters are worth opening on preemptively, rather than reacting once feared.
        public bool WarriorShouldOpenWithBerserkerRage()
        {
            return WorldState.IsTargetFearCaster && !CurrentTimeInsideDuration(BerserkerRageTime, WowGameplayConstants.BERSERKER_RAGE_COOLDOWN_MILLIS);
        }

        public bool WarriorShouldCastBattleShout(WowWarriorClassState classState)
        {
            return !classState.BattleShoutActive && WorldState.ResourcePercent >= WowGameplayConstants.BATTLE_SHOUT_RAGE_COST;
        }

        public bool WarriorShouldCastOverpower(WowWarriorClassState classState)
        {
            return classState.OverpowerUsable && WorldState.ResourcePercent >= WowGameplayConstants.OVERPOWER_RAGE_COST;
        }

        public bool WarriorShouldCastExecute(WowWarriorClassState classState)
        {
            return classState.KnowsExecute &&
                WorldState.TargetHpPercent <= WowGameplayConstants.EXECUTE_HP_THRESHOLD &&
                WorldState.ResourcePercent >= WowGameplayConstants.EXECUTE_RAGE_COST;
        }

        // Bleed-immune targets never get Rend, regardless of anything else. Otherwise, Rend
        // once it's actually trained, we can afford it, and the target isn't already bled,
        // but only if it's worth the rage: either high enough HP that Rend's DoT will have
        // time to tick, or a runner mob -- those flee at low HP, so Rend's damage-over-time
        // keeps ticking (and helps finish it off) even after it breaks line of sight/melee range.
        public bool WarriorShouldCastRend(WowWarriorClassState classState)
        {
            if (WorldState.IsTargetBleedImmune)
            {
                return false;
            }

            return classState.KnowsRend &&
                WorldState.ResourcePercent >= WowGameplayConstants.REND_RAGE_COST &&
                !classState.TargetHasRend &&
                (WorldState.TargetHpPercent > WowPlayerConstants.REND_HP_THRESHOLD || WorldState.IsTargetRunnerMob);
        }

        // classState.MortalStrikeOrBloodThirstCooledDown alone isn't a safe usability check --
        // it's decoded from a Lua cooldown query that reads ready for a spell the player hasn't
        // even trained yet (see KnowsMortalStrikeOrBloodthirst()'s comment in
        // WarriorFunctions.lua) -- so KnowsMortalStrikeOrBloodthirst has to gate it too. Used
        // for both the multi- and single-attacker branches below -- those used to differ (only
        // the single-attacker one had a "PlayerLevel >= 40" gate), but replacing that with the
        // real trained-or-not check makes both branches' gating identical.
        public bool WarriorShouldCastMortalStrikeOrBloodthirst(WowWarriorClassState classState)
        {
            return classState.KnowsMortalStrikeOrBloodthirst &&
                classState.MortalStrikeOrBloodThirstCooledDown &&
                WorldState.ResourcePercent >= WowGameplayConstants.MORTAL_STRIKE_BLOODTHIRST_RAGE_COST;
        }

        // Cleave only if we have enough spare rage to bloodthirst right after.
        public bool WarriorShouldCastCleave()
        {
            return WorldState.ResourcePercent >= (WowGameplayConstants.MORTAL_STRIKE_BLOODTHIRST_RAGE_COST + WowGameplayConstants.CLEAVE_RAGE_COST);
        }

        // Heroic only if we have enough spare rage to bloodthirst right after, unless we
        // haven't trained Mortal Strike/Bloodthirst yet and can't cast it at all.
        public bool WarriorShouldCastHeroicStrike(WowWarriorClassState classState)
        {
            return WorldState.ResourcePercent >= (WowGameplayConstants.MORTAL_STRIKE_BLOODTHIRST_RAGE_COST + WowGameplayConstants.HEROIC_STRIKE_RAGE_COST) ||
                (!classState.KnowsMortalStrikeOrBloodthirst && WorldState.ResourcePercent >= WowGameplayConstants.HEROIC_STRIKE_RAGE_COST);
        }

        public async Task<bool> WarriorStartBattleReadyRecoverTask(WowWarriorClassState classState)
        {
            if (WorldState.PlayerHpPercent < WowPlayerConstants.EAT_FOOD_HP_THRESHOLD)
            {
                await WowInput.PressKey(WowInput.EAT_FOOD);
            }

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
                await StartWalkForwardTask();
                await Task.Delay(500);
                await EndWalkForwardTask();
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
            bool tooManyAttackers = WorldState.AttackerCount >= FarmingConfig.TooManyAttackersThreshold;

            if (tooManyAttackers)
            {
                SlackHelper.SendMessageToChannel($"TOO MANY ATTACKERS HELP");

                // cast retaliation once GCD is cooled down
                await WaitForGlobalCooldownTask();
                await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_RETALIATION_KEY);

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
