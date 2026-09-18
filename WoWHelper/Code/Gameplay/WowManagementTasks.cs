using InputManager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using WindowsGameAutomationTools.Images;
using WindowsGameAutomationTools.Slack;
using WoWHelper.Code;
using WoWHelper.Code.WorldState;
using WoWHelper.Shared;
using static WoWHelper.Code.WowPlayerStates;

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
            return !WorldState.OnLoginScreen;
        }

        public async Task<bool> RecoverFromLostWindowFocusTask()
        {
            SlackHelper.SendMessageToChannel("Lost focus on WoWClassic window! Refocusing...");
            await FocusOnWindowTask();

            Mouse.Move(FarmingConfig.ScreenConfiguration.LootDefaultX, FarmingConfig.ScreenConfiguration.LootDefaultX);
            Mouse.PressButton(Mouse.MouseKeys.Left);

            await Task.Delay(300);

            await KeyUpMovementKeys();

            LogoutTriggered = true;
            LogoutReason = "Lost window focus";

            return true;
        }

        public async Task<bool> EveryWorldStateUpdateTasks()
        {
            // ping + refocus if something stole foreground focus from WoW -- every task
            // this method (and its callers) run afterward assumes WoW is the window
            // actually receiving our keyboard/mouse input.  Excluded during
            // WAITING_TO_FOCUS_ON_WINDOW, the startup state before WoW has ever been
            // focused in the first place -- same exclusion the disconnect check below uses.
            // Reads the OS's own notion of the foreground window, not anything decoded off
            // WorldState's pixel row, so (unlike everything past the OnLoginScreen gate
            // below) it's safe to run regardless of whether the addon is rendering yet.
            if (CurrentPlayerState != PlayerState.WAITING_TO_FOCUS_ON_WINDOW)
            {
                IntPtr wowHandle = ScreenCapture.GetWindowHandleByName("WowClassic");
                if (wowHandle != IntPtr.Zero && ScreenCapture.GetForegroundWindow() != wowHandle)
                {
                    await RecoverFromLostWindowFocusTask();
                }
            }

            // ping if logged out (still needs testing.  they changed login screen??)
            // The one check in this method that legitimately needs to run while
            // WorldState.OnLoginScreen is true -- it's specifically watching for that flag's
            // OWN transition (not-on-login-screen -> on-login-screen), so it has to sit
            // before the "everything past here needs a real row" gate below rather than
            // behind it.
            if (!PreviousWorldState.OnLoginScreen &&
                WorldState.OnLoginScreen &&
                !LogoutTriggered &&
                CurrentPlayerState != PlayerState.WAITING_TO_FOCUS_ON_WINDOW)
            {
                SlackHelper.SendMessageToChannel($"DISCONNECT?? Unexpectedly found self on logout screen");
            }

            // Everything below reads WorldState fields decoded off the addon's pixel row.
            // WowWorldState.UpdateFromBitmap decodes that row unconditionally every capture,
            // but its own comment admits the row is garbage whenever the addon isn't actually
            // rendering it yet -- and OnLoginScreen IS that "isn't rendering yet" signal, true
            // not just on the literal login/character-select screen but also during the
            // startup window before/while focusing the WoW window. One gate here instead of
            // repeating !WorldState.OnLoginScreen on every check below -- a garbage read
            // anywhere past this point (a false LogoffMobSeen, a bogus PlayerClass, a
            // coincidentally-low PlayerHpPercent feeding the Petri Alt+F4 check, etc.) could
            // otherwise trigger real actions (logout, alt+f4, alerts) before the addon ever
            // painted a real row.
            if (WorldState.OnLoginScreen)
            {
                return true;
            }

            // Resolve CombatConfiguration/ClassState as soon as the addon gives us a real
            // class read, independent of the RESOLVE_FARMING_CONFIGURATION player state --
            // see WowConfigResolutionTasks.ResolveCombatConfiguration for why (short version:
            // this task runs every tick, including ones before that state ever gets a chance
            // to run, e.g. the bot started while already mid-combat). No-ops quietly once
            // resolved.
            ResolveCombatConfiguration();

            // don't drown
            if (WorldState.Underwater)
            {
                await GetOutOfWater();
            }

            // ping if unseen message -- shared with WaitForWorldBuffThenLogoffTask, which
            // polls WorldState in its own loop rather than going through this method.
            AlertOnUnseenWhisper();

            // ping on level up. Guarded on LocationConfiguration being resolved -- this task
            // runs every tick, including the handful before RESOLVE_FARMING_CONFIGURATION has
            // picked one (see WowPlayer.ResolveFarmingConfigurationTask), during which
            // FarmingConfig.LogoffLevel (a LocationConfiguration passthrough) isn't safe to read.
            if (FarmingConfig.LocationConfiguration != null && PreviousWorldState.Initialized && WorldState.PlayerLevel == PreviousWorldState.PlayerLevel + 1)
            {
                string levelUpMessage = $"Leveled up from {PreviousWorldState.PlayerLevel} to {WorldState.PlayerLevel}!";

                // Newly-unlocked routes only (MinimumLevel exactly matches the level just
                // reached) -- a config that was already eligible before this level-up isn't
                // "new" news, so it's left out to keep the message short.
                List<string> newlyEligibleConfigTitles = WowLocationConfigs.ALL_LOCATIONS
                    .Where(config => config.MinimumLevel == WorldState.PlayerLevel)
                    .Select(config => config.Title)
                    .ToList();
                if (newlyEligibleConfigTitles.Count > 0)
                {
                    levelUpMessage += $" Newly eligible route(s): {string.Join(", ", newlyEligibleConfigTitles)}";
                }

                SlackHelper.SendMessageToChannel(levelUpMessage);

                if (FarmingConfig.LogoffLevel == WorldState.PlayerLevel)
                {
                    LogoutTriggered = true;
                    LogoutReason = $"Reached log out level {FarmingConfig.LogoffLevel}";
                }
            }

            // Bail immediately if we've spotted a mob from LOGOFF_IF_SEEN_MOB_NAMES
            // (CreatureConfig.lua, e.g. "Watery Invader") anywhere nearby -- checked every
            // tick regardless of player state (like the level-up check above), rather than
            // waiting for the CHECK_FOR_LOGOUT state, since we want out the moment it's seen.
            if (WorldState.LogoffMobSeen && !LogoutTriggered)
            {
                LogoutTriggered = true;
                LogoutReason = "Logoff-if-seen mob spotted (see LOGOFF_IF_SEEN_MOB_NAMES in CreatureConfig.lua)";
                SlackHelper.SendMessageToChannel($"Logging out, Logoff-if-seen mob spotted!");
            }

            // Bail if latency's been sustained above 300ms for 10 straight cycles (10
            // seconds -- see HasHighLatency() in WoWFunctions.lua for the cycle/threshold
            // constants). Same "spot it once, trigger logout" pattern as LogoffMobSeen above.
            if (WorldState.HighLatency && !LogoutTriggered)
            {
                LogoutTriggered = true;
                LogoutReason = "High latency sustained for 10+ seconds (see HasHighLatency() in WoWFunctions.lua)";
                SlackHelper.SendMessageToChannel($"Logging out, high latency detected!");
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

        // Edge-triggered Slack screenshot the first tick a new whisper shows up. Pulled out
        // of EveryWorldStateUpdateTasks so WaitForWorldBuffThenLogoffTask -- which sits in
        // its own polling loop and never runs EveryWorldStateUpdateTasks -- can alert on
        // whispers too. Reads the PreviousWorldState/WorldState pair, so it has to be called
        // once per UpdateWorldStateAsync tick or whisper edges get missed.
        public void AlertOnUnseenWhisper()
        {
            if (PreviousWorldState.Initialized && !PreviousWorldState.HasUnseenWhisper && WorldState.HasUnseenWhisper)
            {
                _ = SlackFileUploadWorkaround.UploadScreenshotToChannelAsync(
                    title: "Unseen Whisper!",
                    cropRegion: FarmingConfig.ScreenConfiguration.SlackScreenshotCropRegion);
            }
        }

        public async Task<bool> SetLogoutVariablesTask()
        {
            // LocationConfiguration can still be null here -- e.g. the bot was started while
            // the character was already in combat, so CoreGameplayLoopTask's "already in
            // combat" short-circuit jumped straight to IN_CORE_COMBAT_LOOP and
            // RESOLVE_FARMING_CONFIGURATION (the only place LocationConfiguration gets set --
            // see WowConfigResolutionTasks.cs) never got a chance to run. Every check below
            // reads LocationConfiguration unconditionally, so log out now rather than NRE
            // trying to validate a route we were never told.
            if (FarmingConfig.LocationConfiguration == null)
            {
                LogoutTriggered = true;
                LogoutReason = "LocationConfiguration was never resolved (bot likely started mid-combat, before RESOLVE_FARMING_CONFIGURATION got a chance to run) -- logging out rather than guessing a route";
                await Task.Delay(0);
                return LogoutTriggered;
            }

            float closestWaypointDistance = WowPathfinding.GetDistanceToClosestWaypoint(WorldState.PlayerLocation, FarmingConfig.LocationConfiguration.Waypoints);

            // Checked first so a wrong-zone/under-level/too-far-away start gives the clearest
            // possible reason, rather than getting masked behind some other logout condition
            // that also happens to be true on the very first tick.
            if (FarmingConfig.LocationConfiguration.MinimumLevel > 0 && WorldState.PlayerLevel < FarmingConfig.LocationConfiguration.MinimumLevel)
            {
                LogoutTriggered = true;
                LogoutReason = $"Below minimum level for this route (level {WorldState.PlayerLevel}, need {FarmingConfig.LocationConfiguration.MinimumLevel}+)";
            }
            // Zone.Unknown means this route's config forgot to set Zone -- skip the check rather
            // than have a misconfigured route always immediately abort every session.
            else if (FarmingConfig.LocationConfiguration.Zone != WowZone.Unknown && WorldState.CurrentZone != FarmingConfig.LocationConfiguration.Zone)
            {
                LogoutTriggered = true;
                LogoutReason = $"Wrong zone for this route (currently {WorldState.CurrentZone}, expected {FarmingConfig.LocationConfiguration.Zone})";
            }
            else if (closestWaypointDistance > WowPlayerConstants.MAX_DISTANCE_FROM_ROUTE_WAYPOINT)
            {
                LogoutTriggered = true;
                LogoutReason = $"Too far from this route's waypoints (closest is {closestWaypointDistance:0.00}, allowed {WowPlayerConstants.MAX_DISTANCE_FROM_ROUTE_WAYPOINT:0.00})";
            }
            // LogoutOnLowDynamiteEnabled is toggled live in-game via the addon's
            // /yyconfig menu (YoyokazooUI.lua) -- see WowWorldState.LogoutOnLowDynamiteEnabled.
            else if (WorldState.LogoutOnLowDynamiteEnabled && WorldState.LowOnDynamite)
            {
                LogoutTriggered = true;
                LogoutReason = $"Low on Dynamite";
            }
            // TODO: Config this somehow??
            else if (WorldState.LowOnHealthPotions)
            {
                LogoutTriggered = true;
                LogoutReason = $"Low on Health Potions";
            }
            // EngageMethod.Pull now covers both Warrior's ranged bow/gun pull (which needs
            // ammo) and Shaman's spell pull (which never does -- and would otherwise
            // always read as "low on ammo", since a caster's ammo slot is just empty, not
            // merely low). Only Warrior can actually run out of ammo, so gate on class too.
            else if (WorldState.LowOnAmmo &&
                FarmingConfig.CombatConfiguration == Code.Gameplay.WowCombatConfiguration.Warrior &&
                FarmingConfig.EngageMethod == WowLocationConfiguration.EngagementMethod.Pull)
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
            // Same deal as LogoutOnLowDynamiteEnabled above -- toggled live via /yyconfig.
            else if (WorldState.LogoutOnFullBagsEnabled && WorldState.BagsAreFull)
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
            if (!FullBagsAlertSent && WorldState.BagsAreFull)
            {
                SlackHelper.SendMessageToChannel($"Bags are full!");
                FullBagsAlertSent = true;
            }

            await Task.Delay(0);

            return LogoutTriggered;
        }

        // Loops until WorldState.HasDesiredWorldBuff comes back true -- which world buff that
        // checks for (Ony/Rend/ZG) is chosen in-game via the addon's /yyconfig "Desired world
        // buff" selector (YoyokazooUI.lua/WoWFunctions.lua), not hardcoded here. Meant to be
        // run while standing in a city/raid waiting area for a world buff to land: each
        // iteration waits up to WowPlayerConstants.WORLD_BUFF_WAIT_MILLIS, polling WorldState
        // at the normal UpdateWorldStateAsync cadence (same pattern as WaitUnlessInCombatTask
        // in WowCommonCombatTasks.cs) so the buff is noticed the moment it lands instead of
        // only at the end of the wait, then taps strafe-left/strafe-right briefly to reset
        // WoW's AFK kick timer before waiting again. Once the buff is seen, sends a Slack
        // alert and logs out. Also runs AlertOnUnseenWhisper() on every poll -- this loop
        // deliberately doesn't go through EveryWorldStateUpdateTasks (nothing else in there
        // applies while parked in a city waiting for a buff), but sitting idle for a long
        // stretch is exactly when a whisper is most likely to show up and want a ping.
        public async Task<bool> WaitForWorldBuffThenLogoffTask()
        {
            Console.WriteLine("Waiting for desired world buff...");
            await UpdateWorldStateAsync();
            AlertOnUnseenWhisper();

            while (!WorldState.HasDesiredWorldBuff)
            {
                long deadline = DateTimeOffset.Now.ToUnixTimeMilliseconds() + WowPlayerConstants.WORLD_BUFF_WAIT_MILLIS;
                while (!WorldState.HasDesiredWorldBuff && DateTimeOffset.Now.ToUnixTimeMilliseconds() < deadline)
                {
                    await UpdateWorldStateAsync();
                    AlertOnUnseenWhisper();
                }

                if (WorldState.HasDesiredWorldBuff)
                {
                    break;
                }

                // Nudge left/right -- doesn't move the player anywhere real, just enough
                // input to reset the client's AFK timer.
                Keyboard.KeyDown(WowInput.STRAFE_LEFT);
                await Task.Delay(200);
                Keyboard.KeyUp(WowInput.STRAFE_LEFT);

                Keyboard.KeyDown(WowInput.STRAFE_RIGHT);
                await Task.Delay(200);
                Keyboard.KeyUp(WowInput.STRAFE_RIGHT);
            }

            Console.WriteLine("Desired world buff detected, logging out.");
            SlackHelper.SendMessageToChannel("Got the desired world buff! Logging out.");
            await StartLogoutTask();

            return true;
        }

        public async Task<bool> StartLogoutTask()
        {
            Console.WriteLine($"Starting logout: {LogoutReason}");
            await Task.Delay(0);
            await WowInput.PressKey(WowInput.LOGOUT_MACRO);
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
            await WaitUnlessInCombatTask(1500);
            return true;
        }

        public async Task<bool> SkinTask()
        {
            Mouse.Move(LootX, LootY);
            Mouse.PressButton(Mouse.MouseKeys.Right);

            // Give the client a moment to register the right-click and start the Skinning
            // cast bar, then check WorldState.IsCurrentlySkinning (see UIFunctions.lua's
            // pixel row / WoWFunctions.lua's IsCurrentlySkinning()) -- if it never started
            // (e.g. the corpse wasn't actually skinnable, or the click missed), there's
            // nothing to wait out, so return immediately instead of sitting through the
            // rest of the old flat 3000ms wait. UpdateWorldState() (not the Async variant)
            // since we just want an immediate re-capture here, not another throttled wait
            // on top of the 200ms we already did.
            await Task.Delay(200);
            UpdateWorldState();

            if (WorldState.IsCurrentlySkinning)
            {
                // Skinning is actually in progress -- wait out the rest of its ~3s cast.
                // WaitUnlessInCombatTask keeps this interruptible if a mob aggroes mid-skin,
                // same protection the old flat wait had.
                await WaitUnlessInCombatTask(2800);
            }

            return true;
        }

        public async Task<bool> PetriAltF4Task()
        {
            await WaitForGlobalCooldownTask();
            await WowInput.PressKeyWithShift(WowInput.SHIFT_PETRIFICATION_FLASK);
            await Task.Delay(750);
            await WowInput.PressKeyWithAlt(WowInput.ALT_FORCE_QUIT_KEY);
            return true;
        }

        public async Task<bool> ThrowTargetDummyTask()
        {
            Mouse.Move(FarmingConfig.ScreenConfiguration.DynamiteAndDummyX, FarmingConfig.ScreenConfiguration.DynamiteAndDummyY);
            await Task.Delay(50);
            await WowInput.PressKeyWithShift(WowInput.SHIFT_TARGET_DUMMY);
            await Task.Delay(1000);
            return true;
        }

        public async Task<bool> PutMoneyInTradeTask()
        {
            Mouse.Move(215, 350);
            await Task.Delay(500);
            Mouse.PressButton(Mouse.MouseKeys.Left);
            await Task.Delay(200);

            await WowInput.PressKey(System.Windows.Forms.Keys.D2);
            await Task.Delay(200);

            Mouse.Move(290, 350);
            await Task.Delay(500);
            Mouse.PressButton(Mouse.MouseKeys.Left);
            await Task.Delay(200);

            await WowInput.PressKey(System.Windows.Forms.Keys.D1);
            await Task.Delay(200);
            await WowInput.PressKey(System.Windows.Forms.Keys.D0);
            await Task.Delay(200);

            return true;
        }

        public async Task<bool> AcceptTradeTask()
        {
            Mouse.Move(440, 1025);
            await Task.Delay(500);
            Mouse.PressButton(Mouse.MouseKeys.Left);
            await Task.Delay(200);

            // Time for the button to become ungreyed out
            await Task.Delay(2000);

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

        public async Task<bool> CancelTradeTask()
        {

            Mouse.Move(650, 237);
            await Task.Delay(500);
            Mouse.PressButton(Mouse.MouseKeys.Left);
            await Task.Delay(200);

            return true;
        }


        public async Task<bool> CupidTradeLoopTask()
        {
            Console.WriteLine("Kicking off cupid trade loop");
            var currentTradeState = TradeState.WAITING_FOR_TRADE_WINDOW;

            HashSet<string> tradeBlocklist = new HashSet<string>();

            await FocusOnWindowTask();
            await Task.Delay(500);

            long loopIterations = 0;

            while (true)
            {
                loopIterations++;

                Bitmap bmp = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(WorldState.ScreenConfig.CaptureRectangle);
                bool tradeWindowUp = WorldState.ScreenConfig.TradeWindowScreenPositions.MatchesSourceImage(bmp);
                bool tradeWindowAccepted = WorldState.ScreenConfig.TradeWindowAcceptedScreenPositions.MatchesSourceImage(bmp);
                bool tradeWindowConfirmationUp = WorldState.ScreenConfig.TradeWindowConfirmationScreenPositions.MatchesSourceImage(bmp);
                String tradeRecipient = WorldState.ScreenConfig.TradeWindowRecipientTextArea.GetText(TesseractEngineSingleton.Instance, bmp).Trim();
                bmp.Dispose();

                if (currentTradeState == TradeState.POPULATING_TRADE_WINDOW && !tradeWindowUp)
                {
                    //Console.WriteLine("Trade canceled");
                    currentTradeState = TradeState.WAITING_FOR_TRADE_WINDOW;
                }

                if (currentTradeState == TradeState.WAITING_FOR_TRADE_WINDOW && loopIterations % 10000 == 0)
                {
                    Keyboard.KeyDown(WowInput.STRAFE_LEFT);
                    await Task.Delay(100);
                    Keyboard.KeyUp(WowInput.STRAFE_LEFT);
                }
                else if(currentTradeState == TradeState.WAITING_FOR_TRADE_WINDOW && loopIterations % 5000 == 0)
                {
                    Keyboard.KeyDown(WowInput.STRAFE_RIGHT);
                    await Task.Delay(100);
                    Keyboard.KeyUp(WowInput.STRAFE_RIGHT);
                }

                switch (currentTradeState)
                {
                    case TradeState.WAITING_FOR_TRADE_WINDOW:
                        //Console.WriteLine("Waiting for trade window");
                        if (tradeWindowUp)
                        {
                            currentTradeState = TradeState.CHECKING_BLOCKLIST;
                        }
                        break;
                    case TradeState.CHECKING_BLOCKLIST:
                        //Console.WriteLine($"Checking blocklist for {tradeRecipient}");
                        if(tradeBlocklist.Contains(tradeRecipient))
                        {
                            Console.WriteLine($"Trade opened by previous recipient, {tradeRecipient}, canceling");
                            //currentTradeState = TradeState.CANCELING_TRADE;
                            currentTradeState = TradeState.POPULATING_TRADE_WINDOW;
                        }
                        else
                        {
                            currentTradeState = TradeState.POPULATING_TRADE_WINDOW;
                        }
                        break;
                    case TradeState.POPULATING_TRADE_WINDOW:
                        //Console.WriteLine($"Populating trade window");
                        await PutMoneyInTradeTask();
                        await AcceptTradeTask();
                        currentTradeState = TradeState.WAITING_FOR_TRADE_ACCEPTANCE;
                        break;
                    case TradeState.WAITING_FOR_TRADE_ACCEPTANCE:
                        //Console.WriteLine($"Waiting for trade acceptance");
                        if (tradeWindowAccepted)
                        {
                            //Console.WriteLine($"Trade accepted");
                            currentTradeState = TradeState.CONFIRMING_TRADE;
                        }
                        break;
                    case TradeState.CONFIRMING_TRADE:
                        //Console.WriteLine($"Confirming trade");
                        
                        await AcceptTradeConfirmationTask();
                        Console.WriteLine($"Accepting trade, adding {tradeRecipient} to the blocklist");
                        tradeBlocklist.Add(tradeRecipient);

                        await Task.Delay(2000); // give it a couple seconds for the trade to complete so we don't screenshot instantly and think we're in a new trade

                        currentTradeState = TradeState.WAITING_FOR_TRADE_WINDOW;

                        break;
                    case TradeState.CANCELING_TRADE:
                        //Console.WriteLine($"Canceling trade");
                        await CancelTradeTask();
                        currentTradeState = TradeState.WAITING_FOR_TRADE_WINDOW;
                        break;
                }
            }

            Console.WriteLine("Exited Core Gameplay");
            Environment.Exit(0);

            return true;
        }
    }
}
