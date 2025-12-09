function EncodeFloatToColor(value)
    -- Clamp to the representable range:
    -- 0 ... (255*255 + 255 + 1)
    if value < 0 then value = 0 end
    if value > 255*255 + 255 + 0.999 then
        value = 255*255 + 255 + 0.999
    end

    -- R = 255-blocks
    local R = math.floor(value / 255)

    -- Remainder after removing R*255
    local remainder = value - R * 255

    -- G = integer remainder
    local G = math.floor(remainder)

    -- Fractional remainder
    local frac = remainder - G

    -- B = fractional part mapped to 0–255
    local B = math.floor(frac * 255 + 0.5)

    return R/255, G/255, B/255
end

-- Creates a numeric indicator with a label.
-- parent      = parent frame
-- label       = "Range", "HP%", etc.
-- valueFunc   = function that returns a number (or string)
-- orderY      = vertical stacking index
--
-- Returns: { frame = <frame>, update = <function> }

local function CreateNumberIndicator(parent, label, valueFunc, orderY)
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

local function CreateIndicator(parent, label, colorFunc, orderY)
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
        local ok = colorFunc()
        if ok then
            tex:SetColorTexture(0, 1, 0, 1)  -- green
        else
            tex:SetColorTexture(1, 0, 0, 1)  -- red
        end
    end

    -- Initial state
    UpdateBox()

    return {
        frame = box,
        update = UpdateBox,
    }
end

local function IsInMeleeRange()
    if UnitExists("target") and UnitCanAttack("player", "target") then
        -- 3 = "duel / inspect / trade" distance, roughly 5 yards (melee-ish)
        if CheckInteractDistance("target", 3) then
            return true
        end
    end
    return false
end

local function IsInCombat()
    return UnitAffectingCombat("player")
end

local function CanChargeTarget()
    local unit = "target"

    -- Basic checks
    if not UnitExists(unit) then
        return false
    end

    if not UnitCanAttack("player", unit) then
        return false
    end

    if UnitIsDead(unit) or UnitIsDeadOrGhost(unit) then
        return false
    end

    -- don't kill greys
    if UnitLevel("player") - UnitLevel("target") >= 5 then
        return false
    end

    -- unit in combat, probably not with us
    if UnitAffectingCombat(unit) then
        return false
    end

    -- Resolve Charge spell name from its ID (100 is the standard Charge ID)
    local chargeName = GetSpellInfo(100)
    if not chargeName then
        -- Should never happen unless spell is unknown (e.g., very low level)
        return false
    end

    -- 1) Range check
    local inRange = IsSpellInRange(chargeName, unit)
    if inRange ~= 1 then
        return false
    end

    -- 2) Cooldown check
    local start, duration, enabled = GetSpellCooldown(chargeName)

    -- If it's not enabled, you can't use it (e.g., disabled by stance/form)
    if enabled == 0 then
        return false
    end

    -- If start == 0 and duration == 0, spell is ready
    if start == 0 or duration == 0 then
        return true
    end

    local remaining = start + duration - GetTime()
    if remaining <= 0 then
        return true
    end

    return false
end

function IsAnyNextSwingSpellQueued()
    -- Action queue abilities always satisfy IsCurrentSpell()
    -- So check if ANY known next-swing spell is current.
    if IsCurrentSpell("Heroic Strike") then return true end
    if IsCurrentSpell("Cleave")        then return true end
    return false
end

-- ---------------------------------------------------------------------------------------------------------------------------------------------

--------------------------------------------------
-- Health: player (percent 0–100)
--------------------------------------------------
local function GetPlayerHealthPercent()
    local hp  = UnitHealth("player")
    local max = UnitHealthMax("player")
    if max == 0 then return 0 end
    return math.floor((hp / max) * 100 + 0.5)
end

--------------------------------------------------
-- Resource: player (rage / mana / energy, percent 0–100)
--------------------------------------------------
local function GetPlayerResourcePercent()
    local powerType = UnitPowerType("player")            -- e.g. 0 = mana, 1 = rage, 3 = energy
    local cur = UnitPower("player", powerType)
    local max = UnitPowerMax("player", powerType)

    if max == 0 then return 0 end
    return math.floor((cur / max) * 100 + 0.5)
end

--------------------------------------------------
-- Target health (percent 0–100)
--------------------------------------------------
local function GetTargetHealthPercent()
    if not UnitExists("target") then
        return 0
    end

    local hp  = UnitHealth("target")
    local max = UnitHealthMax("target")
    if max == 0 then return 0 end
    return math.floor((hp / max) * 100 + 0.5)
