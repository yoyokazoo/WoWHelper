using System.Drawing;
using System.Numerics;
using WindowsGameAutomationTools.ImageDetection;
using WindowsGameAutomationTools.Images;
using WoWHelper.Code.Config;
using WoWHelper.Code.Gameplay;
using WoWHelper.Code.WorldState;

namespace WoWHelper
{
    public class WowWorldState
    {
        public bool Initialized { get; private set; }
        
        public float MapX { get; private set; }
        public float MapY { get; private set; }
        public Vector2 PlayerLocation { get; private set; }
        public float FacingDegrees { get; private set; }

        // The three status bars (pixel rows 0-2)
        public int PlayerHpPercent { get; private set; }
        public int ResourcePercent { get; private set; }
        public int TargetHpPercent { get; private set; }

        // MultiIntOne's three bytes
        public int AttackerCount { get; private set; }
        public int PlayerLevel { get; private set; }
        public WowZone CurrentZone { get; private set; }

        public bool IsInCombat { get; private set; }

        public bool IsAutoAttacking { get; private set; }
        public bool LowOnHealthPotions { get; private set; }
        public bool LowOnDynamite { get; private set; }
        public bool LowOnAmmo { get; private set; }
        public bool GCDCooledDown { get; private set; }
        public bool BagsAreFull { get; private set; }
        public bool IsPlayerPetrified { get; private set; }
        public bool HasUnseenWhisper { get; private set; }
        public bool IsCurrentlyCasting { get; private set; }
        public bool EnemyNameplatesAreTurnedOn { get; private set; }
        public bool CurrentTargetInCombatWithUs { get; private set; }
        public bool IsTargetCasterMob { get; private set; }
        public bool IsTargetRunnerMob { get; private set; }
        public bool IsTargetFireImmune { get; private set; }
        public bool IsTargetNatureImmune { get; private set; }
        public bool IsTargetCasting { get; private set; }
        public bool PlayerIsPoisoned { get; private set; }
        public bool PlayerIsDiseased { get; private set; }
        public bool TargetRecentlyEvaded { get; private set; }

        // MultiBoolTwo boxes 1-3/5-6 (see MULTI_BOOL_TWO_FIELDS in WoWFunctions.lua).
        public bool IsTargetLongRangeCaster { get; private set; }
        public bool LogoffMobSeen { get; private set; }
        public bool IsCurrentlySkinning { get; private set; }
        // Mob-name-list lookups (BLEED_IMMUNE_MOB_NAMES/FEAR_CASTER_MOB_NAMES in
        // CreatureConfig.lua), same pattern as IsTargetLongRangeCaster above -- class-agnostic
        // even though only Warrior consumes them today (see WowWarriorHandler.SingleCombatTask).
        public bool IsTargetBleedImmune { get; private set; }
        public bool IsTargetFearCaster { get; private set; }

        // MultiBoolTwo boxes 9/10. Run-specific settings toggled live in-game via the addon's /yyconfig menu (YoyokazooUIDB.logoutOnLowDynamite/
        // logoutOnFullBags, IsLogoutOnLowDynamiteEnabled()/IsLogoutOnFullBagsEnabled() in
        // YoyokazooUI.lua) rather than hardcoded on the C# side -- see
        // WowManagementTasks.SetLogoutVariablesTask(), the only reader of these.
        public bool LogoutOnLowDynamiteEnabled { get; private set; }
        public bool LogoutOnFullBagsEnabled { get; private set; }

        // MultiBoolTwo box 11. Also a /yyconfig-driven setting rather than
        // a live game-state query -- true when the player currently has whichever world buff
        // is selected by the addon's "Desired world buff" selector (HasDesiredWorldBuff() in
        // WoWFunctions.lua, WORLD_BUFF_CHOICES for the list of choices). Consumed by
        // WowManagementTasks.WaitForWorldBuffThenLogoffTask().
        public bool HasDesiredWorldBuff { get; private set; }

        // MultiBoolTwo box 12. Unlike LogoutOnLowDynamiteEnabled/LogoutOnFullBagsEnabled/
        // HasDesiredWorldBuff above, this is a live game-state query, not a /yyconfig
        // setting. True once HasHighLatency()
        // (WoWFunctions.lua) has seen 10 consecutive per-second latency samples all above
        // 300ms (10 sustained seconds). Consumed by
        // WowManagementTasks.EveryWorldStateUpdateTasks(), same "spot it once, trigger
        // logout" pattern as LogoffMobSeen above.
        public bool HighLatency { get; private set; }

        // MultiBoolTwo box 13. Live game-state query like HighLatency
        // above. True once IsCombatStalemate() (YoyokazooUI.lua) has seen the player in
        // combat for COMBAT_STALEMATE_SECONDS (30s) with no damage/miss combat-log event
        // involving the player or their pet in that time (EVADE misses excluded) -- i.e.
        // aggroed by something that can't reach us, e.g. a mob stuck on the shore while
        // we're in the water, where combat never drops and nothing ever happens. Consumed
        // by WowManagementTasks.EveryWorldStateUpdateTasks() -- which starts the logout
        // itself and waits for the login screen there, rather than only setting
        // LogoutTriggered like LogoffMobSeen/HighLatency, since the combat loop can't reach
        // the logout states while combat never drops.
        public bool CombatStalemate { get; private set; }

