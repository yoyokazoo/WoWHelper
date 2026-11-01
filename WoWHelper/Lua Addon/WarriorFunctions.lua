------------------------------------------------------------
-- Warrior-specific game-state checks.
--
-- Moved out of WoWFunctions.lua as part of splitting shared state
-- (MultiBool/MultiInt) from class-specific state (ClassBool/ClassInt -- see
-- the dispatchers in WoWFunctions.lua and GetWarriorClassBoolOne/Two below).
------------------------------------------------------------

-- Class trainer spells, read by AllSkillsKnownForThisLevel()/
-- CanAffordToTrainAllSkills() in WoWFunctions.lua. spellId is the specific
-- rank trained at that level; cost is in copper (base, before rep discount).
-- Levels 1-60, sourced from the WhatsTraining addon's Classes/Vanilla/
-- Warrior.lua (Wowhead blocks automated fetches). Not yet verified live.
--
-- Deliberately absent: quest-granted spells (Defensive Stance/Sunder Armor
-- rank 1/Taunt at 10, Berserker Stance/Intercept rank 1 at 30), talent rank
-- 1s, and weapon skills (weapon masters, not class trainers). Later ranks of
-- talent/quest spells carry requiresSpellId (see GetUnlearnedTrainerSpells
-- in WoWFunctions.lua) so a character without the talent/quest isn't stuck
-- reading "unlearned" forever.
local MORTAL_STRIKE_R1 = 12294
local BLOODTHIRST_R1 = 23881
local SHIELD_SLAM_R1 = 23922
local INTERCEPT_R1 = 20252
WARRIOR_TRAINER_SPELLS = {
    { level = 1,  name = "Battle Shout",          spellId = 6673,  cost = 10 },
    { level = 4,  name = "Charge",                spellId = 100,   cost = 100 },
    { level = 4,  name = "Rend",                  spellId = 772,   cost = 100 },
    { level = 6,  name = "Parry",                 spellId = 3127,  cost = 100 },
    { level = 6,  name = "Thunder Clap",          spellId = 6343,  cost = 100 },
    { level = 8,  name = "Hamstring",             spellId = 1715,  cost = 200 },
    { level = 8,  name = "Heroic Strike (R2)",    spellId = 284,   cost = 200 },
    { level = 10, name = "Bloodrage",             spellId = 2687,  cost = 600 },
    { level = 10, name = "Rend (R2)",             spellId = 6546,  cost = 600 },
    { level = 12, name = "Battle Shout (R2)",     spellId = 5242,  cost = 1000 },
    { level = 12, name = "Overpower",             spellId = 7384,  cost = 1000 },
    { level = 12, name = "Shield Bash",           spellId = 72,    cost = 1000 },
    { level = 14, name = "Demoralizing Shout",    spellId = 1160,  cost = 1500 },
    { level = 14, name = "Revenge",               spellId = 6572,  cost = 1500 },
    { level = 16, name = "Heroic Strike (R3)",    spellId = 285,   cost = 2000 },
    { level = 16, name = "Mocking Blow",          spellId = 694,   cost = 2000 },
    { level = 16, name = "Shield Block",          spellId = 2565,  cost = 2000 },
    { level = 18, name = "Disarm",                spellId = 676,   cost = 3000 },
    { level = 18, name = "Thunder Clap (R2)",     spellId = 8198,  cost = 3000 },
    { level = 20, name = "Cleave",                spellId = 845,   cost = 4000 },
    { level = 20, name = "Dual Wield",            spellId = 674,   cost = 4000 },
    { level = 20, name = "Rend (R3)",             spellId = 6547,  cost = 4000 },
    { level = 20, name = "Retaliation",           spellId = 20230, cost = 4000 },
    { level = 22, name = "Battle Shout (R3)",     spellId = 6192,  cost = 6000 },
    { level = 22, name = "Sunder Armor (R2)",     spellId = 7405,  cost = 6000 },
    { level = 22, name = "Intimidating Shout",    spellId = 5246,  cost = 6000 },
    { level = 24, name = "Demoralizing Shout (R2)", spellId = 6190, cost = 8000 },
    { level = 24, name = "Execute",               spellId = 5308,  cost = 8000 },
    { level = 24, name = "Heroic Strike (R4)",    spellId = 1608,  cost = 8000 },
    { level = 24, name = "Revenge (R2)",          spellId = 6574,  cost = 8000 },
    { level = 26, name = "Challenging Shout",     spellId = 1161,  cost = 10000 },
    { level = 26, name = "Charge (R2)",           spellId = 6178,  cost = 10000 },
    { level = 26, name = "Mocking Blow (R2)",     spellId = 7400,  cost = 10000 },
    { level = 28, name = "Overpower (R2)",        spellId = 7887,  cost = 11000 },
    { level = 28, name = "Shield Wall",           spellId = 871,   cost = 11000 },
    { level = 28, name = "Thunder Clap (R3)",     spellId = 8204,  cost = 11000 },
    { level = 30, name = "Cleave (R2)",           spellId = 7369,  cost = 12000 },
    { level = 30, name = "Rend (R4)",             spellId = 6548,  cost = 12000 },
    { level = 30, name = "Slam",                  spellId = 1464,  cost = 12000 },
    { level = 32, name = "Battle Shout (R4)",     spellId = 11549, cost = 14000 },
    { level = 32, name = "Berserker Rage",        spellId = 18499, cost = 14000 },
    { level = 32, name = "Execute (R2)",          spellId = 20658, cost = 14000 },
    { level = 32, name = "Hamstring (R2)",        spellId = 7372,  cost = 14000 },
    { level = 32, name = "Heroic Strike (R5)",    spellId = 11564, cost = 14000 },
    { level = 32, name = "Shield Bash (R2)",      spellId = 1671,  cost = 14000 },
    { level = 34, name = "Demoralizing Shout (R3)", spellId = 11554, cost = 16000 },
    { level = 34, name = "Revenge (R3)",          spellId = 7379,  cost = 16000 },
    { level = 34, name = "Sunder Armor (R3)",     spellId = 8380,  cost = 16000 },
    { level = 36, name = "Mocking Blow (R3)",     spellId = 7402,  cost = 18000 },
    { level = 36, name = "Whirlwind",             spellId = 1680,  cost = 18000 },
    { level = 38, name = "Pummel",                spellId = 6552,  cost = 20000 },
    { level = 38, name = "Slam (R2)",             spellId = 8820,  cost = 20000 },
    { level = 38, name = "Thunder Clap (R4)",     spellId = 8205,  cost = 20000 },
    { level = 40, name = "Cleave (R3)",           spellId = 11608, cost = 22000 },
    { level = 40, name = "Execute (R3)",          spellId = 20660, cost = 22000 },
    { level = 40, name = "Heroic Strike (R6)",    spellId = 11565, cost = 22000 },
    { level = 40, name = "Plate Mail",            spellId = 750,   cost = 22000 },
    { level = 40, name = "Rend (R5)",             spellId = 11572, cost = 22000 },
    { level = 42, name = "Battle Shout (R5)",     spellId = 11550, cost = 32000 },
    { level = 42, name = "Intercept (R2)",        spellId = 20616, cost = 32000, requiresSpellId = INTERCEPT_R1 },
    { level = 44, name = "Demoralizing Shout (R4)", spellId = 11555, cost = 34000 },
    { level = 44, name = "Overpower (R3)",        spellId = 11584, cost = 34000 },
    { level = 44, name = "Revenge (R4)",          spellId = 11600, cost = 34000 },
    { level = 46, name = "Charge (R3)",           spellId = 11578, cost = 36000 },
    { level = 46, name = "Mocking Blow (R4)",     spellId = 20559, cost = 36000 },
    { level = 46, name = "Slam (R3)",             spellId = 11604, cost = 36000 },
    { level = 46, name = "Sunder Armor (R4)",     spellId = 11596, cost = 36000 },
    { level = 48, name = "Bloodthirst (R2)",      spellId = 23892, cost = 2000,  requiresSpellId = BLOODTHIRST_R1 },
    { level = 48, name = "Execute (R4)",          spellId = 20661, cost = 40000 },
    { level = 48, name = "Heroic Strike (R7)",    spellId = 11566, cost = 40000 },
    -- WhatsTraining lists 200 for these two, almost certainly a typo for
    -- 2000 (Bloodthirst R2 above is 2000, and all three R3s/R4s match).
    { level = 48, name = "Mortal Strike (R2)",    spellId = 21551, cost = 2000,  requiresSpellId = MORTAL_STRIKE_R1 },
    { level = 48, name = "Shield Slam (R2)",      spellId = 23923, cost = 2000,  requiresSpellId = SHIELD_SLAM_R1 },
    { level = 48, name = "Thunder Clap (R5)",     spellId = 11580, cost = 40000 },
    { level = 50, name = "Cleave (R4)",           spellId = 11609, cost = 42000 },
    { level = 50, name = "Recklessness",          spellId = 1719,  cost = 42000 },
    { level = 50, name = "Rend (R6)",             spellId = 11573, cost = 42000 },
    { level = 52, name = "Battle Shout (R6)",     spellId = 11551, cost = 54000 },
    { level = 52, name = "Intercept (R3)",        spellId = 20617, cost = 54000, requiresSpellId = INTERCEPT_R1 },
    { level = 52, name = "Shield Bash (R3)",      spellId = 1672,  cost = 54000 },
    { level = 54, name = "Bloodthirst (R3)",      spellId = 23893, cost = 2800,  requiresSpellId = BLOODTHIRST_R1 },
    { level = 54, name = "Demoralizing Shout (R5)", spellId = 11556, cost = 56000 },
    { level = 54, name = "Hamstring (R3)",        spellId = 7373,  cost = 56000 },
    { level = 54, name = "Mortal Strike (R3)",    spellId = 21552, cost = 2800,  requiresSpellId = MORTAL_STRIKE_R1 },
    { level = 54, name = "Revenge (R5)",          spellId = 11601, cost = 56000 },
    { level = 54, name = "Shield Slam (R3)",      spellId = 23924, cost = 2800,  requiresSpellId = SHIELD_SLAM_R1 },
    { level = 54, name = "Slam (R4)",             spellId = 11605, cost = 56000 },
    { level = 56, name = "Execute (R5)",          spellId = 20662, cost = 58000 },
    { level = 56, name = "Heroic Strike (R8)",    spellId = 11567, cost = 58000 },
    { level = 56, name = "Mocking Blow (R5)",     spellId = 20560, cost = 58000 },
    { level = 58, name = "Pummel (R2)",           spellId = 6554,  cost = 60000 },
    { level = 58, name = "Sunder Armor (R5)",     spellId = 11597, cost = 60000 },
    { level = 58, name = "Thunder Clap (R6)",     spellId = 11581, cost = 60000 },
    { level = 60, name = "Bloodthirst (R4)",      spellId = 23894, cost = 3100,  requiresSpellId = BLOODTHIRST_R1 },
    { level = 60, name = "Cleave (R5)",           spellId = 20569, cost = 62000 },
    { level = 60, name = "Mortal Strike (R4)",    spellId = 21553, cost = 3100,  requiresSpellId = MORTAL_STRIKE_R1 },
    { level = 60, name = "Overpower (R4)",        spellId = 11585, cost = 62000 },
    { level = 60, name = "Rend (R7)",             spellId = 11574, cost = 62000 },
    { level = 60, name = "Shield Slam (R4)",      spellId = 23925, cost = 3100,  requiresSpellId = SHIELD_SLAM_R1 },
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
        local spellName = GetSpellInfo(12292)
        local start, duration, enabled = GetSpellCooldown(spellName or 12292)
        local remaining = (start and duration) and (start + duration - GetTime()) or nil
        print(string.format(
            "|cffff9900[sweep]|r CanCastSweepingStrikes: spellName(12292)=%s start=%s duration=%s enabled=%s remaining=%s -> ignoringGCD=%s plainCooledDown=%s",
            tostring(spellName), tostring(start), tostring(duration), tostring(enabled),
            tostring(remaining), tostring(result), tostring(SpellIsCooledDown(12292))))
    end

    return result
end

------------------------------------------------------------
-- Packs Warrior-specific state into the ClassBool/ClassInt pixels. Called
-- via the GetClassBoolOne/Two/GetClassIntOne dispatchers in WoWFunctions.lua
-- once UnitClass("player") resolves to WARRIOR, and read on the C# side by
-- WowWarriorClassState.UpdateFromBitmap. ClassBoolOne's R/G bytes are now
-- fully packed (R8/G1/G4/G5/G6 were added once PlayerLevel-based training
-- gates on the C# side were replaced with real spellbook checks; G8 is
-- CanCastSweepingStrikes, added once WarriorShouldCastSweepingStrikes wired it
-- into the rotation -- its trained-yet check is folded into
-- CanCastSweepingStrikes() itself rather than spending a separate bit on it,
-- since nothing needed that independently of the cooldown check -- see that
-- function's own comment above for why that's safe here but isn't for
-- KnowsCharge/KnowsMortalStrikeOrBloodthirst, which do still need their own
-- bits. G7 is reserved again as a result).
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
    local boolG7 = false -- reserved
    local boolG8 = CanCastSweepingStrikes()

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
