function IsInCombat()
    return UnitAffectingCombat("player")
end

function ShouldWeAttackTarget()
    local unit = "target"

    -- Basic checks
    if not UnitExists(unit) then
        return false
    end

    if not UnitCanAttack("player", unit) then
        return false
    end

    if UnitIsPVP(unit) then
        return false
    end

    if UnitIsDead(unit) or UnitIsDeadOrGhost(unit) then
        return false
    end

    -- Don't kill greys. Blizzard's real grey-mob cutoff isn't a flat level gap --
    -- it's a table that changes bracket by the player's own level (see Wowpedia's
    -- "creature difficulty color" table) -- so a hardcoded gap constant drifts wrong
    -- at some level ranges. Confirmed live: a level 18 character attacked a level 11
    -- target (a 7-level gap) that conned grey in-game, but the old ">= 8" gap check
    -- let it through. Ask the client's own difficulty-color function instead of
    -- reimplementing Blizzard's table by hand, so this tracks whatever the real
    -- current-patch table is instead of a number that can silently go stale again.
    local difficultyColor = GetCreatureDifficultyColor(UnitLevel(unit))
    if difficultyColor and difficultyColor.r == 0.5 and difficultyColor.g == 0.5 and difficultyColor.b == 0.5 then
        return false
    end

    -- don't charge oranges
    --if UnitLevel("player") - UnitLevel(unit) <= -3 then
    -- temp changing
    if UnitLevel("player") - UnitLevel(unit) <= -4 then
        return false
    end

    -- unit in combat, probably not with us
    if UnitAffectingCombat(unit) then
        return false
    end

    return true
end

function SpellIsCooledDown(spellId)
    local spellName = GetSpellInfo(spellId)
    if not spellName then
        -- Should never happen unless spell is unknown (e.g., very low level)
        return false
    end

    local start, duration, enabled = GetSpellCooldown(spellName)

    if start == nil then
        return false
    end

    -- If start == 0 and duration == 0, spell is ready
    if start == 0 and duration == 0 then
        return true
    end

    local remaining = start + duration - GetTime()
    if remaining <= 0 then
        return true
    end

    return false
end

-- Classic: this spellId is commonly used to query the global cooldown
local GCD_SPELL_ID = 61304

-- Returns true if the *spell's own* cooldown is finished.
-- If the only cooldown present is the GCD, returns true.
function SpellIsCooledDownIgnoringGCD(spellId)
    local spellName = GetSpellInfo(spellId)
    if not spellName then
        return false
    end

    local start, duration, enabled = GetSpellCooldown(spellName)

    -- If API returns nil or spell is unusable for some reason
    if not start or enabled == 0 then
        return false
    end

    -- Ready
    if duration == 0 then
        return true
    end

    -- Compute remaining
    local remaining = start + duration - GetTime()
    if remaining <= 0 then
        return true
    end

    -- If we're here, spell has a cooldown reported. It might just be the GCD.
    local gcdStart, gcdDuration = GetSpellCooldown(GCD_SPELL_ID)

    -- If no GCD data, fall back to original behavior
    if not gcdStart or gcdDuration == 0 then
        return false
    end

    -- Heuristic:
    -- If the spell cooldown window is essentially the GCD window, ignore it.
    -- Use a small tolerance because durations can differ by a few ms.
    local eps = 0.05

    local isJustGCD =
        math.abs(start - gcdStart) <= eps and
        duration <= (gcdDuration + eps)

    if isJustGCD then
        return true
    end

    -- Otherwise, the spell is on a real cooldown beyond GCD.
    return false
end

function SpellIsInRangeAndCooledDown(spellId)
    local unit = "target"

    -- Resolve spell name from its ID
    local spellName = GetSpellInfo(spellId)
    if not spellName then
        -- Should never happen unless spell is unknown (e.g., very low level)
        return false
    end

    -- 1) Range check
    local inRange = IsSpellInRange(spellName, unit)
    if inRange ~= 1 then
        return false
    end

    -- 2) Cooldown check
    local start, duration, enabled = GetSpellCooldown(spellName)

    -- removing this check for now -> we should be using macros to hop into correct stance
    -- If it's not enabled, you can't use it (e.g., disabled by stance/form)
    --if enabled == 0 then
    --    return false
    --end

    -- If start == 0 and duration == 0, spell is ready
    if start == 0 and duration == 0 then
        return true
    end

    local remaining = start + duration - GetTime()
    if remaining <= 0 then
        return true
    end

    return false
end

function SpellIsInRange(spellId)
    local unit = "target"

    -- Resolve spell name from its ID
    local spellName = GetSpellInfo(spellId)
    if not spellName then
        -- Should never happen unless spell is unknown (e.g., very low level)
        return false
    end

    -- 1) Range check
    local inRange = IsSpellInRange(spellName, unit)
    if inRange ~= 1 then
        return false
    end

    return true
end

-- CanChargeTarget() and CanShootTarget() moved to WarriorFunctions.lua.

-- Shared between Shaman and Warlock (branches internally below), so it
-- stays here rather than moving to a single class file -- see the note atop
-- ShamanFunctions.lua/WarlockFunctions.lua about why it isn't split further.
function CanSpellcastPullTarget()
    if not ShouldWeAttackTarget() then
        return false
    end

    local _, classFile = UnitClass("player")

    local spellId

    if classFile == "SHAMAN" then
        spellId = 403 -- Lightning Bolt (Rank 1)
    elseif classFile == "WARLOCK" then
        spellId = 686 -- Shadow Bolt (Rank 1), known from level 1 -- TODO: verify in-game
    else
        return false
    end

    -- Goes on cooldown as soon as casting starts, so range check is sufficient
    return SpellIsInRange(spellId)
end

-- WaitingToShoot() and IsAnyNextSwingSpellQueued() moved to WarriorFunctions.lua.

