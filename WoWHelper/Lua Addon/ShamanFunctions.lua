------------------------------------------------------------
-- Shaman-specific game-state checks.
--
-- Moved out of WoWFunctions.lua as part of splitting shared state
-- (MultiBool/MultiInt) from class-specific state (ClassBool/ClassInt -- see
-- the dispatchers in WoWFunctions.lua and GetShamanClassBoolOne/Two below).
--
-- Note: CanSpellcastPullTarget() stays in WoWFunctions.lua -- it's shared
-- between Mage and Shaman (branches internally on class), and giving it the
-- same name here and in MageFunctions.lua would collide (last file loaded
-- in the .toc silently wins, since addon globals are one flat namespace).
------------------------------------------------------------

function HasRockbiterWeaponMainHand()
    local hasMainHandEnchant,
          mainHandExpiration,
          mainHandCharges,
          hasOffHandEnchant,
          offHandExpiration,
          offHandCharges = GetWeaponEnchantInfo()

    return hasMainHandEnchant == true
end

function ShouldCastRockbiterWeapon()
    return IsSpellKnown(8017) and IsSpellUsable(8017) and not HasRockbiterWeaponMainHand()
end

function ShouldCastLightningShield()
    return IsSpellKnown(324) and IsSpellUsable(324) and not HasBuffNamed("Lightning Shield")
end

-- Earth Shock Rank 1, 8042
function CanCastEarthShock()
    return SpellIsCooledDown(8042) and IsSpellUsable(8042)
end

-- Pure range check, independent of cooldown -- see SpellIsInRange() in
-- WoWFunctions.lua.
function IsInEarthShockRange()
    return SpellIsInRange(8042)
end

-- TODO: set dynamically on startup and on levelup
-- rank 1, 8050
-- rank 2, 8052
-- rank 3, 8053
-- rank 4, 10447
-- rank 5,
-- rank 6,
function ShouldCastFlameShock()
    return SpellIsCooledDown(8050) and IsSpellUsable(8050) and not TargetHasFlameShock()
end

function TargetHasFlameShock()
  return TargetHasDebuffSpellName("Flame Shock")
end

-- Name-matched rather than a hardcoded spell ID (see IsSpellKnownByName() in
-- WoWFunctions.lua) -- the macro these get bound to already casts by the
-- same literal names ("Cure Poison"/"Cure Disease").
function CanCurePoison()
    return IsSpellKnownByName("Cure Poison")
end

function CanCureDisease()
    return IsSpellKnownByName("Cure Disease")
end

------------------------------------------------------------
-- Packs Shaman-specific state into the ClassBool/ClassInt pixels. Called via
-- the GetClassBoolOne/Two/GetClassIntOne dispatchers in WoWFunctions.lua
-- once UnitClass("player") resolves to SHAMAN. Decoded on the other end in
-- the same bit order -- keep both sides in sync. R7/R8
-- (CanCurePoison/CanCureDisease) fully pack the byte.
------------------------------------------------------------
function GetShamanClassBoolOne()
    local boolR1 = ShouldCastRockbiterWeapon()
    local boolR2 = ShouldCastLightningShield()
    local boolR3 = CanCastEarthShock()
    local boolR4 = ShouldCastFlameShock()
    local boolR5 = TargetHasFlameShock()
    local boolR6 = CanSpellcastPullTarget()
    local boolR7 = CanCurePoison()
    local boolR8 = CanCureDisease()

    local rByte = EncodeBooleansToByte(boolR1, boolR2, boolR3, boolR4, boolR5, boolR6, boolR7, boolR8)

    return rByte/255.0, 0, 0
end

-- R1 (IsInEarthShockRange) is the first flag packed here -- R2-R8 and the
-- G/B bytes are still reserved for future Shaman-specific flags.
function GetShamanClassBoolTwo()
    local boolR1 = IsInEarthShockRange()

    local rByte = EncodeBooleansToByte(boolR1, false, false, false, false, false, false, false)

    return rByte/255.0, 0, 0
end

function GetShamanClassIntOne()
    -- Reserved for future Shaman-specific numeric values; none needed yet.
    return 0, 0, 0
end