end

--------------------------------------------------
-- Map X coord (0–100, normalized across map)
--------------------------------------------------
local function GetPlayerMapX()
    if not C_Map or not C_Map.GetBestMapForUnit then
        return 0
    end

    local mapID = C_Map.GetBestMapForUnit("player")
    if not mapID then
        return 0
    end

    local pos = C_Map.GetPlayerMapPosition(mapID, "player")
    if not pos then
        return 0
    end

    -- pos.x is 0–1 across the map; convert to 0–100
    return math.floor((pos.x or 0) * 10000 + 0.5) / 100  -- two decimals
end

--------------------------------------------------
-- Map Y coord (0–100, normalized across map)
--------------------------------------------------
local function GetPlayerMapY()
    if not C_Map or not C_Map.GetBestMapForUnit then
        return 0
    end

    local mapID = C_Map.GetBestMapForUnit("player")
    if not mapID then
        return 0
    end

    local pos = C_Map.GetPlayerMapPosition(mapID, "player")
    if not pos then
        return 0
    end

    -- pos.y is 0–1; convert to 0–100
    return math.floor((pos.y or 0) * 10000 + 0.5) / 100  -- two decimals
end

--------------------------------------------------
-- Player facing as a normalized 2D direction
-- Returns X and Y where:
--   facing due East:  x =  1, y =  0
--   facing due North: x =  0, y = -1
--   facing due West:  x = -1, y =  0
--   facing due South: x =  0, y =  1
--------------------------------------------------
local function GetPlayerFacingVector()
    local facing = GetPlayerFacing()
    if not facing then
        return 0, -1  -- default “north-ish”
    end

    local x = -math.sin(facing)
    local y = -math.cos(facing)

    -- Should already be unit length, but normalize defensively.
    --local len = math.sqrt(x*x + y*y)
    --if len > 0 then
    --    x, y = x / len, y / len
    --end

    return x, y
end

--------------------------------------------------
-- Convenience wrappers if you want to use them with CreateNumberIndicator:
-- (Each returns a single number)
--------------------------------------------------

local function GetPlayerFacingX()
    local x, _ = GetPlayerFacingVector()
    return math.floor(x * 100) / 100  -- two decimals
end

local function GetPlayerFacingY()
    local _, y = GetPlayerFacingVector()
    return math.floor(y * 100) / 100  -- two decimals
end

local function round2(n)
    --if n == nil then
    --    return "0"
    --end
    return math.floor(n * 100 + 0.5) / 100
end

local function GetPlayerFacingInRadians()
    return round2(GetPlayerFacing())
end

local function GetPlayerFacingInDegrees()
    return round2(GetPlayerFacing() * 180 / math.pi)
end

local function CountAttackers()
    local count = 0
    local plates = C_NamePlate.GetNamePlates()

    for _, plate in ipairs(plates) do
        local unit = plate.namePlateUnitToken
        if unit
            and UnitCanAttack("player", unit)
            and UnitAffectingCombat(unit)
            and UnitIsUnit(unit.."target", "player")
        then
            count = count + 1
        end
    end

    return count
end

-- ---------------------------------------------------------------------------------------------------------------------------------------------

local xpTracker = {}
xpTracker.startXP = 0
xpTracker.currentXP = 0
xpTracker.totalGained = 0
xpTracker.startLevel = 0

-- Create a frame to be our black box
local frame = CreateFrame("Frame", "HelloWorldFrame", UIParent, "BackdropTemplate")

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

-- Create the "Hello World" text on top
local text = frame:CreateFontString("HELLO", "OVERLAY", "GameFontNormalSmall")
text:SetPoint("BOTTOMLEFT", frame, "BOTTOMLEFT", 0, 0)
text:SetText("Hello World")
text:SetTextColor(1, 1, 1, 1)  -- white text


-- Red square indicator inside the frame
--local meleeIndicator = frame:CreateTexture(nil, "ARTWORK")
--meleeIndicator:SetSize(30, 30)                        -- 30x30 square
--meleeIndicator:SetPoint("TOPRIGHT", frame, "TOPRIGHT", -10, -10)
--meleeIndicator:SetColorTexture(1, 0, 0, 1)            -- solid red (RGBA)
--meleeIndicator:Hide()                                 -- start hidden

local numIndicators = {}

