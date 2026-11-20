------------------------------------------------------------
-- Warlock-specific game-state checks, and Warlock's Class pixel-row field
-- lists (WARLOCK_CLASS_BOOL_ONE/TWO_FIELDS, bottom of this file).
--
-- Note: CanSpellcastPullTarget() stays in WoWFunctions.lua -- it's shared
-- between Shaman/Warlock (branches internally on class); giving it the same
-- name here would collide (last file loaded in the .toc silently wins,
-- since addon globals are one flat namespace).
------------------------------------------------------------

-- Class trainer spells -- same shape as WARRIOR_TRAINER_SPELLS
-- (WarriorFunctions.lua). Levels 1-60, built from the Classic Era trainer
-- schedule (levels and rank spell IDs) with costs taken from the Forever
-- per-level prices in WARRIOR_TRAINER_SPELLS -- NOT from a /yyspells dump, so
-- any Forever-specific changes to the Warlock trainer aren't reflected yet.
-- Regenerate from a /yyspells dump at a Forever Warlock trainer when possible.
-- Absent: starting spells (Shadow Bolt/Demon Skin rank 1, Summon Imp),
-- quest-granted demons (Voidwalker/Succubus/Felhunter), mounts, and talent
-- rank 1s. Classic Era's book-only ranks (e.g. Shadow Bolt rank 10) aren't
-- trainer rows either.
local SHADOWBURN_R1 = 17877
local SIPHON_LIFE_R1 = 18265
local CONFLAGRATE_R1 = 17962
local DARK_PACT_R1 = 18220
WARLOCK_TRAINER_SPELLS = {
    { level = 1,  name = "Immolate",                     spellId = 348,     cost = 10 },
    { level = 4,  name = "Corruption",                   spellId = 172,     cost = 100 },
    { level = 4,  name = "Curse of Weakness",            spellId = 702,     cost = 100 },
    { level = 6,  name = "Life Tap",                     spellId = 1454,    cost = 100 },
    { level = 6,  name = "Shadow Bolt (R2)",             spellId = 695,     cost = 100 },
    { level = 8,  name = "Curse of Agony",               spellId = 980,     cost = 200 },
    { level = 8,  name = "Fear",                         spellId = 5782,    cost = 200 },
    { level = 10, name = "Create Healthstone (Minor)",   spellId = 6201,    cost = 700 },
    { level = 10, name = "Demon Skin (R2)",              spellId = 696,     cost = 700 },
    { level = 10, name = "Drain Soul",                   spellId = 1120,    cost = 700 },
    { level = 10, name = "Immolate (R2)",                spellId = 707,     cost = 700 },
    { level = 12, name = "Curse of Weakness (R2)",       spellId = 1108,    cost = 900 },
    { level = 12, name = "Health Funnel",                spellId = 755,     cost = 900 },
    { level = 12, name = "Shadow Bolt (R3)",             spellId = 705,     cost = 900 },
    { level = 14, name = "Corruption (R2)",              spellId = 6222,    cost = 1100 },
    { level = 14, name = "Curse of Recklessness",        spellId = 704,     cost = 1100 },
    { level = 14, name = "Drain Life",                   spellId = 689,     cost = 1100 },
    { level = 16, name = "Life Tap (R2)",                spellId = 1455,    cost = 1500 },
    { level = 16, name = "Unending Breath",              spellId = 5697,    cost = 1500 },
    { level = 18, name = "Create Soulstone (Minor)",     spellId = 693,     cost = 3200 },
    { level = 18, name = "Curse of Agony (R2)",          spellId = 1014,    cost = 3200 },
    { level = 18, name = "Searing Pain",                 spellId = 5676,    cost = 3200 },
    { level = 20, name = "Demon Armor",                  spellId = 706,     cost = 3400 },
    { level = 20, name = "Health Funnel (R2)",           spellId = 3698,    cost = 3400 },
    { level = 20, name = "Immolate (R3)",                spellId = 1094,    cost = 3400 },
    { level = 20, name = "Rain of Fire",                 spellId = 5740,    cost = 3400 },
    { level = 20, name = "Ritual of Summoning",          spellId = 698,     cost = 3400 },
    { level = 20, name = "Shadow Bolt (R4)",             spellId = 1088,    cost = 3400 },
    { level = 22, name = "Create Healthstone (Lesser)",  spellId = 6202,    cost = 5500 },
    { level = 22, name = "Curse of Weakness (R3)",       spellId = 6205,    cost = 5500 },
    { level = 22, name = "Drain Life (R2)",              spellId = 699,     cost = 5500 },
    { level = 22, name = "Eye of Kilrogg",               spellId = 126,     cost = 5500 },
    { level = 24, name = "Corruption (R3)",              spellId = 6223,    cost = 6400 },
    { level = 24, name = "Drain Soul (R2)",              spellId = 8288,    cost = 6400 },
    { level = 24, name = "Sense Demons",                 spellId = 5500,    cost = 6400 },
    { level = 24, name = "Shadowburn (R2)",              spellId = 18867,   cost = 6400,   requiresSpellId = SHADOWBURN_R1 },
    { level = 26, name = "Curse of Tongues",             spellId = 1714,    cost = 7600 },
    { level = 26, name = "Detect Lesser Invisibility",   spellId = 132,     cost = 7600 },
    { level = 26, name = "Life Tap (R3)",                spellId = 1456,    cost = 7600 },
    { level = 26, name = "Searing Pain (R2)",            spellId = 17919,   cost = 7600 },
    { level = 28, name = "Banish",                       spellId = 710,     cost = 11000 },
    { level = 28, name = "Create Firestone (Lesser)",    spellId = 6366,    cost = 11000 },
    { level = 28, name = "Curse of Agony (R3)",          spellId = 6217,    cost = 11000 },
    { level = 28, name = "Curse of Recklessness (R2)",   spellId = 7658,    cost = 11000 },
    { level = 28, name = "Health Funnel (R3)",           spellId = 3699,    cost = 11000 },
    { level = 28, name = "Shadow Bolt (R5)",             spellId = 1106,    cost = 11000 },
    { level = 30, name = "Create Soulstone (Lesser)",    spellId = 20752,   cost = 12000 },
    { level = 30, name = "Demon Armor (R2)",             spellId = 1086,    cost = 12000 },
    { level = 30, name = "Drain Life (R3)",              spellId = 709,     cost = 12000 },
    { level = 30, name = "Enslave Demon",                spellId = 1098,    cost = 12000 },
    { level = 30, name = "Hellfire",                     spellId = 1949,    cost = 12000 },
    { level = 30, name = "Immolate (R4)",                spellId = 2941,    cost = 12000 },
    { level = 32, name = "Curse of the Elements",        spellId = 1490,    cost = 13000 },
    { level = 32, name = "Curse of Weakness (R4)",       spellId = 7646,    cost = 13000 },
    { level = 32, name = "Fear (R2)",                    spellId = 6213,    cost = 13000 },
    { level = 32, name = "Shadow Ward",                  spellId = 6229,    cost = 13000 },
    { level = 32, name = "Shadowburn (R3)",              spellId = 18868,   cost = 13000,  requiresSpellId = SHADOWBURN_R1 },
    { level = 34, name = "Corruption (R4)",              spellId = 7648,    cost = 14000 },
    { level = 34, name = "Create Healthstone",           spellId = 5699,    cost = 14000 },
    { level = 34, name = "Rain of Fire (R2)",            spellId = 6219,    cost = 14000 },
    { level = 34, name = "Searing Pain (R3)",            spellId = 17920,   cost = 14000 },
    { level = 36, name = "Create Firestone",             spellId = 17951,   cost = 18000 },
    { level = 36, name = "Create Spellstone",            spellId = 2362,    cost = 18000 },
    { level = 36, name = "Health Funnel (R4)",           spellId = 3700,    cost = 18000 },
    { level = 36, name = "Life Tap (R4)",                spellId = 11687,   cost = 18000 },
    { level = 36, name = "Shadow Bolt (R6)",             spellId = 7641,    cost = 18000 },
    { level = 38, name = "Curse of Agony (R4)",          spellId = 11711,   cost = 19000 },
    { level = 38, name = "Detect Invisibility",          spellId = 2970,    cost = 19000 },
    { level = 38, name = "Drain Life (R4)",              spellId = 7651,    cost = 19000 },
    { level = 38, name = "Drain Soul (R3)",              spellId = 8289,    cost = 19000 },
    { level = 38, name = "Siphon Life (R2)",             spellId = 18879,   cost = 19000,  requiresSpellId = SIPHON_LIFE_R1 },
    { level = 40, name = "Create Soulstone",             spellId = 20755,   cost = 23000 },
    { level = 40, name = "Demon Armor (R3)",             spellId = 11733,   cost = 23000 },
    { level = 40, name = "Howl of Terror",               spellId = 5484,    cost = 23000 },
    { level = 40, name = "Immolate (R5)",                spellId = 11665,   cost = 23000 },
    { level = 40, name = "Shadowburn (R4)",              spellId = 18869,   cost = 23000,  requiresSpellId = SHADOWBURN_R1 },
    { level = 42, name = "Curse of Recklessness (R3)",   spellId = 7659,    cost = 30000 },
    { level = 42, name = "Curse of Weakness (R5)",       spellId = 11707,   cost = 30000 },
    { level = 42, name = "Death Coil",                   spellId = 6789,    cost = 30000 },
    { level = 42, name = "Hellfire (R2)",                spellId = 11683,   cost = 30000 },
    { level = 42, name = "Searing Pain (R4)",            spellId = 17921,   cost = 30000 },
    { level = 42, name = "Shadow Ward (R2)",             spellId = 11739,   cost = 30000 },
    { level = 44, name = "Corruption (R5)",              spellId = 11671,   cost = 31000 },
    { level = 44, name = "Curse of Shadow",              spellId = 17862,   cost = 31000 },
    { level = 44, name = "Enslave Demon (R2)",           spellId = 11725,   cost = 31000 },
    { level = 44, name = "Health Funnel (R5)",           spellId = 11693,   cost = 31000 },
    { level = 44, name = "Shadow Bolt (R7)",             spellId = 11659,   cost = 31000 },
    { level = 46, name = "Create Firestone (Greater)",   spellId = 17952,   cost = 32000 },
    { level = 46, name = "Create Healthstone (Greater)", spellId = 11729,   cost = 32000 },
    { level = 46, name = "Curse of the Elements (R2)",   spellId = 11721,   cost = 32000 },
    { level = 46, name = "Drain Life (R5)",              spellId = 11699,   cost = 32000 },
    { level = 46, name = "Life Tap (R5)",                spellId = 11688,   cost = 32000 },
    { level = 46, name = "Rain of Fire (R3)",            spellId = 11677,   cost = 32000 },
    { level = 48, name = "Banish (R2)",                  spellId = 18647,   cost = 33000 },
    { level = 48, name = "Conflagrate (R2)",             spellId = 18930,   cost = 33000,  requiresSpellId = CONFLAGRATE_R1 },
    { level = 48, name = "Create Spellstone (Greater)",  spellId = 17727,   cost = 33000 },
    { level = 48, name = "Curse of Agony (R5)",          spellId = 11712,   cost = 33000 },
    { level = 48, name = "Shadowburn (R5)",              spellId = 18870,   cost = 33000,  requiresSpellId = SHADOWBURN_R1 },
    { level = 48, name = "Siphon Life (R3)",             spellId = 18880,   cost = 33000,  requiresSpellId = SIPHON_LIFE_R1 },
    { level = 48, name = "Soul Fire",                    spellId = 6353,    cost = 33000 },
    { level = 50, name = "Create Soulstone (Greater)",   spellId = 20756,   cost = 36000 },
    { level = 50, name = "Curse of Tongues (R2)",        spellId = 11719,   cost = 36000 },
    { level = 50, name = "Dark Pact (R2)",               spellId = 18937,   cost = 36000,  requiresSpellId = DARK_PACT_R1 },
    { level = 50, name = "Death Coil (R2)",              spellId = 17925,   cost = 36000 },
    { level = 50, name = "Demon Armor (R4)",             spellId = 11734,   cost = 36000 },
    { level = 50, name = "Detect Greater Invisibility",  spellId = 11743,   cost = 36000 },
    { level = 50, name = "Immolate (R6)",                spellId = 11667,   cost = 36000 },
    { level = 50, name = "Inferno",                      spellId = 1122,    cost = 36000 },
    { level = 50, name = "Searing Pain (R5)",            spellId = 17922,   cost = 36000 },
    { level = 52, name = "Curse of Weakness (R6)",       spellId = 11708,   cost = 39000 },
    { level = 52, name = "Drain Soul (R4)",              spellId = 11675,   cost = 39000 },
    { level = 52, name = "Health Funnel (R6)",           spellId = 11694,   cost = 39000 },
    { level = 52, name = "Shadow Bolt (R8)",             spellId = 11660,   cost = 39000 },
    { level = 52, name = "Shadow Ward (R3)",             spellId = 11740,   cost = 39000 },
    { level = 54, name = "Conflagrate (R3)",             spellId = 18931,   cost = 40000,  requiresSpellId = CONFLAGRATE_R1 },
    { level = 54, name = "Corruption (R6)",              spellId = 11672,   cost = 40000 },
    { level = 54, name = "Drain Life (R6)",              spellId = 11700,   cost = 40000 },
    { level = 54, name = "Hellfire (R3)",                spellId = 11684,   cost = 40000 },
    { level = 54, name = "Howl of Terror (R2)",          spellId = 17928,   cost = 40000 },
    { level = 56, name = "Create Firestone (Major)",     spellId = 17953,   cost = 42000 },
    { level = 56, name = "Curse of Recklessness (R4)",   spellId = 11717,   cost = 42000 },
    { level = 56, name = "Curse of Shadow (R2)",         spellId = 17937,   cost = 42000 },
    { level = 56, name = "Enslave Demon (R3)",           spellId = 11726,   cost = 42000 },
    { level = 56, name = "Fear (R3)",                    spellId = 6215,    cost = 42000 },
    { level = 56, name = "Life Tap (R6)",                spellId = 11689,   cost = 42000 },
    { level = 56, name = "Shadowburn (R6)",              spellId = 18871,   cost = 42000,  requiresSpellId = SHADOWBURN_R1 },
    { level = 56, name = "Soul Fire (R2)",               spellId = 17924,   cost = 42000 },
    { level = 58, name = "Create Healthstone (Major)",   spellId = 11730,   cost = 48000 },
    { level = 58, name = "Curse of Agony (R6)",          spellId = 11713,   cost = 48000 },
    { level = 58, name = "Death Coil (R3)",              spellId = 17926,   cost = 48000 },
    { level = 58, name = "Rain of Fire (R4)",            spellId = 11678,   cost = 48000 },
    { level = 58, name = "Searing Pain (R6)",            spellId = 17923,   cost = 48000 },
    { level = 58, name = "Siphon Life (R4)",             spellId = 18881,   cost = 48000,  requiresSpellId = SIPHON_LIFE_R1 },
    { level = 60, name = "Conflagrate (R4)",             spellId = 18932,   cost = 49000,  requiresSpellId = CONFLAGRATE_R1 },
    { level = 60, name = "Create Soulstone (Major)",     spellId = 20757,   cost = 49000 },
    { level = 60, name = "Create Spellstone (Major)",    spellId = 17728,   cost = 49000 },
    { level = 60, name = "Curse of Doom",                spellId = 603,     cost = 49000 },
    { level = 60, name = "Curse of the Elements (R3)",   spellId = 11722,   cost = 49000 },
    { level = 60, name = "Dark Pact (R3)",               spellId = 18938,   cost = 49000,  requiresSpellId = DARK_PACT_R1 },
    { level = 60, name = "Demon Armor (R5)",             spellId = 11735,   cost = 49000 },
    { level = 60, name = "Health Funnel (R7)",           spellId = 11695,   cost = 49000 },
    { level = 60, name = "Immolate (R7)",                spellId = 11668,   cost = 49000 },
    { level = 60, name = "Ritual of Doom",               spellId = 18540,   cost = 49000 },
    { level = 60, name = "Shadow Bolt (R9)",             spellId = 11661,   cost = 49000 },
    { level = 60, name = "Shadow Ward (R4)",             spellId = 28610,   cost = 49000 },
}

