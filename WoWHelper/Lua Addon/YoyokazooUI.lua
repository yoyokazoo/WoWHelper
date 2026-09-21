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

-- Combat-stalemate detection: we're in combat, but nobody is actually hitting
-- anybody. The motivating case was getting aggroed by a mob that couldn't
-- path to the player (player in water, mob stuck on the shore) -- combat never
-- drops, the mob never swings, the bot never lands a hit, and the whole thing
-- sat there for 5+ minutes. Tracked as "time of the last combat-log damage/
-- miss event involving the player (or their pet)", re-stamped on entering
-- combat so the clock starts fresh for every fight rather than carrying a
-- stale timestamp over from the previous one. IsCombatStalemate() below is
-- true once that's older than COMBAT_STALEMATE_SECONDS while still in combat;
-- the C# side logs out on it (WowManagementTasks.EveryWorldStateUpdateTasks).
-- EVADE misses deliberately do NOT count as activity -- they're exactly what
-- a stuck mob produces when we swing at it, so counting them would mask the
-- very case this exists for.
local COMBAT_STALEMATE_SECONDS = 30
local lastCombatActivityTime = nil
-- Set true to print every combat-log event that counts as activity, for
-- checking in-game which subevents actually fire during a stalemate.
local COMBAT_STALEMATE_DEBUG = false
local combatStalemateAnnounced = false

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
-- WowManagementConfigs.FULL_BABYSIT (the only profile the old CURRENT_CONFIG ever used)
-- used to hardcode, before these moved here. Both WowManagementConfiguration and
-- CURRENT_CONFIG have since been removed from the C# side entirely.
--
-- Stored in YoyokazooUIDB, i.e. SavedVariablesPerCharacter (see YoyokazooUI.toc) -- a
-- plain local file under this WoW install's WTF folder, written to disk on logout/reload,
-- never transmitted anywhere. Per-character on purpose: each character can run a
-- different farming setup, so these intentionally don't carry over to other characters on
-- the same account/computer.
if YoyokazooUIDB.logoutOnLowDynamite == nil then
    YoyokazooUIDB.logoutOnLowDynamite = false
end
if YoyokazooUIDB.logoutOnFullBags == nil then
    YoyokazooUIDB.logoutOnFullBags = false
end

-- Auto-sell junk to an open merchant (see the MERCHANT_SHOW handling below) --
-- defaults ON, unlike the two above, since selling gray/whitelisted junk has
-- no downside the way an unwanted auto-logout would. Lua-only, like the
-- dynamite item/healing potion selectors -- never piped to the C# side via
-- GetMultiBoolTwo, since the bot doesn't need to know it happened.
if YoyokazooUIDB.autoSellJunk == nil then
    YoyokazooUIDB.autoSellJunk = true
end

-- Which dynamite-tier item AreWeLowOnDynamite() (WoWFunctions.lua) checks the bag
-- count of -- selectable via the /yyconfig "Dynamite item" selector below instead
-- of being hardcoded. Defaults to Dense Dynamite (18641), what it used to be
-- hardcoded to. See DYNAMITE_ITEM_CHOICES (WoWFunctions.lua) for the full list.
if YoyokazooUIDB.dynamiteItemId == nil then
    YoyokazooUIDB.dynamiteItemId = 18641
end

-- Which healing-potion-tier item AreWeLowOnHealthPotions() (WoWFunctions.lua) checks
-- the bag count of -- selectable via the /yyconfig "Healing potion" selector below
-- instead of being auto-picked from player level. Defaults to Major Healing Potion
-- (13446). See HEALING_POTION_ITEM_CHOICES (WoWFunctions.lua) for the full list.
if YoyokazooUIDB.healingPotionItemId == nil then
    YoyokazooUIDB.healingPotionItemId = 13446
end

-- Which world buff HasDesiredWorldBuff() (WoWFunctions.lua) checks for -- selectable via the
-- /yyconfig "Desired world buff" selector below instead of being hardcoded. Defaults to Ony's
-- Rallying Cry. See WORLD_BUFF_CHOICES (WoWFunctions.lua) for the full list. Read by
-- GetMultiBoolTwo() into MultiBoolTwo's G3, decoded on the C# side into
-- WowWorldState.HasDesiredWorldBuff, consumed by WaitForWorldBuffThenLogoffTask
-- (WowManagementTasks.cs).
if YoyokazooUIDB.desiredWorldBuffId == nil then
    YoyokazooUIDB.desiredWorldBuffId = "ony"
end

