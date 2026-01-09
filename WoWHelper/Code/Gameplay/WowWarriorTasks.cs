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
        public async Task<bool> WarriorCombatLoopTask()
        {
            Console.WriteLine("Kicking off core combat loop");
            bool thrownDynamite = false;
            bool potionUsed = false;
            bool healingTrinketUsed = false;
            bool tooManyAttackersActionsTaken = false;
            bool startOfCombatWiggled = false;

            await WarriorStartAttackTask();

            do
            {
                await UpdateWorldStateAsync();

                // don't drown
                if (WorldState.Underwater)
                {
                    await GetOutOfWater();
                }

                // ping if unseen message
                if (FarmingConfig.AlertOnUnreadWhisper && !(PreviousWorldState?.HasUnseenWhisper ?? true) && WorldState.HasUnseenWhisper)
                {
                    SlackHelper.SendMessageToChannel($"Unseen Whisper!");
                }

                // If we're about to die, petri alt+f4
                if (WorldState.PlayerHpPercent <= WowPlayerConstants.PETRI_ALTF4_HP_THRESHOLD)
                {
                    SlackHelper.SendMessageToChannel($"Petri Alt+F4ed at ~{WorldState.PlayerHpPercent}%!  Consider using Unstuck instead of logging back in");
                    await PetriAltF4Task();
                    Environment.Exit(0);
                    continue;
                }

                // First do our "Make sure we're not standing around doing nothing" checks
                if (await WarriorMakeSureWeAreAttackingEnemyTask())
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
                if (!tooManyAttackersActionsTaken && WorldState.PlayerHpPercent <= WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD)
                {
                    SlackHelper.SendMessageToChannel($"{WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD}% Retal popped, not sure what went wrong!");

                    // cast retaliation once GCD is cooled down
                    while (!WorldState.GCDCooledDown)
                    {
                        await UpdateWorldStateAsync();
                    }
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

                if (!healingTrinketUsed && await WarriorUseDiamondFlaskTask())
                {
                    healingTrinketUsed = true;
                    continue;
                }

                if (!startOfCombatWiggled && PreviousWorldState?.TargetHpPercent == 100 && WorldState.TargetHpPercent < 100)
                {
                    await StartOfCombatWiggle();
                    startOfCombatWiggled = true; // maybe not necessary? if they keep going to 100 maybe they're evading and it's good to keep backing up?
                }

                if (FarmingConfig.PreemptFear && !CurrentTimeInsideDuration(BerserkerRageTime, WowGameplayConstants.BERSERKER_RAGE_COOLDOWN_MILLIS))
                {
                    await WarriorStartOfCombatBerserkerRage();
                    BerserkerRageTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                // Finally, if we've made it this far, do standard combat actions
                if (!WorldState.BattleShoutActive && WorldState.ResourcePercent >= WowGameplayConstants.BATTLE_SHOUT_RAGE_COST)
                {
                    Keyboard.KeyPress(WowInput.WARRIOR_BATTLE_SHOUT_KEY);
                }
                else if (WorldState.OverpowerUsable && WorldState.ResourcePercent >= WowGameplayConstants.OVERPOWER_RAGE_COST)
                {
                    Keyboard.KeyPress(WowInput.WARRIOR_OVERPOWER_KEY);
                }
                else if (WorldState.TargetHpPercent <= WowGameplayConstants.EXECUTE_HP_THRESHOLD && WorldState.ResourcePercent >= WowGameplayConstants.EXECUTE_RAGE_COST)
                {
                    Keyboard.KeyPress(WowInput.WARRIOR_EXECUTE_KEY);
                }
                else if (FarmingConfig.UseRend && !WorldState.TargetHasRend && WorldState.TargetHpPercent > WowPlayerConstants.REND_HP_THRESHOLD && WorldState.ResourcePercent >= WowGameplayConstants.REND_RAGE_COST)
                {
                    Keyboard.KeyPress(WowInput.WARRIOR_REND_KEY);
                }
                else if (WorldState.AttackerCount > 1)
                {
                    if (WorldState.MortalStrikeOrBloodThirstCooledDown && WorldState.ResourcePercent >= WowGameplayConstants.MORTAL_STRIKE_BLOODTHIRST_RAGE_COST)
                    {
                        Keyboard.KeyPress(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST_MACRO);
                    }
                    else if (WorldState.ResourcePercent >= (WowGameplayConstants.MORTAL_STRIKE_BLOODTHIRST_RAGE_COST + WowGameplayConstants.CLEAVE_RAGE_COST))
                    {
                        // Cleave only if we have enough spare rage to bloodthirst right after
                        await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_CLEAVE_MACRO);
                    }
                }
                else if (WorldState.AttackerCount <= 1) // TODO: 0 attackers can happen if I forget to turn enemy nameplates on
                {
                    if (WorldState.MortalStrikeOrBloodThirstCooledDown && WorldState.ResourcePercent >= WowGameplayConstants.MORTAL_STRIKE_BLOODTHIRST_RAGE_COST)
                    {
                        Keyboard.KeyPress(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST_MACRO);
                    }
                    else if (WorldState.ResourcePercent >= (WowGameplayConstants.MORTAL_STRIKE_BLOODTHIRST_RAGE_COST + WowGameplayConstants.HEROIC_STRIKE_RAGE_COST))
                    {
                        // Heroic only if we have enough spare rage to bloodthirst right after
                        Keyboard.KeyPress(WowInput.WARRIOR_HEROIC_STRIKE_KEY);
                    }
                    // TODO: Actually split out Heroic Strike and cast if we have really surplus rage
                }
            } while (WorldState.IsInCombat);

            return true;
        }

        public async Task<bool> WarriorStartBattleReadyRecoverTask()
        {
            if (WorldState.PlayerHpPercent < WowPlayerConstants.EAT_FOOD_HP_THRESHOLD)
            {
                await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_EAT_FOOD_KEY);
            }

            return true;
        }

        public async Task<bool> WarriorWaitUntilBattleReadyTask()
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

        public async Task<bool> WarriorKickOffEngageTask()
        {
            await Task.Delay(0);
            EngageAttempts = 1;

            if (FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Charge)
            {
                Keyboard.KeyPress(WowInput.WARRIOR_CHARGE_KEY);
                return true;
            }
            else if (FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Shoot)
            {
                Keyboard.KeyPress(WowInput.WARRIOR_SHOOT_MACRO);
                return true;
            }

            return false;
        }

        public async Task<bool> WarriorFaceCorrectDirectionToEngageTask()
        {
            EngageAttempts++;

            if (FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Charge)
            {
                await TurnABitToTheLeftTask();
                Keyboard.KeyPress(WowInput.WARRIOR_CHARGE_KEY);
            }
            else if (FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Shoot)
            {
                if (!WorldState.WaitingToShoot)
                {
                    await TurnABitToTheLeftTask();
                    Keyboard.KeyPress(WowInput.WARRIOR_SHOOT_MACRO);
                }
            }

            return CanEngageTarget();
        }

        public async Task<bool> WarriorStartAttackTask()
        {
            await Task.Delay(0);

            // always kick things off with heroic strike macro to /startattack
            Keyboard.KeyPress(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST_MACRO);

            return true;
        }

        public async Task<bool> WarriorMakeSureWeAreAttackingEnemyTask()
        {
            bool attackerJustDied = PreviousWorldState?.AttackerCount > WorldState.AttackerCount && WorldState.AttackerCount > 0;
            bool inCombatButNotAutoAttacking = WorldState.IsInCombat && !WorldState.IsAutoAttacking;
            bool tooFarAway = WorldState.TooFarAway;
            bool facingWrongWay = WorldState.FacingWrongWay; // potentially need to turn in case we're webbed and backing up wont work
            bool targetNeedsToBeInFront = WorldState.TargetNeedsToBeInFront;
            bool invalidTarget = WorldState.InvalidTarget;

            // now since we have more accurate "are they in front" checking, try not backing up if attacker just died
            if (/*attackerJustDied || */facingWrongWay || targetNeedsToBeInFront)
            {
                // one of the mobs just died, scoot back to make sure the next mob is in front of you
                await ScootBackwardsTask();
            }

            if (tooFarAway || invalidTarget)
            {
                // we may have targeted something in the distance then got aggroed by something else, clear target so we pick them up
                Keyboard.KeyPress(WowInput.WARRIOR_CLEAR_TARGET_MACRO);
            }

            if (attackerJustDied || inCombatButNotAutoAttacking || tooFarAway)
            {
                // /startattack
                Keyboard.KeyPress(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST_MACRO);
            }

            return attackerJustDied || inCombatButNotAutoAttacking || tooFarAway || facingWrongWay || targetNeedsToBeInFront || invalidTarget;
        }

        public async Task<bool> WarriorTooManyAttackersTask()
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
                await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_RETALIATION_KEY);

                LogoutReason = "Got into a Retaliation situation, logging off for safety";
                LogoutTriggered = true;
            }

            return tooManyAttackers;
        }

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

        public async Task<bool> WarriorUseHealingTrinketTask()
        {
            bool shouldUseHealingTrinket = WorldState.PlayerHpPercent <= WowGameplayConstants.HEALING_TRINKET_HP_THRESHOLD &&
                !CurrentTimeInsideDuration(HealingTrinketTime, WowGameplayConstants.HEALING_TRINKET_COOLDOWN_MILLIS);

            if (shouldUseHealingTrinket)
            {
                HealingTrinketTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_HEALING_TRINKET);
            }

            return shouldUseHealingTrinket;
        }

        public async Task<bool> WarriorStartOfCombatBerserkerRage()
        {
            await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_BERSERKER_RAGE_MACRO);
            await Task.Delay(150);
            Keyboard.KeyPress(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST_MACRO);
            await Task.Delay(150);
            Keyboard.KeyPress(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST_MACRO);
            await Task.Delay(150);
            Keyboard.KeyPress(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST_MACRO);
            await Task.Delay(150);
            Keyboard.KeyPress(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST_MACRO);

            return true;
        }
    }
}