        // MultiBoolTwo boxes 14/15. Whether the player has trained every
        // class skill available at their current level, and whether they have enough money
        // to train everything still missing (also true when nothing's missing). Both come
        // from hand-maintained per-class trainer tables in the addon (WARRIOR_TRAINER_SPELLS
        // etc.). Inputs to the leveling-goal logic built on WowLevelingConfigs.cs.
        public bool AllSkillsKnownForThisLevel { get; private set; }
        public bool CanAffordToTrainAllSkills { get; private set; }

        // MultiBoolTwo box 16. A melee weapon at yellow durability or
        // worse, or any other equipped piece at red (broken) -- see GearNeedsRepair() in
        // WoWFunctions.lua. Refreshed by the addon only once a minute.
        public bool GearNeedsRepair { get; private set; }

        // MultiBoolTwo box 17. The current target is a valid one to fight
        // at all -- ShouldWeAttackTarget() in WoWFunctions.lua (exists, attackable, alive, not
        // PvP-flagged, doesn't con grey, not too many levels above us, not already in combat).
        // Pure eligibility: unlike CanEngageTarget() / the per-class CanChargeTarget/
        // CanShootTarget/CanSpellcastPullTarget bits, no range or cooldown is folded in, so
        // this can tell "valid but out of range" apart from "not a valid target".
        public bool IsTargetAttackable { get; private set; }

        // Which supported class the player is, from whichever supported-class bit is on
        // (MultiBoolOne box 18 Warrior / 20 Shaman, MultiBoolTwo box 4 Warlock). Null when
        // none is -- an unsupported class, or the addon isn't rendering a real row yet.
        public WowCombatConfiguration? PlayerClass { get; private set; }

        // Red error toasts (UI_ERROR_MESSAGE). Each is true for 0.5s after the toast fires --
        // see HasRecentUiError() in YoyokazooUI.lua.
        public bool FacingWrongWay { get; private set; } // Melee facing the wrong direction
        public bool TargetNeedsToBeInFront { get; private set; } // Spellcast facing the wrong direction
        public bool InvalidTarget { get; private set; } // Spellcast on allied/dead target
        public bool OutOfRange { get; private set; } // Spellcast too far away
        public bool NotInLineOfSight { get; private set; } // Spellcast blocked by geometry
        public bool NoPathAvailable { get; private set; } // Charge can't reach target
        public bool TargetUnreachable => NotInLineOfSight || NoPathAvailable;

        // The sentinel box after the health bar reads exactly WowPixelRows.ADDON_LOADED_COLOR --
        // the addon is loaded and rendering the pixel rows, so the rest of them are meaningful.
        public bool AddonLoaded { get; private set; }

        // Whether the WoW client window was the OS foreground window at capture time --
        // i.e. it's the window actually receiving our keyboard/mouse input, and nothing is
        // covering the screen area we capture. Only set by a live capture
        // (GetWoWWorldState); stays false for bitmap-only updates (tests).
        public bool WowWindowHasFocus { get; private set; }

        // Both of the above: the capture is of WoW, and the addon's pixel row in it is
        // real. Everything decoded off the row is garbage unless this is true.
        public bool IsBotInAValidState => AddonLoaded && WowWindowHasFocus;

        // MultiBoolTwo box 22. The breath timer is running (IsPlayerUnderwater() in
        // WoWFunctions.lua).
        public bool Underwater { get; private set; }

        public Bitmap Bmp { get; private set; }

        public WowScreenConfiguration ScreenConfig { get; private set; }

        public WowWorldState(WowScreenConfiguration screenConfig)
        {
            Initialized = false;
            PlayerHpPercent = -1;
            ResourcePercent = -1;
            TargetHpPercent = -1;
            MapX = -1;
            MapY = -1;
            PlayerLocation = Vector2.Zero;
            FacingDegrees = -1;
            AttackerCount = -1;
            PlayerLevel = -1;
            ScreenConfig = screenConfig;
        }

        public static WowWorldState GetWoWWorldState(WowScreenConfiguration screenConfig)
        {
            WowWorldState currentState = new WowWorldState(screenConfig);

            System.IntPtr wowHandle = ScreenCapture.GetWindowHandleByName(WowPlayer.WOW_EXE_NAME);
            currentState.WowWindowHasFocus = wowHandle != System.IntPtr.Zero && ScreenCapture.GetForegroundWindow() == wowHandle;

            currentState.Bmp = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(screenConfig.CaptureRectangle);
            currentState.UpdateFromBitmap(currentState.Bmp);

            return currentState;
        }

