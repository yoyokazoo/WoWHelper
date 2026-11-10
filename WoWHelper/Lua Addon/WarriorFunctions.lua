------------------------------------------------------------
-- Warrior-specific game-state checks, and Warrior's Class pixel-row field
-- lists (WARRIOR_CLASS_BOOL_ONE/TWO_FIELDS, bottom of this file).
------------------------------------------------------------

-- Class trainer spells, read by AllSkillsKnownForThisLevel()/
-- CanAffordToTrainAllSkills() in WoWFunctions.lua. spellId is the specific
-- rank trained at that level; cost is in copper (base, before rep discount).
-- Levels 1-60, generated from a /yyspells dump of a WoW Forever (client
-- 1.60.1) Warrior trainer -- every row the trainer lists, with its real
-- level, cost, and spell ID (read off the row's tooltip). Forever differs
-- from Classic Era here: different costs, Slam from level 20 (a
-- Forever-specific rank 1, so it has 5 ranks), Berserker Rage at 30, and
-- trainer-taught Tactical Mastery/Victory Rush. To refresh it, re-run
-- /yyspells at a trainer and regenerate from the dump rather than editing
-- by hand. Rank labels are inferred from row order, since the client
-- doesn't report ranks.
--
-- Deliberately absent (the trainer doesn't list them): starting spells
-- (Heroic Strike rank 1), quest-granted spells (Defensive Stance/Sunder
-- Armor rank 1/Taunt, Berserker Stance/Intercept rank 1), talent rank 1s,
-- and weapon skills (weapon masters, not class trainers). Later ranks of
-- talent/quest spells carry requiresSpellId (see GetUnlearnedTrainerSpells
-- in WoWFunctions.lua) so a character without the talent/quest isn't stuck
-- reading "unlearned" forever. Those rank-1 IDs (below) aren't on the
-- trainer, so they're still the Classic Era values -- not verified on
-- Forever.
local MORTAL_STRIKE_R1 = 12294
local BLOODTHIRST_R1 = 23881
local SHIELD_SLAM_R1 = 23922
local INTERCEPT_R1 = 20252
WARRIOR_TRAINER_SPELLS = {
    { level = 1,  name = "Battle Shout",            spellId = 6673,    cost = 10 },
    { level = 4,  name = "Charge",                  spellId = 100,     cost = 100 },
    { level = 4,  name = "Rend",                    spellId = 772,     cost = 100 },
    { level = 6,  name = "Parry",                   spellId = 3127,    cost = 100 },
    { level = 6,  name = "Thunder Clap",            spellId = 6343,    cost = 100 },
    { level = 8,  name = "Hamstring",               spellId = 1715,    cost = 200 },
    { level = 8,  name = "Heroic Strike (R2)",      spellId = 284,     cost = 200 },
    { level = 10, name = "Bloodrage",               spellId = 2687,    cost = 700 },
    { level = 10, name = "Rend (R2)",               spellId = 6546,    cost = 700 },
    { level = 12, name = "Battle Shout (R2)",       spellId = 5242,    cost = 900 },
    { level = 12, name = "Overpower",               spellId = 7384,    cost = 900 },
    { level = 12, name = "Shield Bash",             spellId = 72,      cost = 900 },
    { level = 14, name = "Demoralizing Shout",      spellId = 1160,    cost = 1100 },
    { level = 14, name = "Revenge",                 spellId = 6572,    cost = 1100 },
    { level = 14, name = "Tactical Mastery",        spellId = 1310185, cost = 1100 },
    { level = 16, name = "Heroic Strike (R3)",      spellId = 285,     cost = 1500 },
    { level = 16, name = "Mocking Blow",            spellId = 694,     cost = 1500 },
    { level = 16, name = "Shield Block",            spellId = 2565,    cost = 1500 },
    { level = 18, name = "Disarm",                  spellId = 676,     cost = 3200 },
    { level = 18, name = "Thunder Clap (R2)",       spellId = 8198,    cost = 3200 },
    { level = 20, name = "Cleave",                  spellId = 845,     cost = 3400 },
    { level = 20, name = "Dual Wield",              spellId = 674,     cost = 3400 },
    { level = 20, name = "Rend (R3)",               spellId = 6547,    cost = 3400 },
    { level = 20, name = "Retaliation",             spellId = 20230,   cost = 3400 },
    { level = 20, name = "Slam",                    spellId = 1240193, cost = 3400 },
    { level = 20, name = "Victory Rush",            spellId = 402927,  cost = 3400 },
    { level = 22, name = "Battle Shout (R3)",       spellId = 6192,    cost = 5500 },
    { level = 22, name = "Intimidating Shout",      spellId = 5246,    cost = 5500 },
    { level = 22, name = "Sunder Armor (R2)",       spellId = 7405,    cost = 5500 },
    { level = 24, name = "Demoralizing Shout (R2)", spellId = 6190,    cost = 6400 },
    { level = 24, name = "Execute",                 spellId = 5308,    cost = 6400 },
    { level = 24, name = "Heroic Strike (R4)",      spellId = 1608,    cost = 6400 },
    { level = 24, name = "Revenge (R2)",            spellId = 6574,    cost = 6400 },
    { level = 26, name = "Challenging Shout",       spellId = 1161,    cost = 7600 },
    { level = 26, name = "Charge (R2)",             spellId = 6178,    cost = 7600 },
    { level = 26, name = "Mocking Blow (R2)",       spellId = 7400,    cost = 7600 },
    { level = 28, name = "Overpower (R2)",          spellId = 7887,    cost = 11000 },
    { level = 28, name = "Shield Wall",             spellId = 871,     cost = 11000 },
    { level = 28, name = "Thunder Clap (R3)",       spellId = 8204,    cost = 11000 },
    { level = 30, name = "Berserker Rage",          spellId = 18499,   cost = 12000 },
    { level = 30, name = "Cleave (R2)",             spellId = 7369,    cost = 12000 },
    { level = 30, name = "Rend (R4)",               spellId = 6548,    cost = 12000 },
    { level = 30, name = "Slam (R2)",               spellId = 1464,    cost = 12000 },
    { level = 32, name = "Battle Shout (R4)",       spellId = 11549,   cost = 13000 },
    { level = 32, name = "Execute (R2)",            spellId = 20658,   cost = 13000 },
    { level = 32, name = "Hamstring (R2)",          spellId = 7372,    cost = 13000 },
    { level = 32, name = "Heroic Strike (R5)",      spellId = 11564,   cost = 13000 },
    { level = 32, name = "Shield Bash (R2)",        spellId = 1671,    cost = 13000 },
    { level = 34, name = "Demoralizing Shout (R3)", spellId = 11554,   cost = 14000 },
    { level = 34, name = "Revenge (R3)",            spellId = 7379,    cost = 14000 },
    { level = 34, name = "Sunder Armor (R3)",       spellId = 8380,    cost = 14000 },
    { level = 36, name = "Mocking Blow (R3)",       spellId = 7402,    cost = 18000 },
    { level = 36, name = "Whirlwind",               spellId = 1680,    cost = 18000 },
    { level = 38, name = "Pummel",                  spellId = 6552,    cost = 19000 },
    { level = 38, name = "Slam (R3)",               spellId = 8820,    cost = 19000 },
    { level = 38, name = "Thunder Clap (R4)",       spellId = 8205,    cost = 19000 },
    { level = 40, name = "Cleave (R3)",             spellId = 11608,   cost = 23000 },
    { level = 40, name = "Execute (R3)",            spellId = 20660,   cost = 23000 },
    { level = 40, name = "Heroic Strike (R6)",      spellId = 11565,   cost = 23000 },
    { level = 40, name = "Plate Mail",              spellId = 750,     cost = 23000 },
    { level = 40, name = "Rend (R5)",               spellId = 11572,   cost = 23000 },
    { level = 42, name = "Battle Shout (R5)",       spellId = 11550,   cost = 30000 },
    { level = 42, name = "Intercept (R2)",          spellId = 20616,   cost = 30000,  requiresSpellId = INTERCEPT_R1 },
    { level = 44, name = "Demoralizing Shout (R4)", spellId = 11555,   cost = 31000 },
    { level = 44, name = "Overpower (R3)",          spellId = 11584,   cost = 31000 },
    { level = 44, name = "Revenge (R4)",            spellId = 11600,   cost = 31000 },
    { level = 46, name = "Charge (R3)",             spellId = 11578,   cost = 32000 },
    { level = 46, name = "Mocking Blow (R4)",       spellId = 20559,   cost = 32000 },
    { level = 46, name = "Slam (R4)",               spellId = 11604,   cost = 32000 },
    { level = 46, name = "Sunder Armor (R4)",       spellId = 11596,   cost = 32000 },
    { level = 48, name = "Bloodthirst (R2)",        spellId = 23892,   cost = 33000,  requiresSpellId = BLOODTHIRST_R1 },
    { level = 48, name = "Execute (R4)",            spellId = 20661,   cost = 33000 },
    { level = 48, name = "Heroic Strike (R7)",      spellId = 11566,   cost = 33000 },
    { level = 48, name = "Mortal Strike (R2)",      spellId = 21551,   cost = 33000,  requiresSpellId = MORTAL_STRIKE_R1 },
    { level = 48, name = "Shield Slam (R2)",        spellId = 23923,   cost = 33000,  requiresSpellId = SHIELD_SLAM_R1 },
    { level = 48, name = "Thunder Clap (R5)",       spellId = 11580,   cost = 33000 },
    { level = 50, name = "Cleave (R4)",             spellId = 11609,   cost = 36000 },
    { level = 50, name = "Recklessness",            spellId = 1719,    cost = 36000 },
    { level = 50, name = "Rend (R6)",               spellId = 11573,   cost = 36000 },
    { level = 52, name = "Battle Shout (R6)",       spellId = 11551,   cost = 39000 },
    { level = 52, name = "Intercept (R3)",          spellId = 20617,   cost = 39000,  requiresSpellId = INTERCEPT_R1 },
    { level = 52, name = "Shield Bash (R3)",        spellId = 1672,    cost = 39000 },
    { level = 54, name = "Bloodthirst (R3)",        spellId = 23893,   cost = 40000,  requiresSpellId = BLOODTHIRST_R1 },
    { level = 54, name = "Demoralizing Shout (R5)", spellId = 11556,   cost = 40000 },
    { level = 54, name = "Hamstring (R3)",          spellId = 7373,    cost = 40000 },
    { level = 54, name = "Mortal Strike (R3)",      spellId = 21552,   cost = 40000,  requiresSpellId = MORTAL_STRIKE_R1 },
    { level = 54, name = "Revenge (R5)",            spellId = 11601,   cost = 40000 },
    { level = 54, name = "Shield Slam (R3)",        spellId = 23924,   cost = 40000,  requiresSpellId = SHIELD_SLAM_R1 },
    { level = 54, name = "Slam (R5)",               spellId = 11605,   cost = 40000 },
    { level = 56, name = "Execute (R5)",            spellId = 20662,   cost = 42000 },
    { level = 56, name = "Heroic Strike (R8)",      spellId = 11567,   cost = 42000 },
    { level = 56, name = "Mocking Blow (R5)",       spellId = 20560,   cost = 42000 },
    { level = 58, name = "Pummel (R2)",             spellId = 6554,    cost = 48000 },
    { level = 58, name = "Sunder Armor (R5)",       spellId = 11597,   cost = 48000 },
    { level = 58, name = "Thunder Clap (R6)",       spellId = 11581,   cost = 48000 },
    { level = 60, name = "Bloodthirst (R4)",        spellId = 23894,   cost = 49000,  requiresSpellId = BLOODTHIRST_R1 },
    { level = 60, name = "Cleave (R5)",             spellId = 20569,   cost = 49000 },
    { level = 60, name = "Mortal Strike (R4)",      spellId = 21553,   cost = 49000,  requiresSpellId = MORTAL_STRIKE_R1 },
    { level = 60, name = "Overpower (R4)",          spellId = 11585,   cost = 49000 },
    { level = 60, name = "Rend (R7)",               spellId = 11574,   cost = 49000 },
    { level = 60, name = "Shield Slam (R4)",        spellId = 23925,   cost = 49000,  requiresSpellId = SHIELD_SLAM_R1 },
}

-- Talent build, read by FindNextTalentToLearn() in WoWFunctions.lua. Ordered;
-- each entry is a cumulative target rank ("get this talent to rank N"), and a
-- free point always goes to the first entry not yet reached. Matched by
-- talent name, not tab/index. Only the first 11 points are stubbed in so far.
WARRIOR_TALENT_BUILD = {
    { name = "Deflection",       rank = 2 },
    { name = "Improved Rend",    rank = 3 },
    { name = "Tactical Mastery", rank = 5 },
    { name = "Anger Management", rank = 1 },
}

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
    return Compat_IsCurrentSpell(7918) or Compat_IsCurrentSpell(2480) or Compat_IsCurrentSpell(5019)
end

function IsAnyNextSwingSpellQueued()
    -- Action queue abilities always satisfy IsCurrentSpell()
    -- So check if ANY known next-swing spell is current.
    if Compat_IsCurrentSpell("Heroic Strike") then return true end
    if Compat_IsCurrentSpell("Cleave")        then return true end
    return false
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

-- TEMP diagnostic switch: set true to print exactly what CanCastSweepingStrikes()
-- sees every time it's checked -- talent-known state and the raw GetSpellCooldown()
-- values. Same manually-flipped debug-flag pattern as COUNT_ATTACKERS_DEBUG
-- (WoWFunctions.lua)/COMBAT_STALEMATE_DEBUG (YoyokazooUI.lua). Root-caused the
-- "Sweeping Strikes never fires" bug this was added for (see CanCastSweepingStrikes()
-- below), so back to off by default -- flip back to true if Sweeping Strikes needs
-- debugging again.
local SWEEPING_STRIKES_DEBUG = false

-- Sweeping Strikes, 12292 -- single rank.
--
-- Root-caused live via SWEEPING_STRIKES_DEBUG below: this was hardcoded to 12328,
-- which is actually Death Wish, not Sweeping Strikes -- GetSpellInfo(12328) still
-- resolved to a real spell name (so nothing here errored or returned nil-early), it
-- was just the wrong spell, so this silently checked Death Wish's cooldown instead
-- and Sweeping Strikes itself never got cast. Corrected to 12292.
--
-- The trained-yet check is folded in here rather than exposed as its own
-- KnowsSweepingStrikes() (unlike KnowsRend/KnowsCharge/KnowsMortalStrikeOrBloodthirst/
-- KnowsSunderArmor, which stay separate because the C# side reads at least one of them
-- from more than one place -- KnowsCharge also gates engage-method logic, and
-- KnowsMortalStrikeOrBloodthirst also picks Heroic Strike's rage reserve). Nothing on
-- the C# side ever needed "does the player know Sweeping Strikes" on its own, only
-- paired with the cooldown check, so there was no reason to spend a second ClassBoolOne
-- bit on it. Folding it in is still required, though, not optional: GetSpellInfo(12292)
-- resolves a name -- and GetSpellCooldown() for that name reads (0, 0), i.e. "ready" --
-- for a spell ID the player has never trained at all, the same false-ready quirk
-- KnowsMortalStrikeOrBloodthirst()'s comment above documents for Mortal Strike/
-- Bloodthirst. Without this guard, a Warrior who never spent the talent point would
-- still read "can cast," and since this sits second in the rotation's priority chain
-- (WowWarriorTasks.cs), it would block every lower-priority ability every tick for an
-- ability that was never actually going to fire.
--
-- Uses SpellIsCooledDownIgnoringGCD() (WoWFunctions.lua), not the plain
-- SpellIsCooledDown() every other Warrior cooldown check here uses -- Sweeping
-- Strikes' real cooldown (~30s) is long enough next to the GCD (~1.5s) that plain
-- SpellIsCooledDown() would usually still read correctly, but right at the moment
-- the real cooldown clears, GetSpellCooldown() can still be reporting nothing but
-- the shared GCD window from whatever else was just cast, reading as "still on
-- cooldown" for that tick. SpellIsCooledDownIgnoringGCD() tells the two apart via
-- GetGCDProbeSpell() (WoWFunctions.lua), the same class-appropriate real-spell probe
-- IsGlobalCooldownCooledDown() uses -- see that function's comment for Warrior's own
-- (not ideal, level-4-gated) pick.
function CanCastSweepingStrikes()
    if not IsSpellKnownByName("Sweeping Strikes") then
        if SWEEPING_STRIKES_DEBUG then
            print("|cffff9900[sweep]|r CanCastSweepingStrikes: not trained -> false")
        end
        return false
    end

    local result = SpellIsCooledDownIgnoringGCD(12292)

    if SWEEPING_STRIKES_DEBUG then
        local spellName = Compat_GetSpellInfo(12292)
        local start, duration, enabled = Compat_GetSpellCooldown(spellName or 12292)
        local remaining = (start and duration) and (start + duration - GetTime()) or nil
        print(string.format(
            "|cffff9900[sweep]|r CanCastSweepingStrikes: spellName(12292)=%s start=%s duration=%s enabled=%s remaining=%s -> ignoringGCD=%s plainCooledDown=%s",
            tostring(spellName), tostring(start), tostring(duration), tostring(enabled),
            tostring(remaining), tostring(result), tostring(SpellIsCooledDown(12292))))
    end

    return result
end

function HasBattleShout()
    return HasBuffNamed("Battle Shout")
end

------------------------------------------------------------
-- Warrior's Class pixel rows -- see the field-list comment above
-- MULTI_BOOL_ONE_FIELDS (WoWFunctions.lua) for how these lists work. Decoded
-- on the C# side by WowWarriorClassState, in the same order.
------------------------------------------------------------
WARRIOR_CLASS_BOOL_ONE_FIELDS = {
    "HasBattleShout",                   --  1 (R1)
    "TargetHasRend",                    --  2 (R2)
    "CanChargeTarget",                  --  3 (R3)
    "CanShootTarget",                   --  4 (R4)
    "WaitingToShoot",                   --  5 (R5)
    "IsAnyNextSwingSpellQueued",        --  6 (R6)
    "IsOverpowerUsable",                --  7 (R7)
    "KnowsRend",                        --  8 (R8)
    "KnowsExecute",                     --  9 (G1)
    "CanCastMortalStrikeOrBloodthirst", -- 10 (G2)
    "KnowsMortalStrikeOrBloodthirst",   -- 11 (G3)
    "KnowsCharge",                      -- 12 (G4)
    "TargetHasSunderArmor",             -- 13 (G5)
    "KnowsSunderArmor",                 -- 14 (G6)
    RESERVED,                           -- 15 (G7) was KnowsSweepingStrikes, folded into CanCastSweepingStrikes
    "CanCastSweepingStrikes",           -- 16 (G8)
}

WARRIOR_CLASS_BOOL_TWO_FIELDS = {}
