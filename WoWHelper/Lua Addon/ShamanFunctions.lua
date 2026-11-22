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
-- (WarriorFunctions.lua). Levels 1-60, generated from a /yyspells dump of a
-- WoW Forever (client 1.60.1) Shaman trainer -- every row the trainer lists,
-- with its real cost and spell ID. Rows the character had already learned
-- came back level=0, so their levels are the Classic Era ones (matched by
-- spell ID) or, for Forever-only spells, the level of another row at the
-- same cost. Forever differs from Classic Era here: Fire Nova is a spell
-- (new IDs), not a totem; Call of the Elements/Ancestors/Spirits, Totemic
-- Recall/Call and Riptide are added; Mana Tide's ranks move to 38/48/58;
-- Healing Stream Totem rank 1 isn't on the trainer; and Tranquil Air Totem
-- and the 1.11+ level-60 ranks (Flame Shock 6, Healing Wave 10, Strength of
-- Earth 5, Grace of Air 3) are gone. To refresh it, re-run /yyspells at a
-- trainer and regenerate from the dump rather than editing by hand. Rank
-- labels are inferred from row order.
--
-- Absent (not trainer rows): starting spells (Lightning Bolt/Healing Wave
-- rank 1), quest-granted totem rank 1s (Stoneskin, Searing, Healing Stream),
-- talent rank 1s (Mana Tide, Riptide), and Water Shield.
-- requiresSpellId on a talent/quest spell's later ranks points at its first
-- trainer row's own (Forever-verified) ID; GetUnlearnedTrainerSpells only
-- uses it to get the spell's name, so the gate means "knows any rank of it".
SHAMAN_TRAINER_SPELLS = {
    { level = 1,  name = "Rockbiter Weapon",             spellId = 8017,    cost = 9 },
    { level = 4,  name = "Earth Shock",                  spellId = 8042,    cost = 90 },
    { level = 6,  name = "Earthbind Totem",              spellId = 2484,    cost = 90 },
    { level = 6,  name = "Healing Wave (R2)",            spellId = 332,     cost = 90 },
    { level = 8,  name = "Earth Shock (R2)",             spellId = 8044,    cost = 90 },
    { level = 8,  name = "Lightning Bolt (R2)",          spellId = 529,     cost = 90 },
    { level = 8,  name = "Lightning Shield",             spellId = 324,     cost = 90 },
    { level = 8,  name = "Rockbiter Weapon (R2)",        spellId = 8018,    cost = 90 },
    { level = 8,  name = "Stoneclaw Totem",              spellId = 5730,    cost = 90 },
    { level = 10, name = "Flame Shock",                  spellId = 8050,    cost = 360 },
    { level = 10, name = "Flametongue Weapon",           spellId = 8024,    cost = 360 },
    { level = 10, name = "Strength of Earth Totem",      spellId = 8075,    cost = 360 },
    { level = 12, name = "Ancestral Spirit",             spellId = 2008,    cost = 630 },
    { level = 12, name = "Fire Nova",                    spellId = 408341,  cost = 630 },
    { level = 12, name = "Healing Wave (R3)",            spellId = 547,     cost = 630 },
    { level = 12, name = "Purge",                        spellId = 370,     cost = 630 },
    { level = 14, name = "Earth Shock (R3)",             spellId = 8045,    cost = 810 },
    { level = 14, name = "Lightning Bolt (R3)",          spellId = 548,     cost = 810 },
    { level = 14, name = "Stoneskin Totem (R2)",         spellId = 8154,    cost = 810,    requiresSpellId = 8154 },
    { level = 16, name = "Cure Poison",                  spellId = 526,     cost = 990 },
    { level = 16, name = "Lightning Shield (R2)",        spellId = 325,     cost = 990 },
    { level = 16, name = "Rockbiter Weapon (R3)",        spellId = 8019,    cost = 990 },
    { level = 18, name = "Flame Shock (R2)",             spellId = 8052,    cost = 1080 },
    { level = 18, name = "Flametongue Weapon (R2)",      spellId = 8027,    cost = 1080 },
    { level = 18, name = "Healing Wave (R4)",            spellId = 913,     cost = 1080 },
    { level = 18, name = "Stoneclaw Totem (R2)",         spellId = 6390,    cost = 1080 },
    { level = 18, name = "Tremor Totem",                 spellId = 8143,    cost = 1080 },
    { level = 20, name = "Call of the Elements",         spellId = 66842,   cost = 2070 },
    { level = 20, name = "Frost Shock",                  spellId = 8056,    cost = 2070 },
    { level = 20, name = "Frostbrand Weapon",            spellId = 8033,    cost = 2070 },
    { level = 20, name = "Ghost Wolf",                   spellId = 2645,    cost = 2070 },
    { level = 20, name = "Lesser Healing Wave",          spellId = 8004,    cost = 2070 },
    { level = 20, name = "Lightning Bolt (R4)",          spellId = 915,     cost = 2070 },
    { level = 20, name = "Searing Totem (R2)",           spellId = 6363,    cost = 2070,   requiresSpellId = 6363 },
    { level = 20, name = "Totemic Recall",               spellId = 36936,   cost = 2070 },
    { level = 22, name = "Cure Disease",                 spellId = 2870,    cost = 2970 },
    { level = 22, name = "Fire Nova (R2)",               spellId = 408342,  cost = 2970 },
    { level = 22, name = "Poison Cleansing Totem",       spellId = 8166,    cost = 2970 },
    { level = 22, name = "Totemic Call",                 spellId = 437009,  cost = 2970 },
    { level = 22, name = "Water Breathing",              spellId = 131,     cost = 2970 },
    { level = 24, name = "Ancestral Spirit (R2)",        spellId = 20609,   cost = 3060 },
    { level = 24, name = "Earth Shock (R4)",             spellId = 8046,    cost = 3060 },
    { level = 24, name = "Frost Resistance Totem",       spellId = 8181,    cost = 3060 },
    { level = 24, name = "Healing Wave (R5)",            spellId = 939,     cost = 3060 },
    { level = 24, name = "Lightning Shield (R3)",        spellId = 905,     cost = 3060 },
    { level = 24, name = "Rockbiter Weapon (R4)",        spellId = 10399,   cost = 3060 },
    { level = 24, name = "Stoneskin Totem (R3)",         spellId = 8155,    cost = 3060,   requiresSpellId = 8154 },
    { level = 24, name = "Strength of Earth Totem (R2)", spellId = 8160,    cost = 3060 },
    { level = 26, name = "Far Sight",                    spellId = 6196,    cost = 3870 },
    { level = 26, name = "Flametongue Weapon (R3)",      spellId = 8030,    cost = 3870 },
    { level = 26, name = "Lightning Bolt (R5)",          spellId = 943,     cost = 3870 },
    { level = 26, name = "Magma Totem",                  spellId = 8190,    cost = 3870 },
    { level = 26, name = "Mana Spring Totem",            spellId = 5675,    cost = 3870 },
    { level = 28, name = "Fire Resistance Totem",        spellId = 8184,    cost = 4500 },
    { level = 28, name = "Flame Shock (R3)",             spellId = 8053,    cost = 4500 },
    { level = 28, name = "Flametongue Totem",            spellId = 8227,    cost = 4500 },
    { level = 28, name = "Frostbrand Weapon (R2)",       spellId = 8038,    cost = 4500 },
    { level = 28, name = "Lesser Healing Wave (R2)",     spellId = 8008,    cost = 4500 },
    { level = 28, name = "Stoneclaw Totem (R3)",         spellId = 6391,    cost = 4500 },
    { level = 28, name = "Water Walking",                spellId = 546,     cost = 4500 },
    { level = 30, name = "Astral Recall",                spellId = 556,     cost = 5130 },
    { level = 30, name = "Call of the Ancestors",        spellId = 66843,   cost = 5130 },
    { level = 30, name = "Grounding Totem",              spellId = 8177,    cost = 5130 },
    { level = 30, name = "Healing Stream Totem (R2)",    spellId = 6375,    cost = 5130,   requiresSpellId = 6375 },
    { level = 30, name = "Nature Resistance Totem",      spellId = 10595,   cost = 5130 },
    { level = 30, name = "Reincarnation",                spellId = 20608,   cost = 5130 },
    { level = 30, name = "Searing Totem (R3)",           spellId = 6364,    cost = 5130,   requiresSpellId = 6363 },
    { level = 30, name = "Windfury Weapon",              spellId = 8232,    cost = 5130 },
    { level = 32, name = "Chain Lightning",              spellId = 421,     cost = 6300 },
    { level = 32, name = "Fire Nova (R3)",               spellId = 408343,  cost = 6300 },
    { level = 32, name = "Healing Wave (R6)",            spellId = 959,     cost = 6300 },
    { level = 32, name = "Lightning Bolt (R6)",          spellId = 6041,    cost = 6300 },
    { level = 32, name = "Lightning Shield (R4)",        spellId = 945,     cost = 6300 },
    { level = 32, name = "Purge (R2)",                   spellId = 8012,    cost = 6300 },
    { level = 32, name = "Windfury Totem",               spellId = 8512,    cost = 6300 },
    { level = 34, name = "Frost Shock (R2)",             spellId = 8058,    cost = 7200 },
    { level = 34, name = "Rockbiter Weapon (R5)",        spellId = 16314,   cost = 7200 },
    { level = 34, name = "Sentry Totem",                 spellId = 6495,    cost = 7200 },
    { level = 34, name = "Stoneskin Totem (R4)",         spellId = 10406,   cost = 7200,   requiresSpellId = 8154 },
    { level = 36, name = "Ancestral Spirit (R3)",        spellId = 20610,   cost = 8100 },
    { level = 36, name = "Earth Shock (R5)",             spellId = 10412,   cost = 8100 },
    { level = 36, name = "Flametongue Weapon (R4)",      spellId = 16339,   cost = 8100 },
    { level = 36, name = "Lesser Healing Wave (R3)",     spellId = 8010,    cost = 8100 },
    { level = 36, name = "Magma Totem (R2)",             spellId = 10585,   cost = 8100 },
    { level = 36, name = "Mana Spring Totem (R2)",       spellId = 10495,   cost = 8100 },
    { level = 36, name = "Windwall Totem",               spellId = 15107,   cost = 8100 },
    { level = 38, name = "Disease Cleansing Totem",      spellId = 8170,    cost = 9000 },
    { level = 38, name = "Flametongue Totem (R2)",       spellId = 8249,    cost = 9000 },
    { level = 38, name = "Frost Resistance Totem (R2)",  spellId = 10478,   cost = 9000 },
    { level = 38, name = "Frostbrand Weapon (R3)",       spellId = 10456,   cost = 9000 },
    { level = 38, name = "Lightning Bolt (R7)",          spellId = 10391,   cost = 9000 },
    { level = 38, name = "Mana Tide Totem (R2)",         spellId = 17354,   cost = 9000,   requiresSpellId = 17354 },
    { level = 38, name = "Stoneclaw Totem (R4)",         spellId = 6392,    cost = 9000 },
    { level = 38, name = "Strength of Earth Totem (R3)", spellId = 8161,    cost = 9000 },
    { level = 40, name = "Call of the Spirits",          spellId = 66844,   cost = 10800 },
    { level = 40, name = "Chain Heal",                   spellId = 1064,    cost = 10800 },
    { level = 40, name = "Chain Lightning (R2)",         spellId = 930,     cost = 10800 },
    { level = 40, name = "Flame Shock (R4)",             spellId = 10447,   cost = 10800 },
    { level = 40, name = "Healing Stream Totem (R3)",    spellId = 6377,    cost = 10800,  requiresSpellId = 6375 },
    { level = 40, name = "Healing Wave (R7)",            spellId = 8005,    cost = 10800 },
    { level = 40, name = "Lightning Shield (R5)",        spellId = 8134,    cost = 10800 },
    { level = 40, name = "Mail",                         spellId = 8737,    cost = 10800 },
    { level = 40, name = "Searing Totem (R4)",           spellId = 6365,    cost = 10800,  requiresSpellId = 6363 },
    { level = 40, name = "Windfury Weapon (R2)",         spellId = 8235,    cost = 10800 },
    { level = 42, name = "Fire Nova (R4)",               spellId = 408344,  cost = 16200 },
    { level = 42, name = "Fire Resistance Totem (R2)",   spellId = 10537,   cost = 16200 },
    { level = 42, name = "Grace of Air Totem",           spellId = 8835,    cost = 16200 },
    { level = 42, name = "Windfury Totem (R2)",          spellId = 10613,   cost = 16200 },
    { level = 44, name = "Lesser Healing Wave (R4)",     spellId = 10466,   cost = 17100 },
    { level = 44, name = "Lightning Bolt (R8)",          spellId = 10392,   cost = 17100 },
    { level = 44, name = "Nature Resistance Totem (R2)", spellId = 10600,   cost = 17100 },
    { level = 44, name = "Rockbiter Weapon (R6)",        spellId = 16315,   cost = 17100 },
    { level = 44, name = "Stoneskin Totem (R5)",         spellId = 10407,   cost = 17100,  requiresSpellId = 8154 },
    { level = 46, name = "Chain Heal (R2)",              spellId = 10622,   cost = 18000 },
    { level = 46, name = "Flametongue Weapon (R5)",      spellId = 16341,   cost = 18000 },
    { level = 46, name = "Frost Shock (R3)",             spellId = 10472,   cost = 18000 },
    { level = 46, name = "Magma Totem (R3)",             spellId = 10586,   cost = 18000 },
    { level = 46, name = "Mana Spring Totem (R3)",       spellId = 10496,   cost = 18000 },
    { level = 46, name = "Windwall Totem (R2)",          spellId = 15111,   cost = 18000 },
    { level = 48, name = "Ancestral Spirit (R4)",        spellId = 20776,   cost = 18900 },
    { level = 48, name = "Chain Lightning (R3)",         spellId = 2860,    cost = 18900 },
    { level = 48, name = "Earth Shock (R6)",             spellId = 10413,   cost = 18900 },
    { level = 48, name = "Flametongue Totem (R3)",       spellId = 10526,   cost = 18900 },
    { level = 48, name = "Frostbrand Weapon (R4)",       spellId = 16355,   cost = 18900 },
    { level = 48, name = "Healing Wave (R8)",            spellId = 10395,   cost = 18900 },
    { level = 48, name = "Lightning Shield (R6)",        spellId = 10431,   cost = 18900 },
    { level = 48, name = "Mana Tide Totem (R3)",         spellId = 17359,   cost = 18900,  requiresSpellId = 17354 },
    { level = 48, name = "Stoneclaw Totem (R5)",         spellId = 10427,   cost = 18900 },
    { level = 50, name = "Healing Stream Totem (R4)",    spellId = 10462,   cost = 22500,  requiresSpellId = 6375 },
    { level = 50, name = "Lightning Bolt (R9)",          spellId = 15207,   cost = 22500 },
    { level = 50, name = "Riptide (R2)",                 spellId = 1239242, cost = 22500,  requiresSpellId = 1239242 },
    { level = 50, name = "Searing Totem (R5)",           spellId = 10437,   cost = 22500,  requiresSpellId = 6363 },
    { level = 50, name = "Windfury Weapon (R3)",         spellId = 10486,   cost = 22500 },
    { level = 52, name = "Fire Nova (R5)",               spellId = 408345,  cost = 23400 },
    { level = 52, name = "Flame Shock (R5)",             spellId = 10448,   cost = 23400 },
    { level = 52, name = "Lesser Healing Wave (R5)",     spellId = 10467,   cost = 23400 },
    { level = 52, name = "Strength of Earth Totem (R4)", spellId = 10442,   cost = 23400 },
    { level = 52, name = "Windfury Totem (R3)",          spellId = 10614,   cost = 23400 },
    { level = 54, name = "Chain Heal (R3)",              spellId = 10623,   cost = 24300 },
    { level = 54, name = "Frost Resistance Totem (R3)",  spellId = 10479,   cost = 24300 },
    { level = 54, name = "Rockbiter Weapon (R7)",        spellId = 16316,   cost = 24300 },
    { level = 54, name = "Stoneskin Totem (R6)",         spellId = 10408,   cost = 24300,  requiresSpellId = 8154 },
    { level = 56, name = "Chain Lightning (R4)",         spellId = 10605,   cost = 25200 },
    { level = 56, name = "Flametongue Weapon (R6)",      spellId = 16342,   cost = 25200 },
    { level = 56, name = "Grace of Air Totem (R2)",      spellId = 10627,   cost = 25200 },
    { level = 56, name = "Healing Wave (R9)",            spellId = 10396,   cost = 25200 },
    { level = 56, name = "Lightning Bolt (R10)",         spellId = 15208,   cost = 25200 },
    { level = 56, name = "Lightning Shield (R7)",        spellId = 10432,   cost = 25200 },
    { level = 56, name = "Magma Totem (R4)",             spellId = 10587,   cost = 25200 },
    { level = 56, name = "Mana Spring Totem (R4)",       spellId = 10497,   cost = 25200 },
    { level = 56, name = "Windwall Totem (R3)",          spellId = 15112,   cost = 25200 },
    { level = 58, name = "Fire Resistance Totem (R3)",   spellId = 10538,   cost = 28800 },
    { level = 58, name = "Flametongue Totem (R4)",       spellId = 16387,   cost = 28800 },
    { level = 58, name = "Frost Shock (R4)",             spellId = 10473,   cost = 28800 },
    { level = 58, name = "Frostbrand Weapon (R5)",       spellId = 16356,   cost = 28800 },
    { level = 58, name = "Mana Tide Totem (R4)",         spellId = 1325256, cost = 28800,  requiresSpellId = 17354 },
    { level = 58, name = "Stoneclaw Totem (R6)",         spellId = 10428,   cost = 28800 },
    { level = 60, name = "Ancestral Spirit (R5)",        spellId = 20777,   cost = 29700 },
    { level = 60, name = "Earth Shock (R7)",             spellId = 10414,   cost = 29700 },
    { level = 60, name = "Healing Stream Totem (R5)",    spellId = 10463,   cost = 29700,  requiresSpellId = 6375 },
    { level = 60, name = "Lesser Healing Wave (R6)",     spellId = 10468,   cost = 29700 },
    { level = 60, name = "Nature Resistance Totem (R3)", spellId = 10601,   cost = 29700 },
    { level = 60, name = "Riptide (R3)",                 spellId = 1239243, cost = 29700,  requiresSpellId = 1239242 },
    { level = 60, name = "Searing Totem (R6)",           spellId = 10438,   cost = 29700,  requiresSpellId = 6363 },
    { level = 60, name = "Windfury Weapon (R4)",         spellId = 16362,   cost = 29700 },
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
    "CanSpellcastPullTarget",     -- 6 (R6) WoWFunctions.lua -- shared with other casters
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
