------------------------------------------------------------
-- Warrior-specific game-state checks.
--
-- Moved out of WoWFunctions.lua as part of splitting shared state
-- (MultiBool/MultiInt) from class-specific state (ClassBool/ClassInt -- see
-- the dispatchers in WoWFunctions.lua and GetWarriorClassBoolOne/Two below).
------------------------------------------------------------

function IsOverpowerUsable()
    return IsSpellUsable(7384) -- Overpower rank 1
end

-- rank 1, 772
-- rank 2, 6546
-- rank 3, 6547
-- rank 4, 6548
-- rank 5, 11572
-- rank 6, 11573
-- rank 7, 11574
function TargetHasRend()
    return TargetHasDebuffSpellId(11574)
end

-- 100 is level 1 charge, but still works since range doesnt change and shares cooldown
function CanChargeTarget()
    if not ShouldWeAttackTarget() then
        return false
    end

    return SpellIsInRangeAndCooledDown(100)
end

function CanChargeTargetColor()
    return GetColorFromSingleBool(CanChargeTarget())
end

-- 75,    -- Auto Shot (Hunter)
-- 2480,  -- Shoot Bow
-- 7918,  -- Shoot Gun
-- 7919,  -- Shoot Crossbow
function CanShootTarget()
    if not ShouldWeAttackTarget() then
        return false
    end

    return SpellIsInRangeAndCooledDown(7918)
end

-- shoot gun, shoot crossbow
function WaitingToShoot()
    return IsCurrentSpell(7918) or IsCurrentSpell(2480) or IsCurrentSpell(5019)
end

function IsAnyNextSwingSpellQueued()
    -- Action queue abilities always satisfy IsCurrentSpell()
    -- So check if ANY known next-swing spell is current.
    if IsCurrentSpell("Heroic Strike") then return true end
    if IsCurrentSpell("Cleave")        then return true end
    return false
end

function IsAnyNextSwingSpellQueuedColor()
    return GetColorFromSingleBool(IsAnyNextSwingSpellQueued())
end

-- WW rank 1, 1680
function CanCastWhirlwind()
    return SpellIsCooledDown(1680)
end

-- Sweeping Strikes, 12292
function CanCastSweepingStrikes()
    return SpellIsCooledDown(12292)
end

-- Mortal Strike, 12294
-- Bloodthirst, 23881
-- only one can be active at a time, so do both in one
function CanCastMortalStrikeOrBloodthirst()
    return SpellIsCooledDown(12294) or SpellIsCooledDown(23881)
end

------------------------------------------------------------
-- Packs Warrior-specific state into the ClassBool/ClassInt pixels. Called
-- via the GetClassBoolOne/Two/GetClassIntOne dispatchers in WoWFunctions.lua
-- once UnitClass("player") resolves to WARRIOR. NOT wired into C# yet, and
-- NOT yet removed from GetMultiBoolOne/Two -- see the "duplicated in
-- ClassBool" comments there for what should eventually be stripped out.
------------------------------------------------------------
function GetWarriorClassBoolOne()
    local boolR1 = HasBuffNamed("Battle Shout")
    local boolR2 = TargetHasRend()
    local boolR3 = CanChargeTarget()
    local boolR4 = CanShootTarget()
    local boolR5 = WaitingToShoot()
    local boolR6 = IsAnyNextSwingSpellQueued()
    local boolR7 = IsOverpowerUsable()
    local boolR8 = CanCastWhirlwind()

    local rByte = EncodeBooleansToByte(boolR1, boolR2, boolR3, boolR4, boolR5, boolR6, boolR7, boolR8)

    local boolG1 = CanCastSweepingStrikes()
    local boolG2 = CanCastMortalStrikeOrBloodthirst()
    local boolG3 = false
    local boolG4 = false
    local boolG5 = false
    local boolG6 = false
    local boolG7 = false
    local boolG8 = false

    local gByte = EncodeBooleansToByte(boolG1, boolG2, boolG3, boolG4, boolG5, boolG6, boolG7, boolG8)

    return rByte/255.0, gByte/255.0, 0
end

function GetWarriorClassBoolTwo()
    -- Reserved for future Warrior-specific flags; everything currently
    -- tracked fits in ClassBoolOne above.
    return 0, 0, 0
end

function GetWarriorClassIntOne()
    -- Reserved for future Warrior-specific numeric values; none needed yet.
    return 0, 0, 0
end
