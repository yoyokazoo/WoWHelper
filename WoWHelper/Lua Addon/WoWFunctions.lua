--------------------------------------------------
-- API compatibility shims (Classic Era vs. modern / WoW Forever clients)
--------------------------------------------------
-- Modern clients removed these old globals in favor of C_Spell / C_Item /
-- C_UnitAuras, several of which return a table instead of multiple values.
-- Each Compat_ function uses the old global if it exists (Classic Era, behavior
-- unchanged) and otherwise calls the namespaced replacement and converts its
-- result back to the old return shape, so call sites don't need to change.
-- Named Compat_* rather than redefining the Blizzard globals, so other addons'
-- "does this API exist" checks aren't fooled.

function Compat_GetSpellInfo(spell)
    if GetSpellInfo then return GetSpellInfo(spell) end
    local info = C_Spell and C_Spell.GetSpellInfo and C_Spell.GetSpellInfo(spell)
    if not info then return nil end
    -- old shape: name, rank, icon, castTime, minRange, maxRange, spellId
    return info.name, nil, info.iconID, info.castTime, info.minRange, info.maxRange, info.spellID
end

-- Midnight-rules clients (WoW Forever) make spell cooldowns secret in combat:
-- comparing/doing math on startTime/duration/modRate throws ("duration is a
-- secret number value"). isEnabled and isActive stay readable, and isActive
-- was confirmed live (via /yycd on Battle Shout, in and out of combat) to go
-- true exactly while the cooldown/GCD is running. So when the timings are
-- secret, Compat_GetSpellCooldown answers from isActive instead:
--   isActive false -> "ready":       (0, 0, enabled)
--   isActive true  -> "on cooldown": (GetTime(), SECRET_COOLDOWN_STAND_IN_DURATION, enabled)
-- The real remaining time is unknown, so the "on cooldown" shape is a
-- stand-in that just makes every caller's remaining-time math come out
-- positive. Note SpellIsCooledDownIgnoringGCD() compares a spell's window
-- against the GCD probe's, and two stand-ins built the same frame match --
-- so while the GCD is running, a spell on a longer real cooldown reads
-- "ready apart from the GCD". It reads correctly again once the GCD ends.
--
-- If isActive itself is unreadable (secret/missing -- not seen so far), we
-- fall back to "ready": a false "ready" just means the bot presses an ability
-- that isn't up yet (the game ignores it), while a false "on cooldown" would
-- stall every rotation completely.
--
-- SECRET_COOLDOWN_DEBUG prints, once per spell, which fields of the
-- C_Spell.GetSpellCooldown table came back secret.
SECRET_COOLDOWN_DEBUG = false
local SECRET_COOLDOWN_STAND_IN_DURATION = 1
local reportedSecretCooldownSpells = {}

local function ReportSecretCooldown(spell, info)
    if not SECRET_COOLDOWN_DEBUG or reportedSecretCooldownSpells[spell] then return end
    reportedSecretCooldownSpells[spell] = true

    local parts = {}
    if type(info) == "table" then
        for key, value in pairs(info) do
            table.insert(parts, tostring(key) .. "=" .. (issecretvalue(value) and "SECRET" or tostring(value)))
        end
    else
        table.insert(parts, "(old GetSpellCooldown global, no table)")
    end
    print("|cffff9900[cooldown]|r " .. tostring(spell) .. " secret in combat: " .. table.concat(parts, ", "))
end

--------------------------------------------------
-- Cooldown watcher (/yycd, YoyokazooUI.lua) -- debug-only. Confirms whether
-- C_Spell.GetSpellCooldown's isActive field (readable in combat even when
-- startTime/duration are secret) actually tracks a running cooldown. Polls
-- each watched spell every COOLDOWN_WATCH_INTERVAL seconds and prints only
-- when isActive changes, with a timestamp, whether we're in combat, and the
-- remaining time when it's readable (out of combat), so isActive can be
-- checked against real numbers out of combat and then watched in combat.
-- Also prints each successful player cast, to line the flips up against.
--------------------------------------------------
local COOLDOWN_WATCH_INTERVAL = 0.05
local cooldownWatchSpells = {}      -- spellId -> last seen isActive (or "unseen")
local cooldownWatchFrame = nil
local cooldownWatchElapsed = 0

local function CooldownWatchPrint(text)
    print(string.format("|cff40c0ff[cdwatch %.2f]|r %s", GetTime(), text))
end

local function DescribeMaybeSecret(value)
    if IsAnySecretValue(value) then return "SECRET" end
    return tostring(value)
end

local function SampleWatchedCooldown(spellId)
    local info = C_Spell.GetSpellCooldown(spellId)
    if not info then
        return "noinfo", "no cooldown info (spell unknown?)"
    end

    local isActive = info.isActive
    if IsAnySecretValue(isActive) then
        return "secret", "isActive=SECRET"
    end

    local remaining
    if IsAnySecretValue(info.startTime, info.duration) then
        remaining = "SECRET"
    elseif info.duration == 0 then
        remaining = "0"
    else
        remaining = string.format("%.2f", info.startTime + info.duration - GetTime())
    end

    return tostring(isActive), string.format("isActive=%s remaining=%s", tostring(isActive), remaining)
end

local function CooldownWatchTick(self, elapsed)
    cooldownWatchElapsed = cooldownWatchElapsed + elapsed
    if cooldownWatchElapsed < COOLDOWN_WATCH_INTERVAL then return end
    cooldownWatchElapsed = 0

    for spellId, lastState in pairs(cooldownWatchSpells) do
        local ok, state, description = pcall(SampleWatchedCooldown, spellId)
        if not ok then
            state, description = "error", "error: " .. tostring(state)
        end
        if state ~= lastState then
            cooldownWatchSpells[spellId] = state
            CooldownWatchPrint(string.format("%s (%d) %s combat=%s",
                tostring(C_Spell.GetSpellName and C_Spell.GetSpellName(spellId) or "?"), spellId,
                description, tostring(UnitAffectingCombat("player"))))
        end
    end
end

local function CooldownWatchEvent(self, event, unit, castGUID, spellId)
    -- Events still fire on a hidden frame (only OnUpdate stops), so check
    -- the watch list rather than relying on Hide().
    if not next(cooldownWatchSpells) then return end
    local name = "?"
    if not IsAnySecretValue(spellId) and C_Spell.GetSpellName then
        name = tostring(C_Spell.GetSpellName(spellId))
    end
    CooldownWatchPrint(string.format("cast succeeded: %s (%s)", name, DescribeMaybeSecret(spellId)))
end

function IsCooldownWatchRunning()
    return cooldownWatchFrame ~= nil and cooldownWatchFrame:IsShown()
end

-- Adds spellId to the watch list (or removes it if already watched), and
-- starts/stops the watcher as the list becomes non-empty/empty.
function ToggleCooldownWatchSpell(spellId)
    if not (C_Spell and C_Spell.GetSpellCooldown) then
        print("YoyokazooUI: /yycd needs C_Spell.GetSpellCooldown (not on this client).")
        return
    end

    if cooldownWatchSpells[spellId] ~= nil then
        cooldownWatchSpells[spellId] = nil
        print("YoyokazooUI: stopped watching cooldown of spell " .. spellId)
    else
        cooldownWatchSpells[spellId] = "unseen"
        print("YoyokazooUI: watching cooldown of spell " .. spellId)
    end

    if not cooldownWatchFrame then
        cooldownWatchFrame = CreateFrame("Frame")
        cooldownWatchFrame:SetScript("OnUpdate", CooldownWatchTick)
        cooldownWatchFrame:SetScript("OnEvent", CooldownWatchEvent)
        cooldownWatchFrame:RegisterUnitEvent("UNIT_SPELLCAST_SUCCEEDED", "player")
    end

    if next(cooldownWatchSpells) then
        cooldownWatchFrame:Show()
    else
        cooldownWatchFrame:Hide()
    end
end

function StopCooldownWatch()
    wipe(cooldownWatchSpells)
    if cooldownWatchFrame then cooldownWatchFrame:Hide() end
    print("YoyokazooUI: cooldown watch stopped.")
end

-- Answer for a cooldown whose timings came back secret -- see the comment
-- above SECRET_COOLDOWN_DEBUG. info may be nil (old global path, no table).
local function SecretCooldownFromIsActive(info)
    if type(info) ~= "table" or info.isActive == nil or IsAnySecretValue(info.isActive, info.isEnabled) then
        return 0, 0, 1, 1
    end

    local enabled = info.isEnabled and 1 or 0
    if info.isActive then
        return GetTime(), SECRET_COOLDOWN_STAND_IN_DURATION, enabled, 1
    end
    return 0, 0, enabled, 1
end

function Compat_GetSpellCooldown(spell)
    if GetSpellCooldown then
        local start, duration, enabled, modRate = GetSpellCooldown(spell)
        if IsAnySecretValue(start, duration, enabled) then
            -- The old global has no isActive -- borrow it from C_Spell if present.
            local info = C_Spell and C_Spell.GetSpellCooldown and C_Spell.GetSpellCooldown(spell)
            ReportSecretCooldown(spell, info)
            return SecretCooldownFromIsActive(info)
        end
        return start, duration, enabled, modRate
    end

    local info = C_Spell and C_Spell.GetSpellCooldown and C_Spell.GetSpellCooldown(spell)
    if not info then return nil end

    -- Check before touching anything: even the isEnabled truthiness test
    -- below would throw on a secret.
    if IsAnySecretValue(info.startTime, info.duration, info.isEnabled) then
        ReportSecretCooldown(spell, info)
        return SecretCooldownFromIsActive(info)
    end

    -- old shape: start, duration, enabled (1/0 number, not a boolean)
    return info.startTime, info.duration, (info.isEnabled and 1 or 0), info.modRate
end

function Compat_IsSpellInRange(spell, unit)
    if IsSpellInRange then return IsSpellInRange(spell, unit) end
    if not (C_Spell and C_Spell.IsSpellInRange) then return nil end
    local inRange = C_Spell.IsSpellInRange(spell, unit)
    -- old shape: 1 = in range, 0 = out of range, nil = not applicable
    if inRange == nil then return nil end
    return inRange and 1 or 0
end

function Compat_IsUsableSpell(spell)
    if IsUsableSpell then return IsUsableSpell(spell) end
    if not (C_Spell and C_Spell.IsSpellUsable) then return false end
    return C_Spell.IsSpellUsable(spell)
end

function Compat_IsCurrentSpell(spell)
    if IsCurrentSpell then return IsCurrentSpell(spell) end
    if not (C_Spell and C_Spell.IsCurrentSpell) then return false end
    return C_Spell.IsCurrentSpell(spell)
end

function Compat_GetItemInfo(item)
    if GetItemInfo then return GetItemInfo(item) end
    if not (C_Item and C_Item.GetItemInfo) then return nil end
    return C_Item.GetItemInfo(item)
end

function Compat_GetItemInfoInstant(item)
    if GetItemInfoInstant then return GetItemInfoInstant(item) end
    if not (C_Item and C_Item.GetItemInfoInstant) then return nil end
    return C_Item.GetItemInfoInstant(item)
end

function Compat_EquipItemByName(item, slot)
    if EquipItemByName then return EquipItemByName(item, slot) end
    if C_Item and C_Item.EquipItemByName then return C_Item.EquipItemByName(item, slot) end
end

function Compat_IsSpellKnown(spellId)
    if IsSpellKnown then return IsSpellKnown(spellId) end
    if C_SpellBook and C_SpellBook.IsSpellKnown then return C_SpellBook.IsSpellKnown(spellId) end
    return IsPlayerSpell and IsPlayerSpell(spellId) or false
end

function Compat_GetItemCount(item, includeBank, includeCharges)
    if GetItemCount then return GetItemCount(item, includeBank, includeCharges) end
    if not (C_Item and C_Item.GetItemCount) then return 0 end
    return C_Item.GetItemCount(item, includeBank, includeCharges)