local hpNum      = CreateNumberIndicator(frame, "HP%",      GetPlayerHealthPercent,    0)
local resNum     = CreateNumberIndicator(frame, "Resource", GetPlayerResourcePercent,  1)
local tgtHpNum   = CreateNumberIndicator(frame, "TgtHP%",   GetTargetHealthPercent,    2)
local mapXNum    = CreateNumberIndicator(frame, "MapX",     GetPlayerMapX,            3)
local mapYNum    = CreateNumberIndicator(frame, "MapY",     GetPlayerMapY,            4)
--local facingXNum = CreateNumberIndicator(frame, "Radians",    GetPlayerFacingInRadians,         5)
local facingYNum = CreateNumberIndicator(frame, "Degrees",    GetPlayerFacingInDegrees,         5)
local attackers = CreateNumberIndicator(frame, "Attackers",    CountAttackers,         6)

table.insert(numIndicators, hpNum.update)
table.insert(numIndicators, resNum.update)
table.insert(numIndicators, tgtHpNum.update)
table.insert(numIndicators, mapXNum.update)
table.insert(numIndicators, mapYNum.update)
--table.insert(numIndicators, facingXNum.update)
table.insert(numIndicators, facingYNum.update)
table.insert(numIndicators, attackers.update)

local inRangeBox = CreateIndicator(frame, "InRange", IsInMeleeRange, 0)
local inCombatBox = CreateIndicator(frame, "InCombat", IsInCombat, 1)
local canChargeTarget = CreateIndicator(frame, "CanChargeTarget", CanChargeTarget, 2)
local heroicQueued = CreateIndicator(frame, "Heroic Queued", IsAnyNextSwingSpellQueued, 3)

local indicators = {
    inRangeBox.update,
    inCombatBox.update,
    canChargeTarget.update,
    heroicQueued.update
}


local function IsFacingTarget()
    if not UnitExists("target") then
        text:SetText("not UnitExists(target)")
        return false
    end

    local px, py = UnitPosition("player")
    local tx, ty = UnitPosition("target")

    if not px or not tx then
        text:SetText("not px or not tx")
        return false
    end

    local playerFacing = GetPlayerFacing()       -- 0–2pi radians
    local angleToTarget = math.atan2(ty - py, tx - px)

    local diff = angleToTarget - playerFacing
    diff = (diff + math.pi) % (2 * math.pi) - math.pi  -- normalize

    text:SetText("success")

    return math.abs(diff) < (math.pi / 2)
end

-- Melee Range Indicator

local checkInterval = 0.1    -- seconds between checks
local elapsedSinceCheck = 0
local elapsedTime = 0

frame:SetScript("OnUpdate", function(self, elapsed)
    --elapsedSinceCheck = elapsedSinceCheck + elapsed
    --if elapsedSinceCheck < checkInterval then
    --    return
    --end
    --elapsedSinceCheck = 0

    --local inRange = false
    --if UnitExists("target") and UnitCanAttack("player", "target") then
        -- 3 = "duel / inspect / trade" distance, roughly 5 yards (melee-ish)
    --    if CheckInteractDistance("target", 3) then
    --        inRange = true
    --    end
    --end

    --local isFacing = IsFacingTarget();
    --local isFacing = IsUsableSpell("Attack")
    --if inRange then
    --    if isFacing then
    --        meleeIndicator:SetColorTexture(0, 1, 0, 1)
    --    else
    --        meleeIndicator:SetColorTexture(1, 1, 0, 1)
    --    end
    --else
    --    meleeIndicator:SetColorTexture(1, 0, 0, 1)
    --end

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

frame:RegisterEvent("PLAYER_ENTERING_WORLD")
frame:RegisterEvent("PLAYER_XP_UPDATE")
frame:RegisterEvent("PLAYER_LEVEL_UP")

frame:SetScript("OnEvent", function(self, event, ...)
    if event == "PLAYER_ENTERING_WORLD" then
        xpTracker.startLevel = UnitLevel("player")
        xpTracker.startXP    = UnitXP("player")
        xpTracker.currentXP  = xpTracker.startXP
        xpTracker.totalGained = 0

        print("XP session started. Level:", xpTracker.startLevel, "XP:", xpTracker.startXP)
    end

    if event == "PLAYER_XP_UPDATE" then
        local newXP = UnitXP("player")
        local diff = newXP - xpTracker.currentXP

        -- Normal XP gain
        if diff > 0 then
            xpTracker.totalGained = xpTracker.totalGained + diff
            print("Gained:", diff, "XP | Session total:", xpTracker.totalGained)
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