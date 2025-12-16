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

function CanChargeTarget()
    local unit = "target"

    -- Basic checks
    if not UnitExists(unit) then
        return falsed
    end

    if not UnitCanAttack("player", unit) then
        return false
    end

    if UnitIsPVP("target") then
        return false
    end

    if UnitIsDead(unit) or UnitIsDeadOrGhost(unit) then
        return false
    end

    -- don't kill greys
    if UnitLevel("player") - UnitLevel("target") >= 7 then
        return false
    end

    -- don't charge oranges
    if UnitLevel("player") - UnitLevel("target") <= -3 then
        return false
    end

    -- unit in combat, probably not with us
    if UnitAffectingCombat(unit) then
        return false
    end

    -- Resolve Charge spell name from its ID (100 is the standard Charge ID)
    local chargeName = GetSpellInfo(100)
    if not chargeName then
        -- Should never happen unless spell is unknown (e.g., very low level)
        return false
    end

    -- 1) Range check
    local inRange = IsSpellInRange(chargeName, unit)
    if inRange ~= 1 then
        return false
    end

    -- 2) Cooldown check
    local start, duration, enabled = GetSpellCooldown(chargeName)

    -- If it's not enabled, you can't use it (e.g., disabled by stance/form)
    if enabled == 0 then
        return false
    end

    -- If start == 0 and duration == 0, spell is ready
    if start == 0 or duration == 0 then
        return true
    end

    local remaining = start + duration - GetTime()
    if remaining <= 0 then
        return true
    end

    return false
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

function HasBattleShout()
    local i = 1
    while true do
        local name, icon = UnitBuff("player", i)
        if not name then
            return false -- no more buffs; not found
        end
        if name == "Battle Shout" then
            return true
        end
        i = i + 1
    end
end

function IsOverpowerUsable()
    return IsUsableSpell(7384) -- overpower rank 1
end

function AreEnemyNameplatesTurnedOn()
    return GetCVarBool("nameplateShowEnemies")
end

-- lesser healing potion, level 3, 858
-- healing potion, level 12, 929
-- greater healing potion, level 21, 1710
-- superior healing potion, level 35, 3928
-- major healing potion, level 45, 13446
function AreWeLowOnHealthPotions()
    local healthPotCount = GetItemCount(1710, false)
    return healthPotCount < 2
end

-- heavy dynamite, 4378
-- explosive sheep, 4384
-- big bronze bomb, 4380
-- solid dynamite, 10507
-- dense dynamite, 18641
-- hi-explosive bomb, 10562 
function AreWeLowOnDynamite()
    local dynamiteCount = GetItemCount(4380, false)
    return dynamiteCount < 2
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

    if spellId == 6548 then
      return true
    end
  end
  return false
end

function GetMultiBoolOne()
    -- IsAttacking
    local bool1 = IsAttacking()
    local bool2 = HasBattleShout()
    local bool3 = AreWeLowOnHealthPotions()
    local bool4 = AreWeLowOnDynamite()
    local bool5 = TargetHasRend()
    local bool6 = true
    local bool7 = true
    local bool8 = IsOverpowerUsable()

    local rByte = EncodeBooleansToByte(bool1, bool2, bool3, bool4, bool5, bool6, bool7, bool8)

    --print(rByte)

    return rByte/255.0, 0, 0
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