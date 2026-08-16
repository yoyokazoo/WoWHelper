function IsInMeleeRange()
    if UnitExists("target") and UnitCanAttack("player", "target") then
        -- 3 = "duel / inspect / trade" distance, roughly 5 yards (melee-ish)
        if CheckInteractDistance("target", 3) then
            return true
        end
    end
    return false
end

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

    -- don't kill greys
    if UnitLevel("player") - UnitLevel(unit) >= 8 then
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

-- Shared between Mage and Shaman (branches internally below), so it stays
-- here rather than moving to a single class file -- see the note atop
-- MageFunctions.lua/ShamanFunctions.lua about why it isn't split further.
function CanSpellcastPullTarget()
    if not ShouldWeAttackTarget() then
        return false
    end

    local _, classFile = UnitClass("player")

    local spellId

    if classFile == "SHAMAN" then
        spellId = 403 -- Lightning Bolt (Rank 1)
    else
        -- Mage logic
        -- Frostbolt (Rank 1) learned at level 4
        if UnitLevel("player") < 4 then
            spellId = 133 -- Fireball (Rank 1)
        else
            spellId = 116 -- Frostbolt (Rank 1)
        end
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

-- overpower rank 1, 7384
-- fireblast rank 1, 2136
-- cone of cold rank 1, 
function IsSpellUsable(spellId)
    return IsUsableSpell(spellId)
end

function AreEnemyNameplatesTurnedOn()
    return GetCVarBool("nameplateShowEnemies")
end

function AreWeLowOnHealthPotions()
    local level = UnitLevel("player")
    -- who cares before 5
    if level < 10 then -- temp
        return false
    end

    local healthPotId = 13446 -- major healing potion, level 45

    if level < 12 then
        healthPotId = 858 -- lesser healing potion, level 3
    elseif level < 21 then
        healthPotId = 929 -- healing potion, level 12
    elseif level < 35 then
        healthPotId = 1710 -- greater healing potion, level 21
    elseif level < 45 then
        healthPotId = 3928 -- superior healing potion, level 35
    end

    local healthPotCount = GetItemCount(healthPotId, false)
    return healthPotCount < 2
end

-- heavy dynamite, 4378
-- explosive sheep, 4384
-- big bronze bomb, 4380
-- solid dynamite, 10507
-- dense dynamite, 18641
-- hi-explosive bomb, 10562 
function AreWeLowOnDynamite()
    local dynamiteCount = GetItemCount(10507, false)
    return dynamiteCount < 2
end

-- light shot, 2516
-- rough arrow, 2512
function AreWeLowOnAmmo()
    local ammoCount = GetItemCount(2512, false)
    return ammoCount < 2
end

-- Level 1 Conjured Water, 5350
-- Level 5 Conjured Fresh Water, 2288
-- Level 15 Conjured Purified Water, 2136
-- Level 25 Conjured Spring Water, 3772
-- Level 35 Conjured Mineral Water, 8077
-- Level 45 Conjured Sparkling Water, 8078
-- Level 55 Conjured Crystal Water, 8079
-- ShouldWeSummonWater() and ShouldWeSummonFood() moved to MageFunctions.lua.

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

-- Was checking Lightning Shield's (324) cooldown as a stand-in for the GCD --
-- broken for any non-Shaman character (and low-level Shamans without it
-- yet), since it depends on the character actually knowing a specific
-- class's spell. Query the GCD's own spell ID directly instead, same
-- technique SpellIsCooledDownIgnoringGCD already uses above -- class-
-- agnostic, no GetSpellInfo() lookup needed.
function IsGlobalCooldownCooledDown()
    local start, duration = GetSpellCooldown(GCD_SPELL_ID)

    if not start or duration == 0 then
        return true
    end

    local remaining = start + duration - GetTime()
    return remaining <= 0
end

