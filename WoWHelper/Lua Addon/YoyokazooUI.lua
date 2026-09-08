local xpTracker = {}
xpTracker.startXP = 0
xpTracker.currentXP = 0
xpTracker.totalGained = 0
xpTracker.startLevel = 0
xpTracker.startTime   = 0

local UNSEEN_WINDOW_SECONDS = 60
local lastWhisperTime = nil

-- Same "sticky flag via timestamp" pattern as HasUnseenWhisper() below --
-- an EVADE combat-log miss is a single instantaneous event, but this only
-- gets polled once per tick, so latch it true for a few seconds after
-- the fact instead of requiring the poll to land on the exact same frame the
-- event fired.
local EVADE_WINDOW_SECONDS = 3
local lastEvadeTime = nil

-- PLAYER_ENTERING_WORLD fires on every loading screen, not just the initial login --
-- zoning, taxis, death+release, and hearthing all re-fire it. InitializeIndicators()/
-- InitializePixelRow() build a fresh set of frames/textures every time they're called
-- with no cleanup of the old set, so without a guard each re-fire stacks a whole new
-- copy of the debug frame's text directly on top of the previous one. Neither function
-- needs to re-run after it's succeeded once -- both already poll live values (and, for
-- the pixel row, re-calibrate screen scale) every tick via their own OnUpdate handlers,
-- so nothing about them goes stale across a zone change.
--
-- Tracked as two SEPARATE flags (not one shared uiInitialized) so each one only counts
-- as done once it actually succeeds -- see the pcall wrapping below.
local indicatorsInitialized = false
local pixelRowInitialized = false

-- YoyokazooUIDB is a SavedVariablesPerCharacter table (see YoyokazooUI.toc).
-- The .toc also sets "## LoadSavedVariablesFirst 1", which guarantees this
-- global is already populated from disk (if it exists) before this file
-- executes -- so it's safe to default-initialize it directly here at load
-- time instead of waiting for ADDON_LOADED.
YoyokazooUIDB = YoyokazooUIDB or {}
if YoyokazooUIDB.debugFrameEnabled == nil then
    YoyokazooUIDB.debugFrameEnabled = true -- default on, matches the old always-on behavior
end

-- Run-specific settings, toggled live via the /yyconfig menu below instead of being
-- hardcoded in the C# side's WowManagementConfiguration. Defaults here match what
-- WowManagementConfigs.FULL_BABYSIT (the only profile CURRENT_CONFIG actually uses) used
-- to hardcode, before these moved here.
--
-- Stored in YoyokazooUIDB, i.e. SavedVariablesPerCharacter (see YoyokazooUI.toc) -- a
-- plain local file under this WoW install's WTF folder, written to disk on logout/reload,
-- never transmitted anywhere. Per-character on purpose: each character can run a
-- different farming setup, so these intentionally don't carry over to other characters on
-- the same account/computer.
if YoyokazooUIDB.logoutOnLowDynamite == nil then
    YoyokazooUIDB.logoutOnLowDynamite = true
end
if YoyokazooUIDB.logoutOnFullBags == nil then
    YoyokazooUIDB.logoutOnFullBags = false
end

-- Which dynamite-tier item AreWeLowOnDynamite() (WoWFunctions.lua) checks the bag
-- count of -- selectable via the /yyconfig "Dynamite item" selector below instead
-- of being hardcoded. Defaults to Dense Dynamite (18641), what it used to be
-- hardcoded to. See DYNAMITE_ITEM_CHOICES (WoWFunctions.lua) for the full list.
if YoyokazooUIDB.dynamiteItemId == nil then
    YoyokazooUIDB.dynamiteItemId = 18641
end

-- Read by GetMultiBoolTwo() (WoWFunctions.lua) to pack these into MultiBoolTwo's R4/R5,
-- decoded on the C# side into WowWorldState.LogoutOnLowDynamiteEnabled/
-- LogoutOnFullBagsEnabled.
function IsLogoutOnLowDynamiteEnabled()
    return YoyokazooUIDB.logoutOnLowDynamite
end

function IsLogoutOnFullBagsEnabled()
    return YoyokazooUIDB.logoutOnFullBags
end

