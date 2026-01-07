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
        public async Task<bool> MageCombatLoopTask()
        {
            Console.WriteLine("Kicking off core combat loop");
            bool thrownDynamite = false;
            bool potionUsed = false;
            bool tooManyAttackersActionsTaken = false;

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
                /*
                if (WorldState.PlayerHpPercent <= WowPlayerConstants.PETRI_ALTF4_HP_THRESHOLD)
                {
                    SlackHelper.SendMessageToChannel($"Petri Alt+F4ed at ~{WorldState.PlayerHpPercent}%!  Consider using Unstuck instead of logging back in");
                    await PetriAltF4Task();
                    Environment.Exit(0);
                    continue;
                }
                */

                // Next, check if we need to pop any big cooldowns
                if (!tooManyAttackersActionsTaken && await MageTooManyAttackersTask())
                {
                    tooManyAttackersActionsTaken = true;
                    continue;
                }

                // Just in case, if for some reason things are going really poorly, try to pop retal regardless
                if (!tooManyAttackersActionsTaken && WorldState.PlayerHpPercent <= WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD)
                {
                    SlackHelper.SendMessageToChannel($"{WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD}% Retal popped, not sure what went wrong!");

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

                // Finally, if we've made it this far, do standard combat actions
                if (WorldState.ShouldRefreshMageArmor)
                {
                    Keyboard.KeyPress(WowInput.MAGE_FROST_ARMOR);
                }
                else if (WorldState.IsInMeleeRange && WorldState.IsFireblastCooledDown)
                {
                    Keyboard.KeyPress(WowInput.MAGE_FIREBLAST);
                }
                else
                {
                    if (WorldState.IsInMeleeRange)
                    {
                        Keyboard.KeyPress(WowInput.MAGE_WAND);
                    }
                    else
                    {
                        Keyboard.KeyPress(WowInput.MAGE_FROSTBOLT);
                    }
                }
            } while (WorldState.IsInCombat);

            return true;
        }

        public async Task<bool> MageStartBattleReadyRecoverTask()
        {
            if (WorldState.PlayerHpPercent < WowPlayerConstants.EAT_FOOD_HP_THRESHOLD)
            {
                await WowInput.PressKeyWithShift(WowInput.MAGE_SHIFT_EAT_FOOD);
            }

            if (WorldState.ResourcePercent < WowPlayerConstants.DRINK_WATER_MP_THRESHOLD)
            {
                Keyboard.KeyPress(WowInput.MAGE_DRINK_WATER);
            }

            return true;
        }

        public async Task<bool> MageWaitUntilBattleReadyTask()
        {
            // For now, I don't care if dynamite is cooled down.  If we dynamited and didn't have to potion, we're probably safe enough to keep going
            // especially since the dynamite cooldown is so short it'll probably be up by the time we need it again.

            bool hpRecovered = WorldState.PlayerHpPercent >= WowPlayerConstants.STOP_RESTING_HP_THRESHOLD;
            bool mpRecovered = WorldState.ResourcePercent >= WowPlayerConstants.STOP_RESTING_MP_THRESHOLD;
            bool potionIsCooledDown = !WowPlayer.CurrentTimeInsideDuration(HealthPotionTime, WowGameplayConstants.POTION_COOLDOWN_MILLIS);
            bool battleReady = hpRecovered && mpRecovered && potionIsCooledDown;

            if (battleReady)
            {
                await ScootForwardsTask();

                // after passive recovery, rebuff and reconjure if needed, and return false if we did anything
                // TODO: make this a bit smarter so we take mana pool into account,
                // and we don't try to wait to conjure food/water when we could get aggroed
                bool buffedOrConjured = false;
                if (WorldState.ShouldRefreshArcaneIntellect)
                {
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(WowInput.MAGE_SHIFT_ARCANE_INTELLECT);
                    buffedOrConjured = true;
                }

                if (WorldState.ShouldRefreshMageArmor)
                {
                    await WaitForGlobalCooldownTask();
                    Keyboard.KeyPress(WowInput.MAGE_FROST_ARMOR);
                    buffedOrConjured = true;
                }

                if (WorldState.ShouldSummonFood)
                {
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(WowInput.MAGE_SHIFT_CONJURE_FOOD);
                    await Task.Delay(1500);
                    buffedOrConjured = true;
                }

                if (WorldState.ShouldSummonWater)
                {
                    await WaitForGlobalCooldownTask();
                    Keyboard.KeyPress(WowInput.MAGE_CONJURE_WATER);
                    await Task.Delay(1500);
                    buffedOrConjured = true;
                }

                if (buffedOrConjured)
                {
                    return false;
                }
            }

            return battleReady;
        }

        public async Task<bool> MageKickOffEngageTask()
        {
            await Task.Delay(0);
            EngageAttempts = 1;

            Keyboard.KeyPress(WowInput.MAGE_FROSTBOLT);
            return true;
        }

        public async Task<bool> MageFaceCorrectDirectionToEngageTask()
        {
            EngageAttempts++;

            if (!WorldState.IsCurrentlyCasting)
            {
                await TurnABitToTheLeftTask();
                Keyboard.KeyPress(WowInput.MAGE_FROSTBOLT);
            }

            return CanEngageTarget();
        }

        // Eventually we want to nova, blink, run, but until then just send a slack message and keep blasting
        public async Task<bool> MageTooManyAttackersTask()
        {
            await Task.Delay(0);
            bool tooManyAttackers = WorldState.AttackerCount >= FarmingConfig.TooManyAttackersThreshold;

            if (tooManyAttackers)
            {
                SlackHelper.SendMessageToChannel($"TOO MANY ATTACKERS HELP");

                LogoutReason = "Got into a too many attackers situation, logging off for safety";
                LogoutTriggered = true;
            }

            return tooManyAttackers;
        }
    }
}