-- Read by GetMultiBoolTwo() (WoWFunctions.lua) to pack these into MultiBoolTwo's G1/G2,
-- decoded on the C# side into WowWorldState.LogoutOnLowDynamiteEnabled/
-- LogoutOnFullBagsEnabled.
function IsLogoutOnLowDynamiteEnabled()
    return YoyokazooUIDB.logoutOnLowDynamite
end

function IsLogoutOnFullBagsEnabled()
    return YoyokazooUIDB.logoutOnFullBags
end

function IsAutoSellJunkEnabled()
    return YoyokazooUIDB.autoSellJunk
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
function GetDesiredWorldBuffId()
    return YoyokazooUIDB.desiredWorldBuffId
end

function GetDynamiteItemId()
    local itemId = YoyokazooUIDB.dynamiteItemId
    if type(itemId) ~= "number" then
        print("YoyokazooUI: dynamiteItemId was invalid (" .. tostring(itemId) .. "), resetting to default (Dense Dynamite, 18641).")
        itemId = 18641
        YoyokazooUIDB.dynamiteItemId = itemId
    end
    return itemId
end

-- Read by AreWeLowOnHealthPotions() (WoWFunctions.lua). Not piped to the C# side at
-- all -- same as GetDynamiteItemId() above, only the Lua side needs it, where the
-- actual bag-count check happens. Same defensive re-default/repair as
-- GetDynamiteItemId() too, for the same reason.
function GetHealingPotionItemId()
    local itemId = YoyokazooUIDB.healingPotionItemId
    if type(itemId) ~= "number" then
        print("YoyokazooUI: healingPotionItemId was invalid (" .. tostring(itemId) .. "), resetting to default (Major Healing Potion, 13446).")
        itemId = 13446
        YoyokazooUIDB.healingPotionItemId = itemId
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
frame:RegisterEvent("PLAYER_REGEN_DISABLED")
frame:RegisterEvent("MERCHANT_SHOW")
frame:RegisterEvent("MERCHANT_CLOSED")

-- Auto-sell-junk (see the MERCHANT_SHOW/MERCHANT_CLOSED handling below):
-- sells one queued slot every AUTO_SELL_TICK_SECONDS rather than looping
-- through the whole queue in one go -- firing a burst of sells in a single
-- frame is known to silently drop some of them. autoSellGeneration is bumped
-- on every MERCHANT_SHOW and MERCHANT_CLOSED; each scheduled tick captures
-- the generation it was queued under and bails if that's gone stale (the
-- merchant closed, or a newer MERCHANT_SHOW superseded it) instead of
-- calling UseContainerItem on a closed merchant, which would use/equip the
-- item instead of selling it.
local AUTO_SELL_TICK_SECONDS = 0.2
local autoSellGeneration = 0

-- MERCHANT_SHOW fires before Blizzard's own handler has necessarily called
-- MerchantFrame:Show() -- confirmed live: the very first tick (index 1),
-- called synchronously out of the MERCHANT_SHOW handler below, consistently
-- saw MerchantFrame exists=true, shown=false, which killed the whole chain
-- before it ever got to sell anything. Rather than assume any fixed number of
-- frames is enough for Blizzard's handler to catch up (RunNextFrame's single-
-- frame defer, used for LOOT_BIND_CONFIRM above, isn't a good match here --
-- that was papering over a different kind of race), retry on a short timer,
-- capped so a merchant window that genuinely never shows (window closed
-- before it opened, some other addon interfering, etc.) doesn't retry
-- forever.
local MERCHANT_NOT_SHOWN_RETRY_SECONDS = 0.1
local MERCHANT_NOT_SHOWN_MAX_RETRIES = 25 -- 25 * 0.1s = 2.5s max wait

