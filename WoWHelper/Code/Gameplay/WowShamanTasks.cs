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
        public async Task<bool> ShamanCombatLoopTask()
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

                if (!startOfCombatWiggled && PreviousWorldState?.TargetHpPercent == 100 && WorldState.TargetHpPercent < 100)
                {
                    await StartOfCombatWiggle();
                    startOfCombatWiggled = true; // maybe not necessary? if they keep going to 100 maybe they're evading and it's good to keep backing up?
                }

                // Finally, if we've made it this far, do standard combat actions
                if (!WorldState.HasRockbiterWeaponOn)
                {
                    await WowInput.PressKeyWithShift(WowInput.SHAMAN_SHIFT_ROCKBITER_WEAPON);
                }
                if (WorldState.CanCastEarthShock && (WorldState.AttackerCount > 1 || WorldState.TargetHpPercent > 20)) // don't shock almost dead targets unless we have multiples
                {
                    Keyboard.KeyPress(WowInput.SHAMAN_SHOCK);
                }
                else// if (WorldState.AttackerCount <= 1)
                {
                    // we should already be attacking
                    //Keyboard.KeyPress(WowInput.SHAMAN_ATTACK);
                }
            } while (WorldState.IsInCombat);

            return true;
        }

        public async Task<bool> ShamanStartBattleReadyRecoverTask()
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

        public async Task<bool> ShamanWaitUntilBattleReadyTask()
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
            }

            return battleReady;
        }

        public async Task<bool> ShamanKickOffEngageTask()
        {
            EngageAttempts = 1;

            Keyboard.KeyPress(WowInput.SHAMAN_LIGHTNING_BOLT);
            await Task.Delay(500); // IsCurrentlyCasting can take a little bit to update, give it a buffer
            return true;
        }

        public async Task<bool> ShamanFaceCorrectDirectionToEngageTask()
        {
            EngageAttempts++;

            Console.WriteLine($"ShamanFaceCorrectDirectionToEngageTask, EngageAttempts {EngageAttempts}, WorldState.IsCurrentlyCasting? {WorldState.IsCurrentlyCasting}");
            if (!WorldState.IsCurrentlyCasting)
            {
                await TurnABitToTheLeftTask();
                Keyboard.KeyPress(WowInput.SHAMAN_LIGHTNING_BOLT);
                await Task.Delay(500); // IsCurrentlyCasting can take a little bit to update, give it a buffer
                await UpdateWorldStateAsync();
            }

            return CanEngageTarget();
        }

        public async Task<bool> ShamanEmergencyTask()
        {
            await Task.Delay(0);
            bool tooManyAttackers = WorldState.AttackerCount >= FarmingConfig.TooManyAttackersThreshold;
            bool emergencyHpThreshold = WorldState.PlayerHpPercent <= WowPlayerConstants.OH_SHIT_RETAL_HP_THRESHOLD;

            if (tooManyAttackers || emergencyHpThreshold)
            {
                string warningMessage = tooManyAttackers ? "TOO MANY ATTACKERS HELP" : $"Emergency HP Threshold hit ({WorldState.PlayerHpPercent})";
                SlackHelper.SendMessageToChannel(warningMessage);

                // Figure out what to do here.  War stomp? Magma Totem? War stomp -> ghost wolf -> run to safety?

                LogoutReason = $"Got into an emergency situation ({warningMessage}), logging off for safety";
                LogoutTriggered = true;
            }

            return tooManyAttackers;
        }
    }
}