        public void UpdateFromBitmap(Bitmap bmp)
        {
            // The sentinel box is the one exact-color check -- if it's not there, nothing
            // else in the pixel rows means anything.
            AddonLoaded = WowPixelRows.ReadAddonLoaded(bmp);
            if (!AddonLoaded)
            {
                return;
            }

            Initialized = true;

            // Bars still render when the client hands Lua secret values, which Lua itself
            // can't do math on -- so the percents come from here.
            PlayerHpPercent = WowPixelRows.ReadStatusBarPercent(bmp, PixelRow.PlayerHealthBar, c => c.G);
            ResourcePercent = WowPixelRows.ReadStatusBarPercent(bmp, PixelRow.ResourceBar, c => c.B);
            TargetHpPercent = WowPixelRows.ReadStatusBarPercent(bmp, PixelRow.TargetHealthBar, c => c.R);

            MapX = WowPixelRows.ReadBits(bmp, PixelRow.MapX).FixedPoint;
            MapY = WowPixelRows.ReadBits(bmp, PixelRow.MapY).FixedPoint;
            PlayerLocation = new Vector2(MapX, MapY);
            FacingDegrees = WowPixelRows.ReadBits(bmp, PixelRow.FacingDegrees).FixedPoint;

            // Set by whichever supported-class bit is on -- see PlayerClass.
            PlayerClass = null;
            UpdateMultiBoolOne(WowPixelRows.ReadBits(bmp, PixelRow.MultiBoolOne));
            UpdateMultiBoolTwo(WowPixelRows.ReadBits(bmp, PixelRow.MultiBoolTwo));
            UpdateMultiIntOne(WowPixelRows.ReadBits(bmp, PixelRow.MultiIntOne));
        }

        // Box numbers MUST match MULTI_BOOL_ONE_FIELDS in WoWFunctions.lua.
        private void UpdateMultiBoolOne(PixelRowBits bits)
        {
            IsAutoAttacking = bits[1];
            LowOnHealthPotions = bits[2];
            LowOnDynamite = bits[3];
            LowOnAmmo = bits[4];
            GCDCooledDown = bits[5];
            BagsAreFull = bits[6];
            IsInCombat = bits[7];
            IsPlayerPetrified = bits[8];

            HasUnseenWhisper = bits[9];
            // 10 reserved
            IsCurrentlyCasting = bits[11];
            EnemyNameplatesAreTurnedOn = bits[12];
            CurrentTargetInCombatWithUs = bits[13];
            IsTargetCasterMob = bits[14];
            IsTargetRunnerMob = bits[15];
            IsTargetFireImmune = bits[16];

            // True for a few seconds after the player's own attack drew an EVADE miss against
            // the current target (see HasRecentTargetEvade() in YoyokazooUI.lua) -- i.e. the
            // target is stuck evading, e.g. leashed on the far side of terrain it can't path
            // across.
            TargetRecentlyEvaded = bits[17];

            // 18 and 20 are supported-class bits (19 reserved) -- see PlayerClass.
            if (bits[18])
            {
                PlayerClass = WowCombatConfiguration.Warrior;
            }
            else if (bits[20])
            {
                PlayerClass = WowCombatConfiguration.Shaman;
            }

            PlayerIsPoisoned = bits[21];
            PlayerIsDiseased = bits[22];
            IsTargetNatureImmune = bits[23];
            IsTargetCasting = bits[24];
        }

        // Box numbers MUST match MULTI_BOOL_TWO_FIELDS in WoWFunctions.lua.
        private void UpdateMultiBoolTwo(PixelRowBits bits)
        {
            IsTargetLongRangeCaster = bits[1];
            LogoffMobSeen = bits[2];
            IsCurrentlySkinning = bits[3];

            if (bits[4])
            {
                PlayerClass = WowCombatConfiguration.Warlock;
            }

            IsTargetBleedImmune = bits[5];
            IsTargetFearCaster = bits[6];
            FacingWrongWay = bits[7];
            TargetNeedsToBeInFront = bits[8];

            LogoutOnLowDynamiteEnabled = bits[9];
            LogoutOnFullBagsEnabled = bits[10];
            HasDesiredWorldBuff = bits[11];
            HighLatency = bits[12];
            CombatStalemate = bits[13];
            AllSkillsKnownForThisLevel = bits[14];
            CanAffordToTrainAllSkills = bits[15];
            GearNeedsRepair = bits[16];

            IsTargetAttackable = bits[17];
            InvalidTarget = bits[18];
            OutOfRange = bits[19];
            NotInLineOfSight = bits[20];
            NoPathAvailable = bits[21];
            Underwater = bits[22];
        }

        // Byte order MUST match the EncodeBytes call for row 11 in InitializePixelRow()
        // (UIFunctions.lua).
        private void UpdateMultiIntOne(PixelRowBits bits)
        {
            AttackerCount = bits.Byte(0);
            PlayerLevel = bits.Byte(1);
            CurrentZone = (WowZone)bits.Byte(2); // see GetCurrentZoneId() in WoWFunctions.lua
        }
    }
}
