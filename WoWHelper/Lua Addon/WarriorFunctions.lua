------------------------------------------------------------
-- Warrior-specific game-state checks.
--
-- Moved out of WoWFunctions.lua as part of splitting shared state
-- (MultiBool/MultiInt) from class-specific state (ClassBool/ClassInt -- see
-- the dispatchers in WoWFunctions.lua and GetWarriorClassBoolOne/Two below).
------------------------------------------------------------

-- Overpower rank 1, 7384 (GetSpellCooldown is queried by name, so rank 1's ID
-- covers every rank). Both halves are needed: IsUsableSpell only reports
-- whether the spell's *conditions* are met -- enough rage, and the 5s
-- "target dodged" proc window is open -- and says nothing about cooldown.
-- Overpower's own 5s cooldown overlaps that window almost exactly, so
-- without the cooldown check this read true for the whole cooldown after
-- every use, and the C# rotation (WarriorShouldCastOverpower, second in
-- priority) kept pressing it -- and skipping everything below it -- until the
-- window closed. Same pairing Shaman's CanCastEarthShock() uses.
function IsOverpowerUsable()
    return SpellIsCooledDown(7384) and IsSpellUsable(7384)
end

-- Name-matched rather than a hardcoded spell ID -- Rend's 7 ranks (772,
-- 6546, 6547, 6548, 11572, 11573, 11574) are different spell IDs, so a
-- character without the max rank yet (i.e. anyone below the level that
-- trains rank 7) would never match a single hardcoded ID here even with
-- Rend already ticking on the target, same fix Shaman/Warlock's
-- TargetHasFlameShock()/debuff checks already use.
function TargetHasRend()
    return TargetHasDebuffSpellName("Rend")
end

-- Whether the player has trained Charge yet -- name-matched rather than a hardcoded
-- ID, same reasoning as KnowsRend()/KnowsExecute()/KnowsMortalStrikeOrBloodthirst()
-- below. Used both here and on the C# side in place of a hardcoded "PlayerLevel >= 4"
-- gate (Charge's normal training level).
function KnowsCharge()
    return IsSpellKnownByName("Charge")
end

-- 100 is level 1 charge, but still works since range doesnt change and shares cooldown
function CanChargeTarget()
    if not ShouldWeAttackTarget() then
        return false
    end

    -- Charge isn't learned yet -- below that, treat the target as always
    -- "chargeable" rather than checking a spell the player doesn't know yet.
    if not KnowsCharge() then
        return true
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

-- Mortal Strike, 12294
-- Bloodthirst, 23881
-- only one can be active at a time, so do both in one
function CanCastMortalStrikeOrBloodthirst()
    return SpellIsCooledDown(12294) or SpellIsCooledDown(23881)
end

-- Whether the player has trained Rend yet -- name-matched rather than a
-- hardcoded ID for the same reason TargetHasRend() above is (7 ranks, 7
-- different spell IDs). Used on the C# side in place of a hardcoded
-- "PlayerLevel >= 4" gate, the level Rend's rank 1 trains at -- checking the
-- actual spellbook instead of the level tracks a private server's/talent
-- respec's real trained-ability state instead of assuming Blizzard's normal
-- Vanilla training levels.
function KnowsRend()
    return IsSpellKnownByName("Rend")
end

-- Same idea as KnowsRend() above, replacing a hardcoded "PlayerLevel >= 24"
-- gate (Execute's normal training level).
function KnowsExecute()
    return IsSpellKnownByName("Execute")
end

-- Same idea as KnowsRend() above, replacing a hardcoded "PlayerLevel >= 40"
-- gate (Mortal Strike/Bloodthirst's normal training level) -- only one of
-- the two names is ever actually known, same "only one active at a time"
-- deal as CanCastMortalStrikeOrBloodthirst() above.
function KnowsMortalStrikeOrBloodthirst()
    return IsSpellKnownByName("Mortal Strike") or IsSpellKnownByName("Bloodthirst")
end

-- Same idea as KnowsRend() above, replacing a hardcoded "PlayerLevel >= 10"
-- gate (Sunder Armor's normal training level). Only one rank is ever known
-- at a time, but it's still name-matched since the rank's spell ID changes.
function KnowsSunderArmor()
    return IsSpellKnownByName("Sunder Armor")
end

-- Whether the target already has at least one stack of Sunder Armor on it
-- (anyone's -- another warrior's stack counts too, the armor reduction is the
-- same). Name-matched for the same reason TargetHasRend() is: 5 ranks, 5
-- spell IDs. Only "any stack at all" matters to the C# side, which just wants
-- one application on the target, not a full 5-stack -- so this is a bool,
-- not the stack count.
function TargetHasSunderArmor()
    return TargetHasDebuffSpellName("Sunder Armor")
end

------------------------------------------------------------
-- Packs Warrior-specific state into the ClassBool/ClassInt pixels. Called
-- via the GetClassBoolOne/Two/GetClassIntOne dispatchers in WoWFunctions.lua
-- once UnitClass("player") resolves to WARRIOR, and read on the C# side by
-- WowWarriorClassState.UpdateFromBitmap. G7-G8 are reserved (previously
-- R8/G1/G4/G5/G6 were too -- see the "whether we know it" spell checks above,
-- added there once PlayerLevel-based training gates on the C# side were
-- replaced with real spellbook checks -- and previously CanCastWhirlwind()/
-- CanCastSweepingStrikes() -- removed since nothing ever consumed the
-- decoded WhirlwindCooledDown/SweepingStrikesCooledDown fields on the C#
-- side; re-add here if Whirlwind/Sweeping Strikes gets wired into the actual
-- rotation).
------------------------------------------------------------
function GetWarriorClassBoolOne()
    local boolR1 = HasBuffNamed("Battle Shout")
    local boolR2 = TargetHasRend()
    local boolR3 = CanChargeTarget()
    local boolR4 = CanShootTarget()
    local boolR5 = WaitingToShoot()
    local boolR6 = IsAnyNextSwingSpellQueued()
    local boolR7 = IsOverpowerUsable()
    local boolR8 = KnowsRend()

    local rByte = EncodeBooleansToByte(boolR1, boolR2, boolR3, boolR4, boolR5, boolR6, boolR7, boolR8)

    local boolG1 = KnowsExecute()
    local boolG2 = CanCastMortalStrikeOrBloodthirst()
    local boolG3 = KnowsMortalStrikeOrBloodthirst()
    local boolG4 = KnowsCharge()
    local boolG5 = TargetHasSunderArmor()
    local boolG6 = KnowsSunderArmor()
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
