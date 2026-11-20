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
-- (WarriorFunctions.lua). Levels 1-60, built from the Classic Era trainer
-- schedule (levels and rank spell IDs) with costs taken from the Forever
-- per-level prices in WARRIOR_TRAINER_SPELLS -- NOT from a /yyspells dump, so
-- any Forever-specific changes to the Shaman trainer aren't reflected yet.
-- Regenerate from a /yyspells dump at a Forever Shaman trainer when possible.
-- Absent: starting spells (Lightning Bolt/Healing Wave rank 1), the
-- Call of Fire quest's Searing Totem rank 1, and talent rank 1s.
local SEARING_TOTEM_R1 = 3599
local MANA_TIDE_TOTEM_R1 = 16190
SHAMAN_TRAINER_SPELLS = {
    { level = 1,  name = "Rockbiter Weapon",             spellId = 8017,    cost = 10 },
    { level = 4,  name = "Earth Shock",                  spellId = 8042,    cost = 100 },
    { level = 4,  name = "Stoneskin Totem",              spellId = 8071,    cost = 100 },
    { level = 6,  name = "Earthbind Totem",              spellId = 2484,    cost = 100 },
    { level = 6,  name = "Healing Wave (R2)",            spellId = 332,     cost = 100 },
    { level = 8,  name = "Earth Shock (R2)",             spellId = 8044,    cost = 200 },
    { level = 8,  name = "Lightning Bolt (R2)",          spellId = 529,     cost = 200 },
    { level = 8,  name = "Lightning Shield",             spellId = 324,     cost = 200 },
    { level = 8,  name = "Rockbiter Weapon (R2)",        spellId = 8018,    cost = 200 },
    { level = 8,  name = "Stoneclaw Totem",              spellId = 5730,    cost = 200 },
    { level = 10, name = "Flame Shock",                  spellId = 8050,    cost = 700 },
    { level = 10, name = "Flametongue Weapon",           spellId = 8024,    cost = 700 },
    { level = 10, name = "Strength of Earth Totem",      spellId = 8075,    cost = 700 },
    { level = 12, name = "Ancestral Spirit",             spellId = 2008,    cost = 900 },
    { level = 12, name = "Fire Nova Totem",              spellId = 1535,    cost = 900 },
    { level = 12, name = "Healing Wave (R3)",            spellId = 547,     cost = 900 },
    { level = 12, name = "Purge",                        spellId = 370,     cost = 900 },
    { level = 14, name = "Earth Shock (R3)",             spellId = 8045,    cost = 1100 },
    { level = 14, name = "Lightning Bolt (R3)",          spellId = 548,     cost = 1100 },
    { level = 14, name = "Stoneskin Totem (R2)",         spellId = 8154,    cost = 1100 },
    { level = 16, name = "Cure Poison",                  spellId = 526,     cost = 1500 },
    { level = 16, name = "Lightning Shield (R2)",        spellId = 325,     cost = 1500 },
    { level = 16, name = "Rockbiter Weapon (R3)",        spellId = 8019,    cost = 1500 },
    { level = 18, name = "Flame Shock (R2)",             spellId = 8052,    cost = 3200 },
    { level = 18, name = "Flametongue Weapon (R2)",      spellId = 8027,    cost = 3200 },
    { level = 18, name = "Healing Wave (R4)",            spellId = 913,     cost = 3200 },
    { level = 18, name = "Stoneclaw Totem (R2)",         spellId = 6390,    cost = 3200 },
    { level = 18, name = "Tremor Totem",                 spellId = 8143,    cost = 3200 },
    { level = 20, name = "Frost Shock",                  spellId = 8056,    cost = 3400 },
    { level = 20, name = "Frostbrand Weapon",            spellId = 8033,    cost = 3400 },
    { level = 20, name = "Ghost Wolf",                   spellId = 2645,    cost = 3400 },
    { level = 20, name = "Healing Stream Totem",         spellId = 5394,    cost = 3400 },
    { level = 20, name = "Lesser Healing Wave",          spellId = 8004,    cost = 3400 },
    { level = 20, name = "Lightning Bolt (R4)",          spellId = 915,     cost = 3400 },
    { level = 20, name = "Searing Totem (R2)",           spellId = 6363,    cost = 3400,   requiresSpellId = SEARING_TOTEM_R1 },
    { level = 22, name = "Cure Disease",                 spellId = 2870,    cost = 5500 },
    { level = 22, name = "Fire Nova Totem (R2)",         spellId = 8498,    cost = 5500 },
    { level = 22, name = "Poison Cleansing Totem",       spellId = 8166,    cost = 5500 },
    { level = 22, name = "Water Breathing",              spellId = 131,     cost = 5500 },
    { level = 24, name = "Ancestral Spirit (R2)",        spellId = 20609,   cost = 6400 },
    { level = 24, name = "Earth Shock (R4)",             spellId = 8046,    cost = 6400 },
    { level = 24, name = "Frost Resistance Totem",       spellId = 8181,    cost = 6400 },
    { level = 24, name = "Healing Wave (R5)",            spellId = 939,     cost = 6400 },
    { level = 24, name = "Lightning Shield (R3)",        spellId = 905,     cost = 6400 },
    { level = 24, name = "Rockbiter Weapon (R4)",        spellId = 10399,   cost = 6400 },
    { level = 24, name = "Stoneskin Totem (R3)",         spellId = 8155,    cost = 6400 },
    { level = 24, name = "Strength of Earth Totem (R2)", spellId = 8160,    cost = 6400 },
    { level = 26, name = "Far Sight",                    spellId = 6196,    cost = 7600 },
    { level = 26, name = "Flametongue Weapon (R3)",      spellId = 8030,    cost = 7600 },
    { level = 26, name = "Lightning Bolt (R5)",          spellId = 943,     cost = 7600 },
    { level = 26, name = "Magma Totem",                  spellId = 8190,    cost = 7600 },
    { level = 26, name = "Mana Spring Totem",            spellId = 5675,    cost = 7600 },
    { level = 28, name = "Fire Resistance Totem",        spellId = 8184,    cost = 11000 },
    { level = 28, name = "Flame Shock (R3)",             spellId = 8053,    cost = 11000 },
    { level = 28, name = "Flametongue Totem",            spellId = 8227,    cost = 11000 },
    { level = 28, name = "Frostbrand Weapon (R2)",       spellId = 8038,    cost = 11000 },
    { level = 28, name = "Lesser Healing Wave (R2)",     spellId = 8008,    cost = 11000 },
    { level = 28, name = "Stoneclaw Totem (R3)",         spellId = 6391,    cost = 11000 },
    { level = 28, name = "Water Walking",                spellId = 546,     cost = 11000 },
    { level = 30, name = "Astral Recall",                spellId = 556,     cost = 12000 },
    { level = 30, name = "Grounding Totem",              spellId = 8177,    cost = 12000 },
    { level = 30, name = "Healing Stream Totem (R2)",    spellId = 6375,    cost = 12000 },
    { level = 30, name = "Nature Resistance Totem",      spellId = 10595,   cost = 12000 },
    { level = 30, name = "Reincarnation",                spellId = 20608,   cost = 12000 },
    { level = 30, name = "Searing Totem (R3)",           spellId = 6364,    cost = 12000,  requiresSpellId = SEARING_TOTEM_R1 },
    { level = 30, name = "Windfury Weapon",              spellId = 8232,    cost = 12000 },
    { level = 32, name = "Chain Lightning",              spellId = 421,     cost = 13000 },
    { level = 32, name = "Fire Nova Totem (R3)",         spellId = 8499,    cost = 13000 },
    { level = 32, name = "Healing Wave (R6)",            spellId = 959,     cost = 13000 },
    { level = 32, name = "Lightning Bolt (R6)",          spellId = 6041,    cost = 13000 },
    { level = 32, name = "Lightning Shield (R4)",        spellId = 945,     cost = 13000 },
    { level = 32, name = "Purge (R2)",                   spellId = 8012,    cost = 13000 },
    { level = 32, name = "Windfury Totem",               spellId = 8512,    cost = 13000 },
    { level = 34, name = "Frost Shock (R2)",             spellId = 8058,    cost = 14000 },
    { level = 34, name = "Rockbiter Weapon (R5)",        spellId = 16314,   cost = 14000 },
    { level = 34, name = "Sentry Totem",                 spellId = 6495,    cost = 14000 },
    { level = 34, name = "Stoneskin Totem (R4)",         spellId = 10406,   cost = 14000 },
    { level = 36, name = "Ancestral Spirit (R3)",        spellId = 20610,   cost = 18000 },
    { level = 36, name = "Earth Shock (R5)",             spellId = 10412,   cost = 18000 },
    { level = 36, name = "Flametongue Weapon (R4)",      spellId = 16339,   cost = 18000 },
    { level = 36, name = "Lesser Healing Wave (R3)",     spellId = 8010,    cost = 18000 },
    { level = 36, name = "Magma Totem (R2)",             spellId = 10585,   cost = 18000 },
    { level = 36, name = "Mana Spring Totem (R2)",       spellId = 10495,   cost = 18000 },
    { level = 36, name = "Windwall Totem",               spellId = 15107,   cost = 18000 },
    { level = 38, name = "Disease Cleansing Totem",      spellId = 8170,    cost = 19000 },
    { level = 38, name = "Flametongue Totem (R2)",       spellId = 8249,    cost = 19000 },
    { level = 38, name = "Frost Resistance Totem (R2)",  spellId = 10478,   cost = 19000 },
    { level = 38, name = "Frostbrand Weapon (R3)",       spellId = 10456,   cost = 19000 },
    { level = 38, name = "Lightning Bolt (R7)",          spellId = 10391,   cost = 19000 },
    { level = 38, name = "Stoneclaw Totem (R4)",         spellId = 6392,    cost = 19000 },
    { level = 38, name = "Strength of Earth Totem (R3)", spellId = 8161,    cost = 19000 },
    { level = 40, name = "Chain Heal",                   spellId = 1064,    cost = 23000 },
    { level = 40, name = "Chain Lightning (R2)",         spellId = 930,     cost = 23000 },
    { level = 40, name = "Flame Shock (R4)",             spellId = 10447,   cost = 23000 },
    { level = 40, name = "Healing Stream Totem (R3)",    spellId = 6377,    cost = 23000 },
    { level = 40, name = "Healing Wave (R7)",            spellId = 8005,    cost = 23000 },
    { level = 40, name = "Lightning Shield (R5)",        spellId = 8134,    cost = 23000 },
    { level = 40, name = "Mail",                         spellId = 8737,    cost = 23000 },
    { level = 40, name = "Searing Totem (R4)",           spellId = 6365,    cost = 23000,  requiresSpellId = SEARING_TOTEM_R1 },
    { level = 40, name = "Windfury Weapon (R2)",         spellId = 8235,    cost = 23000 },
    { level = 42, name = "Fire Nova Totem (R4)",         spellId = 11314,   cost = 30000 },
    { level = 42, name = "Fire Resistance Totem (R2)",   spellId = 10537,   cost = 30000 },
    { level = 42, name = "Grace of Air Totem",           spellId = 8835,    cost = 30000 },
    { level = 42, name = "Windfury Totem (R2)",          spellId = 10613,   cost = 30000 },
    { level = 44, name = "Lesser Healing Wave (R4)",     spellId = 10466,   cost = 31000 },
    { level = 44, name = "Lightning Bolt (R8)",          spellId = 10392,   cost = 31000 },
    { level = 44, name = "Nature Resistance Totem (R2)", spellId = 10600,   cost = 31000 },
    { level = 44, name = "Rockbiter Weapon (R6)",        spellId = 16315,   cost = 31000 },
    { level = 44, name = "Stoneskin Totem (R5)",         spellId = 10407,   cost = 31000 },
    { level = 46, name = "Chain Heal (R2)",              spellId = 10622,   cost = 32000 },
    { level = 46, name = "Flametongue Weapon (R5)",      spellId = 16341,   cost = 32000 },
    { level = 46, name = "Frost Shock (R3)",             spellId = 10472,   cost = 32000 },
    { level = 46, name = "Magma Totem (R3)",             spellId = 10586,   cost = 32000 },
    { level = 46, name = "Mana Spring Totem (R3)",       spellId = 10496,   cost = 32000 },
    { level = 46, name = "Windwall Totem (R2)",          spellId = 15111,   cost = 32000 },
    { level = 48, name = "Ancestral Spirit (R4)",        spellId = 20776,   cost = 33000 },
    { level = 48, name = "Chain Lightning (R3)",         spellId = 2860,    cost = 33000 },
    { level = 48, name = "Earth Shock (R6)",             spellId = 10413,   cost = 33000 },
    { level = 48, name = "Flametongue Totem (R3)",       spellId = 10526,   cost = 33000 },
    { level = 48, name = "Frostbrand Weapon (R4)",       spellId = 16355,   cost = 33000 },
    { level = 48, name = "Healing Wave (R8)",            spellId = 10395,   cost = 33000 },
    { level = 48, name = "Lightning Shield (R6)",        spellId = 10431,   cost = 33000 },
    { level = 48, name = "Mana Tide Totem (R2)",         spellId = 17354,   cost = 33000,  requiresSpellId = MANA_TIDE_TOTEM_R1 },
    { level = 48, name = "Stoneclaw Totem (R5)",         spellId = 10427,   cost = 33000 },
    { level = 50, name = "Healing Stream Totem (R4)",    spellId = 10462,   cost = 36000 },
    { level = 50, name = "Lightning Bolt (R9)",          spellId = 15207,   cost = 36000 },
    { level = 50, name = "Searing Totem (R5)",           spellId = 10437,   cost = 36000,  requiresSpellId = SEARING_TOTEM_R1 },
    { level = 50, name = "Tranquil Air Totem",           spellId = 25908,   cost = 36000 },
    { level = 50, name = "Windfury Weapon (R3)",         spellId = 10486,   cost = 36000 },
    { level = 52, name = "Fire Nova Totem (R5)",         spellId = 11315,   cost = 39000 },
    { level = 52, name = "Flame Shock (R5)",             spellId = 10448,   cost = 39000 },
    { level = 52, name = "Lesser Healing Wave (R5)",     spellId = 10467,   cost = 39000 },
    { level = 52, name = "Strength of Earth Totem (R4)", spellId = 10442,   cost = 39000 },
    { level = 52, name = "Windfury Totem (R3)",          spellId = 10614,   cost = 39000 },
    { level = 54, name = "Chain Heal (R3)",              spellId = 10623,   cost = 40000 },
    { level = 54, name = "Frost Resistance Totem (R3)",  spellId = 10479,   cost = 40000 },
    { level = 54, name = "Rockbiter Weapon (R7)",        spellId = 16316,   cost = 40000 },
    { level = 54, name = "Stoneskin Totem (R6)",         spellId = 10408,   cost = 40000 },
    { level = 56, name = "Chain Lightning (R4)",         spellId = 10605,   cost = 42000 },
    { level = 56, name = "Flametongue Weapon (R6)",      spellId = 16342,   cost = 42000 },
    { level = 56, name = "Grace of Air Totem (R2)",      spellId = 10627,   cost = 42000 },
    { level = 56, name = "Healing Wave (R9)",            spellId = 10396,   cost = 42000 },
    { level = 56, name = "Lightning Bolt (R10)",         spellId = 15208,   cost = 42000 },
    { level = 56, name = "Lightning Shield (R7)",        spellId = 10432,   cost = 42000 },
    { level = 56, name = "Magma Totem (R4)",             spellId = 10587,   cost = 42000 },
    { level = 56, name = "Mana Spring Totem (R4)",       spellId = 10497,   cost = 42000 },
    { level = 56, name = "Nature Resistance Totem (R3)", spellId = 10601,   cost = 42000 },
    { level = 56, name = "Windwall Totem (R3)",          spellId = 15112,   cost = 42000 },
    { level = 58, name = "Fire Resistance Totem (R3)",   spellId = 10538,   cost = 48000 },
    { level = 58, name = "Flametongue Totem (R4)",       spellId = 16387,   cost = 48000 },
    { level = 58, name = "Frost Shock (R4)",             spellId = 10473,   cost = 48000 },
    { level = 58, name = "Frostbrand Weapon (R5)",       spellId = 16356,   cost = 48000 },
    { level = 58, name = "Mana Tide Totem (R3)",         spellId = 17359,   cost = 48000,  requiresSpellId = MANA_TIDE_TOTEM_R1 },
    { level = 58, name = "Stoneclaw Totem (R6)",         spellId = 10428,   cost = 48000 },
    { level = 60, name = "Ancestral Spirit (R5)",        spellId = 20777,   cost = 49000 },
    { level = 60, name = "Earth Shock (R7)",             spellId = 10414,   cost = 49000 },
    { level = 60, name = "Flame Shock (R6)",             spellId = 29228,   cost = 49000 },
    { level = 60, name = "Grace of Air Totem (R3)",      spellId = 25359,   cost = 49000 },
    { level = 60, name = "Healing Stream Totem (R5)",    spellId = 10463,   cost = 49000 },
    { level = 60, name = "Healing Wave (R10)",           spellId = 25357,   cost = 49000 },
    { level = 60, name = "Lesser Healing Wave (R6)",     spellId = 10468,   cost = 49000 },
    { level = 60, name = "Searing Totem (R6)",           spellId = 10438,   cost = 49000,  requiresSpellId = SEARING_TOTEM_R1 },
    { level = 60, name = "Strength of Earth Totem (R5)", spellId = 25361,   cost = 49000 },
    { level = 60, name = "Windfury Weapon (R4)",         spellId = 16362,   cost = 49000 },
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

-- Shaman macros for CreateOrUpdateMacros() (WoWFunctions.lua) -- the source of
-- truth for what's behind each WowShamanHandler.cs Input key.
SHAMAN_MACROS = {
    { name = "2 Bolt", lines = {
        "#showtooltip [mod:shift] Lightning Bolt(Rank 1); Lightning Bolt",
        "/cast [nomod] Lightning Bolt",
        "/cast [mod:shift] Lightning Bolt(Rank 1)",
    } },
    { name = "3 Shock", lines = {
        "#showtooltip [mod:shift] Flame Shock; Earth Shock",
        "/use [nomod] Earth Shock",
        "/use [mod:shift] Flame Shock",
    } },
    { name = "4 Buff", lines = {
        "#showtooltip [mod:shift] Rockbiter Weapon; Lightning Shield",
        "/use [nomod] Lightning Shield",
        "/use [mod:shift] Rockbiter Weapon",
    } },
    { name = "5 Cure", lines = {
        "#showtooltip [mod:shift] Cure Disease; Cure Poison",
        "/use [nomod] Cure Poison",
        "/use [mod:shift] Cure Disease",
    } },
    { name = "6 Frost Shock", lines = {
        "#showtooltip Frost Shock",
        "/cast Frost Shock",
    } },
}