end

-- Converts a C_UnitAuras AuraData table to UnitBuff/UnitDebuff's old shape:
-- name, icon, count, debuffType, duration, expirationTime, source,
-- isStealable, nameplateShowPersonal, spellId
local function UnpackAuraDataCompat(aura)
    if not aura then return nil end
    if AuraUtil and AuraUtil.UnpackAuraData then
        return AuraUtil.UnpackAuraData(aura)
    end
    return aura.name, aura.icon, aura.applications, aura.dispelName, aura.duration,
        aura.expirationTime, aura.sourceUnit, aura.isStealable,
        aura.nameplateShowPersonal, aura.spellId
end

-- Midnight-rules clients (WoW Forever) make auras secret in combat:
-- C_UnitAuras.Get*DataByIndex throws ("auras cannot be accessed when secret").
-- Treat that the same as "no aura at this index" so callers' scan loops just
-- end, instead of the error unwinding through the pixel-row populate
-- functions. Consequence: every aura-based flag reads "not present" in combat
-- on those clients. Also bail if the name itself comes back secret, since
-- every caller compares it.
-- BUFF_READ_DEBUG prints, once per distinct (unit, outcome, in-combat)
-- combination, how SafeAuraQuery's aura read went: pcall error (with message),
-- nil at index 1 (no auras at all), aura with a secret name (and whether its
-- spellId is still readable), or a normal readable aura. Temporary, for
-- confirming why HasBattleShout reads false in combat -- see also /yybuff
-- (DebugBuffRead below).
BUFF_READ_DEBUG = false
local reportedBuffReadOutcomes = {}

-- Set by SafeAuraQuery whenever a query couldn't actually be read (pcall
-- error or secret name) -- i.e. "not found" means "couldn't look", not "absent".
-- HasBuffNamedOrRemembered resets it before each scan.
local lastAuraQueryWasBlind = false

local function ReportBuffReadOutcome(unit, outcome)
    if not BUFF_READ_DEBUG then return end
    local inCombat = UnitAffectingCombat("player") and true or false
    local key = tostring(unit) .. "|" .. outcome .. "|" .. tostring(inCombat)
    if reportedBuffReadOutcomes[key] then return end
    reportedBuffReadOutcomes[key] = true
    print(string.format("|cffff9900[buffread]|r unit=%s inCombat=%s -> %s",
        tostring(unit), tostring(inCombat), outcome))
end

local function SafeAuraQuery(queryFunc, unit, index, filter)
    local ok, aura = pcall(queryFunc, unit, index, filter)
    if not ok then
        lastAuraQueryWasBlind = true
        ReportBuffReadOutcome(unit, "pcall error: " .. DescribeMaybeSecret(aura))
        return nil
    end
    if not aura then
        if index == 1 then ReportBuffReadOutcome(unit, "nil at index 1 (no auras returned)") end
        return nil
    end
    if IsAnySecretValue(aura.name) then
        lastAuraQueryWasBlind = true
        ReportBuffReadOutcome(unit, "name secret; spellId=" .. DescribeMaybeSecret(aura.spellId))
        return nil
    end
    ReportBuffReadOutcome(unit, "readable")
    return UnpackAuraDataCompat(aura)
end

function Compat_UnitBuff(unit, index, filter)
    if UnitBuff then return UnitBuff(unit, index, filter) end
    if not (C_UnitAuras and C_UnitAuras.GetBuffDataByIndex) then return nil end
    return SafeAuraQuery(C_UnitAuras.GetBuffDataByIndex, unit, index, filter)
end

function Compat_UnitAura(unit, index, filter)
    if UnitAura then return UnitAura(unit, index, filter) end
    if not (C_UnitAuras and C_UnitAuras.GetAuraDataByIndex) then return nil end
    -- old UnitAura defaulted to helpful auras when no filter was passed
    return SafeAuraQuery(C_UnitAuras.GetAuraDataByIndex, unit, index, filter or "HELPFUL")
end

function Compat_UnitDebuff(unit, index, filter)
    if UnitDebuff then return UnitDebuff(unit, index, filter) end
    if not (C_UnitAuras and C_UnitAuras.GetDebuffDataByIndex) then return nil end
    return SafeAuraQuery(C_UnitAuras.GetDebuffDataByIndex, unit, index, filter)
end

-- How often "rare change" state (bag fullness, consumable counts, gear
-- durability, trainable skills) is actually recomputed. These get polled on the
-- pixel row's ~50-100ms OnUpdate cadence (plus the debug frame's), but none of
-- them change anywhere near that fast, and some (bag scans) allocate a table per
-- occupied slot -- so compute once on the first call (startup) and then at most
-- once per interval, returning the cached value in between.
local RARE_STATE_REFRESH_INTERVAL_SECONDS = 60

-- Wraps computeFn so it runs on the first call and then at most once every
-- intervalSeconds, returning the last computed value in between.
local function CacheOnInterval(computeFn, intervalSeconds)
    local lastCheckTime = nil
    local cachedValue = nil

    return function()
        local now = GetTime()
        if not lastCheckTime or (now - lastCheckTime) >= intervalSeconds then
            cachedValue = computeFn()
            lastCheckTime = now
        end

        return cachedValue
    end
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

    -- don't pull anything 4+ levels above us
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
    local spellName = Compat_GetSpellInfo(spellId)
    if not spellName then
        -- Should never happen unless spell is unknown (e.g., very low level)
        return false
    end

    local start, duration, enabled = Compat_GetSpellCooldown(spellName)

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

-- Classic: this spellId is commonly used to query the global cooldown -- but probing it
-- directly never showed a cooldown in testing on this client (start/duration stayed 0
-- for every cast, any class), so it's kept only as GetGCDProbeSpell()'s last-resort
-- fallback for a class with no real probe spell picked yet, not used directly anywhere.
local GCD_SPELL_ID = 61304

-- Picks the class-appropriate REAL spell to probe for the shared global cooldown's own
-- start/duration -- shared by IsGlobalCooldownCooledDown() and
-- SpellIsCooledDownIgnoringGCD() below, so there's exactly one place that decides which
-- probe spell each class uses, not two copies that could drift out of sync (same class of
-- coupling as everything else CLAUDE.md's "keep in sync" notes call out). A probe spell
-- must be known early (ideally from level 1) and have NO cooldown of its own beyond the
-- GCD -- a spell with a real independent cooldown (e.g. Charge) would read as "on
-- cooldown" long after the GCD itself clears, and an ability that doesn't trigger the GCD
-- at all (e.g. Heroic Strike -- Warrior's original, wrong pick here) would never reflect
-- it either.
--
-- Confirmed via testing: Shaman (Lightning Bolt Rank 1, same spell ID
-- CanSpellcastPullTarget() already uses above).
--
-- Warrior: Battle Shout Rank 1 (6673) -- known from level 1, triggers the GCD, no
-- cooldown of its own. Replaced Rend Rank 1 (772), which isn't trainable until level 4,
-- so a level 1-3 Warrior got no cooldown info at all and always read as cooled down.
-- Confirmed live on the Forever client (/yycd: isActive tracks the GCD in and out of
-- combat); not yet re-checked on Classic Era. (The generic GCD spell 61304 doesn't help here either:
-- on the Forever client it returns no cooldown info at all, "spell unknown".)
--
-- NOT yet confirmed via testing: Warlock (Shadow Bolt Rank 1, same spell ID
-- CanSpellcastPullTarget() uses above, known from level 1, no cooldown beyond GCD). Verify
-- the same way Shaman was (debug log around a cast, watch GCDCooledDown flip false->true)
-- before trusting it.
function GetGCDProbeSpell()
    local _, classFile = UnitClass("player")

    if classFile == "SHAMAN" then
        return 403 -- Lightning Bolt (Rank 1)
    elseif classFile == "WARRIOR" then
        return 6673 -- Battle Shout (Rank 1)
    elseif classFile == "WARLOCK" then
        return 686 -- Shadow Bolt (Rank 1) -- UNTESTED
    end

    return GCD_SPELL_ID -- no real probe picked for this class yet -- confirmed NOT to work
end

-- Returns true if the *spell's own* cooldown is finished.
-- If the only cooldown present is the GCD, returns true.
function SpellIsCooledDownIgnoringGCD(spellId)
    local spellName = Compat_GetSpellInfo(spellId)
    if not spellName then
        return false
    end

    local start, duration, enabled = Compat_GetSpellCooldown(spellName)

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

    -- If we're here, spell has a cooldown reported. It might just be the GCD -- probe the
    -- same class-appropriate real spell IsGlobalCooldownCooledDown() does (GetGCDProbeSpell()
    -- above), not the GCD_SPELL_ID placeholder directly (confirmed not to work on this client).
    local gcdStart, gcdDuration = Compat_GetSpellCooldown(GetGCDProbeSpell())

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
    local spellName = Compat_GetSpellInfo(spellId)
    if not spellName then
        -- Should never happen unless spell is unknown (e.g., very low level)
        return false
    end

    -- 1) Range check
    local inRange = Compat_IsSpellInRange(spellName, unit)
    if inRange ~= 1 then
        return false
    end

    -- 2) Cooldown check. Deliberately ignores the "enabled" return (spell
    -- disabled by stance/form) -- the bot's macros hop into the right stance.
    local start, duration = Compat_GetSpellCooldown(spellName)

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
    local spellName = Compat_GetSpellInfo(spellId)
    if not spellName then
        -- Should never happen unless spell is unknown (e.g., very low level)
        return false
    end

    -- 1) Range check
    local inRange = Compat_IsSpellInRange(spellName, unit)
    if inRange ~= 1 then
        return false
    end

    return true
end

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

-- Midnight-rules clients (WoW Forever) can return "secret" values from
-- UnitHealth/UnitPower, aura and cooldown queries, etc.; any arithmetic or
-- comparison on one throws. issecretvalue() doesn't exist on older clients
-- (Classic Era), so guard it.
function IsAnySecretValue(...)
    if not issecretvalue then return false end
    for i = 1, select("#", ...) do
        if issecretvalue((select(i, ...))) then return true end
    end
    return false
end

--------------------------------------------------
-- Map X coord (0-100, normalized across map)
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

    -- pos.x is 0-1 across the map; convert to 0-100
    return math.floor((pos.x or 0) * 10000 + 0.5) / 100  -- two decimals
end

--------------------------------------------------
-- Map Y coord (0-100, normalized across map)
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

    -- pos.y is 0-1; convert to 0-100
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
    ["Orgrimmar"] = 16,
    ["Teldrassil"] = 17,
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

-- Player facing in degrees, 0-360 (0 = north, counterclockwise), to two decimals.
function GetPlayerFacingInDegrees()
    local facing = GetPlayerFacing()
    if facing == nil then
        return 0
    end
    return round2(facing * 180 / math.pi)
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
            local name = UnitName(unit)
            -- pcall'd: on Midnight-rules clients the name may be a secret
            -- value, which can't be used as a table key -- treat as not ignored.
            local ok, isIgnored = pcall(function()
                return name ~= nil and ATTACKER_COUNT_IGNORED_MOB_NAMES[name] == true
            end)
            local ignored = ok and isIgnored

            if COUNT_ATTACKERS_DEBUG then
                print(string.format(
                    "  %s (%s): canAttack=%s inCombat=%s targetingMe=%s ignored=%s",
                    unit, tostring(name or "?"),
                    tostring(canAttack), tostring(inCombat), tostring(targetingMe),
                    tostring(ignored)))
            end

            if canAttack and inCombat and targetingMe and not ignored then
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

    local playerFacing = GetPlayerFacing()       -- 0-2pi radians
    local angleToTarget = math.atan2(ty - py, tx - px)

    local diff = angleToTarget - playerFacing
    diff = (diff + math.pi) % (2 * math.pi) - math.pi  -- normalize

    text:SetText("success")

    return math.abs(diff) < (math.pi / 2)
