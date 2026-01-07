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
            bool shouldThrowDynamite = WorldState.AttackerCount > 1;

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
    }
}