--------------------------------------------------
-- Health: player (percent 0�100)
--------------------------------------------------
function GetPlayerHealthPercent()
    local hp  = UnitHealth("player")
    local max = UnitHealthMax("player")
    if max == 0 then return 0 end
    return math.floor((hp / max) * 100 + 0.5)
end

--------------------------------------------------
-- Resource: player (rage / mana / energy, percent 0�100)
--------------------------------------------------
function GetPlayerResourcePercent()
    local powerType = UnitPowerType("player")            -- e.g. 0 = mana, 1 = rage, 3 = energy
    local cur = UnitPower("player", powerType)
    local max = UnitPowerMax("player", powerType)

    if max == 0 then return 0 end
    return math.floor((cur / max) * 100 + 0.5)
end

--------------------------------------------------
-- Target health (percent 0�100)
--------------------------------------------------
function GetTargetHealthPercent()
    if not UnitExists("target") then
        return 0
    end

    local hp  = UnitHealth("target")
    local max = UnitHealthMax("target")
    if max == 0 then return 0 end
    return math.floor((hp / max) * 100 + 0.5)
end

--------------------------------------------------
-- Map X coord (0�100, normalized across map)
--------------------------------------------------
function GetPlayerMapX()
    if not C_Map or not C_Map.GetBestMapForUnit then
        return 0
    end

    local mapID = C_Map.GetBestMapForUnit("player")
    if not mapID then
        return 0
    end

    local pos = C_Map.GetPlayerMapPosition(mapID, "player")
    if not pos then
        return 0
    end

    -- pos.x is 0�1 across the map; convert to 0�100
    return math.floor((pos.x or 0) * 10000 + 0.5) / 100  -- two decimals
end

--------------------------------------------------
-- Map Y coord (0�100, normalized across map)
--------------------------------------------------
function GetPlayerMapY()
    if not C_Map or not C_Map.GetBestMapForUnit then
        return 0
    end

    local mapID = C_Map.GetBestMapForUnit("player")
    if not mapID then
        return 0
    end

    local pos = C_Map.GetPlayerMapPosition(mapID, "player")
    if not pos then
        return 0
    end

    -- pos.y is 0�1; convert to 0�100
    return math.floor((pos.y or 0) * 10000 + 0.5) / 100  -- two decimals
end

--------------------------------------------------
-- Current zone, as a numeric ID (fits in a single byte channel) -- NOT
-- Blizzard's internal map ID, which doesn't fit in one. MUST stay in sync
-- with the numbering used on the decoding side.
--------------------------------------------------
local ZONE_NAME_TO_ID = {
    ["Durotar"] = 0,
    ["Mulgore"] = 1,
    ["The Barrens"] = 2,
    ["Ashenvale"] = 3,
    ["Stonetalon Mountains"] = 4,
    ["Hillsbrad Foothills"] = 5,
    ["Thousand Needles"] = 6,
    ["Desolace"] = 7,
    ["Tanaris"] = 8,
    ["Feralas"] = 9,
    ["Felwood"] = 10,
    ["Western Plaguelands"] = 11,
    ["Silithus"] = 12,
    ["Azshara"] = 13,
    ["Winterspring"] = 14,
    ["Tirisfal Glades"] = 15,
}

-- 255 = current zone isn't one of the known farming zones on the decoding
-- side.
function GetCurrentZoneId()
    -- GetRealZoneText(), not GetZoneText(), so subzone/instance overlap
    -- doesn't change the result -- location configs are zone-level, not
    -- subzone-level.
    local zoneName = GetRealZoneText()
    return ZONE_NAME_TO_ID[zoneName] or 255
end

--------------------------------------------------
-- Player facing as a normalized 2D direction
-- Returns X and Y where:
--   facing due East:  x =  1, y =  0
--   facing due North: x =  0, y = -1
--   facing due West:  x = -1, y =  0
--   facing due South: x =  0, y =  1
--------------------------------------------------
function GetPlayerFacingVector()
    local facing = GetPlayerFacing()
    if not facing then
        return 0, -1  -- default �north-ish�
    end

    local x = -math.sin(facing)
    local y = -math.cos(facing)

    -- Should already be unit length, but normalize defensively.
    --local len = math.sqrt(x*x + y*y)
    --if len > 0 then
    --    x, y = x / len, y / len
    --end

    return x, y
end

--------------------------------------------------
-- Convenience wrappers if you want to use them with CreateNumberIndicator:
-- (Each returns a single number)
--------------------------------------------------

function GetPlayerFacingX()
    local x, _ = GetPlayerFacingVector()
    return math.floor(x * 100) / 100  -- two decimals
end

function GetPlayerFacingY()
    local _, y = GetPlayerFacingVector()
    return math.floor(y * 100) / 100  -- two decimals
end

function GetPlayerFacingInRadians()
    return round2(GetPlayerFacing())
end

function GetPlayerFacingInDegrees()
    local facing = GetPlayerFacing()
    if (facing == nil) then
        return 0
    end
    return round2(GetPlayerFacing() * 180 / math.pi)
end

-- TEMP diagnostic switch: set true to print per-nameplate condition results.
local COUNT_ATTACKERS_DEBUG = false

-- Highest simultaneous nameplate WoW will assign a "nameplateN" unit token
-- to. Generous on purpose -- UnitExists() on a token past the real cap just
-- returns false harmlessly, so overshooting costs nothing.
local MAX_NAMEPLATE_INDEX = 40

