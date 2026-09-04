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

-- Measures the real physical-pixels-per-UIParent-local-unit ratio by
-- comparing GetPhysicalScreenSize() against UIParent's own reported size,
-- instead of trusting UIParent:GetEffectiveScale() / PixelUtil.
--
-- On at least one client (WoW Classic Era 1.15.9, post Edit-Mode UI update)
-- these disagree: GetEffectiveScale() read 0.9, but the real ratio measured
-- this way was 1.6875 -- confirmed by matching FOUR independent rendered
-- sizes exactly (15, 1/0.9-compensated 15, 3, and 6 unit requests all scaled
-- by precisely 1.6875, not 0.9 or 1/0.9). Comparing GetPhysicalScreenSize()
-- to UIParent:GetWidth()/GetHeight() self-calibrates regardless of *why*
-- GetEffectiveScale() is wrong here, so it should keep working even if this
-- particular quirk changes or gets patched later.
local function GetPhysicalPixelsPerLocalUnit()
    local physWidth = GetPhysicalScreenSize()
    return physWidth / UIParent:GetWidth()
end

-- Creates a single exact pixel anchored at a fixed (x, y) offset from the
-- very top-left corner of the screen (not the draggable debug frame).
-- x, y, and size are real screen pixels; converted to UIParent's local
-- coordinate units via GetPhysicalPixelsPerLocalUnit() every update, so this
-- stays correct even if the ratio changes live (resolution change, or
-- whatever is causing GetEffectiveScale() to disagree with it).
--
-- parent    = parent frame (UIParent)
-- x, y      = offset from the top-left corner of the screen, in real screen pixels
-- size      = swatch width/height, in real screen pixels
-- colorFunc = function returning r, g, b (each 0-1)
--
-- Returns: { frame = <texture>, update = <function> }
function CreatePixelSwatch(parent, x, y, size, colorFunc)
    local tex = parent:CreateTexture(nil, "OVERLAY")
    tex:SetColorTexture(0, 0, 0, 1)

    local function UpdateSwatch()
        local pixelsPerUnit = GetPhysicalPixelsPerLocalUnit()
        local localSize = size / pixelsPerUnit

        tex:ClearAllPoints()
        tex:SetSize(localSize, localSize)
        tex:SetPoint("TOPLEFT", parent, "TOPLEFT", x / pixelsPerUnit, -(y / pixelsPerUnit))

        local r, g, b = colorFunc()
        tex:SetColorTexture(r, g, b, 1)
    end

    UpdateSwatch()

    return {
        frame = tex,
        update = UpdateSwatch,
    }
end

-- Section 1 of the redesigned addon UI: a row of exact pixels pinned to the
-- screen's top-left corner at PIXEL_SIZE spacing -- one swatch per field
-- actually consumed elsewhere, in a fixed, resolution-independent spot, in
-- the same order it's consumed in. Debug-only values (individual HP%/
-- attacker count/etc, single-bool indicators) live only in the
-- InitializeIndicators() debug frame below, not here -- this row is
-- intentionally condensed to exactly what's used. MultiBoolTwo/ClassBoolTwo/
-- ClassIntOne aren't here either since nothing decodes them yet -- add a
-- swatch back here if/when a field actually needs one of them.
function InitializePixelRow()
    -- Size of each swatch, in real screen pixels; also its spacing, so
    -- swatches never overlap. The center pixel of each PIXEL_SIZE x
    -- PIXEL_SIZE block (offset PIXEL_SIZE//2 from this swatch's top-left
    -- corner) is what gets read, for margin against any residual edge blur.
    -- Must match the pixel-row layout on the decoding side if this changes.
    local PIXEL_SIZE = 3

    local swatches = {}

    local function AddSwatch(index, colorFunc)
        table.insert(swatches, CreatePixelSwatch(UIParent, index * PIXEL_SIZE, 0, PIXEL_SIZE, colorFunc))
    end

    -- Fixed sentinel color, exactly (96, 255, 117) -- never produced by any of the
    -- encoded/computed swatches below, so it's possible to tell "addon is loaded and
    -- this pixel row is real" (exact match) apart from "on the login screen, or
    -- otherwise not rendering" (anything else, including whatever's actually at this
    -- screen position when the addon isn't loaded) via a single exact-color check,
    -- instead of the old login-screen-specific text/UI pixel signature match.
    AddSwatch(0, function() return 96/255, 255/255, 117/255 end)
    AddSwatch(1, function() return EncodeFloatToColor(GetPlayerMapX()) end)
    AddSwatch(2, function() return EncodeFloatToColor(GetPlayerMapY()) end)
    AddSwatch(3, function() return EncodeFloatToColor(GetPlayerFacingInDegrees()) end)
    AddSwatch(4, GetMultiBoolOne)
    AddSwatch(5, GetMultiIntOne)
    AddSwatch(6, GetMultiIntTwo)
    AddSwatch(7, GetClassBoolOne)

    local checkInterval = 0.05
    local elapsedTime = 0

    local updaterFrame = CreateFrame("Frame")
    updaterFrame:SetScript("OnUpdate", function(self, elapsed)
        elapsedTime = elapsedTime + elapsed
        if elapsedTime < checkInterval then return end
        elapsedTime = 0

        for _, swatch in ipairs(swatches) do
            swatch.update()
        end
    end)
