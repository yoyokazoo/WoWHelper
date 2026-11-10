--------------------------------------------------
-- Debug frame widgets (InitializeIndicators() below) -- human-facing only;
-- the bot never reads the debug frame.
--------------------------------------------------

-- A labeled number. valueFunc returns a number (or string); nil shows "?".
-- Returns the update function.
function CreateNumberIndicator(parent, label, valueFunc, orderY)
    local row = CreateFrame("Frame", nil, parent)
    row:SetSize(120, 20)
    row:SetPoint("TOPLEFT", parent, "TOPLEFT", 10, -10 - (orderY * 24))

    local valueText = row:CreateFontString(nil, "OVERLAY", "GameFontNormalLarge")
    valueText:SetPoint("LEFT", row, "LEFT", 0, 0)
    valueText:SetText("?")

    local labelText = row:CreateFontString(nil, "OVERLAY", "GameFontNormalSmall")
    labelText:SetPoint("LEFT", row, "LEFT", 60, 0)
    labelText:SetText(label)

    return function()
        local value = valueFunc()
        valueText:SetText(value == nil and "?" or tostring(value))
    end
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

--------------------------------------------------
-- Run-specific settings menu (/yyconfig) -- toggles like "log out on low
-- dynamite"/"log out on full bags", flipped live for the current run. Each
-- row is backed by a YoyokazooUIDB field (see YoyokazooUI.lua, which owns the
-- SavedVariable and the get/set wiring). Some reach the C# side as boxes in
-- the MultiBoolTwo pixel row (MULTI_BOOL_TWO_FIELDS, WoWFunctions.lua); others,
-- like the "Dynamite item"/"Healing potion" selectors, are Lua-only (only
-- AreWeLowOnDynamite()/AreWeLowOnHealthPotions() ever need them).
--
-- options: array of either
--   { label = "...", get = function() end, set = function(bool) end }               -- checkbox (default)
-- or
--   { label = "...", type = "selector", choices = { { id = ..., label = "..." }, ... },
--     get = function() return currentId end, set = function(id) end }               -- cycling button
-- Returns the frame, hidden by default -- the caller (YoyokazooUI.lua's
-- /yyconfig handler) shows/hides it.
--------------------------------------------------
function CreateSettingsMenu(options)
    local ROW_HEIGHT = 26

    local frame = CreateFrame("Frame", "YoyokazooUISettingsFrame", UIParent, "BackdropTemplate")
    frame:SetSize(260, 40 + (#options * ROW_HEIGHT))
    frame:SetPoint("CENTER", UIParent, "CENTER", 0, 100)
    frame:SetFrameStrata("DIALOG")
    frame:SetMovable(true)
    frame:EnableMouse(true)
    frame:RegisterForDrag("LeftButton")
    frame:SetScript("OnDragStart", frame.StartMoving)
    frame:SetScript("OnDragStop", frame.StopMovingOrSizing)

    frame:SetBackdrop({
        bgFile = "Interface\\ChatFrame\\ChatFrameBackground",
        edgeFile = "Interface\\Tooltips\\UI-Tooltip-Border",
        tile = false,
        edgeSize = 12,
        insets = { left = 2, right = 2, top = 2, bottom = 2 }
    })
    frame:SetBackdropColor(0, 0, 0, 0.9)

    local title = frame:CreateFontString(nil, "OVERLAY", "GameFontNormalLarge")
    title:SetPoint("TOP", frame, "TOP", 0, -10)
    title:SetText("YoyokazooUI Settings")

    -- Standard Blizzard close-button template (the same "X" used on every stock UI
    -- panel) -- just hides the frame, same as running /yyconfig again.
    local closeButton = CreateFrame("Button", nil, frame, "UIPanelCloseButton")
    closeButton:SetPoint("TOPRIGHT", frame, "TOPRIGHT", -2, -2)
    closeButton:SetScript("OnClick", function() frame:Hide() end)

    for i, option in ipairs(options) do
        local rowY = -35 - ((i - 1) * ROW_HEIGHT)

        if option.type == "selector" then
            -- Cycling button: each click advances to the next entry in
            -- option.choices (wrapping around), rather than a dropdown -- fewer
            -- moving parts for a handful of choices like the dynamite tiers.
            local label = frame:CreateFontString(nil, "OVERLAY", "GameFontNormal")
            label:SetPoint("TOPLEFT", frame, "TOPLEFT", 15, rowY - 4)
            label:SetText(option.label)

            local button = CreateFrame("Button", nil, frame, "UIPanelButtonTemplate")
            button:SetSize(110, 22)
            button:SetPoint("TOPRIGHT", frame, "TOPRIGHT", -15, rowY)

            local function LabelForCurrentChoice()
                local currentId = option.get()
                for _, choice in ipairs(option.choices) do
                    if choice.id == currentId then
                        return choice.label
                    end
                end
                return tostring(currentId)
            end

            button:SetText(LabelForCurrentChoice())

            button:SetScript("OnClick", function()
                local currentId = option.get()
                local currentIndex = 1
                for idx, choice in ipairs(option.choices) do
                    if choice.id == currentId then
                        currentIndex = idx
                        break
                    end
                end

                local nextChoice = option.choices[(currentIndex % #option.choices) + 1]
                option.set(nextChoice.id)
                button:SetText(nextChoice.label)
            end)
        else
            local check = CreateFrame("CheckButton", nil, frame, "UICheckButtonTemplate")
            check:SetPoint("TOPLEFT", frame, "TOPLEFT", 15, rowY)
            check:SetChecked(option.get())

            local label = check:CreateFontString(nil, "OVERLAY", "GameFontNormal")
            label:SetPoint("LEFT", check, "RIGHT", 4, 0)
            label:SetText(option.label)

            -- GetChecked() returns 1/nil in Classic, not true/false -- normalize before
            -- handing off to option.set so callers (and the saved variable) always store a
            -- real boolean.
            check:SetScript("OnClick", function(self)
                option.set(self:GetChecked() and true or false)
            end)
        end
    end

    frame:Hide()
    return frame
end

--------------------------------------------------
-- The bot's pixel rows -- the ONLY thing the C# side reads (WowPixelRows.cs,
-- WowWorldState.cs, Wow*ClassState.cs). A block of PIXEL_SIZE-tall rows
-- pinned to the screen's literal top-left corner, with every size and offset
-- given in real screen pixels and converted via
-- GetPhysicalPixelsPerLocalUnit(), so no per-resolution calibration is
-- needed. Row N covers physical y = N*PIXEL_SIZE through N*PIXEL_SIZE + 2;
-- the C# side reads the center pixel of each bar row / box, for margin
-- against edge blur.
--
--   Row  0  player health bar (green) -- plus the ADDON_LOADED_COLOR
--           sentinel box right after it, at x = PIXEL_BAR_WIDTH
--   Row  1  player resource bar (blue)
--   Row  2  target health bar (red)
--   Row  3  (empty -- spacing between the bars and the bit rows)
--   Row  4  map X                fixed point
--   Row  5  map Y                fixed point
--   Row  6  facing, in degrees   fixed point
--   Row  7  MultiBoolOne         bool fields (MULTI_BOOL_ONE_FIELDS)
--   Row  8  MultiBoolTwo         bool fields (MULTI_BOOL_TWO_FIELDS)
--   Row  9  ClassBoolOne         bool fields (<CLASS>_CLASS_BOOL_ONE_FIELDS)
--   Row 10  ClassBoolTwo         bool fields (<CLASS>_CLASS_BOOL_TWO_FIELDS)
--   Row 11  MultiIntOne          bytes: attacker count, player level, zone ID
--
-- Bars fill left to right, so the number of filled pixels is the percent --
-- and a StatusBar accepts secret values (Midnight-rules clients), which Lua
-- itself can't do math on. Rows 4-11 are bit rows: PIXEL_ROW_BITS boxes,
-- green = 1, red = 0 (see MathFunctions.lua for the encodings).
--
-- Row order and every constant below MUST match WowPixelRows.cs. A one-sided
-- change doesn't fail loudly -- it silently misreads every row it shifts.
--------------------------------------------------
PIXEL_SIZE = 3
PIXEL_BAR_WIDTH = 100  -- so each filled bar pixel is exactly 1%
local PIXEL_ROW_COUNT = 12

-- Exact-match sentinel: if the C# side sees exactly this color, the addon is
-- loaded and the rows are real. Nothing else here ever draws it.
ADDON_LOADED_COLOR = { 96/255, 255/255, 117/255 }  -- (96, 255, 117)

PIXEL_BAR_EMPTY_COLOR        = { 0, 0, 0 }  -- (0, 0, 0)
PLAYER_HEALTH_BAR_FILL_COLOR = { 0, 1, 0 }  -- (0, 255, 0)
RESOURCE_BAR_FILL_COLOR      = { 0, 0, 1 }  -- (0, 0, 255)
TARGET_HEALTH_BAR_FILL_COLOR = { 1, 0, 0 }  -- (255, 0, 0)

BIT_ONE_COLOR  = { 0, 1, 0 }  -- (0, 255, 0)
BIT_ZERO_COLOR = { 1, 0, 0 }  -- (255, 0, 0)

local PIXEL_ROW_REFRESH_SECONDS = 0.05

-- Bar value sources: each returns (current, max), possibly secret -- passed
-- straight to the StatusBar with no Lua-side math or comparisons.
function GetPlayerHealthBarValues()
    return UnitHealth("player"), UnitHealthMax("player")
end

function GetPlayerResourceBarValues()
    return UnitPower("player"), UnitPowerMax("player")
end

function GetTargetHealthBarValues()
    -- UnitExists isn't secret. With no target, show an empty bar rather than
    -- whatever a 0/0 StatusBar happens to render.
    if not UnitExists("target") then return 0, 1 end
    return UnitHealth("target"), UnitHealthMax("target")
end

local function PlaceInPixels(container, region, pixelsPerUnit, x, y, width, height)
    region:ClearAllPoints()
    region:SetSize(width / pixelsPerUnit, height / pixelsPerUnit)
    region:SetPoint("TOPLEFT", container, "TOPLEFT", x / pixelsPerUnit, -(y / pixelsPerUnit))
end

-- Each element is { name, layout(pixelsPerUnit), update() }: layout
-- positions it (re-run only when the pixel/unit ratio changes), update
-- redraws its value every refresh (nil for static elements).

local function CreateStatusBarElement(container, row, fillColor, valuesFunc)
    local bar = CreateFrame("StatusBar", nil, container)
    bar:SetStatusBarTexture("Interface\\Buttons\\WHITE8X8")
    bar:SetStatusBarColor(fillColor[1], fillColor[2], fillColor[3], 1)
    bar:SetOrientation("HORIZONTAL")

    local background = bar:CreateTexture(nil, "BACKGROUND")
    background:SetAllPoints(bar)
    background:SetColorTexture(PIXEL_BAR_EMPTY_COLOR[1], PIXEL_BAR_EMPTY_COLOR[2], PIXEL_BAR_EMPTY_COLOR[3], 1)

    return {
        name = "row " .. row .. " (bar)",
        layout = function(pixelsPerUnit)
            PlaceInPixels(container, bar, pixelsPerUnit, 0, row * PIXEL_SIZE, PIXEL_BAR_WIDTH, PIXEL_SIZE)
        end,
        update = function()
            local current, max = valuesFunc()
            bar:SetMinMaxValues(0, max)
            bar:SetValue(current)
        end,
    }
end

local function CreateSolidSwatchElement(container, name, x, y, color)
    local tex = container:CreateTexture(nil, "OVERLAY")
    tex:SetColorTexture(color[1], color[2], color[3], 1)

    return {
        name = name,
        layout = function(pixelsPerUnit)
            PlaceInPixels(container, tex, pixelsPerUnit, x, y, PIXEL_SIZE, PIXEL_SIZE)
        end,
    }
end

-- bitsFunc returns the row's integer value (see the Encode* helpers in
-- MathFunctions.lua); box i shows bit i-1.
local function CreateBitRowElement(container, row, bitsFunc)
    local boxes = {}
    for i = 1, PIXEL_ROW_BITS do
        boxes[i] = container:CreateTexture(nil, "OVERLAY")
    end

    return {
        name = "row " .. row,
        layout = function(pixelsPerUnit)
            for i, box in ipairs(boxes) do
                PlaceInPixels(container, box, pixelsPerUnit, (i - 1) * PIXEL_SIZE, row * PIXEL_SIZE, PIXEL_SIZE, PIXEL_SIZE)
            end
        end,
        update = function()
            local n = bitsFunc()
            for _, box in ipairs(boxes) do
                local bitValue = n % 2
                n = (n - bitValue) / 2
                local color = (bitValue == 1) and BIT_ONE_COLOR or BIT_ZERO_COLOR
                box:SetColorTexture(color[1], color[2], color[3], 1)
            end
        end,
    }
end

function InitializePixelRow()
    -- Its own TOOLTIP-strata frame, so no other UI (e.g. the player frame,
    -- which sits in this corner) can draw over the rows.
    local container = CreateFrame("Frame", nil, UIParent)
    container:SetFrameStrata("TOOLTIP")
    container:SetPoint("TOPLEFT", UIParent, "TOPLEFT", 0, 0)

    local elements = {
        CreateStatusBarElement(container, 0, PLAYER_HEALTH_BAR_FILL_COLOR, GetPlayerHealthBarValues),
        CreateSolidSwatchElement(container, "addon-loaded sentinel", PIXEL_BAR_WIDTH, 0, ADDON_LOADED_COLOR),
        CreateStatusBarElement(container, 1, RESOURCE_BAR_FILL_COLOR, GetPlayerResourceBarValues),
        CreateStatusBarElement(container, 2, TARGET_HEALTH_BAR_FILL_COLOR, GetTargetHealthBarValues),

        -- Row 3 is left empty: spacing between the bars and the bit rows.

        CreateBitRowElement(container, 4, function() return EncodeFixedPoint(GetPlayerMapX()) end),
        CreateBitRowElement(container, 5, function() return EncodeFixedPoint(GetPlayerMapY()) end),
        CreateBitRowElement(container, 6, function() return EncodeFixedPoint(GetPlayerFacingInDegrees()) end),

        CreateBitRowElement(container, 7, function() return EncodeBoolFields("MultiBoolOne", MULTI_BOOL_ONE_FIELDS) end),
        CreateBitRowElement(container, 8, function() return EncodeBoolFields("MultiBoolTwo", MULTI_BOOL_TWO_FIELDS) end),
        CreateBitRowElement(container, 9, function() return EncodeBoolFields("ClassBoolOne", GetPlayerClassFields("CLASS_BOOL_ONE_FIELDS")) end),
        CreateBitRowElement(container, 10, function() return EncodeBoolFields("ClassBoolTwo", GetPlayerClassFields("CLASS_BOOL_TWO_FIELDS")) end),

        CreateBitRowElement(container, 11, function()
            return EncodeBytes(CountAttackers(), UnitLevel("player"), GetCurrentZoneId())
        end),
    }

    local lastPixelsPerUnit = nil
    local reportedErrors = {}

    local function Refresh()
        local pixelsPerUnit = GetPhysicalPixelsPerLocalUnit()
        -- UIParent can briefly report no width during loading (inf/NaN here).
        if not pixelsPerUnit or pixelsPerUnit ~= pixelsPerUnit or pixelsPerUnit <= 0 or pixelsPerUnit == math.huge then return end

        if pixelsPerUnit ~= lastPixelsPerUnit then
            lastPixelsPerUnit = pixelsPerUnit
            container:SetSize((PIXEL_BAR_WIDTH + PIXEL_SIZE) / pixelsPerUnit, (PIXEL_ROW_COUNT * PIXEL_SIZE) / pixelsPerUnit)
            for _, element in ipairs(elements) do
                element.layout(pixelsPerUnit)
            end
        end

        -- Each element is pcall'd so one failing can't stop the rest from
        -- updating; it keeps its last drawn value. Each distinct error is
        -- printed once.
        for _, element in ipairs(elements) do
            if element.update then
                local ok, err = pcall(element.update)
                if not ok then
                    local key = element.name .. tostring(err)
                    if not reportedErrors[key] then
                        reportedErrors[key] = true
                        print(string.format("|cffff4040YoyokazooUI pixel %s update failed (combat=%s):|r %s",
                            element.name, tostring(InCombatLockdown()), tostring(err)))
                    end
                end
            end
        end
    end

    Refresh()

    local elapsedTime = 0
    container:SetScript("OnUpdate", function(self, elapsed)
        elapsedTime = elapsedTime + elapsed
        if elapsedTime < PIXEL_ROW_REFRESH_SECONDS then return end
        elapsedTime = 0
        Refresh()
    end)
end

-- The human-only debug frame (/yydebug): map X/Y and facing, for reading off
-- coordinates while recording routes. Its size (YoyokazooUI.lua) fits exactly
-- these rows -- resize it there if rows are added. Each update is pcall'd so
-- one failing value doesn't freeze the rest.
function InitializeIndicators()
    local updates = {
        CreateNumberIndicator(YoyokazooUIFrame, "MapX",    GetPlayerMapX,            0),
        CreateNumberIndicator(YoyokazooUIFrame, "MapY",    GetPlayerMapY,            1),
        CreateNumberIndicator(YoyokazooUIFrame, "Degrees", GetPlayerFacingInDegrees, 2),
    }

    local checkInterval = 0.1
    local elapsedTime = 0

    YoyokazooUIFrame:SetScript("OnUpdate", function(self, elapsed)
        elapsedTime = elapsedTime + elapsed
        if elapsedTime < checkInterval then return end
        elapsedTime = 0

        for _, update in ipairs(updates) do
            pcall(update)
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
-- earlier prototype -- to keep the search unambiguous, and only if that
-- target isn't tapped by someone else (see ShouldMarkCurrentTarget below).
-- At most one marker is ever shown: retargeting hides the previous one, and
-- so does the target's plate disappearing (NAME_PLATE_UNIT_REMOVED, plus a
-- polling reconcile as a backstop).
--------------------------------------------------
-- In real screen pixels, not UI units -- converted via
-- GetPhysicalPixelsPerLocalUnit() (same as the pixel row) so the marker is
-- exactly this size on every client/UI scale. (It used to be 12 UI units,
-- which came out ~20px on Classic Era at ratio 1.6875 but ~14-15px on the
-- Forever client at ratio 1.2.)
local NAMEPLATE_MARKER_SIZE_PIXELS = 20
local NAMEPLATE_MARKER_COLOR = { 1, 0, 1 } -- magenta -- keep in sync with
-- the corresponding sentinel color on the decoding side

-- Only one of these should ever be shown at a time (the current target's).
-- Tracked separately from plate.wowHelperTargetMarker (which still exists
-- per-plate, so a recycled plate doesn't get a duplicate marker built) so
-- retargeting can hide the PREVIOUS target's marker -- otherwise a stale
-- marker left visible from an old target could be found by the search
-- instead of the actual current target's.
local currentlyShownMarker = nil
-- The plate currentlyShownMarker is anchored to. The marker is parented to
-- UIParent (not the plate -- see AddTargetMarker), so it does NOT hide when
-- its plate hides: when the target walks out of nameplate range the plate is
-- hidden/recycled but the marker would otherwise stay frozen on screen at the
-- plate's last position (or follow the plate onto whatever unit it gets
-- recycled for). Tracked so NAME_PLATE_UNIT_REMOVED and the OnUpdate
-- reconcile below can tell whether the marker is still on the right plate.
local currentMarkerPlate = nil

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
        marker:SetPoint("CENTER", plate, "CENTER", 0, 0)

        local tex = marker:CreateTexture(nil, "OVERLAY")
        tex:SetAllPoints(marker)
        tex:SetColorTexture(NAMEPLATE_MARKER_COLOR[1], NAMEPLATE_MARKER_COLOR[2], NAMEPLATE_MARKER_COLOR[3])

        plate.wowHelperTargetMarker = marker
    end

    -- Re-sized on every show, not just at creation, in case the pixel/unit
    -- ratio changed since (UI scale or resolution change mid-session).
    local localSize = NAMEPLATE_MARKER_SIZE_PIXELS / GetPhysicalPixelsPerLocalUnit()
    plate.wowHelperTargetMarker:SetSize(localSize, localSize)

    if currentlyShownMarker and currentlyShownMarker ~= plate.wowHelperTargetMarker then
        currentlyShownMarker:Hide()
    end

    plate.wowHelperTargetMarker:Show()
    currentlyShownMarker = plate.wowHelperTargetMarker
    currentMarkerPlate = plate
end

local function HideCurrentMarker()
    if currentlyShownMarker then
        currentlyShownMarker:Hide()
        currentlyShownMarker = nil
    end
    currentMarkerPlate = nil
end

-- A target tagged by someone outside our group gets no marker at all. The
-- C# side's PathfindingLoopTask treats "marker on screen but CanEngageTarget
-- is false" as "target's just out of range, walk towards it" -- a tapped mob
-- also reads as not-engageable, so without this the bot would chase after
-- someone else's kill. Nothing else that uses the marker (walking into
-- melee range, in-combat re-facing) ever runs against a tap-denied target,
-- so hiding it here can't starve those.
local function ShouldMarkCurrentTarget()
    return UnitExists("target") and not UnitIsTapDenied("target")
end

local function MarkCurrentTargetPlate()
    if not ShouldMarkCurrentTarget() then
        HideCurrentMarker()
        return
    end
    local plate = C_NamePlate and C_NamePlate.GetNamePlateForUnit and C_NamePlate.GetNamePlateForUnit("target")
    if plate then
        AddTargetMarker(plate)
    else
        -- The new target exists but doesn't have a nameplate yet (e.g. too
        -- far away) -- hide the PREVIOUS target's marker immediately rather
        -- than leaving it lingering on the wrong unit until this target's
        -- plate eventually appears via NAME_PLATE_UNIT_ADDED, which also
        -- calls this function and will show the right marker once that
        -- happens.
        HideCurrentMarker()
    end
end

local targetMarkerFrame = CreateFrame("Frame")
targetMarkerFrame:RegisterEvent("PLAYER_TARGET_CHANGED")
targetMarkerFrame:RegisterEvent("NAME_PLATE_UNIT_ADDED")
targetMarkerFrame:RegisterEvent("NAME_PLATE_UNIT_REMOVED")
targetMarkerFrame:SetScript("OnEvent", function(self, event, unit)
    if event == "PLAYER_TARGET_CHANGED" then
        MarkCurrentTargetPlate()
    elseif event == "NAME_PLATE_UNIT_ADDED" and UnitIsUnit(unit, "target") then
        MarkCurrentTargetPlate()
    elseif event == "NAME_PLATE_UNIT_REMOVED" and currentMarkerPlate then
        -- The target's plate going away (e.g. it moved out of nameplate
        -- range) doesn't hide the marker on its own, since the marker isn't
        -- parented to the plate. Check both the unit and the plate itself,
        -- in case either lookup is unreliable mid-removal; the OnUpdate
        -- reconcile below is the backstop if both miss.
        local removedPlate = C_NamePlate and C_NamePlate.GetNamePlateForUnit and C_NamePlate.GetNamePlateForUnit(unit)
        if UnitIsUnit(unit, "target") or removedPlate == currentMarkerPlate then
            HideCurrentMarker()
        end
    end
end)

