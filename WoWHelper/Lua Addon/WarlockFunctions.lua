------------------------------------------------------------
-- Warlock-specific game-state checks.
--
-- STUB: no Warlock-specific checks implemented yet. Follow the pattern in
-- ShamanFunctions.lua/MageFunctions.lua -- add real checks above the
-- Get*ClassBool*/ClassInt* functions below, then pack them in (bit order
-- here MUST match WowWarlockClassState.UpdateFromBitmap on the C# side --
-- add both sides together, see "Adding a new pixel" in CLAUDE.md).
--
-- Note: CanSpellcastPullTarget() stays in WoWFunctions.lua -- it's shared
-- between Mage/Shaman/Warlock (branches internally on class); giving it the
-- same name here would collide (last file loaded in the .toc silently wins,
-- since addon globals are one flat namespace).
------------------------------------------------------------

------------------------------------------------------------
-- Packs Warlock-specific state into the ClassBool/ClassInt pixels. Called via
-- the GetClassBoolOne/Two/GetClassIntOne dispatchers in WoWFunctions.lua once
-- UnitClass("player") resolves to WARLOCK. All-zero (fully reserved) until
-- real fields get added above.
------------------------------------------------------------
function GetWarlockClassBoolOne()
    -- TODO: pack real Warlock checks here (e.g. Corruption/Immolate DoT
    -- tracking, curse selection, pet state), same pattern as
    -- GetShamanClassBoolOne in ShamanFunctions.lua.
    return 0, 0, 0
end

function GetWarlockClassBoolTwo()
    -- Reserved for future Warlock-specific flags.
    return 0, 0, 0
end

function GetWarlockClassIntOne()
    -- Reserved for future Warlock-specific numeric values; none needed yet.
    return 0, 0, 0
end