end

function InitializeIndicators()
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
    local multiBoolTwo = CreateIndicator(YoyokazooUIFrame, "Multi Bool Two", GetMultiBoolTwo, 5)
    local multiIntOne = CreateIndicator(YoyokazooUIFrame, "Multi Int One", GetMultiIntOne, 6)
    local multiIntTwo = CreateIndicator(YoyokazooUIFrame, "Multi Int Two", GetMultiIntTwo, 7)
    local classBoolOne = CreateIndicator(YoyokazooUIFrame, "Class Bool One", GetClassBoolOne, 8)
    local classBoolTwo = CreateIndicator(YoyokazooUIFrame, "Class Bool Two", GetClassBoolTwo, 9)
    local classIntOne = CreateIndicator(YoyokazooUIFrame, "Class Int One", GetClassIntOne, 10)

    local indicators = {
        inRangeBox.update,
        inCombatBox.update,
        canChargeTarget.update,
        heroicQueued.update,
        multiBoolOne.update,
        multiBoolTwo.update,
        multiIntOne.update,
        multiIntTwo.update,
        classBoolOne.update,
        classBoolTwo.update,
        classIntOne.update
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

    local xpRemaining = UnitXPMax("player") - UnitXP("player")
    local hoursToLevel
    if xpPerHour <= 0 then
        hoursToLevel = math.huge -- avoid div-by-zero when no XP gained yet
    else
        hoursToLevel = xpRemaining / xpPerHour
    end

    print(string.format(
        "%s Session XP: %d | XP/hr: %.0f | Hours to level: %.1f",
        prefix or "",
        totalGained,
        xpPerHour,
        hoursToLevel
    ))
end

function PositionLootFrameCenter()
  if not LootFrame then return end
  LootFrame:ClearAllPoints()
  LootFrame:SetPoint("CENTER", UIParent, "CENTER", 0, 0)
  print("LOOT FRAME CENTERED")
end







------ Chat GPT picker buttons, but things are a bit spotty.  Shelving for now

--[=[

--[[
Creates a "picker" button:
- Shows a single square parent button.
- On hover, spawns (and shows) a vertical list of option buttons (each an item).
- Clicking an option:
    - Saves the chosen itemID into SavedVariables (your table)
    - Updates parent icon to the chosen item�s icon
    - Hides the options

Classic-friendly notes:
- Uses GetItemIcon(itemID) (works in Classic-era clients)
- Tooltips use GameTooltip:SetItemByID(itemID)
- You must define your SavedVariables table in your .toc, e.g.:
    ## SavedVariables: MyAddonDB
and ensure MyAddonDB exists at runtime.
]]

local function EnsureDB(db, key)
    if type(db) ~= "table" then
        error("db must be a table (SavedVariables root table).")
    end
    if db[key] == nil then
        db[key] = {} -- optional: you can store more later
    end
end

local function SetButtonIcon(button, itemID)
    local icon = GetItemIcon(itemID)
    if icon then
        button.icon:SetTexture(icon)
        button.icon:SetTexCoord(0.08, 0.92, 0.08, 0.92)
        button.icon:Show()
    else
        -- icon might not be cached yet; show question mark
        button.icon:SetTexture("Interface\\Icons\\INV_Misc_QuestionMark")
        button.icon:SetTexCoord(0.08, 0.92, 0.08, 0.92)
        button.icon:Show()
    end
end

local function AttachItemTooltip(frame, itemID)
    frame:SetScript("OnEnter", function(self)
        GameTooltip:SetOwner(self, "ANCHOR_RIGHT")
        GameTooltip:SetItemByID(itemID)
        GameTooltip:Show()
    end)
    frame:SetScript("OnLeave", function()
        GameTooltip:Hide()
    end)
end

local function DumpTableShallow(t, prefix)
    prefix = prefix or ""
    if type(t) ~= "table" then
        print(prefix .. tostring(t))
        return
    end

    for k, v in pairs(t) do
        print(prefix .. tostring(k) .. " = " .. tostring(v))
    end
end

-- db: your SavedVariables root table (e.g., MyAddonDB)
-- storageKey: string key where the selected itemID is stored (e.g., "healingPotionItemID")
-- options: array of { name=..., level=..., itemID=... } (name/level optional, itemID required)
-- parent: UI parent frame
-- x, y: anchor offsets (TOPLEFT of parent by default)
-- size: button size (px)
--
-- Returns: parentButton (Frame)
function CreateHoverItemPickerButton(db, storageKey, options, parent, x, y, size)
    parent = parent or UIParent
    x = x or 0
    y = y or 0
    size = size or 36

    EnsureDB(db, storageKey)

    -- Parent button
    local btn = CreateFrame("Button", nil, parent, "BackdropTemplate")
    btn:SetSize(size, size)
    btn:SetPoint("TOPLEFT", parent, "TOPLEFT", x, y)
    btn:SetFrameStrata("MEDIUM")
    btn:SetClampedToScreen(true)

    btn:SetBackdrop({
        bgFile = "Interface\\Buttons\\WHITE8x8",
        edgeFile = "Interface\\Tooltips\\UI-Tooltip-Border",
        tile = false, edgeSize = 12,
        insets = { left = 2, right = 2, top = 2, bottom = 2 }
    })
    btn:SetBackdropColor(0, 0, 0, 0.65)

    btn.icon = btn:CreateTexture(nil, "ARTWORK")
    btn.icon:SetAllPoints(btn)

    -- Option buttons container (just to keep references)
    btn.optionButtons = {}

    local function HideOptions()
        for _, ob in ipairs(btn.optionButtons) do
            ob:Hide()
        end
    end

    local function ShowOptions()
        for _, ob in ipairs(btn.optionButtons) do
            ob:Show()
        end
    end

    print("db = ", DumpTableShallow(db, " "))
    print("storageKey = ", storageKey)
    print("db[storageKey] = ", DumpTableShallow(db[storageKey], " "))
    print("options", DumpTableShallow(options, " "))
    print("options[1]", options[1])
    print("options[1]", DumpTableShallow(options[1], " "))

    -- Pick initial selection:
    -- 1) from SavedVariables (db[storageKey]) if set
    -- 2) else first option
    local initialItemID = db[storageKey]
    --if not initialItemID then
    --    if options[1] and options[1].itemID then
    --        initialItemID = options[1].itemID
    --        db[storageKey] = initialItemID
    --    end
    --end

    initialItemID = options[1]

    print("InitialItemId = ", DumpTableShallow(initialItemID, " "))
    print("InitialItemId.itemID = ", initialItemID.itemID)

    if initialItemID then
        SetButtonIcon(btn, initialItemID.itemID)
        AttachItemTooltip(btn, initialItemID.itemID)
    else
        btn.icon:SetTexture("Interface\\Icons\\INV_Misc_QuestionMark")
        btn.icon:SetTexCoord(0.08, 0.92, 0.08, 0.92)
    end

    -- Create option buttons (vertical list to the right by default)
    local gap = 4
    for i, opt in ipairs(options) do
        local itemID = opt.itemID
        local ob = CreateFrame("Button", nil, parent, "BackdropTemplate")
        ob:SetSize(size, size)
        ob:SetPoint("TOPLEFT", btn, "TOPRIGHT", gap, -((i - 1) * (size + gap)))
        ob:SetFrameStrata("DIALOG")
        ob:SetClampedToScreen(true)

        ob:SetBackdrop({
            bgFile = "Interface\\Buttons\\WHITE8x8",
            edgeFile = "Interface\\Tooltips\\UI-Tooltip-Border",
            tile = false, edgeSize = 12,
            insets = { left = 2, right = 2, top = 2, bottom = 2 }
        })
        ob:SetBackdropColor(0, 0, 0, 0.75)

        ob.icon = ob:CreateTexture(nil, "ARTWORK")
        ob.icon:SetAllPoints(ob)

        SetButtonIcon(ob, itemID)
        AttachItemTooltip(ob, itemID)

        ob:SetScript("OnClick", function()
            db[storageKey] = itemID

            -- Update parent icon + tooltip to the chosen one
            SetButtonIcon(btn, itemID)
            AttachItemTooltip(btn, itemID)

            HideOptions()
        end)

        -- Keep the dropdown open while hovering option buttons too
        ob:SetScript("OnEnter", function(self)
            GameTooltip:SetOwner(self, "ANCHOR_RIGHT")
            GameTooltip:SetItemByID(itemID)
            GameTooltip:Show()
            ShowOptions()
        end)
        ob:SetScript("OnLeave", function()
            GameTooltip:Hide()
            -- We don't hide immediately here; parent OnLeave handles it (with a short delay).
        end)

        ob:Hide()
        table.insert(btn.optionButtons, ob)
    end

    -- Hover behavior: show options on enter, hide on leave (with small delay to allow moving cursor)
    btn:SetScript("OnEnter", function()
        -- Update tooltip based on current selection
        --local selected = db[storageKey]
        --if selected then
        --    GameTooltip:SetOwner(btn, "ANCHOR_RIGHT")
        --    GameTooltip:SetItemByID(selected)
        --    GameTooltip:Show()
        --end
        ShowOptions()
    end)

    btn:SetScript("OnLeave", function()
        GameTooltip:Hide()

        -- Delay hiding slightly so moving from parent -> option doesn't close it.
        C_Timer.After(0.10, function()
            -- If neither parent nor any option is moused over, hide.
            if not btn:IsMouseOver() then
                for _, ob in ipairs(btn.optionButtons) do
                    if ob:IsMouseOver() then
                        return
                    end
                end
                HideOptions()
            end
        end)
    end)

    -- Utility to query current selection
    btn.GetSelectedItemID = function()
        return db[storageKey]
    end

    -- Utility to force refresh icon from saved vars (e.g., after /reload)
    btn.Refresh = function()
        local selected = db[storageKey]
        if selected then
            SetButtonIcon(btn, selected)
            AttachItemTooltip(btn, selected)
        end
    end

    return btn
end

YoyokazooUIDB = YoyokazooUIDB or {}

        local healingOptions = {
            { name = "Lesser Healing Potion",   itemID = 858   },
            { name = "Healing Potion",          itemID = 929   },
            { name = "Greater Healing Potion",  itemID = 1710  },
            { name = "Superior Healing Potion", itemID = 3928  },
            { name = "Major Healing Potion",    itemID = 13446 },
        }
        local picker = CreateHoverItemPickerButton(YoyokazooUIDB, "healingPotionItemID", healingOptions, UIParent, 50, -50, 36)

]=]

