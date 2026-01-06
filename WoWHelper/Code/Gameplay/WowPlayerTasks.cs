using InputManager;
using System;
using System.Numerics;
using System.Threading.Tasks;
using WindowsGameAutomationTools.Images;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;
using WoWHelper.Code.Gameplay;
using System.Windows.Forms;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        public async Task<bool> StartBattleReadyRecoverTask()
        {
            if (WorldState.PlayerHpPercent < WowPlayerConstants.EAT_FOOD_HP_THRESHOLD)
            {
                await WowInput.PressKeyWithShift(WowInput.WARRIOR_SHIFT_EAT_FOOD_KEY);
            }

            return true;
        }

        public async Task<bool> WaitUntilBattleReadyTask()
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

        public async Task<bool> KickOffEngageTask()
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

        public async Task<bool> FaceCorrectDirectionToEngageTask()
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

        public async Task<bool> StartAttackTask()
        {
            await Task.Delay(0);

            // always kick things off with heroic strike macro to /startattack
            Keyboard.KeyPress(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST_MACRO);

            return true;
        }

        public async Task<bool> MakeSureWeAreAttackingEnemyTask()
        {
            bool attackerJustDied = PreviousWorldState?.AttackerCount > WorldState.AttackerCount && WorldState.AttackerCount > 0;
            bool inCombatButNotAutoAttacking = WorldState.IsInCombat && !WorldState.IsAutoAttacking;
            bool tooFarAway = WorldState.TooFarAway;
            bool facingWrongWay = WorldState.FacingWrongWay; // potentially need to turn in case we're webbed and backing up wont work
            bool targetNeedsToBeInFront = WorldState.TargetNeedsToBeInFront;

            // now since we have more accurate "are they in front" checking, try not backing up if attacker just died
            if (/*attackerJustDied || */facingWrongWay || targetNeedsToBeInFront)
            {
                // one of the mobs just died, scoot back to make sure the next mob is in front of you
                await ScootBackwardsTask();
            }

            if (tooFarAway)
            {
                // we may have targeted something in the distance then got aggroed by something else, clear target so we pick them up
                Keyboard.KeyPress(WowInput.WARRIOR_CLEAR_TARGET_MACRO);
            }

            if (attackerJustDied || inCombatButNotAutoAttacking || tooFarAway)
            {
                // /startattack
                Keyboard.KeyPress(WowInput.WARRIOR_MORTALSTRIKE_BLOODTHIRST_MACRO);
            }

            return attackerJustDied || inCombatButNotAutoAttacking || tooFarAway || facingWrongWay || targetNeedsToBeInFront;
        }

        public async Task<bool> TooManyAttackersTask()
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

        public async Task<bool> UseDiamondFlaskTask()
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

        public async Task<bool> UseHealingTrinketTask()
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

        public async Task<bool> StartOfCombatBerserkerRage()
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