end

function IsAttacking()
    return Compat_IsCurrentSpell(6603) -- Auto Attack
end

-- Returns found, expirationTime (0 = no expiration; may be nil/secret on some clients).
local function FindBuffNamed(buffName)
    local i = 1
    while true do
        local name, _, _, _, _, expirationTime = Compat_UnitBuff("player", i)
        if not name then
            return false -- no more buffs; not found
        end
        if name == buffName then
            return true, expirationTime
        end
        i = i + 1
    end
end

function HasBuffNamed(buffName)
    return (FindBuffNamed(buffName))
end

-- Like HasBuffNamed, but for when the aura read is blind (Midnight-rules
-- clients block every aura API in combat -- confirmed live, see
-- SafeAuraQuery): falls back to the expiration time remembered from the last
-- read that worked. So a buff applied before combat keeps reading true
-- through combat until it would have expired. A buff applied *during* combat
-- can't be seen here at all -- the C# side covers that by remembering when it
-- pressed the key (e.g. WarriorShouldCastBattleShout). Only for buffs whose
-- lifetime is just their duration: a charge-consumed buff (Lightning Shield)
-- or a proc (Clearcasting) would read true after it's actually gone.
local rememberedBuffExpirations = {}

function HasBuffNamedOrRemembered(buffName)
    lastAuraQueryWasBlind = false
    local found, expirationTime = FindBuffNamed(buffName)

    if not lastAuraQueryWasBlind then
        if not found then
            rememberedBuffExpirations[buffName] = nil
        elseif not IsAnySecretValue(expirationTime) then
            -- 0/nil = no expiration (or unknown); treat as lasting until the next real read
            rememberedBuffExpirations[buffName] =
                (expirationTime and expirationTime > 0) and expirationTime or math.huge
        end
        return found
    end

    local expiration = rememberedBuffExpirations[buffName]
    return expiration ~= nil and GetTime() < expiration
end

-- /yybuff (YoyokazooUI.lua): debug-only. Tries every buff-lookup API we could
-- fall back on for one buff (by name and by each rank's spell ID) and prints
-- what each returned -- error, nil, a readable result, or secret fields -- so
-- we can see which (if any) still works in combat on Midnight-rules clients.
-- Defaults to Battle Shout (all ranks).
local DEBUG_BUFF_DEFAULT_NAME = "Battle Shout"
local DEBUG_BUFF_DEFAULT_SPELL_IDS = { 6673, 5242, 6192, 11549, 11550, 11551, 25289 }

local function DescribeAuraTable(aura)
    if aura == nil then return "nil" end
    if issecretvalue and issecretvalue(aura) then return "<secret table>" end
    if type(aura) ~= "table" then return "non-table: " .. DescribeMaybeSecret(aura) end
    return string.format("name=%s spellId=%s expirationTime=%s",
        DescribeMaybeSecret(aura.name), DescribeMaybeSecret(aura.spellId),
        DescribeMaybeSecret(aura.expirationTime))
end

local function DebugProbe(label, func, ...)
    if not func then
        print(string.format("  %s: API missing", label))
        return
    end
    local results = { pcall(func, ...) }
    if not results[1] then
        print(string.format("  %s: ERROR %s", label, DescribeMaybeSecret(results[2])))
        return
    end
    if type(results[2]) == "table" or results[2] == nil then
        print(string.format("  %s: %s", label, DescribeAuraTable(results[2])))
    else
        -- unpacked return shape (AuraUtil.FindAuraByName, UnitBuff): name ... spellId(10th)
        print(string.format("  %s: name=%s spellId=%s", label,
            DescribeMaybeSecret(results[2]), DescribeMaybeSecret(results[11])))
    end
end

function DebugBuffRead(buffName)
    buffName = (buffName and buffName ~= "") and buffName or DEBUG_BUFF_DEFAULT_NAME
    local spellIds = (buffName == DEBUG_BUFF_DEFAULT_NAME) and DEBUG_BUFF_DEFAULT_SPELL_IDS or {}

    print(string.format("|cffff9900[yybuff]|r '%s' inCombat=%s issecretvalue=%s UnitBuff=%s",
        buffName, tostring(UnitAffectingCombat("player") and true or false),
        tostring(issecretvalue ~= nil), tostring(UnitBuff ~= nil)))

    local okHas, has = pcall(HasBuffNamed, buffName)
    print("  HasBuffNamed (current path): " .. (okHas and tostring(has) or ("ERROR " .. DescribeMaybeSecret(has))))

    -- Raw index scan, bypassing SafeAuraQuery, so we see exactly what comes back.
    if C_UnitAuras and C_UnitAuras.GetBuffDataByIndex then
        for i = 1, 40 do
            local ok, aura = pcall(C_UnitAuras.GetBuffDataByIndex, "player", i, "HELPFUL")
            if not ok then
                print(string.format("  GetBuffDataByIndex[%d]: ERROR %s", i, DescribeMaybeSecret(aura)))
                break
            end
            if aura == nil then
                print(string.format("  GetBuffDataByIndex: end of list at %d", i))
                break
            end
            print(string.format("  GetBuffDataByIndex[%d]: %s", i, DescribeAuraTable(aura)))
        end
    else
        print("  GetBuffDataByIndex: API missing")
    end

    local auras = C_UnitAuras or {}
    DebugProbe("GetAuraDataBySpellName", auras.GetAuraDataBySpellName, "player", buffName, "HELPFUL")
    DebugProbe("AuraUtil.FindAuraByName", AuraUtil and AuraUtil.FindAuraByName, buffName, "player", "HELPFUL")
    if UnitBuff then DebugProbe("UnitBuff[1]", UnitBuff, "player", 1) end
    for _, id in ipairs(spellIds) do
        DebugProbe("GetPlayerAuraBySpellID(" .. id .. ")", auras.GetPlayerAuraBySpellID, id)
        if auras.GetUnitAuraBySpellID then
            DebugProbe("GetUnitAuraBySpellID(" .. id .. ")", auras.GetUnitAuraBySpellID, "player", id)
        end
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

-- Drawn into MultiBoolTwo's box 11 (G3, MULTI_BOOL_TWO_FIELDS), decoded on the C# side into
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

-- Prints each IsPlayerUnderwater() flip plus the mirror timers it saw. Off by default.
UNDERWATER_DEBUG = false
local lastUnderwaterDebugValue = nil

-- True while the breath bar is up, i.e. the "BREATH" mirror timer is running (it
-- isn't with a water-breathing effect, which is fine -- nothing to worry about then).
-- Replaces pixel-matching the breath bar's on-screen colors. Drawn into MultiBoolTwo's
-- box 22 (B6), decoded on the C# side into WowWorldState.Underwater.
function IsPlayerUnderwater()
    local underwater = false
    local seen = {}
    for i = 1, (MIRRORTIMER_NUMTIMERS or 3) do
        local timer, value, maxValue, scale, paused = GetMirrorTimerInfo(i)
        if UNDERWATER_DEBUG then
            table.insert(seen, tostring(i) .. "=" .. tostring(timer) .. "/" .. tostring(scale) .. "/" .. tostring(paused))
        end
        if timer == "BREATH" then
            underwater = true
        end
    end
    if UNDERWATER_DEBUG and underwater ~= lastUnderwaterDebugValue then
        lastUnderwaterDebugValue = underwater
        print("YoyokazooUI underwater=" .. tostring(underwater) .. " timers: " .. table.concat(seen, ", "))
    end
    return underwater
end

-- overpower rank 1, 7384
-- fireblast rank 1, 2136
-- cone of cold rank 1,
function IsSpellUsable(spellId)
    return Compat_IsUsableSpell(spellId)
end

-- Whether the player knows a spell, matched by exact name instead of a
-- hardcoded spell ID -- scans the real spellbook (GetNumSpellTabs/
-- GetSpellTabInfo/GetSpellBookItemName) rather than guessing an ID, so it
-- doesn't go stale across ranks the way the hardcoded rank-1 IDs elsewhere
-- in this file can (see the Flame Shock rank TODO in ShamanFunctions.lua).
-- Used for spells where the possible spell ID(s) aren't
-- confidently known, e.g. CanCurePoison/CanCureDisease in ShamanFunctions.lua.
function IsSpellKnownByName(name)
    -- Modern clients (WoW Forever) replaced the spell-tab globals with
    -- C_SpellBook skill lines; Classic Era still has GetNumSpellTabs.
    if not GetNumSpellTabs then
        if not (C_SpellBook and C_SpellBook.GetNumSpellBookSkillLines) then return false end
        local bank = Enum and Enum.SpellBookSpellBank and Enum.SpellBookSpellBank.Player
        for line = 1, C_SpellBook.GetNumSpellBookSkillLines() do
            local info = C_SpellBook.GetSpellBookSkillLineInfo(line)
            if info then
                local offset = info.itemIndexOffset or 0
                for i = offset + 1, offset + (info.numSpellBookItems or 0) do
                    if C_SpellBook.GetSpellBookItemName(i, bank) == name then
                        return true
                    end
                end
            end
        end
        return false
    end

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

-- Returns the name of whatever's currently bound to a key combo, in Blizzard's own
-- binding-string format (e.g. "CTRL-4"), or "" if nothing is bound to it at all. Thin
-- wrapper around GetBindingAction() so callers don't need to know that format
-- themselves -- see FindBoundKeysMessage() below for the case this exists for.
function GetKeyBindingAction(keyString)
    return GetBindingAction(keyString) or ""
end

-- Checks a modifier (e.g. "CTRL"/"SHIFT", Blizzard's own binding-string modifier
-- name) against a list of { display, lookup } keys -- lookup being Blizzard's own
-- binding-string key name (e.g. "4" or "MINUS" for "-"), display being what's
-- actually printed on the key -- and returns one combined "Modifier+display=action,
-- ..." message listing everything WoW's own Key Bindings menu already has bound to
-- one of those combos, or "" if none of them are bound. Shared by every
-- keybind-collision check in YoyokazooUI.lua's PLAYER_ENTERING_WORLD handling (Ctrl+4
-- for Sweeping Strikes, every Shift+<key> our macros use for their [mod:shift] cast)
-- -- those all exist because a macro's own bot-driven keypress (independent of WoW's
-- action-bar keybinding system) never gets a chance to run if the Key Bindings menu
-- already claims that combo first: WoW's own binding intercepts the keypress before
-- the macro ever sees it, so whatever's gated on that modifier silently never fires
-- -- confirmed live as the actual failure mode for Ctrl+4. Returning one combined
-- message (rather than the caller looping key-by-key itself) means a keyboard with
-- several of a modifier's keys already bound reports as one line, not a wall of
-- separate toasts.
function FindBoundKeysMessage(modifierLookup, modifierDisplay, keys)
    local bindings = {}
    for _, keyInfo in ipairs(keys) do
        local binding = GetKeyBindingAction(modifierLookup .. "-" .. keyInfo.lookup)
        if binding ~= "" then
            table.insert(bindings, modifierDisplay .. "+" .. keyInfo.display .. "=" .. binding)
        end
    end
    return table.concat(bindings, ", ")
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

-- Interval-cached (see CacheOnInterval) -- consumable counts are rare-change
-- state, and nothing reading this needs to notice within the minute. Note a
-- /yyconfig selector change also only shows up on the next refresh.
AreWeLowOnHealthPotions = CacheOnInterval(function()
    local count = Compat_GetItemCount(GetHealingPotionItemId(), false)
    return count < 2
end, RARE_STATE_REFRESH_INTERVAL_SECONDS)

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

