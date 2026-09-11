------------------------------------------------------------
-- Warlock-specific game-state checks.
--
-- Moved out of WoWFunctions.lua as part of splitting shared state
-- (MultiBool/MultiInt) from class-specific state (ClassBool/ClassInt -- see
-- the dispatchers in WoWFunctions.lua and GetWarlockClassBoolOne/Two below).
--
-- Note: CanSpellcastPullTarget() stays in WoWFunctions.lua -- it's shared
-- between Mage/Shaman/Warlock (branches internally on class); giving it the
-- same name here would collide (last file loaded in the .toc silently wins,
-- since addon globals are one flat namespace).
------------------------------------------------------------

-- Demon Skin (687, known from level 1) and Demon Armor (706, replaces Demon
-- Skin at level 20) are two differently-named buffs, but only one is ever
-- active on the player at a time -- name-matched via IsSpellKnownByName()/
-- HasBuffNamed() (WoWFunctions.lua) rather than hardcoded spell IDs, same as
-- CanCurePoison/CanCureDisease in ShamanFunctions.lua, so this doesn't need
-- to rank-track which one the player currently knows.
function ShouldCastDemonArmor()
    if HasBuffNamed("Demon Skin") or HasBuffNamed("Demon Armor") then
        return false
    end

    return IsSpellKnownByName("Demon Armor") or IsSpellKnownByName("Demon Skin")
end

------------------------------------------------------------
-- Packs Warlock-specific state into the ClassBool/ClassInt pixels. Called via
-- the GetClassBoolOne/Two/GetClassIntOne dispatchers in WoWFunctions.lua once
-- UnitClass("player") resolves to WARLOCK. R1/R2 are the flags packed here so
-- far -- R3-R8 are still reserved for future Warlock-specific flags.
------------------------------------------------------------
function GetWarlockClassBoolOne()
    local boolR1 = CanSpellcastPullTarget()
    local boolR2 = ShouldCastDemonArmor()

    local rByte = EncodeBooleansToByte(boolR1, boolR2, false, false, false, false, false, false)

    return rByte/255.0, 0, 0
end

function GetWarlockClassBoolTwo()
    -- Reserved for future Warlock-specific flags.
    return 0, 0, 0
end

function GetWarlockClassIntOne()
    -- Reserved for future Warlock-specific numeric values; none needed yet.
    return 0, 0, 0
end
