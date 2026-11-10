--------------------------------------------------
-- Pixel row encoding (see InitializePixelRow() in UIFunctions.lua for the
-- layout). Every bit row is PIXEL_ROW_BITS boxes, green = 1 and red = 0,
-- together encoding one unsigned integer: box 1 (leftmost) is bit 0, the
-- least significant. Each Encode* helper below turns one kind of value into
-- that integer. The C# side decodes the same layout in WowPixelRows.cs --
-- every constant here MUST match its counterpart there.
--------------------------------------------------
PIXEL_ROW_BITS = 24
local PIXEL_ROW_MAX_VALUE = 2^PIXEL_ROW_BITS - 1

-- Fractional bits for EncodeFixedPoint. 15 leaves 9 integer bits (max just
-- under 512), which covers everything encoded this way (map coords 0-100,
-- facing 0-360) at 1/32768 precision. MUST match
-- WowPixelRows.FixedPointFractionBits.
FIXED_POINT_FRACTION_BITS = 15

-- A non-negative number as unsigned fixed point. Clamped to the row's range.
function EncodeFixedPoint(value)
    local n = math.floor((value or 0) * 2^FIXED_POINT_FRACTION_BITS + 0.5)
    if n < 0 then return 0 end
    if n > PIXEL_ROW_MAX_VALUE then return PIXEL_ROW_MAX_VALUE end
    return n
end

local function ClampByte(value)
    value = math.floor(value or 0)
    if value < 0 then return 0 end
    if value > 255 then return 255 end
    return value
end

-- Three 0-255 integers into boxes 1-8, 9-16, and 17-24 (what used to be one
-- pixel's R, G, and B channels).
function EncodeBytes(byte1, byte2, byte3)
    return ClampByte(byte1) + ClampByte(byte2) * 2^8 + ClampByte(byte3) * 2^16
end

-- Placeholder for an unused position in a bool field list, keeping every
-- later field in its box.
RESERVED = false

local function CallAsBool(func)
    -- Normalized inside the pcall: even testing a secret value's truthiness throws.
    return func() and true or false
end

local reportedBoolFieldErrors = {}

-- A list of bool fields into one box each, in list order. Each entry is the
-- NAME of a global function (looked up at call time, so it can live in any
-- file regardless of load order) or RESERVED. Each field is evaluated on its
-- own: one that throws (e.g. on a secret value mid-combat) reads false and
-- prints once per distinct error, without affecting the rest of the row.
function EncodeBoolFields(rowName, fields)
    local n = 0
    for i = 1, math.min(#fields, PIXEL_ROW_BITS) do
        local functionName = fields[i]
        if functionName ~= RESERVED then
            local ok, result = pcall(CallAsBool, _G[functionName])
            if not ok then
                local key = rowName .. i .. tostring(result)
                if not reportedBoolFieldErrors[key] then
                    reportedBoolFieldErrors[key] = true
                    print(string.format("|cffff4040YoyokazooUI: %s box %d (%s) failed, reads false:|r %s",
                        rowName, i, tostring(functionName), tostring(result)))
                end
                result = false
            end
            if result then
                n = n + 2^(i - 1)
            end
        end
    end
    return n
end

function round2(n)
    return math.floor(n * 100 + 0.5) / 100
end
