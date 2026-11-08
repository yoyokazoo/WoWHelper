------------------------------------------------------------
-- Shaman-specific game-state checks, and Shaman's Class pixel-row field
-- lists (SHAMAN_CLASS_BOOL_ONE/TWO_FIELDS, bottom of this file).
--
-- Note: CanSpellcastPullTarget() stays in WoWFunctions.lua -- it's shared
-- between Shaman and Warlock (branches internally on class), and giving it
-- the same name here and in WarlockFunctions.lua would collide (last file
-- loaded in the .toc silently wins, since addon globals are one flat
-- namespace).
------------------------------------------------------------

-- Class trainer spells -- same shape as WARRIOR_TRAINER_SPELLS
-- (WarriorFunctions.lua). TODO: not filled in yet, so Shaman always reads as
-- "nothing to train".
SHAMAN_TRAINER_SPELLS = {
}

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
    return Compat_IsSpellKnown(8017) and IsSpellUsable(8017) and not HasRockbiterWeaponMainHand()
end

function ShouldCastLightningShield()
    return Compat_IsSpellKnown(324) and IsSpellUsable(324) and not HasBuffNamed("Lightning Shield")
end

-- Earth Shock Rank 1, 8042
function CanCastEarthShock()
    return SpellIsCooledDown(8042) and IsSpellUsable(8042)
end

-- Pure range check, independent of cooldown -- see SpellIsInRange() in
-- WoWFunctions.lua. Shocks share both a range and a cooldown category in
-- Classic, so this also covers Frost Shock below -- no separate
-- IsInFrostShockRange needed.
function IsInEarthShockRange()
    return SpellIsInRange(8042)
end

-- Frost Shock Rank 1, 8056. Same damage as Earth Shock but doesn't interrupt
-- a cast -- used only as Earth Shock's substitute against nature-immune
-- targets (see ShamanShouldCastFrostShock in WowShamanTasks.cs). Shares Earth
-- Shock's cooldown category, so checking this button's own cooldown/usability
-- is enough (no separate "is any shock up" check needed).
-- TODO: set dynamically on startup and on levelup, same as ShouldCastFlameShock
-- rank 1, 8056
-- rank 2, 8058
-- rank 3, 10472
-- rank 4, 10473
function CanCastFrostShock()
    return SpellIsCooledDown(8056) and IsSpellUsable(8056)
end

-- Elemental Focus's proc buff -- next two spells cost less mana. Name-matched
-- via HasBuffNamed() (WoWFunctions.lua) rather than a spell ID, same as
-- CanCurePoison/CanCureDisease above.
function HasClearcasting()
    return HasBuffNamed("Clearcasting")
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
-- Shaman's Class pixel rows -- see the field-list comment above
-- MULTI_BOOL_ONE_FIELDS (WoWFunctions.lua) for how these lists work. Decoded
-- on the C# side by WowShamanClassState, in the same order.
------------------------------------------------------------
SHAMAN_CLASS_BOOL_ONE_FIELDS = {
    "ShouldCastRockbiterWeapon",  -- 1 (R1)
    "ShouldCastLightningShield",  -- 2 (R2)
    "CanCastEarthShock",          -- 3 (R3)
    "ShouldCastFlameShock",       -- 4 (R4)
    "TargetHasFlameShock",        -- 5 (R5)
    "CanSpellcastPullTarget",     -- 6 (R6) WoWFunctions.lua -- shared with Warlock
    "CanCurePoison",              -- 7 (R7)
    "CanCureDisease",             -- 8 (R8)
}

SHAMAN_CLASS_BOOL_TWO_FIELDS = {
    "IsInEarthShockRange",        -- 1 (R1)
    "HasClearcasting",            -- 2 (R2)
    "CanCastFrostShock",          -- 3 (R3)
}
