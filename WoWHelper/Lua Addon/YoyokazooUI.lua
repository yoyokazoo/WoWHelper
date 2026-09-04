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
-- with no cleanup of the old set, so without this guard each re-fire stacked a whole
-- new copy of the debug frame's text directly on top of the previous one. Neither
-- function needs to re-run after the first login -- both already poll live values
-- (and, for the pixel row, re-calibrate screen scale) every tick via their own
-- OnUpdate handlers, so nothing about them goes stale across a zone change.
local uiInitialized = false

-- YoyokazooUIDB is a SavedVariablesPerCharacter table (see YoyokazooUI.toc).
-- The .toc also sets "## LoadSavedVariablesFirst 1", which guarantees this
-- global is already populated from disk (if it exists) before this file
-- executes -- so it's safe to default-initialize it directly here at load
-- time instead of waiting for ADDON_LOADED.
YoyokazooUIDB = YoyokazooUIDB or {}
if YoyokazooUIDB.debugFrameEnabled == nil then
    YoyokazooUIDB.debugFrameEnabled = true -- default on, matches the old always-on behavior
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

        if not uiInitialized then
            InitializeIndicators()
            InitializePixelRow()
            ApplyDebugFrameVisibility()
            uiInitialized = true
        end
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