--------------------------------------------------
-- Target bearing marker: a single sentinel-colored square anchored to the
-- CENTER of the current target's nameplate, meant to be found via an
-- ordinary screen-capture pixel search rather than any in-game API.
--
-- Background: reading a target's position/bearing is otherwise impossible
-- from Lua in this client -- UnitPosition("target"),
-- C_Map.GetPlayerMapPosition(mapID, "target"), and even
-- GetNamePlateForUnit("target"):GetCenter() (root AND child frames) are all
-- confirmed blocked by testing (the last one with a hard, pcall-proof
-- "Can't measure restricted regions" security error, not just a nil). That
-- restriction is specifically about MEASURING a secure frame's geometry
-- from Lua -- it says nothing about DRAWING onto one (a completely
-- different, unrestricted action; this is how every nameplate-customization
-- addon already recolors health bars/adds icons).
--
-- A single CENTER marker (not 4 corners, an earlier prototype) is
-- sufficient: bounding-box/apparent-size-based distance was tested and
-- found too subtle to use reliably, and isn't needed anyway -- Earth
-- Shock's own native range check (SpellIsInRangeAndCooledDown) already
-- answers "close enough". Only bearing is missing, and one point gives
-- that when the camera is pitched straight down: the marker's on-screen X
-- relative to the player's own screen position tells left/right of facing,
-- and its Y tells in-front-of/behind -- confirmed by testing (the marker
-- visibly tracks correctly as the player turns via keyboard, confirming the
-- camera stays locked to character facing, which this whole scheme depends
-- on).
--
-- Marked only on the CURRENT target (PLAYER_TARGET_CHANGED, plus
-- NAME_PLATE_UNIT_ADDED for a target whose plate wasn't already visible at
-- the moment it became the target) -- not every nameplate, unlike an
-- earlier prototype -- to keep the search unambiguous. Old markers on a
-- previous target are left in place (harmless clutter, the color only
-- matters while actively searching for the CURRENT target).
--------------------------------------------------
local NAMEPLATE_MARKER_SIZE = 12
local NAMEPLATE_MARKER_COLOR = { 1, 0, 1 } -- magenta -- keep in sync with
-- the corresponding sentinel color on the decoding side

