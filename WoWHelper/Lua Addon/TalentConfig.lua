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