-- The events above only fire when the target or its nameplate changes, but
-- the tap-denied check in ShouldMarkCurrentTarget() can flip on an
-- already-marked target (someone else tags the mob while we're walking
-- towards it), and a missed/out-of-order nameplate event could leave the
-- marker on a plate that no longer belongs to the target. Rather than depend
-- on exactly which events the client fires, just poll: re-derive where the
-- marker should be (the target's current plate, or nowhere) and fix it up if
-- it's anywhere else. Cheap (a couple of unit queries) and far slower than
-- the pixel row's own redraw.
local TARGET_MARKER_RECHECK_INTERVAL = 0.1
local targetMarkerRecheckElapsed = 0
targetMarkerFrame:SetScript("OnUpdate", function(self, elapsed)
    targetMarkerRecheckElapsed = targetMarkerRecheckElapsed + elapsed
    if targetMarkerRecheckElapsed < TARGET_MARKER_RECHECK_INTERVAL then return end
    targetMarkerRecheckElapsed = 0

    if not ShouldMarkCurrentTarget() then
        if currentlyShownMarker then
            HideCurrentMarker()
        end
        return
    end

    local plate = C_NamePlate and C_NamePlate.GetNamePlateForUnit and C_NamePlate.GetNamePlateForUnit("target")
    if not plate or not plate:IsShown() then
        if currentlyShownMarker then
            HideCurrentMarker()
        end
    elseif plate ~= currentMarkerPlate or not currentlyShownMarker then
        AddTargetMarker(plate)
    end
end)
