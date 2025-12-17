local xpTracker = {}
xpTracker.startXP = 0
xpTracker.currentXP = 0
xpTracker.totalGained = 0
xpTracker.startLevel = 0
xpTracker.startTime   = 0

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

frame:RegisterEvent("PLAYER_ENTERING_WORLD")
frame:RegisterEvent("PLAYER_XP_UPDATE")
frame:RegisterEvent("PLAYER_LEVEL_UP")

frame:SetScript("OnEvent", function(self, event, ...)
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
        InitializeIndicators()
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
        local maxBefore = UnitXPMax("player")

        xpTracker.totalGained = xpTracker.totalGained + (maxBefore - xpTracker.currentXP)
        xpTracker.currentXP = 0

        print("Level up! Now level", level)
        print("Total session XP so far:", xpTracker.totalGained)
    end
end)