-- Read by AreWeLowOnDynamite() (WoWFunctions.lua). Not piped to the C# side at
-- all -- unlike the two booleans above, this only ever needs to be known on the
-- Lua side, where the actual bag-count check happens.
--
-- Defensively re-defaults and repairs the saved variable if it's ever
-- missing/invalid, rather than just nil-checking at init time -- seen once in
-- testing returning nil despite the default-init above having already run
-- earlier in this same file's load, cause not yet confirmed. GetItemCount
-- (the only caller, via AreWeLowOnDynamite) throws a hard Lua error on a
-- non-number/string itemInfo, so this guards that directly instead of
-- crashing InitializeIndicators()/InitializePixelRow() on the very first
-- PLAYER_ENTERING_WORLD. The print only fires on the invalid path, so if this
-- turns out to be a recurring issue rather than a one-off, it'll show up
-- again instead of going silent.
function GetDynamiteItemId()
    local itemId = YoyokazooUIDB.dynamiteItemId
    if type(itemId) ~= "number" then
        print("YoyokazooUI: dynamiteItemId was invalid (" .. tostring(itemId) .. "), resetting to default (Dense Dynamite, 18641).")
        itemId = 18641
        YoyokazooUIDB.dynamiteItemId = itemId
    end
    return itemId
end

-- Create a frame to be our black box
local frame = CreateFrame("Frame", "YoyokazooUIFrame", UIParent, "BackdropTemplate")
-- Size and position
frame:SetSize(300, 350)
frame:SetPoint("TOPLEFT", UIParent, "TOPLEFT", 25, -100)
-- Give it a solid black background
frame:SetBackdrop({
    bgFile = "Interface\\ChatFrame\\ChatFrameBackground",
    edgeFile = nil,
    tile = false,
    tileSize = 0,
    edgeSize = 0,
    insets = { left = 0, right = 0, top = 0, bottom = 0 }
})
frame:SetBackdropColor(0, 0, 0, 1)  -- RGBA, 0.8 alpha for slight transparency

-- Shows/hides the whole debug frame per the saved /yydebug toggle. Hiding it
-- is not just cosmetic -- OnUpdate scripts don't fire on a hidden frame, so
-- this also stops InitializeIndicators()'s own OnUpdate loop (UIFunctions.lua)
-- from polling and allocating while debug is off. Declared before OnEvent/
-- the slash command below so both can call it as an upvalue.
local function ApplyDebugFrameVisibility()
    if YoyokazooUIDB.debugFrameEnabled then
        frame:Show()
    else
        frame:Hide()
    end
end

frame:RegisterEvent("CHAT_MSG_WHISPER")
frame:RegisterEvent("PLAYER_ENTERING_WORLD")
frame:RegisterEvent("PLAYER_XP_UPDATE")
frame:RegisterEvent("PLAYER_LEVEL_UP")
frame:RegisterEvent("COMBAT_LOG_EVENT_UNFILTERED")
frame:RegisterEvent("LOOT_BIND_CONFIRM")