-- CanCastWhirlwind(), CanCastSweepingStrikes(), and
-- CanCastMortalStrikeOrBloodthirst() moved to WarriorFunctions.lua.
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

function AreBagsFull()
    return GetTotalFreeBagSlots() == 0
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

-- HasRockbiterWeaponMainHand(), ShouldCastRockbiterWeapon(),
-- ShouldCastLightningShield(), and ShouldCastFlameShock() moved to
-- ShamanFunctions.lua.

-- Class-specific fields (Battle Shout, Rend, Frost Armor, Rockbiter, etc.)
-- moved to GetClassBoolOne/Two (see the dispatchers further down and
-- GetXClassBoolOne/Two in WarriorFunctions.lua/MageFunctions.lua/
-- ShamanFunctions.lua) -- C# now reads those instead of these. Only 12
-- class-agnostic fields are left, which all fit in MultiBoolOne's R+G bytes,
-- so they're packed tightly there and MultiBoolTwo is left fully reserved
-- (rather than each carrying a few scattered bits) as clean room to grow
-- into for the next class-agnostic field, instead of needing a 3rd pixel.
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
    local boolG2 = IsInMeleeRange()
    local boolG3 = IsPlayerCasting()
    local boolG4 = AreEnemyNameplatesTurnedOn()
    local boolG5 = false
    local boolG6 = false
    local boolG7 = false
    local boolG8 = false

    local gByte = EncodeBooleansToByte(boolG1, boolG2, boolG3, boolG4, boolG5, boolG6, boolG7, boolG8)

    -- B byte fully reserved too.
    return rByte/255.0, gByte/255.0, 0
end

-- Fully reserved for future class-agnostic flags -- nothing packed here yet.
function GetMultiBoolTwo()
    return 0, 0, 0
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
    local b = 0

    return r/255.0, g/255.0, b/255.0
end

------------------------------------------------------------
-- ClassBool/ClassInt dispatchers -- the class-specific counterpart to
-- GetMultiBoolOne/Two/GetMultiIntOne/Two above. Each checks the player's
-- class once and delegates to that class's own populate function (see
-- GetWarriorClassBoolOne/Two, GetMageClassBoolOne/Two, and
-- GetShamanClassBoolOne/Two in WarriorFunctions.lua/MageFunctions.lua/
-- ShamanFunctions.lua). Unsupported/unrecognized classes get all-zero. C#
-- reads these via WowPlayer.ClassState (see WowClassState.cs and friends).
------------------------------------------------------------
function GetClassBoolOne()
    local _, classFile = UnitClass("player")

    if classFile == "WARRIOR" then
        return GetWarriorClassBoolOne()
    elseif classFile == "MAGE" then
        return GetMageClassBoolOne()
    elseif classFile == "SHAMAN" then
        return GetShamanClassBoolOne()
    end

    return 0, 0, 0
end

function GetClassBoolTwo()
    local _, classFile = UnitClass("player")

    if classFile == "WARRIOR" then
        return GetWarriorClassBoolTwo()
    elseif classFile == "MAGE" then
        return GetMageClassBoolTwo()
    elseif classFile == "SHAMAN" then
        return GetShamanClassBoolTwo()
    end

    return 0, 0, 0
end

function GetClassIntOne()
    local _, classFile = UnitClass("player")

    if classFile == "WARRIOR" then
        return GetWarriorClassIntOne()
    elseif classFile == "MAGE" then
        return GetMageClassIntOne()
    elseif classFile == "SHAMAN" then
        return GetShamanClassIntOne()
    end

    return 0, 0, 0
end

function IsAttackingColor()
    return GetColorFromSingleBool(IsAttacking())
end

function IsInMeleeRangeColor()
    return GetColorFromSingleBool(IsInMeleeRange())
end

function IsInCombatColor()
    return GetColorFromSingleBool(IsInCombat())
end

-- CanChargeTargetColor() and IsAnyNextSwingSpellQueuedColor() moved to
-- WarriorFunctions.lua.