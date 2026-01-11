using InputManager;
using SlackAPI;
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
            bool lowManaAlerted = false;
            bool tooManyAttackersActionsTaken = false;

            do
            {
                await UpdateWorldStateAsync();

                // TODO: combine these into a shared task
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

                // ping on level up
                if (PreviousWorldState != null && PreviousWorldState.PlayerLevel > 0 && PreviousWorldState.PlayerLevel != WorldState.PlayerLevel)
                {
                    SlackHelper.SendMessageToChannel($"Leveled up from {PreviousWorldState.PlayerLevel} to {WorldState.PlayerLevel}!");
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

                // First do our "Make sure we're not standing around doing nothing" checks
                if (await MageMakeSureWeAreAttackingEnemyTask())
                {
                    continue;
                }

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

                // Next, check if we need to pop any big cooldowns
                if (!tooManyAttackersActionsTaken && await MageTooManyAttackersTask())
                {
                    tooManyAttackersActionsTaken = true;
                    continue;
                }

                // Finally, if we've made it this far, do standard combat actions
                if (!WorldState.MageArmorActive)
                {
                    Keyboard.KeyPress(WowInput.MAGE_FROST_ARMOR);
                }
                else if (WorldState.IsInMeleeRange && WorldState.IsFireblastCooledDown && WorldState.PlayerLevel >= WowGameplayConstants.FIREBLAST_LEVEL)
                {
                    await WaitForGlobalCooldownTask();
                    Keyboard.KeyPress(WowInput.MAGE_FIREBLAST);
                }
                else if (WorldState.AttackerCount > 1 && WorldState.PlayerLevel >= WowGameplayConstants.ARCANE_EXPLOSION_LEVEL)
                {
                    Keyboard.KeyPress(WowInput.MAGE_ARCANE_EXPLOSION);
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
                // after passive recovery, rebuff and reconjure if needed, and return false if we did anything
                // TODO: make this a bit smarter so we take mana pool into account,
                // and we don't try to wait to conjure food/water when we could get aggroed
                bool buffedOrConjured = false;
                if (!WorldState.ArcaneIntellectActive && WorldState.PlayerLevel >= WowGameplayConstants.ARCANE_INTELLECT_LEVEL)
                {
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(WowInput.MAGE_SHIFT_ARCANE_INTELLECT);
                    buffedOrConjured = true;
                }

                if (!WorldState.MageArmorActive) // frost armor is known at level 1!
                {
                    await WaitForGlobalCooldownTask();
                    Keyboard.KeyPress(WowInput.MAGE_FROST_ARMOR);
                    buffedOrConjured = true;
                }

                if (WorldState.ShouldSummonFood && WorldState.PlayerLevel >= WowGameplayConstants.CONJURE_FOOD_LEVEL)
                {
                    await WaitForGlobalCooldownTask();
                    await WowInput.PressKeyWithShift(WowInput.MAGE_SHIFT_CONJURE_FOOD);
                    await Task.Delay(3500);
                    buffedOrConjured = true;
                }

                if (WorldState.ShouldSummonWater && WorldState.PlayerLevel >= WowGameplayConstants.CONJURE_WATER_LEVEL)
                {
                    await WaitForGlobalCooldownTask();
                    Keyboard.KeyPress(WowInput.MAGE_CONJURE_WATER);
                    await Task.Delay(3500);
                    buffedOrConjured = true;
                }

                if (buffedOrConjured)
                {
                    return false;
                }

                await ScootForwardsTask();
            }

            return battleReady;
        }

        public async Task<bool> MageKickOffEngageTask()
        {
            EngageAttempts = 1;

            Console.WriteLine($"MageKickOffEngageTask, clicking frostbolt");
            Keyboard.KeyPress(WowInput.MAGE_FROSTBOLT);
            await Task.Delay(500); // IsCurrentlyCasting can take a little bit to update, give it a buffer
            return true;
        }

        public async Task<bool> MageFaceCorrectDirectionToEngageTask()
        {
            EngageAttempts++;

            Console.WriteLine($"MageFaceCorrectDirectionToEngageTask, EngageAttempts {EngageAttempts}, WorldState.IsCurrentlyCasting? {WorldState.IsCurrentlyCasting}");
            if (!WorldState.IsCurrentlyCasting)
            {
                await TurnABitToTheLeftTask();
                Keyboard.KeyPress(WowInput.MAGE_FROSTBOLT);
                await Task.Delay(500); // IsCurrentlyCasting can take a little bit to update, give it a buffer
            }

            return CanEngageTarget();
        }

        public async Task<bool> MageMakeSureWeAreAttackingEnemyTask()
        {
            bool attackerJustDied = PreviousWorldState?.AttackerCount > WorldState.AttackerCount && WorldState.AttackerCount > 0;
            bool tooFarAway = WorldState.TooFarAway;
            bool facingWrongWay = WorldState.FacingWrongWay; // potentially need to turn in case we're webbed and backing up wont work
            bool targetNeedsToBeInFront = WorldState.TargetNeedsToBeInFront;
            bool invalidTarget = WorldState.InvalidTarget;
            bool outOfRange = WorldState.OutOfRange;

            Console.WriteLine($"MageMakeSureWeAreAttackingEnemyTask: {attackerJustDied} {tooFarAway} {facingWrongWay} {targetNeedsToBeInFront} {invalidTarget} {outOfRange}");
            /*
             * new ColorPosition(1506, 216, ERROR_TEXT_COLOR),
            new ColorPosition(1524, 218, ERROR_TEXT_COLOR),
            new ColorPosition(1531, 219, ERROR_TEXT_COLOR),
             * */
            //Console.WriteLine(string.Join(",", WowImageConstants.TARGET_NEEDS_TO_BE_IN_FRONT_POSITIONS));
            //Console.WriteLine(WorldState.bit)
            //Console.WriteLine(WorldState.Bmp.GetPixel(1506, 216));
            //Console.WriteLine(WorldState.Bmp.GetPixel(1524, 218));
            //Console.WriteLine(WorldState.Bmp.GetPixel(1531, 219));

            if (facingWrongWay)
            {
                // one of the mobs just died, scoot back to make sure the next mob is in front of you
                await ScootBackwardsTask();
            }

            if (targetNeedsToBeInFront)
            {
                // for turning to work, we'd need to click a button after each turn, otherwise the text stays on the screen too long
                //await TurnABitToTheLeftTask();
                await ScootBackwardsTask();
            }

            if (tooFarAway || invalidTarget || outOfRange)
            {
                // we may have targeted something in the distance then got aggroed by something else, clear target so we pick them up
                Keyboard.KeyPress(WowInput.MAGE_CLEAR_TARGET_MACRO);
            }

            return attackerJustDied || tooFarAway || facingWrongWay || targetNeedsToBeInFront || invalidTarget || outOfRange;
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