using InputManager;
using System.Threading.Tasks;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        // ensureValidState updates state before checking
        // Set to false only if you're sure we haven't clicked a button between the last world state update
        public async Task<bool> WaitForGlobalCooldownTask(bool ensureValidState = true)
        {
            if (ensureValidState)
            {
                await UpdateWorldStateAsync();
            }

            while (!WorldState.GCDCooledDown)
            {
                await UpdateWorldStateAsync();
            }

            return true;
        }

        public async Task<bool> ThrowDynamiteTask()
        {
            bool shouldThrowDynamite = WorldState.AttackerCount > 1 && WorldState.PlayerLevel >= WowGameplayConstants.DYNAMITE_LEVEL;

            if (shouldThrowDynamite)
            {
                Mouse.Move(1770, 770);
                await Task.Delay(50);
                Keyboard.KeyPress(WowInput.THROW_DYNAMITE);
                await Task.Delay(50);
                Mouse.PressButton(Mouse.MouseKeys.Left);
                await Task.Delay(1000);
            }

            return shouldThrowDynamite;
        }

        public async Task<bool> UseHealingPotionTask()
        {
            bool shouldUseHealingPotion = WorldState.PlayerHpPercent <= WowGameplayConstants.HEALING_POTION_HP_THRESHOLD;

            if (shouldUseHealingPotion)
            {
                if (FarmingConfig.AlertOnPotionUsed)
                {
                    SlackHelper.SendMessageToChannel("Potion used!");
                }
                await Task.Delay(200); // there's a brief, non-gcd limiter that prevents clicking everything simultaneously
                await WowInput.PressKeyWithShift(WowInput.SHIFT_HEALING_POTION);
            }

            return shouldUseHealingPotion;
        }

        public async Task<bool> MeleeMakeSureWeAreAttackingEnemyTask()
        {
            bool attackerJustDied = PreviousWorldState?.AttackerCount > WorldState.AttackerCount && WorldState.AttackerCount > 0;
            bool inCombatButNotAutoAttacking = WorldState.IsInCombat && !WorldState.IsAutoAttacking;
            bool tooFarAway = WorldState.TooFarAway;
            bool facingWrongWay = WorldState.FacingWrongWay; // potentially need to turn in case we're webbed and backing up wont work
            bool targetNeedsToBeInFront = WorldState.TargetNeedsToBeInFront;
            bool invalidTarget = WorldState.InvalidTarget;
            bool outOfRange = WorldState.OutOfRange;

            if (facingWrongWay || targetNeedsToBeInFront)
            {
                // scoot back to make sure the mob is in front of you
                await ScootBackwardsTask();
            }

            // TODO: count attackers, add bool to see if our currently targeted mob is tagged by us
            // so we don't spam clear if we don't have to
            if (tooFarAway || invalidTarget || outOfRange)
            {
                // we may have targeted something in the distance then got aggroed by something else, clear target so we pick them up
                Keyboard.KeyPress(WowInput.CLEAR_TARGET_MACRO);
            }

            if (attackerJustDied || inCombatButNotAutoAttacking || tooFarAway)
            {
                // /startattack
                Keyboard.KeyPress(WowInput.START_ATTACK);
            }

            return attackerJustDied || inCombatButNotAutoAttacking || tooFarAway || facingWrongWay || targetNeedsToBeInFront || invalidTarget || outOfRange;
        }

        public async Task<bool> StartAttackTask()
        {
            await Task.Delay(0);

            // always kick things off with /startattack
            Keyboard.KeyPress(WowInput.START_ATTACK);

            return true;
        }
    }
}