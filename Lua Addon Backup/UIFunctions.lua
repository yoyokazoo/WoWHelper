-- Creates a numeric indicator with a label.
-- parent      = parent frame
-- label       = "Range", "HP%", etc.
-- valueFunc   = function that returns a number (or string)
-- orderY      = vertical stacking index
--
-- Returns: { frame = <frame>, update = <function> }

function CreateNumberIndicator(parent, label, valueFunc, orderY)
    local box = CreateFrame("Frame", nil, parent)
    box:SetSize(20, 20)
    box:SetPoint("TOPLEFT", parent, "TOPLEFT", 10, -10 - (orderY * 24))

    -- Background texture (colored square)
    local tex = box:CreateTexture(nil, "ARTWORK")
    tex:SetAllPoints(box)
    tex:SetColorTexture(0, 1, 0, 1)  -- default green

    local row = CreateFrame("Frame", nil, parent)
    row:SetSize(120, 20)
    row:SetPoint("TOPLEFT", parent, "TOPLEFT", 40, -10 - (orderY * 24))

    -- Numeric text (left)
    local valueText = row:CreateFontString(nil, "OVERLAY", "GameFontNormalLarge")
    valueText:SetPoint("LEFT", row, "LEFT", 0, 0)
    valueText:SetText("0")

    -- Label text (right)
    local labelText = row:CreateFontString(nil, "OVERLAY", "GameFontNormalSmall")
    labelText:SetPoint("LEFT", row, "LEFT", 60, 0)
    labelText:SetText(label)

    -- Update method
    local function UpdateRow()
        local v = valueFunc()
        if v == nil then
            valueText:SetText("?")
        else
            valueText:SetText(tostring(v))
            local r, g, b = EncodeFloatToColor(v)
            tex:SetColorTexture(r, g, b, 1)
        end
    end

    UpdateRow()

    return {
        frame = row,
        update = UpdateRow,
    }
end

-- Creates a labeled indicator box.
-- parent      = parent frame
-- label       = string label
-- colorFunc   = a function returning true/false
-- orderY      = vertical offset (for stacking multiple boxes)
--
-- Returns a table { frame = <frame>, update = <update func> }

function CreateIndicator(parent, label, colorFunc, orderY)
    local box = CreateFrame("Frame", nil, parent)
    box:SetSize(20, 20)
    box:SetPoint("TOPLEFT", parent, "TOPLEFT", 170, -10 - (orderY * 24))

    -- Background texture (colored square)
    local tex = box:CreateTexture(nil, "ARTWORK")
    tex:SetAllPoints(box)
    tex:SetColorTexture(1, 0, 0, 1)  -- default red

    -- Label
    local text = box:CreateFontString(nil, "OVERLAY", "GameFontNormalSmall")
    text:SetPoint("LEFT", box, "RIGHT", 8, 0)
    text:SetText(label)

    -- Update method toggles the color
    local function UpdateBox()
        local r, g, b = colorFunc()
        tex:SetColorTexture(r, g, b, 1)
    end

    -- Initial state
    UpdateBox()

    return {
        frame = box,
        update = UpdateBox,
    }
end

function InitializeIndicators()
    -- Create the "Hello World" text on bottom, can be used for debug
    local text = YoyokazooUIFrame:CreateFontString("HELLO", "OVERLAY", "GameFontNormalSmall")
    text:SetPoint("BOTTOMLEFT", frame, "BOTTOMLEFT", 0, 0)
    text:SetText("Hello World")
    text:SetTextColor(1, 1, 1, 1)  -- white text

    local numIndicators = {}

    local hpNum      = CreateNumberIndicator(YoyokazooUIFrame, "HP%",      GetPlayerHealthPercent,    0)
    local resNum     = CreateNumberIndicator(YoyokazooUIFrame, "Resource", GetPlayerResourcePercent,  1)
    local tgtHpNum   = CreateNumberIndicator(YoyokazooUIFrame, "TgtHP%",   GetTargetHealthPercent,    2)
    local mapXNum    = CreateNumberIndicator(YoyokazooUIFrame, "MapX",     GetPlayerMapX,            3)
    local mapYNum    = CreateNumberIndicator(YoyokazooUIFrame, "MapY",     GetPlayerMapY,            4)
    local facingYNum = CreateNumberIndicator(YoyokazooUIFrame, "Degrees",    GetPlayerFacingInDegrees,         5)
    local attackers = CreateNumberIndicator(YoyokazooUIFrame, "Attackers",    CountAttackers,         6)

    table.insert(numIndicators, hpNum.update)
    table.insert(numIndicators, resNum.update)
    table.insert(numIndicators, tgtHpNum.update)
    table.insert(numIndicators, mapXNum.update)
    table.insert(numIndicators, mapYNum.update)
    table.insert(numIndicators, facingYNum.update)
    table.insert(numIndicators, attackers.update)

    --local inRangeBox = CreateIndicator(YoyokazooUIFrame, "InRange", IsInMeleeRangeColor, 0)
    -- IsAttackingColor
    local inRangeBox = CreateIndicator(YoyokazooUIFrame, "InRange", IsAttackingColor, 0)
    local inCombatBox = CreateIndicator(YoyokazooUIFrame, "InCombat", IsInCombatColor, 1)
    local canChargeTarget = CreateIndicator(YoyokazooUIFrame, "CanChargeTarget", CanChargeTargetColor, 2)
    local heroicQueued = CreateIndicator(YoyokazooUIFrame, "Heroic Queued", IsAnyNextSwingSpellQueuedColor, 3)
    local multiBoolOne = CreateIndicator(YoyokazooUIFrame, "Multi Bool One", GetMultiBoolOne, 4)

    local indicators = {
        inRangeBox.update,
        inCombatBox.update,
        canChargeTarget.update,
        heroicQueued.update,
        multiBoolOne.update
    }

    -- Melee Range Indicator

    local checkInterval = 0.1    -- seconds between checks
    local elapsedSinceCheck = 0
    local elapsedTime = 0

    YoyokazooUIFrame:SetScript("OnUpdate", function(self, elapsed)
        elapsedTime = elapsedTime + elapsed
        if elapsedTime < checkInterval then return end
        elapsedTime = 0

        -- Update all indicator boxes
        for _, update in ipairs(indicators) do
            update()
        end

        for _, update in ipairs(numIndicators) do
            update()
        end
    end)
end

function PrintXpPerHour(prefix, startTime, totalGained)
    -- seconds since session start
    local elapsed = GetTime() - startTime
    if elapsed <= 0 then
        elapsed = 1 -- avoid div-by-zero, silly numbers at 0s
    end

    local hours = elapsed / 3600
    local xpPerHour = totalGained / hours

    print(string.format(
        "%s Session XP: %d | XP/hr: %.0f",
        prefix or "",
        totalGained,
        xpPerHour
    ))
end