-- Demon Skin (687, known from level 1) and Demon Armor (706, replaces Demon
-- Skin at level 20) are two differently-named buffs, but only one is ever
-- active on the player at a time -- name-matched via IsSpellKnownByName()/
-- HasBuffNamed() (WoWFunctions.lua) rather than hardcoded spell IDs, same as
-- CanCurePoison/CanCureDisease in ShamanFunctions.lua, so this doesn't need
-- to rank-track which one the player currently knows.
--
-- Uses HasBuffNamedOrRemembered() (WoWFunctions.lua), same as HasBattleShout():
-- on Midnight-rules clients aura reads are blind in combat, so a buff from
-- before the pull keeps reading true until its remembered expiration. One cast
-- mid-combat is invisible here -- WowWarlockHandler covers that with its own
-- press time (DemonArmorPressTime).
function ShouldCastDemonArmor()
    if HasBuffNamedOrRemembered("Demon Skin") or HasBuffNamedOrRemembered("Demon Armor") then
        return false
    end

    return IsSpellKnownByName("Demon Armor") or IsSpellKnownByName("Demon Skin")
end

-- True if the player knows at least Summon Imp (the base, level-1 warlock pet
-- spell -- gates whether a pet can be summoned at all yet) and doesn't
-- currently have a living pet out (no pet summoned, or the pet died).
-- Name-matched via IsSpellKnownByName() (WoWFunctions.lua), same pattern as
-- ShouldCastDemonArmor() above.
function ShouldSummonPet()
    if not IsSpellKnownByName("Summon Imp") then
        return false
    end

    return not UnitExists("pet") or UnitIsDeadOrGhost("pet")
