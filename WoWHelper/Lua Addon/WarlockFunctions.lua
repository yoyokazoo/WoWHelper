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
-- (WarriorFunctions.lua). Levels 1-60, generated from a /yyspells dump of a
-- WoW Forever (client 1.60.1) Warlock trainer -- every row the trainer lists,
-- with its real level, cost, and spell ID. Forever differs from Classic Era
-- here: Curse of Agony/Doom are Bane of Agony/Doom, Enslave Demon is
-- Subjugate Demon, the Detect/Create *stone spells are ranks of one name,
-- Conflagrate is trainer-taught from 32, Curse of the Elements has new IDs,
-- Drain Mana and Incinerate are added, and Curse of Shadow, Dark Pact,
-- Inferno and Ritual of Doom aren't on the trainer. To refresh it, re-run
-- /yyspells at a trainer and regenerate from the dump rather than editing by
-- hand. Rank labels are inferred from row order.
--
-- Absent (not trainer rows): starting spells (Shadow Bolt/Demon Skin rank 1,
-- Summon Imp), quest-granted demons, and talent rank 1s (Shadowburn, Siphon
-- Life).
-- requiresSpellId on a talent/quest spell's later ranks points at its first
-- trainer row's own (Forever-verified) ID; GetUnlearnedTrainerSpells only
-- uses it to get the spell's name, so the gate means "knows any rank of it".
WARLOCK_TRAINER_SPELLS = {
    { level = 1,  name = "Immolate",                   spellId = 348,     cost = 10 },
    { level = 4,  name = "Corruption",                 spellId = 172,     cost = 100 },
    { level = 4,  name = "Curse of Weakness",          spellId = 702,     cost = 100 },
    { level = 6,  name = "Life Tap",                   spellId = 1454,    cost = 100 },
    { level = 6,  name = "Shadow Bolt (R2)",           spellId = 695,     cost = 100 },
    { level = 8,  name = "Bane of Agony",              spellId = 980,     cost = 200 },
    { level = 8,  name = "Fear",                       spellId = 5782,    cost = 200 },
    { level = 10, name = "Create Healthstone",         spellId = 6201,    cost = 300 },
    { level = 10, name = "Demon Skin (R2)",            spellId = 696,     cost = 300 },
    { level = 10, name = "Drain Soul",                 spellId = 1120,    cost = 300 },
    { level = 10, name = "Immolate (R2)",              spellId = 707,     cost = 300 },
    { level = 12, name = "Curse of Weakness (R2)",     spellId = 1108,    cost = 800 },
    { level = 12, name = "Health Funnel",              spellId = 755,     cost = 800 },
    { level = 12, name = "Shadow Bolt (R3)",           spellId = 705,     cost = 800 },
    { level = 14, name = "Corruption (R2)",            spellId = 6222,    cost = 1000 },
    { level = 14, name = "Curse of Recklessness",      spellId = 704,     cost = 1000 },
    { level = 14, name = "Drain Life",                 spellId = 689,     cost = 1000 },
    { level = 16, name = "Life Tap (R2)",              spellId = 1455,    cost = 1600 },
    { level = 16, name = "Unending Breath",            spellId = 5697,    cost = 1600 },
    { level = 18, name = "Bane of Agony (R2)",         spellId = 1014,    cost = 1700 },
    { level = 18, name = "Create Soulstone",           spellId = 693,     cost = 1700 },
    { level = 18, name = "Searing Pain",               spellId = 5676,    cost = 1700 },
    { level = 20, name = "Curse of the Elements",      spellId = 440892,  cost = 2500 },
    { level = 20, name = "Demon Armor",                spellId = 706,     cost = 2500 },
    { level = 20, name = "Health Funnel (R2)",         spellId = 3698,    cost = 2500 },
    { level = 20, name = "Immolate (R3)",              spellId = 1094,    cost = 2500 },
    { level = 20, name = "Rain of Fire",               spellId = 5740,    cost = 2500 },
    { level = 20, name = "Ritual of Summoning",        spellId = 698,     cost = 2500 },
    { level = 20, name = "Shadow Bolt (R4)",           spellId = 1088,    cost = 2500 },
    { level = 22, name = "Create Healthstone (R2)",    spellId = 6202,    cost = 3800 },
    { level = 22, name = "Curse of Weakness (R3)",     spellId = 6205,    cost = 3800 },
    { level = 22, name = "Drain Life (R2)",            spellId = 699,     cost = 3800 },
    { level = 22, name = "Eye of Kilrogg",             spellId = 126,     cost = 3800 },
    { level = 24, name = "Corruption (R3)",            spellId = 6223,    cost = 4800 },
    { level = 24, name = "Drain Mana",                 spellId = 5138,    cost = 4800 },
    { level = 24, name = "Drain Soul (R2)",            spellId = 8288,    cost = 4800 },
    { level = 24, name = "Sense Demons",               spellId = 5500,    cost = 4800 },
    { level = 24, name = "Shadowburn (R2)",            spellId = 18867,   cost = 4800,   requiresSpellId = 18867 },
    { level = 26, name = "Curse of Tongues",           spellId = 1714,    cost = 5000 },
    { level = 26, name = "Detect Invisibility",        spellId = 132,     cost = 5000 },
    { level = 26, name = "Life Tap (R3)",              spellId = 1456,    cost = 5000 },
    { level = 26, name = "Searing Pain (R2)",          spellId = 17919,   cost = 5000 },
    { level = 28, name = "Bane of Agony (R3)",         spellId = 6217,    cost = 6000 },
    { level = 28, name = "Banish",                     spellId = 710,     cost = 6000 },
    { level = 28, name = "Create Firestone",           spellId = 6366,    cost = 6000 },
    { level = 28, name = "Curse of Recklessness (R2)", spellId = 7658,    cost = 6000 },
    { level = 28, name = "Health Funnel (R3)",         spellId = 3699,    cost = 6000 },
    { level = 28, name = "Shadow Bolt (R5)",           spellId = 1106,    cost = 6000 },
    { level = 30, name = "Create Soulstone (R2)",      spellId = 20752,   cost = 7000 },
    { level = 30, name = "Curse of the Elements (R2)", spellId = 1311676, cost = 7000 },
    { level = 30, name = "Demon Armor (R2)",           spellId = 1086,    cost = 7000 },
    { level = 30, name = "Drain Life (R3)",            spellId = 709,     cost = 7000 },
    { level = 30, name = "Hellfire",                   spellId = 1949,    cost = 7000 },
    { level = 30, name = "Immolate (R4)",              spellId = 2941,    cost = 7000 },
    { level = 30, name = "Subjugate Demon",            spellId = 1098,    cost = 7000 },
    { level = 32, name = "Conflagrate",                spellId = 1293818, cost = 8000 },
    { level = 32, name = "Curse of Weakness (R4)",     spellId = 7646,    cost = 8000 },
    { level = 32, name = "Fear (R2)",                  spellId = 6213,    cost = 8000 },
    { level = 32, name = "Shadow Ward",                spellId = 6229,    cost = 8000 },
    { level = 32, name = "Shadowburn (R3)",            spellId = 18868,   cost = 8000,   requiresSpellId = 18867 },
    { level = 34, name = "Corruption (R4)",            spellId = 7648,    cost = 9000 },
    { level = 34, name = "Create Healthstone (R3)",    spellId = 5699,    cost = 9000 },
    { level = 34, name = "Drain Mana (R2)",            spellId = 6226,    cost = 9000 },
    { level = 34, name = "Rain of Fire (R2)",          spellId = 6219,    cost = 9000 },
    { level = 34, name = "Searing Pain (R3)",          spellId = 17920,   cost = 9000 },
    { level = 36, name = "Create Firestone (R2)",      spellId = 17951,   cost = 10000 },
    { level = 36, name = "Create Spellstone",          spellId = 2362,    cost = 10000 },
    { level = 36, name = "Health Funnel (R4)",         spellId = 3700,    cost = 10000 },
    { level = 36, name = "Life Tap (R4)",              spellId = 11687,   cost = 10000 },
    { level = 36, name = "Shadow Bolt (R6)",           spellId = 7641,    cost = 10000 },
    { level = 38, name = "Bane of Agony (R4)",         spellId = 11711,   cost = 11000 },
    { level = 38, name = "Detect Invisibility (R2)",   spellId = 2970,    cost = 11000 },
    { level = 38, name = "Drain Life (R4)",            spellId = 7651,    cost = 11000 },
    { level = 38, name = "Drain Soul (R3)",            spellId = 8289,    cost = 11000 },
    { level = 38, name = "Siphon Life (R2)",           spellId = 18879,   cost = 11000,  requiresSpellId = 18879 },
    { level = 40, name = "Conflagrate (R2)",           spellId = 17962,   cost = 14000 },
    { level = 40, name = "Create Soulstone (R3)",      spellId = 20755,   cost = 14000 },
    { level = 40, name = "Curse of the Elements (R3)", spellId = 1311677, cost = 14000 },
    { level = 40, name = "Demon Armor (R3)",           spellId = 11733,   cost = 14000 },
    { level = 40, name = "Howl of Terror",             spellId = 5484,    cost = 14000 },
    { level = 40, name = "Immolate (R5)",              spellId = 11665,   cost = 14000 },
    { level = 40, name = "Shadowburn (R4)",            spellId = 18869,   cost = 14000,  requiresSpellId = 18867 },
    { level = 42, name = "Curse of Recklessness (R3)", spellId = 7659,    cost = 15000 },
    { level = 42, name = "Curse of Weakness (R5)",     spellId = 11707,   cost = 15000 },
    { level = 42, name = "Death Coil",                 spellId = 6789,    cost = 15000 },
    { level = 42, name = "Hellfire (R2)",              spellId = 11683,   cost = 15000 },
    { level = 42, name = "Searing Pain (R4)",          spellId = 17921,   cost = 15000 },
    { level = 42, name = "Shadow Ward (R2)",           spellId = 11739,   cost = 15000 },
    { level = 44, name = "Corruption (R5)",            spellId = 11671,   cost = 16000 },
    { level = 44, name = "Drain Mana (R3)",            spellId = 11703,   cost = 16000 },
    { level = 44, name = "Health Funnel (R5)",         spellId = 11693,   cost = 16000 },
    { level = 44, name = "Shadow Bolt (R7)",           spellId = 11659,   cost = 16000 },
    { level = 44, name = "Subjugate Demon (R2)",       spellId = 11725,   cost = 16000 },
    { level = 46, name = "Create Firestone (R3)",      spellId = 17952,   cost = 20000 },
    { level = 46, name = "Create Healthstone (R4)",    spellId = 11729,   cost = 20000 },
    { level = 46, name = "Drain Life (R5)",            spellId = 11699,   cost = 20000 },
    { level = 46, name = "Life Tap (R5)",              spellId = 11688,   cost = 20000 },
    { level = 46, name = "Rain of Fire (R3)",          spellId = 11677,   cost = 20000 },
    { level = 48, name = "Bane of Agony (R5)",         spellId = 11712,   cost = 21000 },
    { level = 48, name = "Banish (R2)",                spellId = 18647,   cost = 21000 },
    { level = 48, name = "Conflagrate (R3)",           spellId = 18930,   cost = 21000 },
    { level = 48, name = "Create Spellstone (R2)",     spellId = 17727,   cost = 21000 },
    { level = 48, name = "Shadowburn (R5)",            spellId = 18870,   cost = 21000,  requiresSpellId = 18867 },
    { level = 48, name = "Siphon Life (R3)",           spellId = 18880,   cost = 21000,  requiresSpellId = 18879 },
    { level = 48, name = "Soul Fire",                  spellId = 6353,    cost = 21000 },
    { level = 50, name = "Create Soulstone (R4)",      spellId = 20756,   cost = 22000 },
    { level = 50, name = "Curse of Tongues (R2)",      spellId = 11719,   cost = 22000 },
    { level = 50, name = "Curse of the Elements (R4)", spellId = 1311680, cost = 22000 },
    { level = 50, name = "Death Coil (R2)",            spellId = 17925,   cost = 22000 },
    { level = 50, name = "Demon Armor (R4)",           spellId = 11734,   cost = 22000 },
    { level = 50, name = "Detect Invisibility (R3)",   spellId = 11743,   cost = 22000 },
    { level = 50, name = "Immolate (R6)",              spellId = 11667,   cost = 22000 },
    { level = 50, name = "Incinerate",                 spellId = 1293812, cost = 22000 },
    { level = 50, name = "Searing Pain (R5)",          spellId = 17922,   cost = 22000 },
    { level = 52, name = "Curse of Weakness (R6)",     spellId = 11708,   cost = 23000 },
    { level = 52, name = "Drain Soul (R4)",            spellId = 11675,   cost = 23000 },
    { level = 52, name = "Health Funnel (R6)",         spellId = 11694,   cost = 23000 },
    { level = 52, name = "Shadow Bolt (R8)",           spellId = 11660,   cost = 23000 },
    { level = 52, name = "Shadow Ward (R3)",           spellId = 11740,   cost = 23000 },
    { level = 54, name = "Conflagrate (R4)",           spellId = 18931,   cost = 24000 },
    { level = 54, name = "Corruption (R6)",            spellId = 11672,   cost = 24000 },
    { level = 54, name = "Drain Life (R6)",            spellId = 11700,   cost = 24000 },
    { level = 54, name = "Drain Mana (R4)",            spellId = 11704,   cost = 24000 },
    { level = 54, name = "Hellfire (R3)",              spellId = 11684,   cost = 24000 },
    { level = 54, name = "Howl of Terror (R2)",        spellId = 17928,   cost = 24000 },
    { level = 56, name = "Create Firestone (R4)",      spellId = 17953,   cost = 25000 },
    { level = 56, name = "Curse of Recklessness (R4)", spellId = 11717,   cost = 25000 },
    { level = 56, name = "Fear (R3)",                  spellId = 6215,    cost = 25000 },
    { level = 56, name = "Life Tap (R6)",              spellId = 11689,   cost = 25000 },
    { level = 56, name = "Shadowburn (R6)",            spellId = 18871,   cost = 25000,  requiresSpellId = 18867 },
    { level = 56, name = "Soul Fire (R2)",             spellId = 17924,   cost = 25000 },
    { level = 58, name = "Bane of Agony (R6)",         spellId = 11713,   cost = 26000 },
    { level = 58, name = "Create Healthstone (R5)",    spellId = 11730,   cost = 26000 },
    { level = 58, name = "Death Coil (R3)",            spellId = 17926,   cost = 26000 },
    { level = 58, name = "Rain of Fire (R4)",          spellId = 11678,   cost = 26000 },
    { level = 58, name = "Searing Pain (R6)",          spellId = 17923,   cost = 26000 },
    { level = 58, name = "Siphon Life (R4)",           spellId = 18881,   cost = 26000,  requiresSpellId = 18879 },
    { level = 58, name = "Subjugate Demon (R3)",       spellId = 11726,   cost = 26000 },
    { level = 60, name = "Bane of Doom",               spellId = 603,     cost = 27000 },
    { level = 60, name = "Conflagrate (R5)",           spellId = 18932,   cost = 27000 },
    { level = 60, name = "Create Soulstone (R5)",      spellId = 20757,   cost = 27000 },
    { level = 60, name = "Create Spellstone (R3)",     spellId = 17728,   cost = 27000 },
    { level = 60, name = "Demon Armor (R5)",           spellId = 11735,   cost = 27000 },
    { level = 60, name = "Health Funnel (R7)",         spellId = 11695,   cost = 27000 },
    { level = 60, name = "Immolate (R7)",              spellId = 11668,   cost = 27000 },
    { level = 60, name = "Incinerate (R2)",            spellId = 1293813, cost = 27000 },
    { level = 60, name = "Shadow Bolt (R9)",           spellId = 11661,   cost = 27000 },
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
    "CanSpellcastPullTarget",  -- 1 (R1) WoWFunctions.lua -- shared with other casters
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
