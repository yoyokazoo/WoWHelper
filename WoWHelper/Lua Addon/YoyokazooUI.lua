local xpTracker = {}
xpTracker.startXP = 0
xpTracker.currentXP = 0
xpTracker.totalGained = 0
xpTracker.startLevel = 0
xpTracker.startTime   = 0

local UNSEEN_WINDOW_SECONDS = 60
local lastWhisperTime = nil

-- Same "sticky flag via timestamp" pattern as HasUnseenWhisper() below --
-- an EVADE combat-log miss is a single instantaneous event, but the bot only
-- polls world state once per tick, so latch it true for a few seconds after
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

frame:RegisterEvent("CHAT_MSG_WHISPER")
frame:RegisterEvent("PLAYER_ENTERING_WORLD")
frame:RegisterEvent("PLAYER_XP_UPDATE")
frame:RegisterEvent("PLAYER_LEVEL_UP")
frame:RegisterEvent("COMBAT_LOG_EVENT_UNFILTERED")

frame:SetScript("OnEvent", function(self, event, ...)
    if event == "CHAT_MSG_WHISPER" then
        lastWhisperTime = GetTime()
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