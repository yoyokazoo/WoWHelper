-- Per-class talent builds, read by FindNextTalentToLearn() in WoWFunctions.lua
-- (picked per class by GetClassTalentBuild() there). Kept in their own file
-- since they grow with every level of a build.
--
-- Each build is ordered; each entry is a cumulative target rank ("get this
-- talent to rank N"), and a free point always goes to the first entry not yet
-- reached -- so consecutive ranks of one talent collapse into one entry, and
-- a talent revisited later (e.g. Deflection) appears again with a higher
-- rank. Matched by exact talent name, not tab/index; a name that isn't found
-- in the client's talent tree stops the scan rather than skipping ahead.
-- Comments give the character level each entry's points land on.

-- Generated from a talent calculator export for levels 10-60.
WARRIOR_TALENT_BUILD = {
    { name = "Deflection",                       rank = 2 }, -- 10-11
    { name = "Improved Rend",                    rank = 3 }, -- 12-14
    { name = "Improved Tactical Mastery",        rank = 3 }, -- 15-17
    { name = "Improved Overpower",               rank = 2 }, -- 18-19
    { name = "Anger Management",                 rank = 1 }, -- 20
    { name = "Deep Wounds",                      rank = 3 }, -- 21-23
    { name = "Deflection",                       rank = 3 }, -- 24
    { name = "Two-Handed Weapon Specialization", rank = 3 }, -- 25-27
    { name = "Impale",                           rank = 2 }, -- 28-29
    { name = "Sweeping Strikes",                 rank = 1 }, -- 30
    { name = "Weaponmaster",                     rank = 5 }, -- 31-35
    { name = "Bloodthrill",                      rank = 2 }, -- 36-37
    { name = "Deflection",                       rank = 5 }, -- 38-39
    { name = "Mortal Strike",                    rank = 1 }, -- 40
    { name = "Cruelty",                          rank = 5 }, -- 41-45
    { name = "Unbridled Wrath",                  rank = 5 }, -- 46-50
    { name = "Blood Craze",                      rank = 3 }, -- 51-53
    { name = "Enrage",                           rank = 5 }, -- 54-58
    { name = "Improved Execute",                 rank = 2 }, -- 59-60
}

-- Generated from a talent calculator export for levels 10-60, with the
-- Restoration block moved ahead of the Elemental block (the export took
-- Elemental first).
--
-- Subject to change: this build leans on Maelstrom Weapon (5 points). If the
-- bot turns out not to be able to use Maelstrom Weapon procs well, those
-- points (and possibly the rest of the deep Enhancement picks) should be
-- reallocated.
SHAMAN_TALENT_BUILD = {
    -- Enhancement
    { name = "Thundering Strikes",               rank = 5 }, -- 10-14
    { name = "Improved Lightning Shield",        rank = 3 }, -- 15-17
    { name = "Mental Dexterity",                 rank = 2 }, -- 18-19
    { name = "Shamanistic Focus",                rank = 1 }, -- 20
    { name = "Elemental Weapons",                rank = 3 }, -- 21-23
    { name = "Mental Dexterity",                 rank = 3 }, -- 24
    { name = "Stormstrike",                      rank = 1 }, -- 25
    { name = "Flurry",                           rank = 4 }, -- 26-29
    { name = "Spirit Weapons",                   rank = 1 }, -- 30
    { name = "Improved Stormstrike",             rank = 2 }, -- 31-32
    { name = "Mental Quickness",                 rank = 2 }, -- 33-34
    { name = "Maelstrom Weapon",                 rank = 1 }, -- 35
    { name = "Flurry",                           rank = 5 }, -- 36
    { name = "Maelstrom Weapon",                 rank = 4 }, -- 37-39
    { name = "Rage of the Farseer",              rank = 1 }, -- 40

    -- Restoration
    { name = "Tidal Focus",                      rank = 5 }, -- 41-45
    { name = "Mindfulness",                      rank = 3 }, -- 46-48

    -- Last Maelstrom Weapon point, kept between the two off-spec blocks
    { name = "Maelstrom Weapon",                 rank = 5 }, -- 49

    -- Elemental
    { name = "Convection",                       rank = 5 }, -- 50-54
    { name = "Elemental Devastation",            rank = 3 }, -- 55-57
    { name = "Concussion",                       rank = 2 }, -- 58-59
    { name = "Elemental Focus",                  rank = 1 }, -- 60
}