-- Only one of these should ever be shown at a time (the current target's).
-- Tracked separately from plate.wowHelperTargetMarker (which still exists
-- per-plate, so a recycled plate doesn't get a duplicate marker built) so
-- retargeting can hide the PREVIOUS target's marker -- otherwise a stale
-- marker left visible from an old target could be found by the search
-- instead of the actual current target's.
local currentlyShownMarker = nil

local function AddTargetMarker(plate)
    if not plate.wowHelperTargetMarker then
        -- A texture parented directly to the nameplate rendered BEHIND its
        -- health bar/name in testing -- nameplates apparently give those a
        -- higher effective draw priority than a plain child texture on the
        -- outer frame gets. Use an entirely separate, high-FrameStrata frame
        -- instead, anchored to the nameplate via SetPoint (a pure visual
        -- binding that doesn't require reading the nameplate's position at
        -- all -- WoW's anchor system resolves it internally every frame) so
        -- it reliably draws on top regardless of the nameplate's own
        -- internal layering.
        local marker = CreateFrame("Frame", nil, UIParent)
        marker:SetFrameStrata("TOOLTIP")
        marker:SetSize(NAMEPLATE_MARKER_SIZE, NAMEPLATE_MARKER_SIZE)
        marker:SetPoint("CENTER", plate, "CENTER", 0, 0)

        local tex = marker:CreateTexture(nil, "OVERLAY")
        tex:SetAllPoints(marker)
        tex:SetColorTexture(NAMEPLATE_MARKER_COLOR[1], NAMEPLATE_MARKER_COLOR[2], NAMEPLATE_MARKER_COLOR[3])

        plate.wowHelperTargetMarker = marker
    end

    if currentlyShownMarker and currentlyShownMarker ~= plate.wowHelperTargetMarker then
        currentlyShownMarker:Hide()
    end

    plate.wowHelperTargetMarker:Show()
    currentlyShownMarker = plate.wowHelperTargetMarker
end

local function MarkCurrentTargetPlate()
    if not UnitExists("target") then
        if currentlyShownMarker then
            currentlyShownMarker:Hide()
            currentlyShownMarker = nil
        end
        return
    end
    local plate = C_NamePlate and C_NamePlate.GetNamePlateForUnit and C_NamePlate.GetNamePlateForUnit("target")
    if plate then
        AddTargetMarker(plate)
    elseif currentlyShownMarker then
        -- The new target exists but doesn't have a nameplate yet (e.g. too
        -- far away) -- hide the PREVIOUS target's marker immediately rather
        -- than leaving it lingering on the wrong unit until this target's
        -- plate eventually appears via NAME_PLATE_UNIT_ADDED, which also
        -- calls this function and will show the right marker once that
        -- happens.
        currentlyShownMarker:Hide()
        currentlyShownMarker = nil
    end
end

local targetMarkerFrame = CreateFrame("Frame")
targetMarkerFrame:RegisterEvent("PLAYER_TARGET_CHANGED")
targetMarkerFrame:RegisterEvent("NAME_PLATE_UNIT_ADDED")
targetMarkerFrame:SetScript("OnEvent", function(self, event, unit)
    if event == "PLAYER_TARGET_CHANGED" then
        MarkCurrentTargetPlate()
    elseif event == "NAME_PLATE_UNIT_ADDED" and UnitIsUnit(unit, "target") then
        MarkCurrentTargetPlate()
    end
end)