-- Interval-cached, same as AreWeLowOnHealthPotions() above.
AreWeLowOnDynamite = CacheOnInterval(function()
    local dynamiteCount = Compat_GetItemCount(GetDynamiteItemId(), false)
    return dynamiteCount < 2
end, RARE_STATE_REFRESH_INTERVAL_SECONDS)

-- light shot, 2516
-- rough arrow, 2512
-- sharp arrow, 2515
-- Interval-cached, same as AreWeLowOnHealthPotions() above.
AreWeLowOnAmmo = CacheOnInterval(function()
    local ammoCount = Compat_GetItemCount(2515, false)
    return ammoCount < 2
end, RARE_STATE_REFRESH_INTERVAL_SECONDS)

-- Prints every equipped slot's durability and the repair decision on each
-- (once-a-minute) GearNeedsRepair() refresh. Flip on if the thresholds below
-- don't match what the character sheet's durability doll shows in-game.
GEAR_REPAIR_DEBUG = false

-- Blizzard's durability doll turns a slot yellow when it's low and red when
-- it's broken (0 durability). The exact "low" ratio isn't exposed to addons;
-- 25% is our approximation of the doll's yellow -- confirm live via
-- GEAR_REPAIR_DEBUG if it seems off.
local DURABILITY_YELLOW_RATIO = 0.25
local WEAPON_CLASS_ID = 2 -- GetItemInfoInstant classID for weapons
local SHIELD_EQUIP_LOC = "INVTYPE_SHIELD"

-- Slots repaired at yellow rather than only at red: the main hand, and an
-- off-hand that's either a weapon or a shield (a held-in-off-hand item has no
-- durability, so it never gets here anyway).
local function IsYellowRepairSlot(slot)
    if slot == INVSLOT_MAINHAND then
        return true
    end

    if slot == INVSLOT_OFFHAND then
        local itemId = GetInventoryItemID("player", slot)
        if not itemId then
            return false
        end
        local _, _, _, _, _, classId = Compat_GetItemInfoInstant(itemId)
        return classId == WEAPON_CLASS_ID
    end

    return false
end

-- True if a melee weapon (main hand, or off-hand weapon) or a shield is at yellow
-- durability or worse, or any other equipped piece (armor, ranged) is red/broken.
-- Slots without durability (neck, rings, cloak, trinkets, empty) return nil from
-- GetInventoryItemDurability and are skipped. Interval-cached -- durability only
-- moves a point at a time per hit taken/dealt or death, so once a minute is plenty.
-- Packed into MultiBoolTwo's G8.
GearNeedsRepair = CacheOnInterval(function()
    local needsRepair = false

    for slot = INVSLOT_FIRST_EQUIPPED, INVSLOT_LAST_EQUIPPED do
        local current, maximum = GetInventoryItemDurability(slot)
        if current and maximum and maximum > 0 then
            local isYellowSlot = IsYellowRepairSlot(slot)
            local slotNeedsRepair
            if isYellowSlot then
                slotNeedsRepair = (current / maximum) <= DURABILITY_YELLOW_RATIO
            else
                slotNeedsRepair = current == 0
            end

            if GEAR_REPAIR_DEBUG then
                print(string.format("GearNeedsRepair: slot %d %d/%d yellowSlot=%s needsRepair=%s",
                    slot, current, maximum, tostring(isYellowSlot), tostring(slotNeedsRepair)))
            end

            if slotNeedsRepair then
                needsRepair = true
            end
        end
    end

    if GEAR_REPAIR_DEBUG then
        print("GearNeedsRepair: " .. tostring(needsRepair))
    end

    return needsRepair
end, RARE_STATE_REFRESH_INTERVAL_SECONDS)

function TargetHasDebuffSpellId(debuffSpellId)
  for i = 1, 40 do
    local _, _, _, _, _, _, _, _, _, spellId = Compat_UnitDebuff("target", i)
    if not spellId then break end

    if spellId == debuffSpellId then
      return true
    end
  end

  return false
end

function TargetHasDebuffSpellName(debuffSpellName)
  for i = 1, 40 do
    local spellName, _, _, _, _, _, _, _, _, _ = Compat_UnitDebuff("target", i)
    if not spellName then break end

    if spellName == debuffSpellName then
      return true
    end
  end

  return false
end

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
-- class's spell. Probes the same class-appropriate real spell
-- SpellIsCooledDownIgnoringGCD() does (GetGCDProbeSpell() above -- see its own comment
-- for the full reasoning and confirmed-vs-untested status of each class's pick) instead
-- of GCD_SPELL_ID directly, which never showed a cooldown in testing on this client.
function IsGlobalCooldownCooledDown()
    local start, duration = Compat_GetSpellCooldown(GetGCDProbeSpell())

    if not start or duration == 0 then
        return true
    end

    local remaining = start + duration - GetTime()
    return remaining <= 0
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

-- GetTotalFreeBagSlots() calls C_Container.GetContainerItemInfo() per bag slot,
-- which allocates a fresh table for every occupied slot -- the single biggest
-- source of the addon's GC churn (confirmed while investigating the ~1MB/sec
-- addon-memory growth shown in the game's addon-memory tooltip). Bag contents
-- don't change on the ~50-100ms cadence AreBagsFull() gets polled at (it's one
-- of the MultiBoolOne pixel-row flags, redrawn every refresh), so cache the real check and only
-- recompute it periodically (CacheOnInterval) instead of every tick.
AreBagsFull = CacheOnInterval(function()
    return GetTotalFreeBagSlots() == 0
end, RARE_STATE_REFRESH_INTERVAL_SECONDS)

-- Auto-sell modes, selectable via the /yyconfig "Auto-sell" selector
-- (YoyokazooUI.lua). GetAutoSellMode() (YoyokazooUI.lua) returns the current
-- one, defaulting to "off".
--   off       -- never sell anything.
--   allowlist -- quality 0 (gray) junk, plus quality 1 (white) items on
--                AUTO_SELL_ALLOWLIST_ITEM_NAMES.
--   blocklist -- everything quality 0-2 (gray/white/green) except items on
--                AUTO_SELL_BLOCKLIST_ITEM_NAMES.
AUTO_SELL_MODE_CHOICES = {
    { id = "off",       label = "Off" },
    { id = "allowlist", label = "Allowlist" },
    { id = "blocklist", label = "Blocklist" },
}

-- Highest item quality blocklist mode will sell (2 = Uncommon/green).
local AUTO_SELL_BLOCKLIST_MAX_QUALITY = 2

-- Item names, beyond plain quality-0 (Poor/gray) junk, that are also worth
-- auto-selling to an open merchant in "allowlist" mode -- e.g. cooking/fishing
-- byproducts that vendor for a few silver and are otherwise just dead bag
-- space. Matched by name (via GetItemInfo(itemID), the same name the tooltip
-- shows) rather than item ID -- unlike DYNAMITE_ITEM_CHOICES/
-- HEALING_POTION_ITEM_CHOICES above, this is a short, manually-curated list
-- rather than a runtime-selectable /yyconfig choice, so there's no id-keyed
-- selector UI to match against. Add more names here as they come up.
AUTO_SELL_ALLOWLIST_ITEM_NAMES = {
    "Tangy Clam Meat",
    "Raw Bristle Whisker Catfish",
    "Turtle Meat",
    "Light Leather",
    "Medium Leather",
    "Heavy Leather",
    "Thick Leather",
    "Light Hide",
    "Medium Hide",
    "Heavy Hide",
    "Stringy Vulture Meat",
    "Mystery Meat",
    "Raw Rockscale Cod",
    "Large Fang",
    "Long Tail Feather",
    "Sharp Claw",
    "Buzzard Wing",
    "Heavy Kodo Meat",
}

-- Item names "blocklist" mode never sells, even though they're quality 0-2.
-- Matched by name, same as the allowlist above. Add more names here as they
-- come up.
AUTO_SELL_BLOCKLIST_ITEM_NAMES = {
    "Minor Healing Potion",
    "Lesser Healing Potion",
    "Healing Potion",
    "Greater Healing Potion",
    "Superior Healing Potion",
    "Major Healing Potion",
    "Skinning Knife",
    "Apprentice's Skinning Satchel",
    "Rough Arrow",
    "Light Shot",
}

local function IsItemNameInList(itemName, list)
    if not itemName then
        return false
    end

    for _, name in ipairs(list) do
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