function CountAttackers()
    local count = 0

    -- Iterate the stable "nameplateN" unit-token range directly instead of
    -- pulling plate.namePlateUnitToken off C_NamePlate.GetNamePlates()'s
    -- frames -- that field went nil for every plate after the Classic Era
    -- 1.15.9 nameplate rework (GetNamePlates() itself still enumerates
    -- plates fine, just not that convenience field anymore). nameplate1,
    -- nameplate2, etc. are documented, stable unit tokens independent of
    -- Blizzard's internal nameplate frame structure, so this should hold up
    -- through future nameplate reworks too.
    for i = 1, MAX_NAMEPLATE_INDEX do
        local unit = "nameplate" .. i

        if UnitExists(unit) then
            local canAttack = UnitCanAttack("player", unit)
            local inCombat = UnitAffectingCombat(unit)
            local targetingMe = UnitIsUnit(unit.."target", "player")

            if COUNT_ATTACKERS_DEBUG then
                print(string.format(
                    "  %s (%s): canAttack=%s inCombat=%s targetingMe=%s",
                    unit, UnitName(unit) or "?",
                    tostring(canAttack), tostring(inCombat), tostring(targetingMe)))
            end

            if canAttack and inCombat and targetingMe then
                count = count + 1
            end
        end
    end

    if COUNT_ATTACKERS_DEBUG then
        print("CountAttackers: total =", count)
    end

    return count
end

function IsFacingTarget()
    if not UnitExists("target") then
        text:SetText("not UnitExists(target)")
        return false
    end

    local px, py = UnitPosition("player")
    local tx, ty = UnitPosition("target")

    if not px or not tx then
        text:SetText("not px or not tx")
        return false
    end

    local playerFacing = GetPlayerFacing()       -- 0�2pi radians
    local angleToTarget = math.atan2(ty - py, tx - px)

    local diff = angleToTarget - playerFacing
    diff = (diff + math.pi) % (2 * math.pi) - math.pi  -- normalize

    text:SetText("success")

    return math.abs(diff) < (math.pi / 2)
end

function IsAttacking()
    return IsCurrentSpell(6603) -- Auto Attack
    --print(IsCurrentAction(9))
    --return IsCurrentAction(81)
end

function HasBuffNamed(buffName)
    local i = 1
    while true do
        local name, icon = UnitBuff("player", i)
        if not name then
            return false -- no more buffs; not found
        end
        if name == buffName then
            return true
        end
        i = i + 1
    end
end

-- World buffs WaitForWorldBuffThenLogoffTask (WowManagementTasks.cs) waits on -- selectable
-- via the /yyconfig "Desired world buff" selector (YoyokazooUI.lua/UIFunctions.lua) instead of
-- being hardcoded, same pattern as DYNAMITE_ITEM_CHOICES below. GetDesiredWorldBuffId()
-- (YoyokazooUI.lua) returns whichever one is currently selected; HasDesiredWorldBuff() checks
-- for that buff by name via HasBuffNamed().
WORLD_BUFF_CHOICES = {
    { id = "ony",  label = "Rallying Cry (Ony)",         buffName = "Rallying Cry of the Dragonslayer" },
    { id = "rend", label = "Warchief's Blessing (Rend)", buffName = "Warchief's Blessing" },
    { id = "zg",   label = "Spirit of Zandalar (ZG)",    buffName = "Spirit of Zandalar" },
}

-- Read by GetMultiBoolTwo() to pack into MultiBoolTwo's R6, decoded on the C# side into
-- WowWorldState.HasDesiredWorldBuff.
function HasDesiredWorldBuff()
    local desiredId = GetDesiredWorldBuffId()
    for _, choice in ipairs(WORLD_BUFF_CHOICES) do
        if choice.id == desiredId then
            return HasBuffNamed(choice.buffName)
        end
    end
    return false
end

-- overpower rank 1, 7384
-- fireblast rank 1, 2136
-- cone of cold rank 1,
function IsSpellUsable(spellId)
    return IsUsableSpell(spellId)
end

-- Whether the player knows a spell, matched by exact name instead of a
-- hardcoded spell ID -- scans the real spellbook (GetNumSpellTabs/
-- GetSpellTabInfo/GetSpellBookItemName) rather than guessing an ID, so it
-- doesn't go stale across ranks the way the hardcoded rank-1 IDs elsewhere
-- in this file can (see the Flame Shock rank TODO above GetMultiBoolOne's
-- Shaman dispatch). Used for spells where the possible spell ID(s) aren't
-- confidently known, e.g. CanCurePoison/CanCureDisease in ShamanFunctions.lua.
function IsSpellKnownByName(name)
    for tab = 1, GetNumSpellTabs() do
        local _, _, offset, numSpells = GetSpellTabInfo(tab)
        for i = offset + 1, offset + numSpells do
            if GetSpellBookItemName(i, BOOKTYPE_SPELL) == name then
                return true
            end
        end
    end

    return false
end

function AreEnemyNameplatesTurnedOn()
    return GetCVarBool("nameplateShowEnemies")
end

-- Whether our current target is actively engaged with US specifically (its
-- target is us), not just "tapped by us" (loot rights) or "in combat with
-- someone" -- true the instant it aggroes onto the player, even before any
-- hit lands either way. Same "unittarget" unit-token trick CountAttackers()
-- already uses for nameplates, applied to our actual target instead.
function CurrentTargetInCombatWithUs()
    if not UnitExists("target") then
        return false
    end

    return UnitIsUnit("targettarget", "player")
end

-- Known healing-potion tiers, selectable via the /yyconfig "Healing potion" selector
-- (YoyokazooUI.lua/UIFunctions.lua) instead of being auto-picked from player level.
-- GetHealingPotionItemId() (YoyokazooUI.lua) returns whichever one is currently
-- selected, defaulting to Major Healing Potion (13446) -- this used to auto-select
-- the highest tier whose minLevel <= player level, which meant a full stack of a
-- lower (still-owned) tier could mask actually being low on the one the player was
-- currently drinking; a manual selector, same as the Dynamite item one, sidesteps
-- that entirely.
HEALING_POTION_ITEM_CHOICES = {
    { id = 858,   label = "Lesser Healing Potion" },
    { id = 929,   label = "Healing Potion" },
    { id = 1710,  label = "Greater Healing Potion" },
    { id = 3928,  label = "Superior Healing Potion" },
    { id = 13446, label = "Major Healing Potion" },
}