local function SellNextAutoSellQueueItem(queue, index, generation, notShownRetries)
    notShownRetries = notShownRetries or 0

    if generation ~= autoSellGeneration then
        AutoSellDebugPrint("tick " .. index .. " bailed: stale generation (queued under " ..
            generation .. ", now " .. autoSellGeneration .. ")")
        return
    end

    if not (MerchantFrame and MerchantFrame:IsShown()) then
        if notShownRetries >= MERCHANT_NOT_SHOWN_MAX_RETRIES then
            AutoSellDebugPrint("tick " .. index .. " giving up: MerchantFrame never shown after " ..
                notShownRetries .. " retries")
            return
        end

        AutoSellDebugPrint("tick " .. index .. " not shown yet (exists=" ..
            tostring(MerchantFrame ~= nil) .. ", shown=" ..
            tostring(MerchantFrame and MerchantFrame:IsShown()) .. ") -- retry " ..
            (notShownRetries + 1) .. "/" .. MERCHANT_NOT_SHOWN_MAX_RETRIES)
        C_Timer.After(MERCHANT_NOT_SHOWN_RETRY_SECONDS, function()
            SellNextAutoSellQueueItem(queue, index, generation, notShownRetries + 1)
        end)
        return
    end

    if index > #queue then
        -- Everything queued at MERCHANT_SHOW time is sold -- close up.
        AutoSellDebugPrint("queue exhausted (" .. #queue .. " sold) -- closing merchant")
        CloseMerchant()
        return
    end

    local entry = queue[index]
    AutoSellDebugPrint("selling " .. index .. "/" .. #queue .. ": bag " .. entry.bag ..
        " slot " .. entry.slot)
    C_Container.UseContainerItem(entry.bag, entry.slot)

    C_Timer.After(AUTO_SELL_TICK_SECONDS, function()
        SellNextAutoSellQueueItem(queue, index + 1, generation)
    end)
end

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

    if event == "MERCHANT_SHOW" then
        -- New generation whether or not auto-sell is even enabled, so a
        -- stray tick from an earlier merchant visit can never bleed into
        -- this one.
        autoSellGeneration = autoSellGeneration + 1

        AutoSellDebugPrint("MERCHANT_SHOW (generation " .. autoSellGeneration ..
            "), autoSellJunk=" .. tostring(IsAutoSellJunkEnabled()) ..
            ", MerchantFrame shown=" .. tostring(MerchantFrame and MerchantFrame:IsShown()))

        if IsAutoSellJunkEnabled() then
            local queue = FindAutoSellQueue()
            -- Only start the chain (and thus only auto-close afterwards) if
            -- there's actually something to sell -- an empty queue leaves
            -- the merchant window open exactly as the player left it.
            if #queue > 0 then
                SellNextAutoSellQueueItem(queue, 1, autoSellGeneration)
            else
                AutoSellDebugPrint("nothing queued -- leaving the merchant window alone")
            end
        else
            AutoSellDebugPrint("auto-sell is OFF in /yyconfig -- doing nothing")
        end
    end

    if event == "MERCHANT_CLOSED" then
        -- Invalidate any in-flight sell chain -- see the comment above
        -- SellNextAutoSellQueueItem.
        autoSellGeneration = autoSellGeneration + 1
        AutoSellDebugPrint("MERCHANT_CLOSED (generation now " .. autoSellGeneration ..
            ") -- any in-flight sell chain is now stale")
    end

    -- Entering combat counts as combat activity: starts the stalemate clock
    -- fresh for this fight (see COMBAT_STALEMATE_SECONDS above).
    if event == "PLAYER_REGEN_DISABLED" then
        lastCombatActivityTime = GetTime()
        combatStalemateAnnounced = false
    end

    if event == "COMBAT_LOG_EVENT_UNFILTERED" then
        local _, subevent, _, sourceGUID, _, _, _, destGUID, _, _, _, missType = CombatLogGetCurrentEventInfo()

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

        -- Any damage or (non-EVADE) miss to or from the player/pet means the
        -- fight is actually happening -- see COMBAT_STALEMATE_SECONDS above.
        -- Suffix match rather than an explicit list so SWING_/RANGE_/SPELL_/
        -- SPELL_PERIODIC_/DAMAGE_SHIELD etc. are all covered.
        local isDamageOrMiss = subevent:find("_DAMAGE$") or subevent:find("_MISSED$")
        if isDamageOrMiss and missType ~= "EVADE" then
            local playerGUID = UnitGUID("player")
            local petGUID = UnitGUID("pet")
            local involvesUs = sourceGUID == playerGUID or destGUID == playerGUID
                or (petGUID and (sourceGUID == petGUID or destGUID == petGUID))
            if involvesUs then
                lastCombatActivityTime = GetTime()
                if COMBAT_STALEMATE_DEBUG then
                    print("YoyokazooUI combat activity: " .. tostring(subevent) .. " " .. tostring(missType))
                end
            end
        end
    end

    if event == "PLAYER_ENTERING_WORLD" then
        -- If we're already mid-combat when the addon loads (e.g. /reload while
        -- stuck), PLAYER_REGEN_DISABLED already fired before we were listening --
        -- start the stalemate clock from here instead so it can still trip.
        if UnitAffectingCombat("player") and not lastCombatActivityTime then
            lastCombatActivityTime = GetTime()
        end

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

