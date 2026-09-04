------------------------------------------------------------
-- Mage-specific game-state checks.
--
-- Moved out of WoWFunctions.lua as part of splitting shared state
-- (MultiBool/MultiInt) from class-specific state (ClassBool/ClassInt -- see
-- the dispatchers in WoWFunctions.lua and GetMageClassBoolOne/Two below).
--
-- Note: CanSpellcastPullTarget() stays in WoWFunctions.lua -- it's shared
-- between Mage and Shaman (branches internally on class), and giving it the
-- same name here and in ShamanFunctions.lua would collide (last file loaded
-- in the .toc silently wins, since addon globals are one flat namespace).
------------------------------------------------------------

function IsMageArmorActive()
    return HasBuffNamed("Frost Armor")
end

function IsArcaneIntellectActive()
    return HasBuffNamed("Arcane Intellect")
end

-- Level 1 Conjured Water, 5350
-- Level 5 Conjured Fresh Water, 2288
-- Level 15 Conjured Purified Water, 2136
-- Level 25 Conjured Spring Water, 3772
-- Level 35 Conjured Mineral Water, 8077
-- Level 45 Conjured Sparkling Water, 8078
-- Level 55 Conjured Crystal Water, 8079
-- TODO: pick based on level
-- TODO: store level in a variable we can reference?
function ShouldWeSummonWater()
    local waterCount = GetItemCount(5350, false)
    return waterCount < 2
end

-- Level 1 Conjured Muffin, 5349
-- Level 5 Conjured Bread, 1113
-- Level 15 Conjured Rye, 1114
-- Level 25 Conjured Pumpernickel, 1487
-- Level 35 Conjured Sourdough, 8075
-- Level 45 Conjured Sweet Roll, 8076
-- Level 55 Conjured Cinnamon Roll, 22895
function ShouldWeSummonFood()
    local foodCount = GetItemCount(1113, false)
    return foodCount < 2
end

-- fireblast rank 1, 2136
function IsFireblastCooledDown()
    return SpellIsCooledDownIgnoringGCD(2136)
end

------------------------------------------------------------
-- Packs Mage-specific state into the ClassBool/ClassInt pixels. Called via
-- the GetClassBoolOne/Two/GetClassIntOne dispatchers in WoWFunctions.lua
-- once UnitClass("player") resolves to MAGE. NOT read yet on the other end,
-- and NOT yet removed from GetMultiBoolOne/Two -- see the "duplicated in
-- ClassBool" comments there for what should eventually be stripped out.
------------------------------------------------------------
function GetMageClassBoolOne()
    local boolR1 = IsMageArmorActive()
    local boolR2 = IsArcaneIntellectActive()
    local boolR3 = ShouldWeSummonWater()
    local boolR4 = ShouldWeSummonFood()
    local boolR5 = IsFireblastCooledDown()
    local boolR6 = CanSpellcastPullTarget()
    local boolR7 = false
    local boolR8 = false

    local rByte = EncodeBooleansToByte(boolR1, boolR2, boolR3, boolR4, boolR5, boolR6, boolR7, boolR8)

    return rByte/255.0, 0, 0
end

function GetMageClassBoolTwo()
    -- Reserved for future Mage-specific flags; everything currently tracked
    -- fits in ClassBoolOne above.
    return 0, 0, 0
end

function GetMageClassIntOne()
    -- Reserved for future Mage-specific numeric values; none needed yet.
    return 0, 0, 0
end