function AreWeLowOnHealthPotions()
    local count = GetItemCount(GetHealingPotionItemId(), false)
    return count < 2
end

-- Known dynamite-tier consumables, selectable via the /yyconfig "Dynamite item"
-- selector (YoyokazooUI.lua/UIFunctions.lua) instead of being hardcoded here.
-- GetDynamiteItemId() (YoyokazooUI.lua) returns whichever one is currently
-- selected, defaulting to Dense Dynamite (18641) -- the item this used to be
-- hardcoded to.
DYNAMITE_ITEM_CHOICES = {
    { id = 4378,  label = "Heavy Dynamite" },
    { id = 4384,  label = "Explosive Sheep" },
    { id = 4380,  label = "Big Bronze Bomb" },
    { id = 10507, label = "Solid Dynamite" },
    { id = 18641, label = "Dense Dynamite" },
    { id = 10562, label = "Hi-Explosive Bomb" },
}

function AreWeLowOnDynamite()
    local dynamiteCount = GetItemCount(GetDynamiteItemId(), false)
    return dynamiteCount < 2
end

-- light shot, 2516
-- rough arrow, 2512
-- sharp arrow, 2515
function AreWeLowOnAmmo()
    local ammoCount = GetItemCount(2515, false)
    return ammoCount < 2
end

function TargetHasDebuffSpellId(debuffSpellId)
  for i = 1, 40 do
    local _, _, _, _, _, _, _, _, _, spellId = UnitDebuff("target", i)
    if not spellId then break end

    if spellId == debuffSpellId then
      return true
    end
  end

  return false
end

function TargetHasDebuffSpellName(debuffSpellName)
  for i = 1, 40 do
    local spellName, _, _, _, _, _, _, _, _, _ = UnitDebuff("target", i)
    if not spellName then break end

    if spellName == debuffSpellName then
      return true
    end
  end

  return false
end

-- TargetHasRend() moved to WarriorFunctions.lua.
-- TargetHasFlameShock() moved to ShamanFunctions.lua.

-- CASTER_MOB_NAMES/RUNNER_MOB_NAMES/FIRE_IMMUNE_MOB_NAMES now live in
-- CreatureConfig.lua -- one place to update every name-based creature list.

-- True if the current target is a caster mob with interruptible spells (per
-- CASTER_MOB_NAMES in CreatureConfig.lua), regardless of whether it's
-- currently casting.
function IsTargetCasterMob()
    if not UnitExists("target") then
        return false
    end

    local name = UnitName("target")
    if not name then
        return false
    end

    return CASTER_MOB_NAMES[name] == true
end

-- True if the current target is a runner mob (per RUNNER_MOB_NAMES in
-- CreatureConfig.lua).
function IsTargetRunnerMob()
    if not UnitExists("target") then
        return false
    end

    local name = UnitName("target")
    if not name then
        return false
    end

    return RUNNER_MOB_NAMES[name] == true
end

-- True if the current target is fire-immune (per FIRE_IMMUNE_MOB_NAMES in
-- CreatureConfig.lua).
function IsTargetFireImmune()
    if not UnitExists("target") then
        return false
    end

    local name = UnitName("target")
    if not name then
        return false
    end

    return FIRE_IMMUNE_MOB_NAMES[name] == true
end

-- True if the current target is nature-immune (per NATURE_IMMUNE_MOB_NAMES in
-- CreatureConfig.lua).
function IsTargetNatureImmune()
    if not UnitExists("target") then
        return false
    end

    local name = UnitName("target")
    if not name then
        return false
    end

    return NATURE_IMMUNE_MOB_NAMES[name] == true
end

-- True if the current target is bleed-immune (per BLEED_IMMUNE_MOB_NAMES in
-- CreatureConfig.lua) -- not worth (re)applying Rend to these.
function IsTargetBleedImmune()
    if not UnitExists("target") then
        return false
    end

    local name = UnitName("target")
    if not name then
        return false
    end

    return BLEED_IMMUNE_MOB_NAMES[name] == true
end

-- True if the current target casts a Fear-type effect (per
-- FEAR_CASTER_MOB_NAMES in CreatureConfig.lua) -- worth opening with
-- Berserker Rage rather than reacting after the fact.
function IsTargetFearCaster()
    if not UnitExists("target") then
        return false
    end

    local name = UnitName("target")
    if not name then
        return false
    end

    return FEAR_CASTER_MOB_NAMES[name] == true
end

-- True if the current target has a ranged attack whose range exceeds Earth
-- Shock's (per LONG_RANGE_CASTER_MOB_NAMES in CreatureConfig.lua).
function IsTargetLongRangeCaster()
    if not UnitExists("target") then
        return false
    end

    local name = UnitName("target")
    if not name then
        return false
    end

    return LONG_RANGE_CASTER_MOB_NAMES[name] == true
end

-- True if any mob from LOGOFF_IF_SEEN_MOB_NAMES (CreatureConfig.lua) is
-- currently visible on a nameplate, OR is our current target -- unlike
-- IsTargetXxx above, neither of these requires the mob be our current
-- target specifically, just present nearby, since the whole point is to
-- bail before we ever engage it. The target check is needed alongside the
-- nameplate scan since we can have something targeted (e.g. via TAB/macro,
-- or a stale target from before it wandered off) beyond nameplate range.
-- Same "nameplateN" unit-token iteration CountAttackers() above uses.
function IsLogoffMobSeen()
    local targetName = UnitExists("target") and UnitName("target")
    if targetName and LOGOFF_IF_SEEN_MOB_NAMES[targetName] then
        return true
    end

    for i = 1, MAX_NAMEPLATE_INDEX do
        local unit = "nameplate" .. i
        if UnitExists(unit) then
            local name = UnitName(unit)
            if name and LOGOFF_IF_SEEN_MOB_NAMES[name] then
                return true
            end
        end
    end

    return false
