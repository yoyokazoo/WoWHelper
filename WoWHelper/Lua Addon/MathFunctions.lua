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

function round2(n)
    return math.floor(n * 100 + 0.5) / 100
end

function GetColorFromSingleBool(singleColorBool)
    if (singleColorBool == true) then
        return 0, 1, 0
    end

    return 1, 0, 0
end

function EncodeBooleansToByte(...)
    local flags = { ... }
    local value = 0

    for i = 1, math.min(#flags, 8) do
        if flags[i] then
            value = value + bit.lshift(1, i - 1)
        end
    end

    return value
end

