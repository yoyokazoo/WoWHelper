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

-- 100 is level 1 charge, but still works since range doesnt change and shares cooldown
function CanChargeTarget()
    if not ShouldWeAttackTarget() then
        return false
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

-- 116
function CanFrostboltTarget()
    if not ShouldWeAttackTarget() then
        return false
    end

    -- it goes on cooldown right when we start casting it, so we can assume its cooled down
    return SpellIsInRange(116)
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

--------------------------------------------------
-- Health: player (percent 0–100)
--------------------------------------------------
function GetPlayerHealthPercent()
    local hp  = UnitHealth("player")
    local max = UnitHealthMax("player")
    if max == 0 then return 0 end
    return math.floor((hp / max) * 100 + 0.5)
end

--------------------------------------------------
-- Resource: player (rage / mana / energy, percent 0–100)
--------------------------------------------------
function GetPlayerResourcePercent()
    local powerType = UnitPowerType("player")            -- e.g. 0 = mana, 1 = rage, 3 = energy
    local cur = UnitPower("player", powerType)
    local max = UnitPowerMax("player", powerType)

    if max == 0 then return 0 end
    return math.floor((cur / max) * 100 + 0.5)
end

--------------------------------------------------
-- Target health (percent 0–100)
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
-- Map X coord (0–100, normalized across map)
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

    -- pos.x is 0–1 across the map; convert to 0–100
    return math.floor((pos.x or 0) * 10000 + 0.5) / 100  -- two decimals
end

--------------------------------------------------
-- Map Y coord (0–100, normalized across map)
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

    -- pos.y is 0–1; convert to 0–100
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
        return 0, -1  -- default “north-ish”
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

function CountAttackers()
    local count = 0
    local plates = C_NamePlate.GetNamePlates()

    for _, plate in ipairs(plates) do
        local unit = plate.namePlateUnitToken
        if unit
            and UnitCanAttack("player", unit)
            and UnitAffectingCombat(unit)
            and UnitIsUnit(unit.."target", "player")
        then
            count = count + 1
        end
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

    local playerFacing = GetPlayerFacing()       -- 0–2pi radians
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
    if level < 5 then
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
function ShouldWeSummonWater()
    local waterCount = GetItemCount(2288, false)
    return waterCount < 2
end

-- Level 1 Conjured Muffin, 5349
-- Level 5 Conjured Bread, 1113
-- Level 15 Conjured Rye, 1114
-- Level 25 Conjured Pumpernickel, 1487
-- Level 35 Conjured Sourdough, 8075
-- Level 45 Conjured Sweet Roll, 8076
-- Level 55 Conjured Cinnamon Roll, 22895
function ShouldWeSummonFood()
    local foodCount = GetItemCount(1113, false)
    return foodCount < 2
end

-- rank 1, 772
-- rank 2, 6546
-- rank 3, 6547
-- rank 4, 6548
-- rank 5, 11572
-- rank 6, 11573
-- rank 7, 11574
function TargetHasRend()
  for i = 1, 40 do
    local _, _, _, _, _, _, _, _, _, spellId = UnitDebuff("target", i)
    if not spellId then break end

    if spellId == 11574 then
      return true
    end
  end
  return false
end

-- Look at Rend, since it has no cooldown beyond GCD (772)
-- SpellIsCooledDown doesn't work if the character doesn't have the spell, so we need to find something everyone has
-- temporarily switching to frost armor (6116)
function IsGlobalCooldownCooledDown()
    return SpellIsCooledDown(6116)
end

-- WW rank 1, 1680
function CanCastWhirlwind()
    return SpellIsCooledDown(1680)
end

-- Sweeping Strikes, 12292
function CanCastSweepingStrikes()
    return SpellIsCooledDown(12292)
end

-- Mortal Strike, 12294
-- Bloodthirst, 23881
-- only one can be active at a time, so do both in one
function CanCastMortalStrikeOrBloodthirst()
    return SpellIsCooledDown(12294) or SpellIsCooledDown(23881)