end

-- True if the target is currently casting or channeling a spell.
function IsTargetCasting()
    return UnitCastingInfo("target") ~= nil
        or UnitChannelInfo("target") ~= nil
end

-- Was checking Lightning Shield's (324) cooldown as a stand-in for the GCD --
-- broken for any non-Shaman character (and low-level Shamans without it
-- yet), since it depends on the character actually knowing a specific
-- class's spell. Query the GCD's own spell ID directly instead, same
-- technique SpellIsCooledDownIgnoringGCD already uses above -- class-
-- agnostic, no GetSpellInfo() lookup needed.
function IsGlobalCooldownCooledDown()
    local _, classFile = UnitClass("player")

    -- GCD_SPELL_ID never showed a cooldown for ANY cast in testing (Rockbiter Weapon or
    -- otherwise) on this client -- confirmed via debug logging, start/duration stayed 0
    -- throughout. Probing a real, always-known low-level spell's own cooldown works instead,
    -- since the GCD blocks it too while active -- as long as the probe spell has no cooldown of
    -- its own beyond the GCD (a spell with a real independent cooldown, e.g. Charge, would read
    -- as "on cooldown" long after the GCD itself clears, so it's not a safe pick here).
    --
    -- Confirmed via testing: Shaman (Lightning Bolt Rank 1, same spell ID CanSpellcastPullTarget()
    -- already uses above). NOT yet confirmed via testing: Warrior (Heroic Strike, known from
    -- level 1, rage-gated with no cooldown beyond GCD -- already referenced by name in
    -- WarriorFunctions.lua), and Warlock (Shadow Bolt Rank 1, same spell ID
    -- CanSpellcastPullTarget() uses above, known from level 1, no cooldown beyond GCD). Verify
    -- these the same way Shaman was (debug log around a cast, watch GCDCooledDown flip
    -- false->true) before trusting them.
    local probeSpell = GCD_SPELL_ID
    if classFile == "SHAMAN" then
        probeSpell = 403 -- Lightning Bolt (Rank 1)
    elseif classFile == "WARRIOR" then
        probeSpell = "Heroic Strike" -- UNTESTED
    elseif classFile == "WARLOCK" then
        probeSpell = 686 -- Shadow Bolt (Rank 1) -- UNTESTED
    end

    local start, duration = GetSpellCooldown(probeSpell)

    if not start or duration == 0 then
        return true
    end

    local remaining = start + duration - GetTime()
    return remaining <= 0
end

-- CanCastMortalStrikeOrBloodthirst() moved to WarriorFunctions.lua.
-- (CanCastWhirlwind()/CanCastSweepingStrikes() used to live there too --
-- removed, see the GetWarriorClassBoolOne() comment in WarriorFunctions.lua.)
-- CanCastEarthShock() moved to ShamanFunctions.lua.

function GetFreeSlotsInBag(bag)
    local total = C_Container.GetContainerNumSlots(bag)
    local free = 0

    for slot = 1, total do
        local itemInfo = C_Container.GetContainerItemInfo(bag, slot)
        if not itemInfo then
            free = free + 1
        end
    end

    return free
end

function GetTotalFreeBagSlots()
    local free = 0

    -- backpack
    free = free + GetFreeSlotsInBag(0)

    -- equipped bags
    for bag = 1, 4 do
        free = free + GetFreeSlotsInBag(bag)
    end

    return free
end

-- GetTotalFreeBagSlots() calls C_Container.GetContainerItemInfo() per bag slot,
-- which allocates a fresh table for every occupied slot -- the single biggest
-- source of the addon's GC churn (confirmed while investigating the ~1MB/sec
-- addon-memory growth shown in the game's addon-memory tooltip). Bag contents
-- don't change on the ~50-100ms cadence AreBagsFull() gets polled at (it's one
-- of the flags packed into GetMultiBoolOne, read by both the pixel-row and
-- debug OnUpdate loops in UIFunctions.lua), so cache the real check and only
-- recompute it periodically instead of every tick.
local BAGS_FULL_CHECK_INTERVAL_SECONDS = 30
local lastBagsFullCheckTime = nil
local cachedBagsFull = false

function AreBagsFull()
    local now = GetTime()
    if not lastBagsFullCheckTime or (now - lastBagsFullCheckTime) >= BAGS_FULL_CHECK_INTERVAL_SECONDS then
        cachedBagsFull = GetTotalFreeBagSlots() == 0
        lastBagsFullCheckTime = now
    end

    return cachedBagsFull
end

-- Item names, beyond plain quality-0 (Poor/gray) junk, that are also worth
-- auto-selling to an open merchant -- e.g. cooking/fishing byproducts that
-- vendor for a few silver and are otherwise just dead bag space. Matched by
-- name (via GetItemInfo(itemID), the same name the tooltip shows) rather
-- than item ID -- unlike DYNAMITE_ITEM_CHOICES/HEALING_POTION_ITEM_CHOICES
-- above, this is a short, manually-curated whitelist rather than a
-- runtime-selectable /yyconfig choice, so there's no id-keyed selector UI
-- to match against. Add more names here as they come up.
AUTO_SELL_WHITELIST_ITEM_NAMES = {
    "Tangy Clam Meat",
    "Raw Bristle Whisker Catfish",
    "Turtle Meat",
    "Light Leather",
    "Medium Leather",
    "Light Hide",
    "Medium Hide",
    "Heavy Hide",
    "Stringy Vulture Meat",
    "Mystery Meat",
    "Raw Rockscale Cod",
    "Heavy Leather",
    "Large Fang",
    "Long Tail Feather",
    "Sharp Claw",
}