frame:SetScript("OnEvent", function(self, event, ...)
    if event == "CHAT_MSG_WHISPER" then
        lastWhisperTime = GetTime()
    end

    if event == "LOOT_BIND_CONFIRM" then
        -- Looting an item that's about to bind to the player (or, if
        -- already bound, still tradeable to the group) doesn't loot it
        -- immediately -- Blizzard's default UI intercepts this event and
        -- throws up a Yes/No StaticPopup ("LOOT_BIND") asking the player to
        -- confirm, which blocks the loot until something clicks it. There's
        -- no CVar to suppress that dialog, and we always want "Yes" here, so
        -- auto-confirm every time instead.
        --
        -- ConfirmLootSlot(lootSlot) is exactly what StaticPopupDialogs
        -- ["LOOT_BIND"].OnAccept does -- we just call it directly rather
        -- than finding and clicking the dialog. (Reference: KyrosKrane
        -- Sylvanblade's "Annoying Pop-up Remover" addon, module_loot.lua,
        -- which hides the StaticPopup and invokes its OnAccept; same net
        -- effect, fewer moving parts since we don't need its
        -- show/hide-state bookkeeping -- this always says yes.)
        local lootSlot = ...
        if lootSlot then
            -- Our frame registers for this event after Blizzard's default UI
            -- does (addons load after the built-in UI), so by the time we see
            -- it the popup has typically already been shown. Hide it too, or
            -- it lingers on screen as a stray dialog even though the loot
            -- itself already completed.
            StaticPopup_Hide("LOOT_BIND")

            -- Must be deferred to the next frame -- calling ConfirmLootSlot
            -- synchronously, still inside this same LOOT_BIND_CONFIRM
            -- dispatch, silently did not confirm the loot (confirmed by
            -- testing in-game). Matches what the reference addon
            -- (AnnoyingPopupRemover's module_loot.lua) actually does via
            -- RunNextFrame rather than calling it inline.
            RunNextFrame(function() ConfirmLootSlot(lootSlot) end)
        end
    end

    if event == "COMBAT_LOG_EVENT_UNFILTERED" then
        local _, subevent, _, sourceGUID, _, _, _, _, _, _, _, missType = CombatLogGetCurrentEventInfo()

        -- SWING_MISSED's missType is the 12th return value (grabbed directly above).
        -- SPELL_MISSED/RANGE_MISSED/SPELL_PERIODIC_MISSED have spellId/spellName/
        -- spellSchool ahead of it in the documented combat-log arg layout, so their
        -- missType lands 3 slots later (index 15) -- re-fetch for those instead of
        -- trusting the SWING_MISSED-shaped unpack above.
        if subevent == "SPELL_MISSED" or subevent == "RANGE_MISSED" or subevent == "SPELL_PERIODIC_MISSED" then
            missType = select(15, CombatLogGetCurrentEventInfo())
        end

        if missType == "EVADE" and sourceGUID == UnitGUID("player") then
            lastEvadeTime = GetTime()
        end
    end

    if event == "PLAYER_ENTERING_WORLD" then
        xpTracker.startLevel = UnitLevel("player")
        xpTracker.startXP    = UnitXP("player")
        xpTracker.currentXP  = xpTracker.startXP
        xpTracker.totalGained = 0
        xpTracker.startTime   = GetTime()

        -- Alert if enemy nameplates are on, since they're needed to count attackers
        local nameplateShowEnemies = AreEnemyNameplatesTurnedOn()
        if not nameplateShowEnemies then
            UIErrorsFrame:AddMessage("Enemy nameplates are off! Turn them on!", 1, 0, 0, nil, 15) 
        end
        

        print("XP session started. Level:", xpTracker.startLevel, "XP:", xpTracker.startXP)

        -- InitializeIndicators() (human-only debug overlay) and InitializePixelRow()
        -- (the ONLY thing the C# bot actually reads) used to run back-to-back with no
        -- error isolation between them -- an uncaught Lua error building the debug
        -- frame would unwind straight out of this whole block, meaning
        -- InitializePixelRow() never even got called, silently freezing every decoded
        -- bot flag (combat state, HP%, casting, zone, all of it) for the rest of the
        -- session. Confirmed happening in testing (a bug in one of GetMultiBoolOne's
        -- inputs, read by InitializeIndicators()'s debug swatch, blocked
        -- InitializePixelRow() from ever running). pcall-isolating each one, with its
        -- own success flag, means a bug in the debug-only frame can never again take
        -- down the real one, and either one that fails keeps retrying on the next
        -- PLAYER_ENTERING_WORLD (zone change, death+release, hearth, /reload) instead
        -- of being stuck for the rest of the session.
        if not indicatorsInitialized then
            local ok, err = pcall(InitializeIndicators)
            if ok then
                indicatorsInitialized = true
            else
                print("YoyokazooUI: InitializeIndicators() failed, debug frame not built (will retry next PLAYER_ENTERING_WORLD): " .. tostring(err))
            end
        end

        if not pixelRowInitialized then
            local ok, err = pcall(InitializePixelRow)
            if ok then
                pixelRowInitialized = true
            else
                print("YoyokazooUI: InitializePixelRow() failed -- the bot's pixel row was NOT built (will retry next PLAYER_ENTERING_WORLD): " .. tostring(err))
            end
        end

        ApplyDebugFrameVisibility()
    end

    if event == "PLAYER_XP_UPDATE" then
        local newXP = UnitXP("player")
        local diff = newXP - xpTracker.currentXP

        -- Normal XP gain
        if diff > 0 then
            xpTracker.totalGained = xpTracker.totalGained + diff
            print("Gained:", diff, "XP | Session total:", xpTracker.totalGained)
            PrintXpPerHour("XP gain.", xpTracker.startTime, xpTracker.totalGained)
        end

        xpTracker.currentXP = newXP
    end

    if event == "PLAYER_LEVEL_UP" then
        local level = ...
        
        -- When you level, XP resets to 0. Compute how much XP the last level needed.
        --local maxBefore = UnitXPMax("player")
        --xpTracker.totalGained = xpTracker.totalGained + (maxBefore - xpTracker.currentXP)
        --xpTracker.currentXP = 0

        print("Level up! Now level", level)
        print("Total session XP so far:", xpTracker.totalGained)

        -- Reset xp/hour, since it'll change level to level
        xpTracker.startXP    = UnitXP("player")
        xpTracker.currentXP  = xpTracker.startXP
        xpTracker.totalGained = 0
        xpTracker.startTime   = GetTime()
    end
end)

function HasUnseenWhisper()
    if not lastWhisperTime then
        return false
    end

    return (GetTime() - lastWhisperTime) <= UNSEEN_WINDOW_SECONDS
end

-- True for EVADE_WINDOW_SECONDS after the player's own attack last drew an
-- EVADE miss against the current target -- i.e. the target is (or very
-- recently was) stuck evading, e.g. leashed on the other side of terrain it
-- can't path across. See COMBAT_LOG_EVENT_UNFILTERED handling above.
function HasRecentTargetEvade()
    if not lastEvadeTime then
        return false
    end

    return (GetTime() - lastEvadeTime) <= EVADE_WINDOW_SECONDS
end

-- /yydebug toggles the debug frame (InitializeIndicators()'s YoyokazooUIFrame,
-- not the pixel row -- that one always runs). The choice is
-- saved into YoyokazooUIDB.debugFrameEnabled, so it persists across logout/
-- reload instead of resetting to on every session.
SLASH_YYDEBUG1 = "/yydebug"
SlashCmdList["YYDEBUG"] = function()
    YoyokazooUIDB.debugFrameEnabled = not YoyokazooUIDB.debugFrameEnabled
    ApplyDebugFrameVisibility()
    print("YoyokazooUI: debug frame " .. (YoyokazooUIDB.debugFrameEnabled and "ON" or "OFF") .. " (saved).")
end

-- /yyconfig toggles the run-specific settings menu (CreateSettingsMenu(), UIFunctions.lua)
-- -- "log out on low dynamite"/"log out on full bags" for now, more can be added to the
-- options list below as they come up. Built once, lazily, on first use rather than
-- unconditionally at load time like the debug frame above, since there's no reason to pay
-- for it on a run that never opens the menu.
local settingsMenu = nil

SLASH_YYCONFIG1 = "/yyconfig"
SlashCmdList["YYCONFIG"] = function()
    if not settingsMenu then
        settingsMenu = CreateSettingsMenu({
            {
                label = "Log out on low dynamite",
                get = IsLogoutOnLowDynamiteEnabled,
                set = function(value)
                    YoyokazooUIDB.logoutOnLowDynamite = value
                    print("YoyokazooUI: Log out on low dynamite " .. (value and "ON" or "OFF") .. " (saved).")
                end,
            },
            {
                label = "Log out on full bags",
                get = IsLogoutOnFullBagsEnabled,
                set = function(value)
                    YoyokazooUIDB.logoutOnFullBags = value
                    print("YoyokazooUI: Log out on full bags " .. (value and "ON" or "OFF") .. " (saved).")
                end,
            },
            {
                label = "Dynamite item",
                type = "selector",
                choices = DYNAMITE_ITEM_CHOICES,
                get = GetDynamiteItemId,
                set = function(id)
                    YoyokazooUIDB.dynamiteItemId = id

                    local chosenLabel = tostring(id)
                    for _, choice in ipairs(DYNAMITE_ITEM_CHOICES) do
                        if choice.id == id then
                            chosenLabel = choice.label
                            break
                        end
                    end
                    print("YoyokazooUI: Dynamite item set to " .. chosenLabel .. " (saved).")
                end,
            },
        })
    end

    if settingsMenu:IsShown() then
        settingsMenu:Hide()
    else
        settingsMenu:Show()
    end
end