end

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

function GetMultiBoolOne()
    local boolR1 = IsAttacking()
    local boolR2 = HasBuffNamed("Battle Shout")
    local boolR3 = AreWeLowOnHealthPotions()
    local boolR4 = AreWeLowOnDynamite()
    local boolR5 = TargetHasRend()
    local boolR6 = CanShootTarget()
    local boolR7 = AreWeLowOnAmmo()
    local boolR8 = IsSpellUsable(7384) -- overpower rank 1, 7384

    local rByte = EncodeBooleansToByte(boolR1, boolR2, boolR3, boolR4, boolR5, boolR6, boolR7, boolR8)

    local boolG1 = IsGlobalCooldownCooledDown()
    local boolG2 = CanCastWhirlwind()
    local boolG3 = CanCastSweepingStrikes()
    local boolG4 = WaitingToShoot()
    local boolG5 = CanCastMortalStrikeOrBloodthirst()
    local boolG6 = AreBagsFull()
    local boolG7 = CanChargeTarget()
    local boolG8 = IsInCombat()

    local gByte = EncodeBooleansToByte(boolG1, boolG2, boolG3, boolG4, boolG5, boolG6, boolG7, boolG8)

    local boolB1 = IsAnyNextSwingSpellQueued()
    local boolB2 = IsPlayerPetrified()
    local boolB3 = HasUnseenWhisper()
    local boolB4 = CanFrostboltTarget()
    local boolB5 = HasBuffNamed("Frost Armor")
    local boolB6 = HasBuffNamed("Arcane Intellect")
    local boolB7 = ShouldWeSummonWater()
    local boolB8 = ShouldWeSummonFood()

    local bByte = EncodeBooleansToByte(boolB1, boolB2, boolB3, boolB4, boolB5, boolB6, boolB7, boolB8)

    return rByte/255.0, gByte/255.0, bByte/255.0
end

function GetMultiBoolTwo()
    local boolR1 = IsInMeleeRange()
    local boolR2 = SpellIsCooledDownIgnoringGCD(2136) -- fireblast rank 1, 2136
    local boolR3 = IsPlayerCasting()
    local boolR4 = AreEnemyNameplatesTurnedOn()
    local boolR5 = false
    local boolR6 = false
    local boolR7 = false
    local boolR8 = SpellIsCooledDownIgnoringGCD(2136)

    local rByte = EncodeBooleansToByte(boolR1, boolR2, boolR3, boolR4, boolR5, boolR6, boolR7, boolR8)

    local boolG1 = false
    local boolG2 = false
    local boolG3 = false
    local boolG4 = false
    local boolG5 = false
    local boolG6 = false
    local boolG7 = false
    local boolG8 = SpellIsCooledDownIgnoringGCD(2136)

    local gByte = EncodeBooleansToByte(boolG1, boolG2, boolG3, boolG4, boolG5, boolG6, boolG7, boolG8)

    local boolB1 = false
    local boolB2 = false
    local boolB3 = false
    local boolB4 = false
    local boolB5 = false
    local boolB6 = false
    local boolB7 = false
    local boolB8 = SpellIsCooledDownIgnoringGCD(2136)

    local bByte = EncodeBooleansToByte(boolB1, boolB2, boolB3, boolB4, boolB5, boolB6, boolB7, boolB8)

    return rByte/255.0, gByte/255.0, bByte/255.0
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

function IsAttackingColor()
    return GetColorFromSingleBool(IsAttacking())
end

function IsInMeleeRangeColor()
    return GetColorFromSingleBool(IsInMeleeRange())
end

function IsInCombatColor()
    return GetColorFromSingleBool(IsInCombat())
end

function CanChargeTargetColor()
    return GetColorFromSingleBool(CanChargeTarget())
end

function IsAnyNextSwingSpellQueuedColor()
    return GetColorFromSingleBool(IsAnyNextSwingSpellQueued())
end