local function IsAutoSellWhitelistedByName(itemName)
    if not itemName then
        return false
    end

    for _, name in ipairs(AUTO_SELL_WHITELIST_ITEM_NAMES) do
        if name == itemName then
            return true
        end
    end

    return false
end

-- Set true to print every step of the auto-sell path to chat: what the bag
-- scan actually sees in each occupied slot, why each slot was or wasn't
-- queued, and (over in YoyokazooUI.lua) the MERCHANT_SHOW/sell-tick side of
-- it. Same manually-flipped debug-flag pattern as COMBAT_STALEMATE_DEBUG in
-- YoyokazooUI.lua, but global rather than local so YoyokazooUI.lua's merchant
-- handling can share the one flag. Confirmed working live (2026-09-21) --
-- root cause was the MERCHANT_SHOW-fires-before-MerchantFrame:Show() race,
-- see MERCHANT_NOT_SHOWN_MAX_RETRIES in YoyokazooUI.lua -- so this defaults
-- off; flip back to true if auto-sell needs debugging again.
AUTO_SELL_DEBUG = false

function AutoSellDebugPrint(message)
    if AUTO_SELL_DEBUG then
        print("|cff66ff99[autosell]|r " .. tostring(message))
    end
end

-- Dumps every key/value pair of a C_Container.GetContainerItemInfo() result
-- instead of reading named fields off it -- the whole point is to find out
-- what this client actually calls them (quality vs itemQuality, hasNoValue vs
-- noValue, ...), so nothing here may assume any particular field exists. Also
-- reports the case where the API handed back something that isn't a table at
-- all, which is what older Classic builds' multiple-return-value version of
-- this function would look like from here.
function AutoSellDescribeItemInfo(itemInfo)
    if itemInfo == nil then
        return "nil"
    end

    if type(itemInfo) ~= "table" then
        return "NOT A TABLE (" .. type(itemInfo) .. "): " .. tostring(itemInfo)
    end

    local parts = {}
    for key, value in pairs(itemInfo) do
        table.insert(parts, tostring(key) .. "=" .. tostring(value))
    end
    table.sort(parts)

    if #parts == 0 then
        return "empty table"
    end

    return table.concat(parts, ", ")
end

