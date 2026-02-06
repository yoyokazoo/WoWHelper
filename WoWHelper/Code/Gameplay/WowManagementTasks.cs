using InputManager;
using System;
using System.Threading.Tasks;
using WindowsGameAutomationTools.Images;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    public partial class WowPlayer
    {
        public async Task<bool> FocusOnWindowTask()
        {
            IntPtr h = ScreenCapture.GetWindowHandleByName("WowClassic");
            if (h == IntPtr.Zero) { return false; }

            ScreenCapture.SetForegroundWindow(h);

            await Task.Delay(1750);
            return true;
        }

        // 
        public async Task<bool> EveryWorldStateUpdateTasks()
        {
            // TODO: combine these into a shared task
            // don't drown
            if (WorldState.Underwater)
            {
                await GetOutOfWater();
            }

            // ping if unseen message
            if (FarmingConfig.AlertOnUnreadWhisper && PreviousWorldState.Initialized && !PreviousWorldState.HasUnseenWhisper && WorldState.HasUnseenWhisper)
            {
                SlackHelper.SendMessageToChannel($"Unseen Whisper!");
            }

            // ping if logged out (still needs testing.  they changed login screen??)
            if (!PreviousWorldState.OnLoginScreen && WorldState.OnLoginScreen && !LogoutTriggered)
            {
                SlackHelper.SendMessageToChannel($"DISCONNECT?? Unexpectedly found self on logout screen");
            }

            // ping on level up
            if (FarmingConfig.AlertOnUnreadWhisper && PreviousWorldState.Initialized && WorldState.PlayerLevel == PreviousWorldState.PlayerLevel + 1)
            {
                SlackHelper.SendMessageToChannel($"Leveled up from {PreviousWorldState.PlayerLevel} to {WorldState.PlayerLevel}!");
                if (FarmingConfig.LogoffLevel == WorldState.PlayerLevel)
                {
                    LogoutTriggered = true;
                    LogoutReason = $"Reached log out level {FarmingConfig.LogoffLevel}";
                }
            }

            // If we're about to die, petri alt+f4
            if (WorldState.PlayerHpPercent <= WowPlayerConstants.PETRI_ALTF4_HP_THRESHOLD && WorldState.PlayerLevel >= WowGameplayConstants.PETRIFICATION_FLASK_LEVEL)
            {
                SlackHelper.SendMessageToChannel($"Petri Alt+F4ed at ~{WorldState.PlayerHpPercent}%!  Consider using Unstuck instead of logging back in");
                await PetriAltF4Task();
                Environment.Exit(0);
            }

            return true;
        }

        public async Task<bool> SetLogoutVariablesTask()
        {
            if (FarmingConfig.LogoutOnLowDynamite && WorldState.LowOnDynamite)
            {
                LogoutTriggered = true;
                LogoutReason = $"Low on Dynamite";
            }
            else if (WorldState.LowOnHealthPotions)
            {
                LogoutTriggered = true;
                LogoutReason = $"Low on Health Potions";
            }
            else if (WorldState.LowOnAmmo && FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Shoot)
            {
                LogoutTriggered = true;
                LogoutReason = $"Low on Ammo";
            }
            else if (!CurrentTimeInsideDuration(FarmStartTime, WowPlayerConstants.FARM_TIME_LIMIT_MILLIS))
            {
                LogoutTriggered = true;
                LogoutReason = $"Farm Time Limit Reached";
            }
            else if (EngageAttempts >= WowPlayerConstants.ENGAGE_ROTATION_ATTEMPTS)
            {
                LogoutTriggered = true;
                LogoutReason = $"Failed to engage target after {WowPlayerConstants.ENGAGE_ROTATION_ATTEMPTS} loops.  Something wrong?";
            }
            else if (FarmingConfig.LogoutOnFullBags && WorldState.BagsAreFull)
            {
                LogoutTriggered = true;
                LogoutReason = $"Bags are full!";
            }
            else if (!WorldState.EnemyNameplatesAreTurnedOn)
            {
                LogoutTriggered = true;
                LogoutReason = $"Enemy nameplates aren't turned on!";
            }

            // also send once-per-session alerts here
            if (FarmingConfig.AlertOnFullBags && !FullBagsAlertSent && WorldState.BagsAreFull)
            {
                SlackHelper.SendMessageToChannel($"Bags are full!");
                FullBagsAlertSent = true;
            }

            await Task.Delay(0);

            return LogoutTriggered;
        }

        public async Task<bool> StartLogoutTask()
        {
            Console.WriteLine($"Starting logout: {LogoutReason}");
            await Task.Delay(0);
            Keyboard.KeyPress(WowInput.LOGOUT_MACRO);
            return true;
        }

        public async Task<bool> CheckIfLoggedOutTask()
        {
            await Task.Delay(0);
            return WorldState.OnLoginScreen;
        }

        public async Task<bool> LootTask()
        {
            Mouse.Move(LootX, LootY);
            Mouse.PressButton(Mouse.MouseKeys.Right);
            await Task.Delay(1500);
            return true;
        }

        public async Task<bool> SkinTask()
        {
            Mouse.Move(LootX, LootY);
            Mouse.PressButton(Mouse.MouseKeys.Right);
            await Task.Delay(3000);
            return true;
        }

        public async Task<bool> PetriAltF4Task()
        {
            while (!WorldState.GCDCooledDown)
            {
                await UpdateWorldStateAsync();
            }

            await WowInput.PressKeyWithShift(WowInput.SHIFT_PETRIFICATION_FLASK);
            await Task.Delay(750);
            await WowInput.PressKeyWithAlt(WowInput.ALT_FORCE_QUIT_KEY);
            return true;
        }

        public async Task<bool> ThrowTargetDummyTask()
        {
            Mouse.ButtonDown(Mouse.MouseKeys.Left);
            await Task.Delay(30);
            // TODO: ?? switch to like dynamite??
            Mouse.MoveRelative(0, 200);
            await Task.Delay(30);
            Mouse.ButtonUp(Mouse.MouseKeys.Left);

            await Task.Delay(250);

            await WowInput.PressKeyWithShift(WowInput.SHIFT_TARGET_DUMMY);
            await Task.Delay(250);
            return true;
        }

        public async Task<bool> PutMoneyInTradeTask()
        {
            Mouse.Move(215, 350);
            await Task.Delay(500);
            Mouse.PressButton(Mouse.MouseKeys.Left);
            await Task.Delay(200);

            Keyboard.KeyPress(System.Windows.Forms.Keys.D1);
            await Task.Delay(200);

            Mouse.Move(290, 350);
            await Task.Delay(500);
            Mouse.PressButton(Mouse.MouseKeys.Left);
            await Task.Delay(200);

            Keyboard.KeyPress(System.Windows.Forms.Keys.D4);
            await Task.Delay(200);
            Keyboard.KeyPress(System.Windows.Forms.Keys.D0);
            await Task.Delay(200);

            return true;
        }

        public async Task<bool> AcceptTradeTask()
        {
            Mouse.Move(440, 1025);
            await Task.Delay(500);
            Mouse.PressButton(Mouse.MouseKeys.Left);
            await Task.Delay(200);

            return true;
        }

        public async Task<bool> AcceptTradeConfirmationTask()
        {
            
            Mouse.Move(1155, 400);
            await Task.Delay(500);
            Mouse.PressButton(Mouse.MouseKeys.Left);
            await Task.Delay(200);

            return true;
        }
    }
}