-- True once we've been in combat for COMBAT_STALEMATE_SECONDS with no damage/
-- miss combat-log event involving us in that time -- i.e. aggroed by something
-- that can't reach us (or that we can't reach). Goes false again the moment
-- combat drops or a real hit lands. See the COMBAT_STALEMATE_SECONDS comment
-- at the top of this file, and PLAYER_REGEN_DISABLED/COMBAT_LOG_EVENT_UNFILTERED
-- handling above.
function IsCombatStalemate()
    if not UnitAffectingCombat("player") or not lastCombatActivityTime then
        return false
    end

    local secondsSinceActivity = GetTime() - lastCombatActivityTime
    local isStalemate = secondsSinceActivity > COMBAT_STALEMATE_SECONDS

    -- One-shot chat line the first time it flips, so it's visible in-game
    -- (and in the chat log) why the bot logged out.
    if isStalemate and not combatStalemateAnnounced then
        combatStalemateAnnounced = true
        print(string.format("YoyokazooUI: combat stalemate -- in combat but no damage/miss events for %.0fs", secondsSinceActivity))
    end

    return isStalemate
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

-- /yysell is a debug command for the auto-sell path (see AUTO_SELL_DEBUG in
-- WoWFunctions.lua): it runs the same FindAutoSellQueue() bag scan the
-- MERCHANT_SHOW handler runs, printing what it saw in every occupied slot and
-- what it would sell -- but never sells anything. Usable anywhere, with no
-- merchant open, so "does the scan find my junk?" can be answered separately
-- from "does the selling work?". It also reports the pieces around the scan
-- that can independently be wrong: the /yyconfig toggle, whether this addon's
-- frame is actually registered for MERCHANT_SHOW, and whether the MerchantFrame
-- is up right now.
SLASH_YYSELL1 = "/yysell"
SlashCmdList["YYSELL"] = function()
    print("YoyokazooUI: auto-sell dry run --")
    print("  autoSellJunk (/yyconfig) = " .. tostring(IsAutoSellJunkEnabled()))
    print("  registered for MERCHANT_SHOW = " .. tostring(frame:IsEventRegistered("MERCHANT_SHOW")))
    print("  MerchantFrame shown = " .. tostring(MerchantFrame and MerchantFrame:IsShown()))

    -- Force the per-slot dumps on for this one scan even if AUTO_SELL_DEBUG is
    -- off -- printing them is the entire point of asking for it by hand.
    local wasDebug = AUTO_SELL_DEBUG
    AUTO_SELL_DEBUG = true
    local queue = FindAutoSellQueue()
    AUTO_SELL_DEBUG = wasDebug

    print("  would sell " .. #queue .. " slot(s) (nothing was sold)")
end

-- /yyconfig toggles the run-specific settings menu (CreateSettingsMenu(), UIFunctions.lua)
-- -- "log out on low dynamite"/"log out on full bags"/"auto-sell junk"/"dynamite item"/
-- "healing potion"/"desired world buff" for now, more can be added to the options list below as they come
-- up. Built once, lazily, on first use rather than
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
                label = "Auto-sell junk",
                get = IsAutoSellJunkEnabled,
                set = function(value)
                    YoyokazooUIDB.autoSellJunk = value
                    print("YoyokazooUI: Auto-sell junk " .. (value and "ON" or "OFF") .. " (saved).")
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
            {
                label = "Healing potion",
                type = "selector",
                choices = HEALING_POTION_ITEM_CHOICES,
                get = GetHealingPotionItemId,
                set = function(id)
                    YoyokazooUIDB.healingPotionItemId = id

                    local chosenLabel = tostring(id)
                    for _, choice in ipairs(HEALING_POTION_ITEM_CHOICES) do
                        if choice.id == id then
                            chosenLabel = choice.label
                            break
                        end
                    end
                    print("YoyokazooUI: Healing potion set to " .. chosenLabel .. " (saved).")
                end,
            },
            {
                label = "Desired world buff",
                type = "selector",
                choices = WORLD_BUFF_CHOICES,
                get = GetDesiredWorldBuffId,
                set = function(id)
                    YoyokazooUIDB.desiredWorldBuffId = id

                    local chosenLabel = tostring(id)
                    for _, choice in ipairs(WORLD_BUFF_CHOICES) do
                        if choice.id == id then
                            chosenLabel = choice.label
                            break
                        end
                    end
                    print("YoyokazooUI: Desired world buff set to " .. chosenLabel .. " (saved).")
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