-- Whether a single bag slot's item should be auto-sold to an open merchant:
-- plain quality 0 (Poor/gray) junk, or a quality 1 (Common/white) item on
-- AUTO_SELL_WHITELIST_ITEM_NAMES above. itemInfo is the table returned by
-- C_Container.GetContainerItemInfo(bag, slot) (see GetFreeSlotsInBag() above
-- for the same API) -- hasNoValue items (quest items, etc, which a vendor
-- won't buy regardless of quality) are skipped even if gray. Not yet
-- confirmed live against this client's actual itemInfo table shape -- flip
-- AUTO_SELL_DEBUG on above and read the per-slot dumps rather than guessing
-- at the field names.
function ShouldAutoSellItem(itemInfo)
    if not itemInfo then
        return false
    end

    if itemInfo.hasNoValue then
        AutoSellDebugPrint("    skipped: hasNoValue is set")
        return false
    end

    if itemInfo.quality == 0 then
        AutoSellDebugPrint("    QUEUED: quality 0 (gray junk)")
        return true
    end

    if itemInfo.quality == 1 then
        -- GetItemInfo returns nil for an item the client hasn't cached yet;
        -- that shows up as name=nil here rather than as a silent non-match.
        local itemName = GetItemInfo(itemInfo.itemID)
        local whitelisted = IsAutoSellWhitelistedByName(itemName)
        AutoSellDebugPrint("    quality 1, name=" .. tostring(itemName) .. " -- " ..
            (whitelisted and "QUEUED (whitelisted)" or "skipped (not whitelisted)"))
        return whitelisted
    end

    AutoSellDebugPrint("    skipped: quality=" .. tostring(itemInfo.quality) ..
        " (" .. type(itemInfo.quality) .. ")")
    return false
end

-- Scans bags 0-4 (backpack + equipped bags -- same range GetTotalFreeBagSlots()
-- above uses) for everything ShouldAutoSellItem() flags, returning a flat list
-- of { bag = ..., slot = ... } entries to sell. Doesn't sell anything itself --
-- see the queued, one-per-tick sell loop in YoyokazooUI.lua's MERCHANT_SHOW
-- handling (selling everything in a single loop iteration is known to
-- silently drop some sells).
function FindAutoSellQueue()
    local queue = {}

    if not (C_Container and C_Container.GetContainerItemInfo and C_Container.GetContainerNumSlots) then
        AutoSellDebugPrint("C_Container.GetContainerItemInfo/GetContainerNumSlots missing -- " ..
            "wrong bag API for this client build")
        return queue
    end

    for bag = 0, 4 do
        local total = C_Container.GetContainerNumSlots(bag)
        AutoSellDebugPrint("bag " .. bag .. ": " .. tostring(total) .. " slots")
        for slot = 1, (total or 0) do
            local itemInfo = C_Container.GetContainerItemInfo(bag, slot)
            if itemInfo ~= nil then
                AutoSellDebugPrint("  " .. bag .. ":" .. slot .. " " .. AutoSellDescribeItemInfo(itemInfo))
            end
            if ShouldAutoSellItem(itemInfo) then
                table.insert(queue, { bag = bag, slot = slot })
            end
        end
    end

    AutoSellDebugPrint("scan finished: " .. #queue .. " slot(s) queued")

    return queue
end

-- True once LATENCY_HIGH_CYCLE_COUNT consecutive latency samples, one taken every
-- LATENCY_CHECK_INTERVAL_SECONDS, have all read above LATENCY_HIGH_THRESHOLD_MS -- i.e.
-- LATENCY_HIGH_CYCLE_COUNT * LATENCY_CHECK_INTERVAL_SECONDS = 10 sustained seconds of bad
-- latency. Same interval-cached pattern as AreBagsFull() above, for the same reason:
-- GetNetStats()'s latencyHome value doesn't change anywhere near the ~50ms pixel-row poll
-- cadence, so sampling on that cadence would just recount the same stale reading. Uses
-- latencyHome (the player's own connection to their realm's datacenter) rather than
-- latencyWorld (Blizzard's internal server-hop latency, noisy for reasons outside the
-- player's own connection). Not latched -- a streak that breaks resets the count to 0 and
-- this goes back to false; nothing needs it to stay true once WowManagementTasks.cs's own
-- LogoutTriggered (C#) latches on the first tick it sees this true.
local LATENCY_CHECK_INTERVAL_SECONDS = 1
local LATENCY_HIGH_THRESHOLD_MS = 300
local LATENCY_HIGH_CYCLE_COUNT = 10

local lastLatencyCheckTime = nil
local consecutiveHighLatencyCycles = 0

function HasHighLatency()
    local now = GetTime()
    if not lastLatencyCheckTime or (now - lastLatencyCheckTime) >= LATENCY_CHECK_INTERVAL_SECONDS then
        lastLatencyCheckTime = now

        local _, _, latencyHome = GetNetStats()
        if latencyHome > LATENCY_HIGH_THRESHOLD_MS then
            consecutiveHighLatencyCycles = consecutiveHighLatencyCycles + 1
        else
            consecutiveHighLatencyCycles = 0
        end
    end

    return consecutiveHighLatencyCycles >= LATENCY_HIGH_CYCLE_COUNT
end

function IsPlayerPetrified()
    for i = 1, 40 do
        local name = UnitAura("player", i)
        if not name then
            return false
        end

        if name == "Petrification" then
            return true
        end
    end

    return false
end

function IsPlayerCasting()
    return UnitCastingInfo("player") ~= nil
        or UnitChannelInfo("player") ~= nil
end

-- True while the player's Skinning cast (the right-click-on-corpse action,
-- shown as a regular cast bar, not a channel) is in progress -- name-matched
-- rather than a spell ID since Skinning isn't cast via a normal spellbook
-- entry/ID the way e.g. CanCurePoison's IsSpellKnownByName() match is.
function IsCurrentlySkinning()
    local name = UnitCastingInfo("player")
    return name == "Skinning"
end

-- True if the player has a debuff of the given dispel type (e.g. "Poison",
-- "Disease", "Magic", "Curse") -- same UnitDebuff() return-value positions
-- as TargetHasDebuffSpellId/Name above (debuffType is the 4th value) --
-- with duration/expirationTime as the 5th/6th values. Only counts if the
-- debuff has more than DEBUFF_TYPE_MIN_REMAINING_SECONDS left (a duration
-- of 0 means no duration/permanent, which always counts).
local DEBUFF_TYPE_MIN_REMAINING_SECONDS = 5

function PlayerHasDebuffType(debuffType)
  for i = 1, 40 do
    local name, _, _, thisDebuffType, duration, expirationTime = UnitDebuff("player", i)
    if not name then break end

    if thisDebuffType == debuffType then
      if duration == 0 or (expirationTime - GetTime()) > DEBUFF_TYPE_MIN_REMAINING_SECONDS then
        return true
      end
    end
  end

  return false
end

function IsPlayerPoisoned()
    return PlayerHasDebuffType("Poison")
end

function IsPlayerDiseased()
    return PlayerHasDebuffType("Disease")
end

-- HasRockbiterWeaponMainHand(), ShouldCastRockbiterWeapon(),
-- ShouldCastLightningShield(), and ShouldCastFlameShock() moved to
-- ShamanFunctions.lua.

-- Class-specific fields (Battle Shout, Rend, Frost Armor, Rockbiter, etc.)
-- moved to GetClassBoolOne/Two (see the dispatchers further down and
-- GetXClassBoolOne/Two in WarriorFunctions.lua/ShamanFunctions.lua/
-- WarlockFunctions.lua) -- those are used instead of these now. All
-- class-agnostic fields fit in MultiBoolOne's R+G bytes; R+G are both fully
-- packed, and the B byte carries HasRecentTargetEvade() (b1), two of the
-- three supported classes the player might be playing (b2 Warrior, b4
-- Shaman -- b3 is reserved/unused, previously Mage, removed along with Mage
-- support; the 3rd class, Warlock, didn't fit once this byte was already
-- full, so it lives in GetMultiBoolTwo's R4 instead -- see there), and
-- IsPlayerPoisoned/IsPlayerDiseased/IsTargetNatureImmune/IsTargetCasting
-- (b5-b8), which fully packs the byte.
function GetMultiBoolOne()
    local boolR1 = IsAttacking()
    local boolR2 = AreWeLowOnHealthPotions()
    local boolR3 = AreWeLowOnDynamite()
    local boolR4 = AreWeLowOnAmmo()
    local boolR5 = IsGlobalCooldownCooledDown()
    local boolR6 = AreBagsFull()
    local boolR7 = IsInCombat()
    local boolR8 = IsPlayerPetrified()

    local rByte = EncodeBooleansToByte(boolR1, boolR2, boolR3, boolR4, boolR5, boolR6, boolR7, boolR8)

    local boolG1 = HasUnseenWhisper()
    local boolG2 = false -- reserved, previously IsInMeleeRange() -- removed, CheckInteractDistance
                          -- proved unreliable; WalkIntoMeleeRangeTask (WowMovementTasks.cs) now
                          -- relies on WorldState.IsInCombat plus the target-marker bearing instead
    local boolG3 = IsPlayerCasting()
    local boolG4 = AreEnemyNameplatesTurnedOn()
    local boolG5 = CurrentTargetInCombatWithUs()
    local boolG6 = IsTargetCasterMob()
    local boolG7 = IsTargetRunnerMob()
    local boolG8 = IsTargetFireImmune()

    local gByte = EncodeBooleansToByte(boolG1, boolG2, boolG3, boolG4, boolG5, boolG6, boolG7, boolG8)

    local boolB1 = HasRecentTargetEvade()

    local _, classFile = UnitClass("player")
    local boolB2 = (classFile == "WARRIOR")
    local boolB3 = false -- reserved, previously (classFile == "MAGE")
    local boolB4 = (classFile == "SHAMAN")

    local boolB5 = IsPlayerPoisoned()
    local boolB6 = IsPlayerDiseased()
    local boolB7 = IsTargetNatureImmune()
    local boolB8 = IsTargetCasting()

    local bByte = EncodeBooleansToByte(boolB1, boolB2, boolB3, boolB4, boolB5, boolB6, boolB7, boolB8)

    return rByte/255.0, gByte/255.0, bByte/255.0
end

-- R1 (IsTargetLongRangeCaster), R2 (IsLogoffMobSeen), R3 (IsCurrentlySkinning),
-- R4 (the 4th "which supported class" bit, Warlock -- see GetMultiBoolOne's
-- B-byte comment above for why it landed here instead of there), R5
-- (IsTargetBleedImmune), and R6 (IsTargetFearCaster) are the flags packed
-- into the R byte so far -- both mob-name lookups, same pattern as
-- IsTargetLongRangeCaster/IsLogoffMobSeen above, so they live here
-- (class-agnostic) rather than in a class's own ClassBool even though only
-- Warrior consumes them today -- R7-R8 are still reserved. G1
-- (IsLogoutOnLowDynamiteEnabled), G2 (IsLogoutOnFullBagsEnabled), and G3
-- (HasDesiredWorldBuff) are packed into the previously-unused G byte instead
-- of continuing into R7/R8 -- run-specific settings toggled live in-game via
-- the /yyconfig menu (YoyokazooUI.lua) rather than live game-state queries
-- like everything else in this row. G4 (HasHighLatency(), see there) is back
-- to being a live game-state query, same category as the R byte -- it just
-- landed in G because the R byte was already full by the time it was added.
-- G5 (IsCombatStalemate(), YoyokazooUI.lua) is likewise a live game-state
-- query -- in combat, but no damage/miss combat-log events involving us for
-- COMBAT_STALEMATE_SECONDS, e.g. aggroed by a mob that can't path to us.
-- G6-G8 and the B byte are still fully reserved for future class-agnostic flags.
function GetMultiBoolTwo()
    local boolR1 = IsTargetLongRangeCaster()
    local boolR2 = IsLogoffMobSeen()
    local boolR3 = IsCurrentlySkinning()

    local _, classFile = UnitClass("player")
    local boolR4 = (classFile == "WARLOCK")

    local boolR5 = IsTargetBleedImmune()
    local boolR6 = IsTargetFearCaster()

    local rByte = EncodeBooleansToByte(boolR1, boolR2, boolR3, boolR4, boolR5, boolR6, false, false)

    local boolG1 = IsLogoutOnLowDynamiteEnabled()
    local boolG2 = IsLogoutOnFullBagsEnabled()
    local boolG3 = HasDesiredWorldBuff()
    local boolG4 = HasHighLatency()
    local boolG5 = IsCombatStalemate()

    local gByte = EncodeBooleansToByte(boolG1, boolG2, boolG3, boolG4, boolG5, false, false, false)

    return rByte/255.0, gByte/255.0, 0
end

function GetMultiIntOne()
    local r = GetPlayerHealthPercent()
    local g = GetPlayerResourcePercent()
    local b = GetTargetHealthPercent()

    return r/255.0, g/255.0, b/255.0
end

function GetMultiIntTwo()
    local r = CountAttackers()
    local g = UnitLevel("player")
    local b = GetCurrentZoneId()

    return r/255.0, g/255.0, b/255.0
end

------------------------------------------------------------
-- ClassBool/ClassInt dispatchers -- the class-specific counterpart to
-- GetMultiBoolOne/Two/GetMultiIntOne/Two above. Each checks the player's
-- class once and delegates to that class's own populate function (see
-- GetWarriorClassBoolOne/Two, GetShamanClassBoolOne/Two, and
-- GetWarlockClassBoolOne/Two in WarriorFunctions.lua/ShamanFunctions.lua/
-- WarlockFunctions.lua). Unsupported/unrecognized classes get all-zero.
------------------------------------------------------------
function GetClassBoolOne()
    local _, classFile = UnitClass("player")

    if classFile == "WARRIOR" then
        return GetWarriorClassBoolOne()
    elseif classFile == "SHAMAN" then
        return GetShamanClassBoolOne()
    elseif classFile == "WARLOCK" then
        return GetWarlockClassBoolOne()
    end

    return 0, 0, 0
end

function GetClassBoolTwo()
    local _, classFile = UnitClass("player")

    if classFile == "WARRIOR" then
        return GetWarriorClassBoolTwo()
    elseif classFile == "SHAMAN" then
        return GetShamanClassBoolTwo()
    elseif classFile == "WARLOCK" then
        return GetWarlockClassBoolTwo()
    end

    return 0, 0, 0
end

function GetClassIntOne()
    local _, classFile = UnitClass("player")

    if classFile == "WARRIOR" then
        return GetWarriorClassIntOne()
    elseif classFile == "SHAMAN" then
        return GetShamanClassIntOne()
    elseif classFile == "WARLOCK" then
        return GetWarlockClassIntOne()
    end

    return 0, 0, 0
end

function IsAttackingColor()
    return GetColorFromSingleBool(IsAttacking())
end

function IsInCombatColor()
    return GetColorFromSingleBool(IsInCombat())
end

-- CanChargeTargetColor() and IsAnyNextSwingSpellQueuedColor() moved to
-- WarriorFunctions.lua.