-- Whether a single bag slot's item should be auto-sold to an open merchant
-- under the given mode (an AUTO_SELL_MODE_CHOICES id -- see there for what
-- each mode sells). itemInfo is the table returned by
-- C_Container.GetContainerItemInfo(bag, slot) (see GetFreeSlotsInBag() above
-- for the same API) -- hasNoValue items (quest items, etc, which a vendor
-- won't buy regardless of quality) are skipped in every mode.
function ShouldAutoSellItem(itemInfo, mode)
    if not itemInfo or mode == "off" then
        return false
    end

    if itemInfo.hasNoValue then
        AutoSellDebugPrint("    skipped: hasNoValue is set")
        return false
    end

    local quality = itemInfo.quality
    if type(quality) ~= "number" then
        AutoSellDebugPrint("    skipped: quality=" .. tostring(quality) .. " (" .. type(quality) .. ")")
        return false
    end

    -- GetItemInfo returns nil for an item the client hasn't cached yet; that
    -- shows up as name=nil here rather than as a silent non-match.
    local itemName = Compat_GetItemInfo(itemInfo.itemID)

    if mode == "allowlist" then
        if quality == 0 then
            AutoSellDebugPrint("    QUEUED: quality 0 (gray junk)")
            return true
        end

        if quality == 1 then
            local allowed = IsItemNameInList(itemName, AUTO_SELL_ALLOWLIST_ITEM_NAMES)
            AutoSellDebugPrint("    quality 1, name=" .. tostring(itemName) .. " -- " ..
                (allowed and "QUEUED (allowlisted)" or "skipped (not allowlisted)"))
            return allowed
        end

        AutoSellDebugPrint("    skipped: quality " .. quality .. " never sold in allowlist mode")
        return false
    end

    if mode == "blocklist" then
        if quality > AUTO_SELL_BLOCKLIST_MAX_QUALITY then
            AutoSellDebugPrint("    skipped: quality " .. quality .. " above blocklist max " ..
                AUTO_SELL_BLOCKLIST_MAX_QUALITY)
            return false
        end

        -- An uncached name can't be checked against the blocklist, so don't
        -- risk selling something that's on it.
        if not itemName then
            AutoSellDebugPrint("    skipped: name not cached, can't check blocklist")
            return false
        end

        local blocked = IsItemNameInList(itemName, AUTO_SELL_BLOCKLIST_ITEM_NAMES)
        AutoSellDebugPrint("    quality " .. quality .. ", name=" .. itemName .. " -- " ..
            (blocked and "skipped (blocklisted)" or "QUEUED"))
        return not blocked
    end

    AutoSellDebugPrint("    skipped: unknown auto-sell mode " .. tostring(mode))
    return false
end

-- Scans bags 0-4 (backpack + equipped bags -- same range GetTotalFreeBagSlots()
-- above uses) for everything ShouldAutoSellItem() flags under the given mode
-- (defaults to the current /yyconfig one), returning a flat list of
-- { bag = ..., slot = ..., quality = ... } entries to sell, ordered all grays
-- first, then whites, then greens. Doesn't sell anything itself -- see the
-- queued, one-per-tick sell loop in YoyokazooUI.lua's MERCHANT_SHOW handling
-- (selling everything in a single loop iteration is known to silently drop
-- some sells).
function FindAutoSellQueue(mode)
    mode = mode or GetAutoSellMode()
    local queue = {}

    if mode == "off" then
        AutoSellDebugPrint("auto-sell mode is off -- nothing queued")
        return queue
    end

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
            if ShouldAutoSellItem(itemInfo, mode) then
                table.insert(queue, { bag = bag, slot = slot, quality = itemInfo.quality })
            end
        end
    end

    -- table.sort isn't stable, so tie-break on bag/slot to keep scan order
    -- within a quality tier.
    table.sort(queue, function(a, b)
        if a.quality ~= b.quality then
            return a.quality < b.quality
        end
        if a.bag ~= b.bag then
            return a.bag < b.bag
        end
        return a.slot < b.slot
    end)

    AutoSellDebugPrint("scan finished (" .. mode .. "): " .. #queue .. " slot(s) queued")

    return queue
end

------------------------------------------------------------
-- Auto-equip upgrades from bags (see the BAG_UPDATE_DELAYED handling and
-- /yyequip in YoyokazooUI.lua for the event/slash-command side).
------------------------------------------------------------

-- Set true to print every step of the auto-equip scan to chat: each candidate
-- bag item's raw GetItemStats() table, its score vs. what's currently equipped,
-- and why anything was skipped. GetItemStats()'s exact key names on this
-- client build (ITEM_MOD_STRENGTH_SHORT etc.) aren't verified from reading
-- source alone -- same caveat as every other WoW Lua API call in this addon
-- (see the top of CLAUDE.md) -- so if scoring looks wrong, flip this on (or
-- run /yyequip, which forces it on for one dry-run scan) and read the dumps
-- rather than guessing.
AUTO_EQUIP_DEBUG = false

function AutoEquipDebugPrint(message)
    if AUTO_EQUIP_DEBUG then
        print("YoyokazooUI auto-equip: " .. message)
    end
end

-- Highest item quality auto-equip will consider: 0 = Poor (gray), 1 = Common
-- (white), 2 = Uncommon (green). Blues (3) and purples (4) are deliberately
-- never auto-equipped -- those are worth a human decision.
local AUTO_EQUIP_MAX_QUALITY = 2

-- Armor equip slots auto-equip handles, keyed by GetItemInfo's equipLoc.
-- Deliberately armor-only: weapons/shields/ranged change what the class
-- rotations can do (2H vs. 1H+shield, weapon skill, etc.) and trinkets are
-- mostly on-use effects a stat score can't see, so those stay manual. Rings
-- list both finger slots -- a ring upgrade replaces whichever equipped ring
-- scores worse.
local AUTO_EQUIP_SLOTS_BY_EQUIP_LOC = {
    INVTYPE_HEAD = { 1 },
    INVTYPE_NECK = { 2 },
    INVTYPE_SHOULDER = { 3 },
    INVTYPE_CHEST = { 5 },
    INVTYPE_ROBE = { 5 },
    INVTYPE_WAIST = { 6 },
    INVTYPE_LEGS = { 7 },
    INVTYPE_FEET = { 8 },
    INVTYPE_WRIST = { 9 },
    INVTYPE_HAND = { 10 },
    INVTYPE_FINGER = { 11, 12 },
    INVTYPE_CLOAK = { 15 },
}

-- Armor-proficiency passive spell per armor subclass (GetItemInfoInstant's
-- subclassID for classID 4, Armor). Checking the passive itself rather than
-- hardcoding class/level rules means e.g. a level-40 Warrior who hasn't
-- trained Plate Mail yet correctly won't be handed plate. Subclass 0
-- (Miscellaneous -- necks, rings) has no proficiency requirement.
local ARMOR_PROFICIENCY_SPELL_BY_SUBCLASS = {
    [1] = 9078, -- Cloth
    [2] = 9077, -- Leather
    [3] = 8737, -- Mail
    [4] = 750,  -- Plate Mail
}

-- Per-class stat weights for scoring gear. "armor" is the item's base armor
-- value (GetItemStats' RESISTANCE0_NAME key). Rough leveling weights for how
-- each class is actually played by the bot (Warrior melee, Shaman
-- Rockbiter-melee + shocks, Warlock caster) -- tune as needed. Stats not
-- listed here (resistances, weapon-only stats, etc.) contribute nothing.
local AUTO_EQUIP_STAT_WEIGHTS = {
    WARRIOR = {
        ITEM_MOD_STRENGTH_SHORT = 1.0,
        ITEM_MOD_AGILITY_SHORT = 0.7,
        ITEM_MOD_STAMINA_SHORT = 0.6,
        ITEM_MOD_SPIRIT_SHORT = 0.05,
        RESISTANCE0_NAME = 0.02,
    },
    SHAMAN = {
        ITEM_MOD_STRENGTH_SHORT = 1.0,
        ITEM_MOD_AGILITY_SHORT = 0.5,
        ITEM_MOD_STAMINA_SHORT = 0.6,
        ITEM_MOD_INTELLECT_SHORT = 0.5,
        ITEM_MOD_SPIRIT_SHORT = 0.2,
        RESISTANCE0_NAME = 0.02,
    },
    WARLOCK = {
        ITEM_MOD_STAMINA_SHORT = 1.0,
        ITEM_MOD_INTELLECT_SHORT = 0.8,
        ITEM_MOD_SPIRIT_SHORT = 0.6,
        RESISTANCE0_NAME = 0.01,
    },
}

local function PlayerKnowsSpellId(spellId)
    if IsPlayerSpell then
        return IsPlayerSpell(spellId)
    end
    return Compat_IsSpellKnown(spellId)
end

-- Weighted stat score for an item link, per AUTO_EQUIP_STAT_WEIGHTS for the
-- player's class. nil link (empty slot) scores 0, so anything with a
-- positive score is an upgrade over nothing.
function GetItemEquipScore(itemLink)
    if not itemLink then
        return 0
    end

    local _, classFile = UnitClass("player")
    local weights = AUTO_EQUIP_STAT_WEIGHTS[classFile]
    if not weights then
        return 0
    end

    local getItemStats = GetItemStats or (C_Item and C_Item.GetItemStats)
    local stats = (getItemStats and getItemStats(itemLink)) or {}
    local score = 0
    local dump = {}
    for statKey, value in pairs(stats) do
        table.insert(dump, statKey .. "=" .. tostring(value))
        score = score + (weights[statKey] or 0) * value
    end

    AutoEquipDebugPrint("    " .. itemLink .. " stats {" .. table.concat(dump, ", ") ..
        "} score=" .. score)

    return score
end

-- Whether a bag item is even eligible to be auto-equipped, independent of
-- whether it's better: quality at most AUTO_EQUIP_MAX_QUALITY, an armor slot
-- we handle, required level met, and armor type proficiency known. Returns the
-- item's candidate inventory slot list, or nil (plus a reason for debugging).
local function GetAutoEquipCandidateSlots(itemLink, quality)
    if quality == nil or quality > AUTO_EQUIP_MAX_QUALITY then
        return nil, "quality " .. tostring(quality) .. " is above green"
    end

    -- GetItemInfo returns nil for an item the client hasn't cached yet; the
    -- next BAG_UPDATE_DELAYED scan will pick it up once it has.
    local _, _, _, _, minLevel, _, _, _, equipLoc = Compat_GetItemInfo(itemLink)
    if not equipLoc then
        return nil, "item info not cached yet"
    end

    local slots = AUTO_EQUIP_SLOTS_BY_EQUIP_LOC[equipLoc]
    if not slots then
        return nil, "equipLoc " .. tostring(equipLoc) .. " not auto-equipped"
    end

    if (minLevel or 0) > UnitLevel("player") then
        return nil, "requires level " .. minLevel
    end

    local _, _, _, _, _, classId, subclassId = Compat_GetItemInfoInstant(itemLink)
    if classId ~= 4 then
        return nil, "not armor (classID " .. tostring(classId) .. ")"
    end

    local proficiencySpell = ARMOR_PROFICIENCY_SPELL_BY_SUBCLASS[subclassId]
    if subclassId ~= 0 and not (proficiencySpell and PlayerKnowsSpellId(proficiencySpell)) then
        return nil, "armor subclass " .. tostring(subclassId) .. " not usable"
    end

    return slots
end

-- Scans bags 0-4 for the single best armor upgrade over what's currently
-- equipped (greens/whites/grays only -- see AUTO_EQUIP_MAX_QUALITY), returning
-- { bag, slot, inventorySlot, itemLink, gain } or nil if nothing beats what's
-- equipped. Only one at a time: equipping moves the old item into the bag,
-- which fires another BAG_UPDATE_DELAYED, which re-runs this scan against the
-- new equipped state -- simpler than reasoning about several swaps at once
-- (e.g. two ring upgrades competing for the same finger slot). "Better" is
-- strictly greater score, so ties never flip-flop.
function FindBestEquipmentUpgrade()
    local best = nil

    for bag = 0, 4 do
        for slot = 1, (C_Container.GetContainerNumSlots(bag) or 0) do
            local itemInfo = C_Container.GetContainerItemInfo(bag, slot)
            if itemInfo and itemInfo.hyperlink then
                local itemLink = itemInfo.hyperlink
                local candidateSlots, reason = GetAutoEquipCandidateSlots(itemLink, itemInfo.quality)
                if not candidateSlots then
                    AutoEquipDebugPrint("  " .. bag .. ":" .. slot .. " " .. itemLink .. " skipped: " .. reason)
                else
                    AutoEquipDebugPrint("  " .. bag .. ":" .. slot .. " " .. itemLink .. " is a candidate")
                    local bagScore = GetItemEquipScore(itemLink)

                    -- For rings, compare against whichever equipped ring is worse.
                    local worstSlot, worstScore = nil, nil
                    for _, inventorySlot in ipairs(candidateSlots) do
                        local equippedScore = GetItemEquipScore(GetInventoryItemLink("player", inventorySlot))
                        if worstScore == nil or equippedScore < worstScore then
                            worstSlot, worstScore = inventorySlot, equippedScore
                        end
                    end

                    local gain = bagScore - worstScore
                    AutoEquipDebugPrint("    vs inventory slot " .. worstSlot .. " (score " .. worstScore ..
                        "): gain " .. gain)
                    if gain > 0 and (best == nil or gain > best.gain) then
                        best = { bag = bag, slot = slot, inventorySlot = worstSlot, itemLink = itemLink, gain = gain }
                    end
                end
            end
        end
    end

    return best
end

------------------------------------------------------------
-- Auto-equip bags (same /yyconfig "Auto-equip upgrades" toggle and same
-- EquipBestUpgradeIfBetter() entry point in YoyokazooUI.lua as gear above).
--
-- Normal bag slots (bag IDs 1-4) take plain bags only (container subclass 0):
-- an empty slot gets any plain bag; with no empty slot, a bag with more slots
-- replaces the smallest equipped plain bag. Equipped specialty bags (soul
-- bags etc.) are never replaced, and specialty bags are never put in a normal
-- slot, since they'd restrict what the slot can hold.
--
-- The reagent bag slot (if this client has one) gets the same treatment, but
-- only Leatherworking Bags (the skinning bags) are candidates for it.
--
-- Unverified live (see the top of CLAUDE.md -- flip AUTO_EQUIP_DEBUG on or run
-- /yyequip and read the dump before guessing):
--   * the reagent bag slot's bag ID (GetReagentBagId() below);
--   * the tooltip's "N Slot Bag" text, which is how a bag item's slot count is
--     read (GetItemInfo doesn't return it);
--   * swapping a bigger bag onto a non-empty equipped one -- the client should
--     move the old bag's contents into the new one.
------------------------------------------------------------

local CONTAINER_ITEM_CLASS_ID = 1
local PLAIN_BAG_SUBCLASS_ID = 0

-- Container subclass of Leatherworking Bags -- the skinning bags, the only
-- kind auto-equipped into the reagent bag slot.
local LEATHERWORKING_BAG_SUBCLASS_ID = 7

-- Bag IDs of the normal (general-purpose) bag slots. Bag 0, the backpack,
-- can't be swapped.
local NORMAL_BAG_IDS = { 1, 2, 3, 4 }

-- Bag ID of the reagent bag slot, or nil if this client doesn't have one
-- (Classic Era doesn't). Uses the client's own enum, so it isn't hardcoded.
local function GetReagentBagId()
    if Enum and Enum.BagIndex then
        return Enum.BagIndex.ReagentBag
    end
    return nil
end

local function Compat_ContainerIDToInventoryID(bagId)
    local fn = (C_Container and C_Container.ContainerIDToInventoryID) or ContainerIDToInventoryID
    if not fn then
        return nil
    end
    local ok, inventorySlot = pcall(fn, bagId)
    if ok then
        return inventorySlot
    end
    return nil
end

local bagScanTooltip = nil

-- The bag item's tooltip lines, via C_TooltipInfo where the client has it,
-- else a hidden GameTooltip.
local function GetBagItemTooltipLines(bag, slot)
    local lines = {}

    if C_TooltipInfo and C_TooltipInfo.GetBagItem then
        local data = C_TooltipInfo.GetBagItem(bag, slot)
        if data and data.lines then
            for _, line in ipairs(data.lines) do
                if line.leftText then
                    table.insert(lines, line.leftText)
                end
            end
        end
        return lines
    end

    if not bagScanTooltip then
        bagScanTooltip = CreateFrame("GameTooltip", "YoyokazooUIBagScanTooltip", nil, "GameTooltipTemplate")
    end
    bagScanTooltip:SetOwner(WorldFrame, "ANCHOR_NONE")
    bagScanTooltip:ClearLines()
    bagScanTooltip:SetBagItem(bag, slot)
    for i = 1, bagScanTooltip:NumLines() do
        local fontString = _G["YoyokazooUIBagScanTooltipTextLeft" .. i]
        local text = fontString and fontString:GetText()
        if text then
            table.insert(lines, text)
        end
    end
    bagScanTooltip:Hide()
    return lines
end

-- How many slots a bag sitting in our bags has, read off its tooltip's
-- "16 Slot Bag" line. nil if it can't be read (e.g. item not cached yet).
local function GetBagItemSlotCount(bag, slot)
    for _, text in ipairs(GetBagItemTooltipLines(bag, slot)) do
        local count = text:match("^(%d+) Slot")
        if count then
            return tonumber(count)
        end
    end
    return nil
end

-- One bag slot's current state: inventory slot, slot count (0 if empty), and
-- the equipped bag's container subclass (nil if empty or not a container).
local function GetEquippedBagSlotInfo(bagId)
    local inventorySlot = Compat_ContainerIDToInventoryID(bagId)
    if not inventorySlot then
        return nil
    end

    local info = { bagId = bagId, inventorySlot = inventorySlot, numSlots = 0, subclassId = nil }
    local itemLink = GetInventoryItemLink("player", inventorySlot)
    if itemLink then
        local _, _, _, _, _, classId, subclassId = Compat_GetItemInfoInstant(itemLink)
        info.itemLink = itemLink
        info.numSlots = C_Container.GetContainerNumSlots(bagId) or 0
        if classId == CONTAINER_ITEM_CLASS_ID then
            info.subclassId = subclassId
        end
    end
    return info
end

-- Picks which of the given equipped bag slots a candidate bag should go in:
-- an empty one first, else the smallest replaceable one the candidate beats
-- on slot count. Never the bag the candidate is sitting in (swapping a bag
-- into its own slot can't work). nil if none.
local function PickBagSlotForCandidate(slotInfos, candidateBag, candidateSlots, canReplace)
    local target = nil
    for _, info in ipairs(slotInfos) do
        if info.bagId ~= candidateBag then
            if info.numSlots == 0 and not info.itemLink then
                return info
            end
            if canReplace(info) and candidateSlots > info.numSlots and
                (target == nil or info.numSlots < target.numSlots) then
                target = info
            end
        end
    end
    return target
end

-- Scans bags 0-4 for one bag worth equipping, returning
-- { bag, slot, inventorySlot, itemLink, numSlots, replacing } (replacing is
-- the swapped-out bag's link, or nil for an empty slot), or nil. One at a
-- time, like FindBestEquipmentUpgrade(): the swap fires BAG_UPDATE_DELAYED,
-- which re-runs this against the new state. Among candidates, the biggest
-- bag wins.
function FindBagToEquip()
    local normalSlots = {}
    for _, bagId in ipairs(NORMAL_BAG_IDS) do
        local info = GetEquippedBagSlotInfo(bagId)
        if info then
            table.insert(normalSlots, info)
        end
    end

    local reagentSlots = {}
    local reagentBagId = GetReagentBagId()
    if reagentBagId then
        local info = GetEquippedBagSlotInfo(reagentBagId)
        if info then
            table.insert(reagentSlots, info)
        end
    end

    for _, info in ipairs(normalSlots) do
        AutoEquipDebugPrint("  bag slot " .. info.bagId .. " (inv " .. info.inventorySlot .. "): " ..
            tostring(info.itemLink) .. ", " .. info.numSlots .. " slots, subclass " .. tostring(info.subclassId))
    end
    AutoEquipDebugPrint("  reagent bag ID = " .. tostring(reagentBagId) ..
        (reagentSlots[1] and (" (inv " .. reagentSlots[1].inventorySlot .. "): " ..
            tostring(reagentSlots[1].itemLink) .. ", " .. reagentSlots[1].numSlots .. " slots") or " (no slot)"))

    local function isPlainBag(info)
        return info.subclassId == PLAIN_BAG_SUBCLASS_ID
    end
    local function anyBag()
        return true
    end

    local best = nil
    for bag = 0, 4 do
        for slot = 1, (C_Container.GetContainerNumSlots(bag) or 0) do
            local itemInfo = C_Container.GetContainerItemInfo(bag, slot)
            if itemInfo and itemInfo.hyperlink then
                local _, _, _, _, _, classId, subclassId = Compat_GetItemInfoInstant(itemInfo.hyperlink)
                if classId == CONTAINER_ITEM_CLASS_ID then
                    local numSlots = GetBagItemSlotCount(bag, slot)
                    AutoEquipDebugPrint("  " .. bag .. ":" .. slot .. " " .. itemInfo.hyperlink ..
                        " is a bag: subclass " .. tostring(subclassId) .. ", " .. tostring(numSlots) .. " slots")

                    local target = nil
                    if numSlots then
                        if subclassId == PLAIN_BAG_SUBCLASS_ID then
                            target = PickBagSlotForCandidate(normalSlots, bag, numSlots, isPlainBag)
                        elseif subclassId == LEATHERWORKING_BAG_SUBCLASS_ID then
                            target = PickBagSlotForCandidate(reagentSlots, bag, numSlots, anyBag)
                        end
                    end

                    if target and (best == nil or numSlots > best.numSlots) then
                        best = {
                            bag = bag, slot = slot, inventorySlot = target.inventorySlot,
                            itemLink = itemInfo.hyperlink, numSlots = numSlots, replacing = target.itemLink,
                        }
                    end
                end
            end
        end
    end

    return best
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
        local name = Compat_UnitAura("player", i)
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
    local name, _, _, thisDebuffType, duration, expirationTime = Compat_UnitDebuff("player", i)
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

-- Every class trainer spell the player should have learned by their current
-- level and hasn't yet. Each class file holds its own trainer table
-- (WARRIOR_TRAINER_SPELLS etc. -- distinct global names, since addon globals
-- are one flat namespace) of { level, name, spellId, cost (copper) } entries,
-- plus an optional requiresSpellId: the entry only counts once the player
-- knows that spell (talent- or quest-granted rank 1s, whose later ranks the
-- trainer only offers if you already have it).
-- Matched by spell ID (rank-specific) rather than IsSpellKnownByName(), since
-- a later rank of an already-known spell is still something to go train.
-- There's no addon API for "what can I train" without a trainer window open,
-- so these tables are hand-maintained -- a class with an empty/missing table
-- reads as "nothing to train".
local function GetClassTrainerSpells()
    local _, classFile = UnitClass("player")

    if classFile == "WARRIOR" then
        return WARRIOR_TRAINER_SPELLS
    elseif classFile == "SHAMAN" then
        return SHAMAN_TRAINER_SPELLS
    elseif classFile == "WARLOCK" then
        return WARLOCK_TRAINER_SPELLS
    end

    return nil
end

-- "Rend (R2)" -> "Rend"; names without a rank suffix are returned as-is.
local function TrainerSpellBaseName(name)
    return (name:gsub(" %(R%d+%)$", ""))
end

-- Learning a higher rank replaces the lower one in Classic's spellbook, so
-- IsPlayerSpell(lowerRankId) reads false afterwards (confirmed live: after
-- training Rend R2, Rend R1 read as unlearned). A rank therefore counts as
-- known if it, or any later entry for the same spell (tables are ordered by
-- level, so later = higher rank), is known.
function TrainerSpellRankKnown(trainerSpells, index)
    local baseName = TrainerSpellBaseName(trainerSpells[index].name)
    for j = index, #trainerSpells do
        local other = trainerSpells[j]
        if TrainerSpellBaseName(other.name) == baseName and PlayerKnowsSpellId(other.spellId) then
            return true
        end
    end
    return false
end

-- Interval-cached (see CacheOnInterval) and shared by both G6/G7 below, so the
-- two always refresh together off the same snapshot. Means a level-up or a
-- trainer visit can take up to a minute to show up -- fine for goal-setting.
local function ComputeUnlearnedTrainerSpells()
    local unlearned = {}
    local trainerSpells = GetClassTrainerSpells()
    if not trainerSpells then
        return unlearned
    end

    local playerLevel = UnitLevel("player")
    for i, spell in ipairs(trainerSpells) do
        -- requiresSpellId gates talent/quest-only ranks (e.g. Mortal Strike
        -- rank 2 needs the talent's rank 1): without it the entry is never
        -- trainable, so it mustn't count as "unlearned" forever. Checked by
        -- name, not ID: once rank 2 is learned, rank 1's ID stops reading as
        -- known (see TrainerSpellRankKnown), which would un-gate rank 3.
        local gateMet = spell.requiresSpellId == nil or IsSpellKnownByName(Compat_GetSpellInfo(spell.requiresSpellId))
        if spell.level <= playerLevel and gateMet and not TrainerSpellRankKnown(trainerSpells, i) then
            table.insert(unlearned, spell)
        end
    end

    return unlearned
end

local GetUnlearnedTrainerSpells = CacheOnInterval(ComputeUnlearnedTrainerSpells, RARE_STATE_REFRESH_INTERVAL_SECONDS)

-- /yytrain debug dump: what G6/G7 currently report (cached) vs. a fresh scan,
-- plus every unlearned entry, to diagnose a "wants to train, nothing to train".
function PrintTrainerSpellDebug()
    local fresh = ComputeUnlearnedTrainerSpells()
    local freshCost = 0
    for _, spell in ipairs(fresh) do
        freshCost = freshCost + spell.cost
    end

    print("YoyokazooUI: trainer-spell check (level " .. UnitLevel("player") .. ", money " .. GetMoney() .. "c) --")
    print("  cached:  AllSkillsKnown=" .. tostring(AllSkillsKnownForThisLevel()) .. " CanAfford=" .. tostring(CanAffordToTrainAllSkills()))
    print("  fresh:   " .. #fresh .. " unlearned, total cost " .. freshCost .. "c")
    for _, spell in ipairs(fresh) do
        print("    L" .. spell.level .. " " .. spell.name .. " (spellId " .. spell.spellId .. ", " .. spell.cost .. "c)"
            .. " IsPlayerSpell=" .. tostring(IsPlayerSpell and IsPlayerSpell(spell.spellId))
            .. " IsSpellKnown=" .. tostring(Compat_IsSpellKnown(spell.spellId)))
    end
end

-- Packed into MultiBoolTwo's G6.
function AllSkillsKnownForThisLevel()
    return #GetUnlearnedTrainerSpells() == 0
end

-- Packed into MultiBoolTwo's G7. True when nothing's left to train, too.
-- Uses the tables' base costs -- doesn't account for the trainer's
-- reputation discount, so this can read false slightly early, never true
-- when we actually can't afford it.
function CanAffordToTrainAllSkills()
    local totalCost = 0
    for _, spell in ipairs(GetUnlearnedTrainerSpells()) do
        totalCost = totalCost + spell.cost
    end

    return totalCost <= GetMoney()
end

------------------------------------------------------------
-- Spell data dump (/yyspells, YoyokazooUI.lua) -- debug-only. For rebuilding
-- a class's <CLASS>_TRAINER_SPELLS table on a client whose spell IDs/ranks
-- differ from the table's source (WoW Forever), from real client data
-- instead of guessing. Collects three sections:
--   trainer   -- every row of an open class trainer (all categories, not
--                just "available"): name, rank, category, level, cost, and
--                the spell ID from the row's link, if the client gives one
--   table     -- each existing table entry's spellId: what the client calls
--                that ID, and whether IsPlayerSpell/IsSpellKnown say it's known
--   spellbook -- every spell in the player's spellbook, with its spell ID
-- Lines go into YoyokazooUIDB.spellDump (the SavedVariables table, written to
-- disk on /reload or logout) rather than only chat, since ~100 chat lines
-- can't be copied out of the game.
------------------------------------------------------------
local function DescribeValue(value)
    if IsAnySecretValue(value) then return "SECRET" end
    return tostring(value)
end

local function SpellIdFromLink(link)
    if type(link) ~= "string" then return nil end
    return tonumber(link:match("|Hspell:(%d+)"))
end

local function ClientSpellName(spellId)
    if C_Spell and C_Spell.GetSpellName then
        return C_Spell.GetSpellName(spellId)
    end
    return (Compat_GetSpellInfo(spellId))
end

local TRAINER_FILTERS = { "available", "unavailable", "used" }

-- GetTrainerServiceInfo's return shape differs by client: Classic Era gives
-- (name, rank, category), but the Forever client gives (name, category,
-- icon) -- no rank at all (confirmed live via /yyspells). Returns
-- name, rank (nil on Forever), category on either.
function GetTrainerServiceNameRankCategory(index)
    local name, second, third = GetTrainerServiceInfo(index)
    if second == "available" or second == "unavailable" or second == "used" then
        return name, nil, second
    end
    return name, second, third
end

-- A trainer row's spell ID and rank text, read back off a hidden tooltip:
-- neither GetTrainerServiceInfo nor GetTrainerServiceItemLink gives the ID
-- on the Forever client. Tries GetSpell() and then GetTooltipData(), since
-- which one a client supports isn't verified yet; the rank usually sits in
-- the first line's right-hand text.
local scanTooltip = nil

local function ReadTrainerServiceTooltip(index)
    if not scanTooltip then
        scanTooltip = CreateFrame("GameTooltip", "YoyokazooUIScanTooltip", nil, "GameTooltipTemplate")
    end
    scanTooltip:SetOwner(WorldFrame, "ANCHOR_NONE")
    scanTooltip:ClearLines()
    scanTooltip:SetTrainerService(index)

    local spellId = nil
    if scanTooltip.GetSpell then
        spellId = select(2, scanTooltip:GetSpell())
    end
    if not spellId and scanTooltip.GetTooltipData then
        local data = scanTooltip:GetTooltipData()
        spellId = data and data.id
    end

    local rightText = _G["YoyokazooUIScanTooltipTextRight1"]
    local rankText = rightText and rightText:GetText()

    scanTooltip:Hide()
    return spellId, rankText
end

local function DumpTrainerServices(lines)
    local numServices = GetNumTrainerServices and GetNumTrainerServices() or 0
    if numServices == 0 or not IsClassTrainerOpen() then
        table.insert(lines, "trainer: no class trainer window open -- open one and run /yyspells again for this section")
        return
    end

    -- Show every category while dumping, then put the window's filters back.
    -- The on/off argument must be a real boolean -- the Forever client
    -- rejects Classic's 1/0 ("Missing on/off parameter").
    local savedFilters = {}
    if GetTrainerServiceTypeFilter and SetTrainerServiceTypeFilter then
        for _, filter in ipairs(TRAINER_FILTERS) do
            savedFilters[filter] = GetTrainerServiceTypeFilter(filter) and true or false
            SetTrainerServiceTypeFilter(filter, true)
        end
        numServices = GetNumTrainerServices() or 0
    end

    for i = 1, numServices do
        local name, rank, category = GetTrainerServiceNameRankCategory(i)
        local cost = GetTrainerServiceCost(i)
        local levelReq = GetTrainerServiceLevelReq and GetTrainerServiceLevelReq(i)
        local linkSpellId = SpellIdFromLink(GetTrainerServiceItemLink and GetTrainerServiceItemLink(i))

        local tooltipOk, tooltipSpellId, tooltipRank = pcall(ReadTrainerServiceTooltip, i)
        if not tooltipOk then
            tooltipRank = "tooltip error: " .. tostring(tooltipSpellId)
            tooltipSpellId = nil
        end
        local spellId = linkSpellId or tooltipSpellId

        table.insert(lines, string.format(
            "trainer %d | %s | rank=%s | tooltipRank=%s | %s | level=%s | cost=%s | spellId=%s | clientName=%s | known=%s",
            i, tostring(name), tostring(rank), tostring(tooltipRank), tostring(category), tostring(levelReq),
            tostring(cost), tostring(spellId), tostring(spellId and ClientSpellName(spellId)),
            DescribeValue(spellId and IsPlayerSpell and IsPlayerSpell(spellId))))
    end

    if SetTrainerServiceTypeFilter then
        for _, filter in ipairs(TRAINER_FILTERS) do
            SetTrainerServiceTypeFilter(filter, savedFilters[filter])
        end
    end
end

local function DumpTrainerTable(lines)
    local trainerSpells = GetClassTrainerSpells()
    if not trainerSpells then
        table.insert(lines, "table: no trainer table for this class")
        return
    end

    for _, spell in ipairs(trainerSpells) do
        table.insert(lines, string.format("table L%d | %s | spellId=%d | clientName=%s | IsPlayerSpell=%s | IsSpellKnown=%s",
            spell.level, spell.name, spell.spellId, tostring(ClientSpellName(spell.spellId)),
            DescribeValue(IsPlayerSpell and IsPlayerSpell(spell.spellId)),
            DescribeValue(Compat_IsSpellKnown(spell.spellId))))
    end
end

local function DumpSpellbook(lines)
    -- Modern clients: C_SpellBook skill lines (same as IsSpellKnownByName()).
    if not GetNumSpellTabs then
        if not (C_SpellBook and C_SpellBook.GetNumSpellBookSkillLines) then
            table.insert(lines, "spellbook: no spellbook API found")
            return
        end
        local bank = Enum and Enum.SpellBookSpellBank and Enum.SpellBookSpellBank.Player
        for line = 1, C_SpellBook.GetNumSpellBookSkillLines() do
            local lineInfo = C_SpellBook.GetSpellBookSkillLineInfo(line)
            if lineInfo then
                local offset = lineInfo.itemIndexOffset or 0
                for i = offset + 1, offset + (lineInfo.numSpellBookItems or 0) do
                    local info = C_SpellBook.GetSpellBookItemInfo and C_SpellBook.GetSpellBookItemInfo(i, bank)
                    if info then
                        table.insert(lines, string.format("spellbook %s | %s | subName=%s | spellId=%s | type=%s",
                            tostring(lineInfo.name), tostring(info.name), tostring(info.subName),
                            tostring(info.spellID), tostring(info.itemType)))
                    else
                        table.insert(lines, "spellbook " .. tostring(lineInfo.name) .. " | " ..
                            tostring(C_SpellBook.GetSpellBookItemName(i, bank)) .. " | (no item info)")
                    end
                end
            end
        end
        return
    end

    for tab = 1, GetNumSpellTabs() do
        local tabName, _, offset, numSpells = GetSpellTabInfo(tab)
        for i = offset + 1, offset + numSpells do
            local name, rank = GetSpellBookItemName(i, BOOKTYPE_SPELL)
            local _, spellId = GetSpellBookItemInfo(i, BOOKTYPE_SPELL)
            table.insert(lines, string.format("spellbook %s | %s | rank=%s | spellId=%s",
                tostring(tabName), tostring(name), tostring(rank), tostring(spellId)))
        end
    end
end

-- Returns the number of lines collected. Each section is pcall'd, so one
-- unexpected API shape doesn't lose the other two -- its error is recorded
-- in the dump instead.
function DumpSpellData()
    local lines = {
        string.format("dump at %s | %s level %d | client %s",
            date("%Y-%m-%d %H:%M:%S"), tostring(select(2, UnitClass("player"))), UnitLevel("player"),
            tostring((GetBuildInfo())))
    }

    for _, section in ipairs({ DumpTrainerServices, DumpTrainerTable, DumpSpellbook }) do
        local ok, err = pcall(section, lines)
        if not ok then
            table.insert(lines, "section failed: " .. tostring(err))
        end
    end

    YoyokazooUIDB.spellDump = lines
    return #lines
end

------------------------------------------------------------
-- Target-finding macro. The bot presses this macro (WowInput.FIND_TARGET_MACRO)
-- to find mobs; its /target lines differ per farming route, so the bot types
-- "/yytarget Mottled,Scorpid" (YoyokazooUI.lua) once it has resolved its route,
-- and SetTargetMacroMobs() rewrites the macro's body in place -- in place, so
-- its keybind survives. The macro itself must already exist (the addon never
-- creates it); IsTargetMacroMissing() drives an on-screen alert on
-- PLAYER_ENTERING_WORLD (YoyokazooUI.lua) if it doesn't.
------------------------------------------------------------
TARGET_MACRO_NAME = "0 Targ"
local MAX_MACRO_BODY_LENGTH = 255

function IsTargetMacroMissing()
    return GetMacroIndexByName(TARGET_MACRO_NAME) == 0
end

-- mobNamesCsv is e.g. "Mottled,Scorpid". Returns true on success, or false plus
-- a reason.
function SetTargetMacroMobs(mobNamesCsv)
    if InCombatLockdown() then
        return false, "can't edit macros in combat"
    end

    local macroIndex = GetMacroIndexByName(TARGET_MACRO_NAME)
    if macroIndex == 0 then
        return false, "no macro named \"" .. TARGET_MACRO_NAME .. "\""
    end

    local lines = { "/cleartarget", "/stopmacro [mod:shift]" }
    for name in string.gmatch(mobNamesCsv or "", "[^,]+") do
        name = strtrim(name)
        if name ~= "" then
            table.insert(lines, "/target " .. name)
        end
    end

    if #lines == 2 then
        return false, "no mob names given"
    end

    local body = table.concat(lines, "\n")
    if #body > MAX_MACRO_BODY_LENGTH then
        return false, "macro body is " .. #body .. " chars, over the " .. MAX_MACRO_BODY_LENGTH .. " limit"
    end

    -- Pass the existing name/icon back unchanged, so only the body changes.
    local name, icon = GetMacroInfo(macroIndex)
    EditMacro(macroIndex, name, icon, body)

    -- Read it back rather than trusting EditMacro silently worked.
    local _, _, newBody = GetMacroInfo(GetMacroIndexByName(TARGET_MACRO_NAME))
    if newBody ~= body then
        return false, "EditMacro didn't take -- body reads back as: " .. tostring(newBody)
    end

    return true
end

------------------------------------------------------------
-- Pixel row bool fields (drawn by InitializePixelRow() in UIFunctions.lua
-- via EncodeBoolFields() in MathFunctions.lua). Each list is one row: entry
-- N is the global function shown in box N, green when it returns true.
-- Entries are function NAMES, looked up each time the row is drawn, so they
-- can live in any file regardless of load order. The C# decoders read the
-- same positions (WowWorldState.cs for these Multi rows, Wow*ClassState.cs
-- for the Class rows) -- change both sides together, and put RESERVED in a
-- retired entry's place rather than removing it, so later boxes don't shift.
--
-- The (R1)/(G3)/(B1) tags are each box's name from when a row was a single
-- pixel's R/G/B bytes: Rn = box n, Gn = box 8+n, Bn = box 16+n. CLAUDE.md
-- still refers to boxes that way.
------------------------------------------------------------
function IsPlayerWarrior() return select(2, UnitClass("player")) == "WARRIOR" end
function IsPlayerShaman()  return select(2, UnitClass("player")) == "SHAMAN"  end
function IsPlayerWarlock() return select(2, UnitClass("player")) == "WARLOCK" end
-- MultiBoolTwo box 23 (B7), decoded into WowWorldState.PlayerIsHorde -- picks which
-- faction's routes/NPCs/zone crossings the bot may use. False means Alliance.
function IsPlayerHorde() return UnitFactionGroup("player") == "Horde" end

MULTI_BOOL_ONE_FIELDS = {
    "IsAttacking",                  --  1 (R1)
    "AreWeLowOnHealthPotions",      --  2 (R2)
    "AreWeLowOnDynamite",           --  3 (R3)
    "AreWeLowOnAmmo",               --  4 (R4)
    "IsGlobalCooldownCooledDown",   --  5 (R5)
    "AreBagsFull",                  --  6 (R6)
    "IsInCombat",                   --  7 (R7)
    "IsPlayerPetrified",            --  8 (R8)
    "HasUnseenWhisper",             --  9 (G1) YoyokazooUI.lua
    RESERVED,                       -- 10 (G2) was IsInMeleeRange -- CheckInteractDistance proved unreliable
    "IsPlayerCasting",              -- 11 (G3)
    "AreEnemyNameplatesTurnedOn",   -- 12 (G4)
    "CurrentTargetInCombatWithUs",  -- 13 (G5)
    "IsTargetCasterMob",            -- 14 (G6)
    "IsTargetRunnerMob",            -- 15 (G7)
    "IsTargetFireImmune",           -- 16 (G8)
    "HasRecentTargetEvade",         -- 17 (B1) YoyokazooUI.lua
    "IsPlayerWarrior",              -- 18 (B2) supported-class bits: exactly one of these and
    RESERVED,                       -- 19 (B3)   MultiBoolTwo's box 4 (Warlock) is true. 19 was
    "IsPlayerShaman",               -- 20 (B4)   Mage, removed along with Mage support.
    "IsPlayerPoisoned",             -- 21 (B5)
    "IsPlayerDiseased",             -- 22 (B6)
    "IsTargetNatureImmune",         -- 23 (B7)
    "IsTargetCasting",              -- 24 (B8)
}

MULTI_BOOL_TWO_FIELDS = {
    "IsTargetLongRangeCaster",      --  1 (R1)
    "IsLogoffMobSeen",              --  2 (R2)
    "IsCurrentlySkinning",          --  3 (R3)
    "IsPlayerWarlock",              --  4 (R4) supported-class bit, see MultiBoolOne box 18
    "IsTargetBleedImmune",          --  5 (R5)
    "IsTargetFearCaster",           --  6 (R6)
    "HasRecentFacingWrongWayError", --  7 (R7) red error toasts: YoyokazooUI.lua
    "HasRecentNotInFrontError",     --  8 (R8)
    "IsLogoutOnLowDynamiteEnabled", --  9 (G1) /yyconfig setting, YoyokazooUI.lua
    "IsLogoutOnFullBagsEnabled",    -- 10 (G2) /yyconfig setting, YoyokazooUI.lua
    "HasDesiredWorldBuff",          -- 11 (G3) buff picked via /yyconfig
    "HasHighLatency",               -- 12 (G4)
    "IsCombatStalemate",            -- 13 (G5) YoyokazooUI.lua
    "AllSkillsKnownForThisLevel",   -- 14 (G6)
    "CanAffordToTrainAllSkills",    -- 15 (G7)
    "GearNeedsRepair",              -- 16 (G8)
    "ShouldWeAttackTarget",         -- 17 (B1) pure eligibility -- no range/cooldown folded in
    "HasRecentInvalidTargetError",  -- 18 (B2) red error toasts: YoyokazooUI.lua
    "HasRecentOutOfRangeError",     -- 19 (B3)
    "HasRecentLineOfSightError",    -- 20 (B4)
    "HasRecentNoPathError",         -- 21 (B5)
    "IsPlayerUnderwater",           -- 22 (B6) breath timer running
    "IsPlayerHorde",                -- 23 (B7) faction, for route/NPC/zone crossing picking
}

-- The player's class's list for one of the Class rows: listSuffix
-- "CLASS_BOOL_ONE_FIELDS" gives e.g. WARRIOR_CLASS_BOOL_ONE_FIELDS
-- (WarriorFunctions.lua) -- so the same box means something different per
-- class. An unsupported class gets an empty list (an all-red row).
function GetPlayerClassFields(listSuffix)
    local _, classFile = UnitClass("player")
    return _G[(classFile or "") .. "_" .. listSuffix] or {}
end

-- Auto-pick the "I require <Class> training." gossip option when talking to a
-- class trainer, so the trainer window opens without anyone clicking the
-- gossip menu (class trainers show a gossip menu first -- training, unlearn
-- talents, etc.). Driven by GOSSIP_SHOW in YoyokazooUI.lua. Lua-only;
-- nothing reaches the pixel row. Confirmed working live.

-- Case-insensitive, any class name in the middle, trailing period optional.
-- Lua patterns, not regex: "%." is a literal period, ".+" is one or more of
-- anything.
local TRAINER_GOSSIP_PATTERN = "^i require .+ training%.?$"

function IsTrainerGossipOptionText(text)
    if type(text) ~= "string" then
        return false
    end
    return string.lower(text):match(TRAINER_GOSSIP_PATTERN) ~= nil
end

-- Returns the matching C_GossipInfo.GetOptions() entry and its position in
-- that list, or nil if this gossip menu has no "I require * training." option.
function FindTrainerGossipOption()
    if not (C_GossipInfo and C_GossipInfo.GetOptions) then
        return nil
    end

    for i, option in ipairs(C_GossipInfo.GetOptions() or {}) do
        if type(option) == "table" and IsTrainerGossipOptionText(option.name) then
            return option, i
        end
    end

    return nil
end

-- Auto-train at a class trainer: once the trainer window is open (TRAINER_SHOW
-- in YoyokazooUI.lua), buy every skill the trainer lists as "available" that
-- we can afford, one per tick. Class trainers only -- IsTradeskillTrainer()
-- trainers are skipped, since buying e.g. "Apprentice Skinning" would spend a
-- profession slot. Lua-only; nothing reaches the pixel row.
--
-- GetTrainerServiceInfo's return shape differs between Classic Era and the
-- Forever client (see GetTrainerServiceNameRankCategory() above -- on Forever
-- this scan used to read the icon ID as the category, so it never bought
-- anything). AUTO_TRAIN_DEBUG prints every row the scan sees and why it was
-- or wasn't picked. Off now that buying is confirmed working on Forever.
AUTO_TRAIN_DEBUG = false

function AutoTrainDebugPrint(message)
    if AUTO_TRAIN_DEBUG then
        print("YoyokazooUI auto-train: " .. message)
    end
end

function IsClassTrainerOpen()
    if IsTradeskillTrainer and IsTradeskillTrainer() then
        return false
    end
    return true
end

-- Returns the index, name, rank and cost of the first affordable "available"
-- trainer service, or nil if there's nothing left we can buy. Re-scanned
-- after every purchase rather than building a queue up front: buying a
-- service removes/reorders rows, so indices from an earlier scan go stale.
function FindNextAffordableTrainerService()
    -- The trainer list is filtered by the window's own Available/Unavailable/
    -- Used checkboxes; if "available" is unchecked, those rows aren't listed
    -- at all.
    if GetTrainerServiceTypeFilter and SetTrainerServiceTypeFilter
        and not GetTrainerServiceTypeFilter("available") then
        AutoTrainDebugPrint("\"available\" filter was off -- turning it on")
        SetTrainerServiceTypeFilter("available", true)  -- boolean: Forever rejects 1/0
    end

    local money = GetMoney()
    local numServices = GetNumTrainerServices() or 0
    AutoTrainDebugPrint("scanning " .. numServices .. " service row(s), money " .. money)

    for i = 1, numServices do
        local name, rank, category = GetTrainerServiceNameRankCategory(i)
        local cost = GetTrainerServiceCost(i)
        AutoTrainDebugPrint("  " .. i .. ": " .. tostring(name) .. " (" .. tostring(rank) .. ") category=" ..
            tostring(category) .. " cost=" .. tostring(cost))

        if category == "available" and type(cost) == "number" and cost <= money then
            return i, name, rank, cost
        end
    end

    return nil
end

-- Auto-pick talents: spends free talent points following the class's talent
-- build table (WARRIOR_TALENT_BUILD in TalentConfig.lua -- Warrior only
-- so far; any other class has no build and does nothing). Driven from
-- YoyokazooUI.lua (CHARACTER_POINTS_CHANGED etc.), gated by the /yyconfig
-- "Auto-pick talents" checkbox. Lua-only; nothing reaches the pixel row.
--
-- UnitCharacterPoints/GetTalentInfo/LearnTalent's behavior on this client
-- build isn't verified live yet (see the top of CLAUDE.md) --
-- AUTO_TALENT_DEBUG prints every talent the scan sees and why it was or
-- wasn't picked. Off now; flip it back on if auto-talent needs debugging.
AUTO_TALENT_DEBUG = false

function AutoTalentDebugPrint(message)
    if AUTO_TALENT_DEBUG then
        print("YoyokazooUI auto-talent: " .. message)
    end
end

local function GetClassTalentBuild()
    local _, classFile = UnitClass("player")

    if classFile == "WARRIOR" then
        return WARRIOR_TALENT_BUILD
    end

    return nil
end

-- Returns tabIndex, talentIndex, name, currentRank, targetRank for the next
-- point to spend, or nil if there's no free point, no build for this class,
-- or the build is finished. Matched by name: a talent's index within its tab
-- isn't guaranteed to follow its position in the tree. A build entry whose
-- name isn't found at all stops the scan (rather than skipping ahead), so a
-- typo can't shift every later point into the wrong talent.
function FindNextTalentToLearn()
    local build = GetClassTalentBuild()
    if not build then
        return nil
    end

    local freePoints = UnitCharacterPoints("player") or 0
    if freePoints < 1 then
        AutoTalentDebugPrint("no free talent points")
        return nil
    end

    local talentsByName = {}
    for tab = 1, (GetNumTalentTabs() or 0) do
        for index = 1, (GetNumTalents(tab) or 0) do
            local name, _, _, _, rank, maxRank = GetTalentInfo(tab, index)
            if name then
                talentsByName[name] = { tab = tab, index = index, rank = rank or 0, maxRank = maxRank }
            end
        end
    end

    for _, entry in ipairs(build) do
        local talent = talentsByName[entry.name]
        if not talent then
            AutoTalentDebugPrint("build talent \"" .. entry.name .. "\" not found -- stopping")
            return nil
        end

        AutoTalentDebugPrint("  " .. entry.name .. ": rank " .. talent.rank .. "/" .. tostring(talent.maxRank) ..
            ", target " .. entry.rank .. " (tab " .. talent.tab .. ", index " .. talent.index .. ")")

        if talent.rank < entry.rank then
            return talent.tab, talent.index, entry.name, talent.rank, entry.rank
        end
    end

    AutoTalentDebugPrint("build finished (" .. freePoints .. " point(s) left unspent)")
    return nil
end