end

-- True if the player knows Immolate and the target doesn't already have the
-- Immolate DoT on it. Name-matched via IsSpellKnownByName()/
-- TargetHasDebuffSpellName() (both WoWFunctions.lua) rather than a hardcoded
-- spell ID/rank, same reasoning as ShouldCastDemonArmor() above -- avoids
-- needing to rank-track Immolate's spell ID as it gets relearned at higher
-- ranks. Same pattern ShamanFunctions.lua's ShouldCastFlameShock()/
-- TargetHasFlameShock() use for Flame Shock.
function ShouldCastImmolate()
    return IsSpellKnownByName("Immolate") and not TargetHasDebuffSpellName("Immolate")
end

-- True if the player knows Corruption and the target doesn't already have the
-- Corruption DoT on it. Same name-matched pattern as ShouldCastImmolate()
-- above.
function ShouldCastCorruption()
    return IsSpellKnownByName("Corruption") and not TargetHasDebuffSpellName("Corruption")
end

------------------------------------------------------------
-- Warlock's Class pixel rows -- see the field-list comment above
-- MULTI_BOOL_ONE_FIELDS (WoWFunctions.lua) for how these lists work. Decoded
-- on the C# side by WowWarlockClassState, in the same order.
------------------------------------------------------------
WARLOCK_CLASS_BOOL_ONE_FIELDS = {
    "CanSpellcastPullTarget",  -- 1 (R1) WoWFunctions.lua -- shared with Shaman
    "ShouldCastDemonArmor",    -- 2 (R2)
    "ShouldSummonPet",         -- 3 (R3)
    "ShouldCastImmolate",      -- 4 (R4)
    "ShouldCastCorruption",    -- 5 (R5)
}

WARLOCK_CLASS_BOOL_TWO_FIELDS = {}

-- Warlock macros for CreateOrUpdateMacros() (WoWFunctions.lua) -- the source of
-- truth for what's behind each WowWarlockHandler.cs Input key.
WARLOCK_MACROS = {
    { name = "2 Bolt", lines = {
        "#showtooltip Shadow Bolt",
        "/cast Shadow Bolt",
    } },
    { name = "3 Dot", lines = {
        "#showtooltip [mod:shift] Immolate; Corruption",
        "/use [nomod] Corruption",
        "/use [mod:shift] Immolate",
    } },
    { name = "4 Buff", lines = {
        "#showtooltip [mod:shift, known:Demon Armor] Demon Armor; [mod:shift, noknown:Demon Armor] Demon Skin; Summon Imp",
        "/use [nomod] Summon Imp",
        "/use [mod:shift, known:Demon Armor] Demon Armor",
        "/use [mod:shift, noknown:Demon Armor] Demon Skin",
    } },
}
