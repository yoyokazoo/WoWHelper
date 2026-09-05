-- Name-based special-case creature lists, all in one place so they're easy to
-- find and update. Classic has no reliable creature-ID API exposed to
-- addons, so every list here keys off UnitName("target") instead. Globals
-- (not local) so WoWFunctions.lua's IsTargetXxx() checks can read them --
-- addon globals are one flat namespace, so plain assignment is enough; see
-- YoyokazooUI.toc for load order (this file loads before WoWFunctions.lua).
--
-- To add a new list (e.g. nature-immune mobs): add a table here following
-- the same pattern, then add an IsTargetXxx() check next to
-- IsTargetFireImmune() in WoWFunctions.lua that reads it.

-- Mobs whose spells are worth interrupting.
CASTER_MOB_NAMES = {
    ["Withered Ancient"] = true,
    ["Saltstone Crystalhide"] = true,
}

-- Mobs that flee/run at low health (or otherwise need special handling to
-- stop them running off).
RUNNER_MOB_NAMES = {
    ["Bloodfury Harpy"] = true,
}

-- Mobs immune (or effectively immune) to fire damage/effects.
FIRE_IMMUNE_MOB_NAMES = {
    ["Rogue Flame Spirit"] = true,
}

-- Mobs immune (or effectively immune) to nature damage/effects.
NATURE_IMMUNE_MOB_NAMES = {
    ["Swirling Vortex"] = true,
}

-- Mobs with a ranged attack whose range exceeds Earth Shock's -- worth
-- closing distance on before trying to interrupt, rather than just standing
-- still and hoping they wander into range.
LONG_RANGE_CASTER_MOB_NAMES = {
    ["Mosshoof Runner"] = true,
    ["Legashi Hellcaller"] = true,
}
