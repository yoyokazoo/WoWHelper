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
-- (WarriorFunctions.lua). TODO: not filled in yet, so Warlock always reads as
-- "nothing to train".
WARLOCK_TRAINER_SPELLS = {
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
