# WoWHelper (CrocBot 2.0)

A World of Warcraft Classic automation bot ("botting" tool) with two halves that
communicate through a screen-pixel protocol instead of memory reading or network
packets:

1. **`WoWHelper/` — C# bot** (.NET Framework 4.7.2, WinForms app, `WoWHelper.sln`)
   Watches the game window, decides what to do, and sends real keyboard/mouse
   input (no memory injection, no packet manipulation).
2. **`WoWHelper/Lua Addon/` — in-game addon** (`YoyokazooUI`)
   Runs inside WoW, reads real game state via the WoW Lua API, and encodes that
   state as RGB colors on small on-screen swatches that the bot's screen capture
   can read back out.

This split exists because WoW Classic doesn't expose an external API — Lua
inside the client can see true game state (HP, cooldowns, buffs, coordinates,
combat flags, etc.), but the external bot process can't call Lua directly. The
addon acts as a one-way state → pixel-color encoder; the bot decodes those
pixels every loop tick into a `WowWorldState` and drives play from there.

**When debugging the Lua addon, add diagnostic logging before guessing at a
fix.** The WoW Lua API's exact behavior for a given client version isn't
verifiable by reading this repo's source — it depends on Blizzard's current
FrameXML/engine internals, which change across patches (e.g. nameplate frame
structure, UI scale plumbing) without being documented here. Two debugging
sagas in this repo confirmed that reasoning from memory about what "should"
be happening burns multiple guess-and-check round trips, while adding
targeted `print()`/temporary debug flags, asking the user to reproduce
in-game, and fixing from the actual returned values converges in one round
trip. Prefer the latter whenever the failure could plausibly stem from more
than one internal API behavior.

## The color-encoding contract (the critical coupling point)

`UIFunctions.lua`'s `InitializePixelRow()` paints a block of **pixel rows**,
each `PIXEL_SIZE` (3) physical pixels tall, pinned to the screen's literal
top-left corner — fixed and resolution-independent (no per-resolution
calibration needed). **It holds exactly what the C# bot reads** — nothing
decorative or debug-only lives here (that's what the separate
`InitializeIndicators()` frame is for, see the Lua addon section below).
Everything is drawn into one container frame at `TOOLTIP` strata, so no other
UI (e.g. the player frame, which sits in that corner) can cover it. Sizes
and offsets are given in physical pixels and converted into UIParent's local
coordinate units via `GetPhysicalPixelsPerLocalUnit()`, a
**self-calibrating** ratio computed as
`GetPhysicalScreenSize() / UIParent:GetWidth()` — NOT
`UIParent:GetEffectiveScale()` and NOT Blizzard's `PixelUtil` library, both
of which were tried first and gave wrong/inconsistent results. On the dev
machine (Classic Era 1.15.9, post Edit-Mode UI update), `GetEffectiveScale()`
read 0.9, but the real measured ratio was 1.6875 — confirmed by matching four
independently-measured rendered sizes exactly, none of which matched any
formula built from 0.9 (on the WoW Forever client at 3440x1440 the ratio is
1.2). Comparing `GetPhysicalScreenSize()` against `UIParent`'s own reported
size sidesteps needing to know *why* `GetEffectiveScale()` disagrees with it,
so it should keep working even if this particular quirk changes or gets
patched later. Elements are only repositioned when that ratio changes; their
values redraw every 0.05s.

The C# side's layout and decoders all live in `Code/Gameplay/WowPixelRows.cs`
(`PixelRow` enum, `WowPixelRows` constants/readers, `PixelRowBits`). It
always reads the **center pixel** of a box or bar row, for margin against
residual edge blur. Every constant there MUST match its Lua counterpart
(`PixelSize`/`PIXEL_SIZE`, `StatusBarWidth`/`PIXEL_BAR_WIDTH`,
`BitsPerRow`/`PIXEL_ROW_BITS`, `FixedPointFractionBits`/
`FIXED_POINT_FRACTION_BITS`, `ADDON_LOADED_COLOR`), and so must the row
order. Row N covers physical y = 3N to 3N+2 (read at y = 3N+1):

| Row | Content | Encoding | Decoded into |
|---|---|---|---|
| 0 | Player health bar, plus the sentinel box right after it at x = 100 (read at (101, 1)) | bar, fill `(0, 255, 0)`; sentinel exactly `ADDON_LOADED_COLOR` (96, 255, 117) | `PlayerHpPercent`; `AddonLoaded` (see below) |
| 1 | Player resource bar | bar, fill `(0, 0, 255)` | `ResourcePercent` |
| 2 | Target health bar (empty with no target) | bar, fill `(255, 0, 0)` | `TargetHpPercent` |
| 3 | *(empty — spacing between the bars and the bit rows)* | — | — |
| 4 | Map X | fixed point | `MapX` |
| 5 | Map Y | fixed point | `MapY` |
| 6 | Facing, degrees | fixed point | `FacingDegrees` |
| 7 | `MultiBoolOne` — `MULTI_BOOL_ONE_FIELDS` (class-agnostic, all 24 boxes used or reserved) | bool fields | various `WowWorldState` bools |
| 8 | `MultiBoolTwo` — `MULTI_BOOL_TWO_FIELDS` (class-agnostic — boxes 1-21 used so far) | bool fields | `IsTargetLongRangeCaster`/`LogoffMobSeen`/`IsCurrentlySkinning`/`IsTargetBleedImmune`/`IsTargetFearCaster`/`LogoutOnLowDynamiteEnabled`/`LogoutOnFullBagsEnabled`/`HasDesiredWorldBuff`/`HighLatency`/`CombatStalemate`/`AllSkillsKnownForThisLevel`/`CanAffordToTrainAllSkills`/`GearNeedsRepair`/`IsTargetAttackable`, plus the red error toast flags `FacingWrongWay`/`TargetNeedsToBeInFront`/`InvalidTarget`/`OutOfRange`/`NotInLineOfSight`/`NoPathAvailable` |
| 9 | `ClassBoolOne` — `<CLASS>_CLASS_BOOL_ONE_FIELDS` (class-specific) | bool fields | a `WowClassState` subtype (see C# architecture section) |
| 10 | `ClassBoolTwo` — `<CLASS>_CLASS_BOOL_TWO_FIELDS` (class-specific — only Shaman's boxes 1-3 used so far) | bool fields | a `WowClassState` subtype (Shaman: `IsInEarthShockRange`/`HasClearcasting`/`CanCastFrostShock`) |
| 11 | `MultiIntOne` — attacker count, player level, zone ID | bytes | `AttackerCount`/`PlayerLevel`/`CurrentZone` |

**Bars** (rows 0-2) are `PIXEL_BAR_WIDTH` (100) physical-pixel `StatusBar`s
at x = 0, empty color `(0, 0, 0)`, filled left to right — filled pixel count
= percent. They exist because Midnight-rules clients (WoW Forever) return
secret values from `UnitHealth`/`UnitPower`, which Lua can't do math on, but
a `StatusBar` accepts secrets in `SetMinMaxValues`/`SetValue` and renders
them itself (the bar value sources pass them through untouched).
`WowPixelRows.ReadStatusBarPercent()` binary-searches the center row for the
first unfilled pixel (at most 7 reads per bar), "filled" meaning the bar's
fill channel is >= 128 (so a half-covered edge pixel rounds to nearest).

**Bit rows** (rows 4-11) are 24 boxes, green `(0, 255, 0)` = 1 and red
`(255, 0, 0)` = 0, together one unsigned integer with **box 1 (leftmost) as
bit 0**. C# reads a box as 1 when its green channel beats its red
(`WowPixelRows.ReadBits()` → `PixelRowBits`). Three encodings, Lua helpers in
`MathFunctions.lua`, C# accessors on `PixelRowBits`:
- **bool fields** (`EncodeBoolFields`, C# `bits[n]`, **1-based** to match the
  Lua lists): a list of global function *names* (looked up at draw time, so
  they can live in any file regardless of load order) or `RESERVED`; entry N
  is box N. Each field is `pcall`'d on its own, so one that throws (e.g. on a
  secret value mid-combat) reads false — printing once per distinct error —
  without affecting the rest of its row. Put `RESERVED` in a retired entry's
  place rather than deleting it, so later boxes don't shift.
- **bytes** (`EncodeBytes`, C# `bits.Byte(0..2)`): three 0-255 ints in boxes
  1-8, 9-16, 17-24 (clamped).
- **fixed point** (`EncodeFixedPoint`, C# `bits.FixedPoint`): unsigned, 15
  fraction bits (max just under 512, 1/32768 precision). The map
  coordinates/facing themselves are still rounded to 0.01 on the Lua side,
  which is the real precision limit.

**Legacy box names:** each bool row used to be a single pixel's packed
R/G/B bytes, and this file (and the code's comments) still name boxes that
way: **Rn = box n, Gn = box 8+n, Bn = box 16+n** (e.g. `MultiBoolTwo`'s G3 is
box 11). The Lua field lists tag every entry with both.

Each pixel-row element's update is `pcall`'d in `InitializePixelRow()`'s
refresh loop (a failing one keeps its last drawn value and prints once per
distinct error), so one failure can't freeze the rest.

The sentinel box is the one exception to the schemes above — a plain
exact-color match (`WowPixelRows.ADDON_LOADED_COLOR`): if it's exactly that
color, the addon is loaded and rendering the rows (so the rest of them are
meaningful) and `AddonLoaded` is true; *any* other color — including
whatever's actually on-screen at that position when the addon isn't loaded,
or another window covering WoW — means `AddonLoaded` is false. This replaced
an older, unrelated mechanism (a multi-point text/UI pixel-signature match
against login-screen-specific colors) since folding it into the rows is more
reliable than matching login-screen chrome. Since a covered WoW window also
hides the sentinel, `AddonLoaded` alone can't tell "logged out" from "lost
focus" — so `WowWorldState` also carries `WowWindowHasFocus` (the WoW window
was the OS foreground window at capture time, checked in `GetWoWWorldState`
only — false for bitmap-only test updates), and `IsBotInAValidState`
(`AddonLoaded && WowWindowHasFocus`) is what gates reading anything else off
the rows (`EveryWorldStateUpdateTasks()`'s early-return, `FocusOnWindowTask`,
the combat short-circuit in `CoreGameplayLoopTask`). "Actually logged out"
(the disconnect alert, `CheckIfLoggedOutTask`) is
`WowWindowHasFocus && !AddonLoaded`. Separately, **text/UI state
matched by exact pixel signature** (trade window, breath bar) is still its own unrelated mechanism — resolution-specific coordinates
in `WowScreenConfigs.cs`, compared via `ImageMatchColorPositions.MatchesSourceImage`,
some validated against bitmaps in `WoWHelperUnitTests/Source Images/`.

**Zone ID (`MultiIntOne`'s third byte, `WowWorldState.CurrentZone`):** a
WowHelper-defined numeric zone ID, NOT Blizzard's internal map ID (which
doesn't fit in a byte). Lua's `GetCurrentZoneId()`
(`WoWFunctions.lua`) looks up `GetRealZoneText()` against a
`ZONE_NAME_TO_ID` table (255 = not a known zone); the numeric values there
MUST stay in sync with the `WowZone` enum in `WowLocationConfiguration.cs`
(`Unknown = 255`) — two independent hardcoded tables that have to agree,
same class of coupling as everything else in this contract. Each
`WowLocationConfiguration` in `WowLocationConfigs.cs` also carries a `Zone`
and `MinimumLevel` (both on its `Route` — see the `Config/` bullet below),
for validating the character is in the
right place/level before a farming route starts — `CurrentZone` is the
runtime half of that check. That validation (plus a third check: is the
player near *any* of the route's own waypoints, via
`WowPathfinding.GetDistanceToClosestWaypoint` against the single global
`WowPlayerConstants.MAX_DISTANCE_FROM_ROUTE_WAYPOINT` threshold — deliberately
one constant sized off the loosest route's own largest adjacent-waypoint gap,
not a per-config value) is wired into `WowManagementTasks.SetLogoutVariablesTask()`,
checked first so a bad start gives the clearest possible logout reason.

**Cross-zone navigation:** map coordinates are per-zone, so routes in
different zones never share a waypoint; `WowZoneCrossing`
(`Config/Definitions/`, instances in `Config/Data/WowZoneCrossings.cs`) is the
link between them instead: a `FromZone`/`FromPoint` -> `ToZone`/`ToPoint` pair
plus `HeadingDegrees` and `MinimumLevel`. Each link is defined once and is
walkable both ways: `ALL_ZONE_CROSSINGS` adds each one's `Reversed()`, which
heads straight back (`HeadingDegrees` + 180) unless `ReturnHeadingDegrees` is
set, for paths that bend at the border. There's no one-way crossing yet.
`FromPoint`/`ToPoint` must be waypoints on routes in their own zones (enforced
by `ZoneCrossingTests.cs`, not at runtime). `WowPathfinding.GetZoneCrossingsBetween`
searches zones for the fewest crossings (null = unreachable, empty = same zone),
which `PickFarmingLocationConfig` and `PickNPCToInteractWith` also use to prefer
the current zone and then the nearest reachable one. `GetRouteToTarget` only
builds one leg at a time: for a target in another zone, it returns a route to
the next crossing's `FromPoint` with `ZoneCrossingAtEnd` set.
`NavigateToEndOfCurrentRouteTask` then goes `CROSSING_ZONE` (`CrossZoneTask`
holds the heading, up to `ZONE_CROSSING_TIMEOUT_MILLIS`) -> `CROSSED_ZONE`,
and the caller rebuilds from the new zone, so no trip progress is stored.
Crossing completion is keyed off `CurrentZone`, checked every tick since the
border can come before `FromPoint`. Walking crossings only so far; a transit
zone must be added to both `WowZone` and `ZONE_NAME_TO_ID`. `PlayerGoal.TRAVEL`
is still an unused stub, since this lives inside navigation rather than a goal.
The Durotar/Orgrimmar crossing is stubbed: `ORGRIMMAR_ENTRANCE_ROUTE`'s waypoint
and the crossing's `HeadingDegrees` still need real values.

**Expected mob roster (`WowLocationConfiguration.ExpectedMobNames`):** every
mob name a route is expected to pull, e.g. `{ "Desert Rumbler" }` for
`LEVEL_58_SILITHUS_RUMBLERS` — distinct from `TargetMacroMobNames` (see
"Per-route target macro" below), which are partial name substrings for a target-cycling macro, not a complete/exact roster.
`WowLocationConfiguration.AllMobsInZoneAreNatureImmune()` checks every name
in that list against `CreatureConfig.NATURE_IMMUNE_MOB_NAMES`
(`Config/Definitions/CreatureConfig.cs`) — a **C# mirror** of
`CreatureConfig.lua`'s `NATURE_IMMUNE_MOB_NAMES` table, since this check
runs at config-selection time, with no live target on screen to decode a
pixel-based `IsTargetNatureImmune` off of. The two lists MUST stay in sync —
same class of coupling as the `WowZone` enum/`ZONE_NAME_TO_ID` split above.
Only `NATURE_IMMUNE_MOB_NAMES` is mirrored so far; mirror more of
`CreatureConfig.lua`'s lists only once something on the C# side actually
needs them. An empty/unset `ExpectedMobNames` makes
`AllMobsInZoneAreNatureImmune()` return `false` rather than vacuously `true`.

**Automatic farming-config resolution:** there's no static "current config"
singleton anymore -- `WowFarmingConfigs.cs`/`CURRENT_CONFIG` were removed,
and so was the `WowFarmingConfiguration` wrapper that replaced it: `WowPlayer`
holds `LocationConfiguration`, `CombatConfiguration` and `ScreenConfiguration`
as three plain properties of its own (there's no `ManagementConfiguration`
anymore either — see the `Config/` bullet in the C# architecture section
above). `ScreenConfiguration` is picked from the monitor resolution
(`WowScreenConfigs.GetForPrimaryScreen()`) or passed into `WowPlayer`'s
constructor. `LocationConfiguration` and `CombatConfiguration` are never
hardcoded; both start out `null`/`WowCombatConfiguration.Unknown` (see
`WowPlayer`'s constructor) and get resolved by two independent
mechanisms in
`WowConfigResolutionTasks.cs`, deliberately split apart (they used to be one
method run only from `PlayerState.RESOLVE_FARMING_CONFIGURATION`) because
that state can be skipped entirely — see below:
- `WowPlayer.ResolveCombatConfiguration()` sets `CombatConfiguration` straight
  from `WowWorldState.PlayerClass` (see the `MultiBoolOne` B-byte bits 2-4
  note below) and builds the matching `ClassState` subtype. It's called every
  tick from `WowManagementTasks.EveryWorldStateUpdateTasks()` — **not** tied
  to `PlayerState.RESOLVE_FARMING_CONFIGURATION` — and early-outs quietly
  (no logging) once `ClassState` is already set, or for as many ticks as
  `WorldState.PlayerClass` is still `null` (addon not rendering a real row
  yet, e.g. still on the login screen). It needed to be tick-driven rather
  than a one-time startup step because `CoreGameplayLoopTask`'s own "already
  in combat" short-circuit (top of its `while` loop) can jump straight to
  `PlayerState.IN_CORE_COMBAT_LOOP` on literally the first tick — e.g. the
  bot was (re)started while the character was already mid-fight — bypassing
  `RESOLVE_FARMING_CONFIGURATION` for the rest of the run. Before this split,
  that meant the combat loop could try to dispatch on a still-`Unknown`
  `CombatConfiguration` with a still-null `ClassState`.
- `WowPlayer.ResolveFarmingConfigurationTask()` picks `LocationConfiguration`
  from `WowLocationConfigs.ALL_LOCATIONS` — an explicit list (deliberately
  not reflection over the class's static fields, so a route can be pulled
  out of auto-selection without deleting it) — by filtering to configs the
  player currently satisfies: the same three checks `SetLogoutVariablesTask()`
  uses to keep validating an already-running route (level, zone, waypoint
  proximity via `WowPathfinding.GetDistanceToClosestWaypoint`/
  `WowPlayerConstants.MAX_DISTANCE_FROM_ROUTE_WAYPOINT`), just run once up
  front instead of every tick. Exactly one match wins; zero or more than one
  means there's no safe automatic choice. This one *is* still tied to
  `PlayerState.RESOLVE_FARMING_CONFIGURATION`, right after the window is
  focused — meaning it's one of the states the "already in combat"
  short-circuit above can skip, so `LocationConfiguration` can end up staying
  `null` for an entire run that started mid-combat. Nothing currently
  re-resolves it if that happens; code that reads `LocationConfiguration`
  either runs somewhere that short-circuit can't reach mid-combat, or (like
  the level-up-alert block in `EveryWorldStateUpdateTasks()`) guards on
  `LocationConfiguration != null` first. `SetLogoutVariablesTask()`
  does not guard, so it would throw if reached in that state — a known gap,
  not yet fixed.

On failure, `ResolveFarmingConfigurationTask()` checks `ClassState == null`
first (i.e. `ResolveCombatConfiguration()` never managed to resolve it) before
doing its own location matching — since the two are independent now, this is
the one place that still needs to fail the whole startup if combat config
truly never resolved. Either failure sends a Slack alert via
`SlackHelper.SendMessageToChannel()`, returns `false`, and `CoreGameplayLoopTask`
sends the state machine straight to `EXITING_CORE_GAMEPLAY_LOOP` (which exits
the process) rather than guessing. `WowPlayer.ClassState` starts `null`
(`UpdateWorldStateAsync`/`UpdateWorldState`/`UpdateFromBitmap` all null-guard
the per-tick `ClassState.UpdateFromBitmap` call) until `ResolveCombatConfiguration()`
builds the concrete subtype; nothing reads `ClassState` before that.

**`MultiBoolOne`'s B boxes (17-24):** B1 carries `TargetRecentlyEvaded` — true for a
few seconds after the player's own attack drew an `EVADE` combat-log miss
against the current target (i.e. the target is stuck evading, e.g. leashed on
the far side of terrain it can't path across). Detected via
`COMBAT_LOG_EVENT_UNFILTERED` in `YoyokazooUI.lua` (`HasRecentTargetEvade()`,
same sticky-timestamp pattern as `HasUnseenWhisper()`), latched for
`EVADE_WINDOW_SECONDS` (3s) rather than requiring the bot's poll to land on
the exact tick the miss fired. Consumed by `MeleeMakeSureWeAreAttackingEnemyTask`
(`WowCommonCombatTasks.cs`) as part of its "this target is stuck, back off/clear
it" checks.

B2 and B4 carry two of the three bot-supported classes the player might be
playing — `IsPlayerWarrior()`/`IsPlayerShaman()` (`WoWFunctions.lua`), plain
`UnitClass("player")` checks. (B3 is `RESERVED` — previously Mage, removed
along with Mage support; not reused, to avoid confusing anything that expects
the old meaning.) The 3rd class, Warlock, didn't fit here — this row was
already full by the time Warlock support was added — so its bit
(`IsPlayerWarlock()`) lives in `MultiBoolTwo`'s R4 instead (see below);
`WowWorldState.UpdateMultiBoolTwo` overrides `PlayerClass` to `Warlock` there
rather than duplicating the "exactly one true" logic across two rows. C#
decodes these into `WowWorldState.PlayerClass` (nullable
`WowCombatConfiguration` — null if none of the four bits are set, i.e. an
unsupported class or the addon isn't rendering a real row yet), which
`WowPlayer.ResolveFarmingConfigurationTask` uses to set
`CombatConfiguration` automatically at startup — see "Automatic
farming-config resolution" below.

B5-B8 carry `IsPlayerPoisoned`/`IsPlayerDiseased` (from `PlayerHasDebuffType()`
in `WoWFunctions.lua`, keyed off `UnitDebuff("player", i)`'s dispel-type return
value) and `IsTargetNatureImmune` (name-based, per `NATURE_IMMUNE_MOB_NAMES` in
`CreatureConfig.lua` — same pattern as `IsTargetFireImmune`)/`IsTargetCasting`
(`UnitCastingInfo`/`UnitChannelInfo` against `"target"`, the target-side
counterpart to the already-existing `IsPlayerCasting`, which packs into
`MultiBoolOne`'s G3 as `WowWorldState.IsCurrentlyCasting`) — this fills
the row.

**Rows only grow when a field needs them.** There's no class-specific
numeric row (the old reserved `ClassIntOne` was dropped, never having been
used) — add a row when something actually needs one. `MultiBoolTwo` and
`ClassBoolTwo` each got a row this way:
`MultiBoolTwo`'s R1 is `IsTargetLongRangeCaster` (per-mob, from
`LONG_RANGE_CASTER_MOB_NAMES` in `CreatureConfig.lua` — mobs whose ranged
attack outranges Earth Shock), bit 2 is `WowWorldState.LogoffMobSeen` (from
`LOGOFF_IF_SEEN_MOB_NAMES` in `CreatureConfig.lua` — mobs dangerous/
undesirable enough that just spotting one anywhere nearby, not necessarily
targeted, should trigger an immediate logout; unlike the other name-based
lists' `IsTargetXxx()` checks, `IsLogoffMobSeen()` in `WoWFunctions.lua`
checks both the current target AND scans all `nameplateN` unit tokens (same
iteration `CountAttackers()` uses) rather than just `"target"` alone — the
target check catches a mob targeted beyond nameplate range, which the
nameplate scan alone would miss — since the whole point is to bail before
ever engaging it — wired into
`WowManagementTasks.EveryWorldStateUpdateTasks()`, checked every tick
regardless of player state, same as the level-up check there), and R3 is
`WowWorldState.IsCurrentlySkinning` (name-matched against the player's
current cast, `UnitCastingInfo("player") == "Skinning"` — Skinning is a
regular cast-bar action, not a channel, and isn't cast via a normal
spellbook ID the way e.g. `CanCurePoison`'s `IsSpellKnownByName()` match is);
and R4 is the 3rd "which supported class" bit, Warlock (see the `MultiBoolOne`
B-box note above for why it landed here instead of there). R5 is
`WowWorldState.IsTargetBleedImmune` (per-mob, from `BLEED_IMMUNE_MOB_NAMES`
in `CreatureConfig.lua` — mobs not worth (re)applying Rend to) and R6 is
`IsTargetFearCaster` (per-mob, from `FEAR_CASTER_MOB_NAMES` in
`CreatureConfig.lua` — mobs worth opening with Berserker Rage against rather
than reacting after the fact); both live here rather than in
`ClassBoolOne`/Warrior's own slice because they're mob-identity facts, the
same class as `IsTargetLongRangeCaster` above, even though only Warrior
consumes either today (`WowWarriorTasks.cs`'s `WarriorCombatLoopTask` reads
`WorldState.IsTargetBleedImmune` when deciding whether to Rend, and
`WorldState.IsTargetFearCaster` when deciding whether to preemptively pop
Berserker Rage) — this replaced an earlier, route-level `UseRend`/
`PreemptFear` pair of booleans on `WowLocationConfiguration` that couldn't
express "some mobs at this route bleed-immune/fear-cast, some don't." R7-8
are red error toast flags (see "Red error toasts" below).

G1 is `WowWorldState.LogoutOnLowDynamiteEnabled` and G2 is
`WowWorldState.LogoutOnFullBagsEnabled` — unlike every other bit in this row,
these two aren't live game-state queries at all. They're run-specific
settings toggled in-game via the addon's `/yyconfig` menu
(`CreateSettingsMenu()` in `UIFunctions.lua`, wired up in `YoyokazooUI.lua`),
saved into `YoyokazooUIDB.logoutOnLowDynamite`/`logoutOnFullBags`
(`YoyokazooUIDB` is the addon's `SavedVariablesPerCharacter` table — see
`YoyokazooUI.toc`), and read by `IsLogoutOnLowDynamiteEnabled()`/
`IsLogoutOnFullBagsEnabled()` (both in `MULTI_BOOL_TWO_FIELDS`). This
replaced hardcoding the equivalent `LogoutOnLowDynamite`/`LogoutOnFullBags`
booleans on the C# side's `WowManagementConfiguration` — see
`WowManagementTasks.SetLogoutVariablesTask()`, the only reader of
`WowWorldState.LogoutOnLowDynamiteEnabled`/`LogoutOnFullBagsEnabled`.
The `/yyconfig` menu also has a "Dynamite item" row, letting which
dynamite-tier item `AreWeLowOnDynamite()` (`WoWFunctions.lua`) checks the bag
count of be picked from `DYNAMITE_ITEM_CHOICES` at runtime instead of being
hardcoded — saved into `YoyokazooUIDB.dynamiteItemId`, read via
`GetDynamiteItemId()`. Unlike the two booleans above, this one is Lua-only
and never reaches the C# side. This is `CreateSettingsMenu()`'s second option
shape (`type = "selector"`, a cycling button through `choices`) alongside its
original checkboxes — see that function's own comment in `UIFunctions.lua`
for the option-table shapes it accepts. The menu has a matching "Healing
potion" row, same selector shape, letting which healing-potion-tier item
`AreWeLowOnHealthPotions()` (`WoWFunctions.lua`) checks the bag count of be
picked from `HEALING_POTION_ITEM_CHOICES` at runtime instead of being
auto-selected — saved into `YoyokazooUIDB.healingPotionItemId`, read via
`GetHealingPotionItemId()`; also Lua-only, same as the Dynamite item
selector. This replaced an earlier version of `AreWeLowOnHealthPotions()`
that auto-picked the highest tier whose min level was <= the player's
current level, which meant a full stack of a lower (still-owned, but no
longer being drunk) tier could mask actually being low on the tier the
player was currently using.

G3 is `WowWorldState.HasDesiredWorldBuff` — same "run-specific setting, not
a plain game-state query" deal as G1/G2: which world buff it checks for
(Ony's Rallying Cry, Rend's Warchief's Blessing, or ZG's Spirit of Zandalar)
is chosen via the `/yyconfig` menu's "Desired world buff" row, another
`type = "selector"` entry cycling through `WORLD_BUFF_CHOICES`
(`WoWFunctions.lua`) — saved into `YoyokazooUIDB.desiredWorldBuffId`, read via
`GetDesiredWorldBuffId()`, and matched against the player's actual buffs by
`HasDesiredWorldBuff()` (`WoWFunctions.lua`, via the existing `HasBuffNamed()`
helper). Unlike the "Dynamite item"/"Healing potion" selectors, this one
*does* reach the C# side — it's in `MULTI_BOOL_TWO_FIELDS` like the two
booleans above.
Consumed by `WowManagementTasks.WaitForWorldBuffThenLogoffTask()`: loops
idle (tapping strafe-left/strafe-right every
`WowPlayerConstants.WORLD_BUFF_WAIT_MILLIS` to dodge WoW's AFK kick) until
this bit comes true, then Slack-alerts and logs out. Wired up to the
`AdHocTest` button (`WowPlayer.AdHocTestTask()`) for now rather than a
dedicated `PlayerState`.

G4 is `WowWorldState.HighLatency` — unlike G1-G3 above, this is back to being
a plain live game-state query (same category as the R flags), not a
`/yyconfig` setting. `HasHighLatency()` (`WoWFunctions.lua`)
samples `GetNetStats()`'s `latencyHome` (the player's own connection to their
realm's datacenter, not `latencyWorld`'s Blizzard-internal server-hop
latency) once every `LATENCY_CHECK_INTERVAL_SECONDS` (1s) rather than on the
row's ~50ms redraw cadence, since latency doesn't change anywhere near that
fast — sampling on the redraw cadence would just recount the same stale
reading. It counts consecutive over-threshold samples
(`LATENCY_HIGH_THRESHOLD_MS`, 300) and returns true once
`LATENCY_HIGH_CYCLE_COUNT` (10) in a row have all been high — 10 cycles * 1s
= 10 sustained seconds — resetting to 0 (and back to false) the moment a
sample comes in under threshold, since nothing needs it to stay latched once
seen: `WowManagementTasks.EveryWorldStateUpdateTasks()` reads it with the
same one-shot `WorldState.HighLatency && !LogoutTriggered` pattern
`LogoffMobSeen` uses above (Slack-alerts and sets `LogoutTriggered`/
`LogoutReason` on the first tick it sees it true).

G5 is `WowWorldState.CombatStalemate` — another live game-state query:
"in combat, but nothing is actually happening." `IsCombatStalemate()`
(`YoyokazooUI.lua`, next to the EVADE tracking it shares a
`COMBAT_LOG_EVENT_UNFILTERED` handler with) stamps
`lastCombatActivityTime` on `PLAYER_REGEN_DISABLED` (entering combat — so
every fight starts its own clock; also seeded on `PLAYER_ENTERING_WORLD` if
already in combat, for a `/reload` while stuck) and on any `*_DAMAGE`/
`*_MISSED` combat-log subevent whose source or dest is the player or their
pet — **except EVADE misses**, which are exactly what a stuck mob produces
when swung at, so counting them would mask the case this exists for. True
once that stamp is older than `COMBAT_STALEMATE_SECONDS` (30) while
`UnitAffectingCombat("player")` is still true. The motivating case: aggroed
by a mob that can't path to the player (player in water, mob stuck on the
shore) — combat never drops, nothing ever hits, and the class combat loops
(`while (WorldState.IsInCombat)`) spin forever. Because of that, the C#
consumer in `EveryWorldStateUpdateTasks()` can't just set `LogoutTriggered`
like `LogoffMobSeen`/`HighLatency` do (the logout states are only reachable
once combat drops) — it sets it, Slack-alerts, presses the logout macro
itself via `StartLogoutTask()`, and then polls for `!AddonLoaded` right
there (up to `WowPlayerConstants.COMBAT_STALEMATE_LOGOUT_WAIT_MILLIS`, 45s)
before `Environment.Exit`, rather than returning to the combat loop — whose
scoot-backwards/re-face handling would move the character and cancel the
logout timer. Logging out in combat is fine here precisely because it's a
stalemate: nothing's hitting us, so the timer runs uninterrupted. If the
login screen never shows up (the mob reached us after all), it Slack-alerts
and falls through; `LogoutTriggered` stays set, so the normal
`CHECK_FOR_LOGOUT` path finishes the job once combat drops.
`COMBAT_STALEMATE_DEBUG` (a local in
`YoyokazooUI.lua`) prints every counted activity event, for checking
in-game which subevents actually fire during a stalemate.

G6 is `WowWorldState.AllSkillsKnownForThisLevel` and G7 is
`CanAffordToTrainAllSkills` (both `WoWFunctions.lua`) — inputs for the
leveling-goal logic built on `WowLevelingConfigs.cs` (see the `Config/`
bullet below). There's no addon API for "what can I train" without a trainer
window open, so both read hand-maintained per-class tables
(`WARRIOR_TRAINER_SPELLS`/`SHAMAN_TRAINER_SPELLS`/`WARLOCK_TRAINER_SPELLS`,
one per class file, entries `{ level, name, spellId, cost }`): any entry
with `level <= UnitLevel("player")` whose rank-specific `spellId` isn't
known (`IsPlayerSpell`) is unlearned; G7 compares their summed copper cost
against `GetMoney()` (base cost, ignoring trainer rep discounts; true when
nothing's unlearned). An entry may also carry `requiresSpellId`: it only
counts once the player knows that spell — for later ranks of talent- or
quest-granted abilities (Mortal Strike/Bloodthirst/Shield Slam/Intercept),
which the trainer only offers if rank 1 is already known. Warrior is filled
in for levels 1-60, generated from a `/yyspells` dump of a WoW Forever
trainer (real per-row levels/costs/spell IDs — Forever's schedule differs
from Classic Era's; starting/quest spells, talent rank 1s, and weapon skills
aren't on the trainer, so they're excluded);
Shaman/Warlock tables are empty, so they always read "all known".
`/yytrain` (`PrintTrainerSpellDebug()`) prints the cached G6/G7 values next
to a fresh uncached scan, listing each unlearned entry with its
`IsPlayerSpell`/`IsSpellKnown` results.
G8 is `WowWorldState.GearNeedsRepair` (`GearNeedsRepair()`,
`WoWFunctions.lua`): true if a melee weapon (main hand, or an off-hand
*weapon*) or a shield is at "yellow" durability or worse
(`DURABILITY_YELLOW_RATIO`, 25% — our approximation of the durability doll's
yellow, not exposed by the API), or any other equipped piece (armor,
ranged) is "red" (0 durability/broken). `GEAR_REPAIR_DEBUG` prints per-slot
durability on each refresh. Nothing on the C# side consumes it yet.

B1 is `WowWorldState.IsTargetAttackable` — `ShouldWeAttackTarget()`
(`WoWFunctions.lua`) on its own: the target exists, is attackable,
alive, not PvP-flagged, doesn't con grey, isn't too far above the player's
level, and isn't already in combat. It is pure eligibility. The per-class
`CanChargeTarget`/`CanShootTarget`/`CanSpellcastPullTarget` bits (and so
`CanEngageTarget()`) AND this same check with range/cooldown, so on their
own they can't tell "valid but out of range" from "not a valid target";
this bit can. Consumed by `PathfindingLoopTask`'s out-of-range chase (see
the target-marker bullet in the C# architecture section).

**Red error toasts** (boxes 7-8 and 18-21) are read from the
`UI_ERROR_MESSAGE` event in `YoyokazooUI.lua` instead of pixel-matching the
on-screen text. That used to be resolution-specific `*Positions` coordinates
in `WowScreenConfigs.cs`, which broke whenever the font or layout changed
(e.g. on the Forever client). The handler compares the event's `message`
against the client's own global string for each error and records a
timestamp; each flag then reads true for `UI_ERROR_FLAG_WINDOW_SECONDS`
(0.5s), since the event fires once but the bot polls on its own cadence.
Box → global string → `WowWorldState` bool: 7 `ERR_BADATTACKFACING` →
`FacingWrongWay`; 8 `SPELL_FAILED_UNIT_NOT_INFRONT` →
`TargetNeedsToBeInFront`; 18 `ERR_GENERIC_NO_TARGET` → `InvalidTarget`;
19 `ERR_OUT_OF_RANGE` → `OutOfRange`; 20 `SPELL_FAILED_LINE_OF_SIGHT` →
`NotInLineOfSight`; 21 `SPELL_FAILED_NOPATH` → `NoPathAvailable` (all
confirmed live with a debug dump). There's no flag for "You are too far
away!" (`ERR_BADATTACKPOS`): it couldn't be triggered live, and nothing
consumed it. `UI_ERROR_MESSAGE_DEBUG` (a local in `YoyokazooUI.lua`, off by
default) prints every toast and which flag it set. Boxes 22-24 are still
free for the next class-agnostic bool.

**Per-route target macro:** the `"0 Targ"` macro (`TARGET_MACRO_NAME`) (`WowInput.FIND_TARGET_MACRO`) is the bot's target finder, and
its `/target` lines are set per route: `WowLocationConfiguration.TargetMacroMobNames`
(comma-separated partial names, e.g. `"Mottled,Scorpid"`) gets typed into
chat by `ResolveFarmingConfigurationTask()` as `/yytarget Mottled,Scorpid`
(via `WowInput.TypeChatLineTask`, which maps each char to a US-layout key —
add any new character to `GetKeyForChar`). The addon's `/yytarget`
(`YoyokazooUI.lua`) calls `SetTargetMacroMobs()`, which `EditMacro`s the
existing macro's body in place (keeping its name/icon/keybind) to
`/cleartarget`, `/stopmacro [mod:shift]`, then one `/target X` per name,
reads it back to verify, and chat-prints the result. It never creates the
macro, and refuses in combat or over 255 chars. A missing macro is
surfaced in-game only, not over the pixel row: `IsTargetMacroMissing()`
drives a red `UIErrorsFrame` toast plus a red chat line on `PLAYER_ENTERING_WORLD`, alongside the
enemy-nameplates-off alert. Every route sets `TargetMacroMobNames` except
`LEVEL_8_MULGORE_MIXED_BEASTS` (its list is still a TODO); a route
without it leaves the macro untouched.

**Rare-change state is interval-cached.** `AreBagsFull`,
`AreWeLowOnHealthPotions`/`Dynamite`/`Ammo`, `GearNeedsRepair`, and the
trainer-spell scan behind G6/G7 (`GetUnlearnedTrainerSpells`, shared so the
two always refresh together) are all wrapped in `CacheOnInterval()` (top of
`WoWFunctions.lua`): computed on the first call (startup), then at most once
every `RARE_STATE_REFRESH_INTERVAL_SECONDS` (60s), returning the cached value
in between rather than recomputing on the pixel row's ~50ms cadence. So any
of these can lag reality by up to a minute (e.g. after a level-up, trainer
visit, vendor sell, or `/yyconfig` item-selector change). Put the next flag
of this kind behind the same helper.

`ClassBoolTwo`'s box 1 is Shaman's
`IsInEarthShockRange` (a pure range check via `SpellIsInRange(8042)`,
independent of `CanCastEarthShock`'s cooldown/usability check), box 2 is
Shaman's `HasClearcasting` (Elemental Focus's proc buff, name-matched via
`HasBuffNamed("Clearcasting")`), and box 3 is Shaman's `CanCastFrostShock`
(cooldown/usability check via `SpellIsCooledDown(8056)`/`IsSpellUsable(8056)`,
same pattern as `CanCastEarthShock`). Frost Shock does the same damage as
Earth Shock but never interrupts a cast, so `WowShamanTasks.cs`'s
`ShamanShouldCastFrostShock()` only uses it as Earth Shock's substitute
against nature-immune targets (`WorldState.IsTargetNatureImmune`) — Earth
Shock is nature damage and does nothing to them. Shocks share both a
cooldown category and a 20-yard range in Classic, so box 1
(`IsInEarthShockRange`) doubles as the range check for Frost Shock too — no
separate range bit needed. Boxes 4-24 are still free for the next
Shaman-specific flag.

**Adding a new field:** for a bool, add its function's name to the right
field list (in a free/`RESERVED` slot, or appended) AND decode the same box
number on the C# side (`WowWorldState.UpdateMultiBool*` or the class's
`Wow*ClassState.UpdateFromBitmap`). For a whole new row, add a
`CreateBitRowElement`/`CreateStatusBarElement` call in `InitializePixelRow()`
AND a `PixelRow` enum value in `WowPixelRows.cs`, at the same row index (and
grow `FarthestReadPixel`/the Lua `PIXEL_ROW_COUNT` if it's the new last row).
Do both sides together — a one-sided change silently breaks the bot (wrong
box read as wrong flag), it won't fail loudly.

**Class split:** class-specific flags (Battle Shout, Rend, Frost Armor,
Rockbiter, etc.) live only in the Class rows, never in `MultiBoolOne/Two`.
`GetPlayerClassFields()` (`WoWFunctions.lua`) picks the player's class's list
by name — `WARRIOR_CLASS_BOOL_ONE_FIELDS` in `WarriorFunctions.lua`,
`SHAMAN_...` in `ShamanFunctions.lua`, `WARLOCK_...` in
`WarlockFunctions.lua` (an unsupported class gets an empty, all-red row) — so
the *same* box means something different depending on which class is
playing. On the C# side, `WowPlayer.ClassState` (a
`WowClassState` subtype — see the C# architecture section) decodes the
matching bits, selected once from `CombatConfiguration` —
itself auto-set at startup from the player's detected class (see "Automatic
farming-config resolution" below), not hardcoded. Note
`CanSpellcastPullTarget()` stays in `WoWFunctions.lua` rather than being
split into the class files — it's shared between Shaman and Warlock under
the same name, and duplicating that name into multiple class files would
collide (last-loaded file wins silently, since addon globals are one flat
namespace). The C#-side mirror of that same constraint: `CanEngageTarget()`
in `WowPlayerCombatConfig.cs` is a thin class-dispatching wrapper for
class-agnostic callers (e.g. `WowMovementTasks.PathfindingLoopTask`), while
`WarriorCanEngageTarget`/`ShamanCanEngageTarget`/`WarlockCanEngageTarget`
(one per `Wow*Tasks.cs`) hold the real per-class
logic and take that class's typed `ClassState` directly. **Warlock is a
work in progress**, filled in incrementally the same way Shaman was — so far
`WowWarlockClassState` decodes ClassBoolOne's R1 (`CanSpellcastPullTarget`,
Shadow Bolt), R2 (`ShouldCastDemonArmor`, true when neither Demon Skin nor
Demon Armor is active — only one of those two differently-named buffs is
ever up at a time, so `ShouldCastDemonArmor()` in `WarlockFunctions.lua`
checks for either), R3 (`ShouldSummonPet`, true once the player knows at
least Summon Imp -- the level-1 pet spell, gating whether a pet can be
summoned at all yet -- and doesn't currently have a living pet out), and R4/R5
(`ShouldCastImmolate`/`ShouldCastCorruption`, true when the player knows that
spell and the target doesn't already have that DoT on it -- same
name-matched-against-`TargetHasDebuffSpellName()` pattern Shaman's
`ShouldCastFlameShock`/`TargetHasFlameShock` use); the rest of
`ClassBoolOne` and all of `ClassBoolTwo` are still free. The rest of the dispatch wiring (enum
value, `WowClassState.Create`, all six
`WowPlayerCombatConfig.cs` switches, the `MultiBoolTwo` R4 class-detect bit)
is also in place.

The `Screen.PrimaryScreen.Bounds`-based per-resolution lookup
(`WowScreenConfigs.GetForPrimaryScreen()`) still selects a `WowScreenConfiguration`, but that
now only matters for the screen-capture crop size and the text/UI-signature
matchers above — the pixel-row positions themselves are the same on every
resolution.

The C# side copies the Lua addon files into the live WoW `AddOns` folder on
startup (`Form1.CopyLuaAddonToWoW`), so the addon in this repo is the source
of truth — edits should be made here, not in the WoW install directory.

## C# bot architecture (`WoWHelper/Code/`)

- **`Gameplay/WowPlayer.cs`** — the core async state-machine loop
  (`CoreGameplayLoopTask`), driven by `PlayerState` enum (`WowPlayerStates.cs`):
  focus window → auto-resolve combat/location config (`Gameplay/
  WowConfigResolutionTasks.cs`, see "Automatic farming-config resolution"
  above) → check logout conditions → recover to "battle ready" → find/
  engage a target via pathfinding → run the combat loop → loot/skin → repeat.
  Related sub-state-machines: `PathfindingState` (waypoint navigation) and
  `TradeState` (used by the "Cupid" trade-gifting loop).
- **`Gameplay/WowWorldState.cs`** — decodes one screen capture into all
  *class-agnostic* bot inputs (see color contract above); `WowPlayer` keeps a
  `PreviousWorldState` + `WorldState` pair each tick to detect edge-triggered
  events (leveled up, logged out unexpectedly, new whisper, etc.).
- **`Gameplay/WowPixelRows.cs`** — the pixel-row layout (`PixelRow` enum,
  layout constants, `ADDON_LOADED_COLOR`) and the low-level readers
  (`ReadAddonLoaded`, `ReadStatusBarPercent`, `ReadBits` → `PixelRowBits`)
  that `WowWorldState` and the `Wow*ClassState`s decode through — the C#
  half of the color-encoding contract above, in one place.
- **`Gameplay/WowClassState.cs`** (abstract) / **`WowWarriorClassState.cs`** /
  **`WowShamanClassState.cs`** /
  **`WowWarlockClassState.cs`** (stub — see "Class split" above) — the
  class-specific counterpart to `WowWorldState`. `ClassBool`/`ClassInt` pixels
  mean something different per class, so rather than one flat object with
  every class's fields (where nothing would stop e.g. Shaman code from reading
  a Warrior-only field and silently getting stale data), each class gets its
  own concrete subtype exposing *only* its own fields — a wrong-class field
  reference is a compile error, not a runtime surprise. `WowPlayer.ClassState`
  holds the one built for `CombatConfiguration` — `null` until
  `ResolveCombatConfiguration()` sets that from the player's detected class
  and builds the matching subtype (see "Automatic farming-config resolution"
  above) — and updates it (in place, same instance — no `PreviousClassState`
  exists, nothing has needed one yet) every tick alongside `WorldState`, off
  the same captured bitmap.
- **`Gameplay/Wow*Tasks.cs`** — behavior/task implementations grouped by
  concern: `WowMovementTasks` (pathfinding/turning/strafing/jumping),
  `WowCommonCombatTasks` (shared combat logic), `WowWarriorTasks` /
  `WowShamanTasks` / `WowWarlockTasks` (class-specific
  rotations, selected via `WowCombatConfiguration` — `WowWarlockTasks` is
  currently a stub, every entry point throws `NotImplementedException`),
  `WowManagementTasks` (logout conditions, low
  supplies, trade window handling, Slack alerts, the `WaitForWorldBuffThenLogoffTask()`
  world-buff-waiting loop — see the `MultiBoolTwo` G3 note in the
  color-encoding contract above). The class-specific task
  files' entry points (dispatched from `WowPlayerCombatConfig.cs`) take their
  own class's `WowClassState` subtype as a **method parameter**, not read off
  `this` — so a Shaman-only field is unreachable from inside a Warrior method's
  scope, not just absent on some shared type. `WowPlayerCombatConfig.cs`
  casts `ClassState` to the right concrete type at each dispatch call site;
  if that cast ever fails, `ClassState` and `CombatConfiguration` have gone
  out of sync, and throwing immediately there is intentional — silently
  reading the wrong class's state would be worse.
- **Stuck detection** (`StuckRecoveryTask`, `WowMovementTasks.cs`) — its own
  small sub-state machine (`StuckRecoveryState`: `NOT_STUCK` → `JUMPED` →
  `WIGGLED_LEFT` → `WIGGLED_RIGHT` → `GAVE_UP`, one step per
  `WowPathfinding.STATIONARY_MILLIS_*` threshold), called at the top of the walk
  helpers (`WalkToWaypointTask`, `WalkToTargetMarkerTask`) rather than being a
  state in any goal's enum — so every walking state gets it, and it only runs
  while walking. "Stationary" = within `MEANINGFUL_LOCATION_CHANGE_AMOUNT` of the
  anchor point. If it goes `STUCK_CHECK_STALE_MILLIS` without being called (we
  weren't walking — combat, NPC interaction, etc.) the next call starts a fresh
  episode, so intentional standing-still never needs special-casing. Giving up
  just sets `LogoutTriggered`; `UpdatePlayerGoalTask` picks `LOG_OUT` next tick.
  This replaces the loop-local stuck detection in the old (commented-out)
  `PathfindingLoopTask`.
- **`Gameplay/WowPathfinding.cs`** — pure-math helpers for waypoint following
  (facing/turn-direction math, angle tolerance that tightens near a waypoint,
  lateral-distance-from-path calc). No side effects, unit-testable.
- **`Gameplay/WowScreenCapture.cs`** — stateless static screen-scraping helpers
  that capture beyond `WowWorldState`'s small per-tick pixel-row crop:
  `FindTargetMarkerOnScreen` (full-screen target-marker search, see below) and
  `CreateHeatmapForLooting` (frame-diffs the loot region and returns the point
  to click — `WowPlayer` stores it into `LootX`/`LootY`). Each takes the
  `WowScreenConfiguration` it needs rather than reading `WowPlayer` state.
- **Target-marker screen tracking** (`WowScreenCapture.FindTargetMarkerOnScreen`,
  `WowMovementTasks.cs`) — there's no addon-legal way to read a target's
  actual position/bearing/distance in this client (`UnitPosition`,
  `C_Map.GetPlayerMapPosition`, and nameplate frame measurement are all
  confirmed blocked), so `UIFunctions.lua` paints a sentinel-colored marker
  (`WowScreenConfiguration.TARGET_MARKER_COLOR`; `NAMEPLATE_MARKER_SIZE_PIXELS`
  = 20x20 real screen pixels, converted via `GetPhysicalPixelsPerLocalUnit()`
  like the pixel row so it's the same size on every client/UI scale) onto the
  current target's nameplate, found via a full-screen pixel search (`FindTargetMarkerOnScreen`
  — a separate, full-resolution capture, not the small fixed-position pixel
  row `WorldState` normally reads). With the bot run camera-pitched straight
  down, the marker's position relative to screen center IS bearing/distance:
  `GetTargetMarkerBearingDegrees`/`TurnToFaceTargetMarkerTask`
  (`WowMovementTasks.cs`) turn to face it (bearing math itself lives in the
  pure `GetBearingDegreesFromMarkerPosition(Point)`, so it can run against an
  already-known marker position without a second full-screen scan), and
  needs no per-resolution calibration (an earlier version,
  `WowScreenConfiguration.DistanceFromTarget`, linearly interpolated the
  marker's Y coordinate between a measured near/far pixel pair and was
  removed): `|bearing| <= 90` means the marker is above screen center (in
  front of the player), `|bearing| > 90` means below (behind), and its
  shrinking magnitude while walking forward is the "getting closer" signal.
  `WalkIntoMeleeRangeTask` (`WowMovementTasks.cs`) walks straight forward
  until `WorldState.IsInCombat` reads true (starting auto-attack is the only
  "we made it" signal trusted to actually stop the walk — an earlier
  `IsInMeleeRange` bool, decoded from a `CheckInteractDistance` check in the
  addon, didn't reliably reflect real melee range and was removed; the
  `MultiBoolOne` box it used, G2, is `RESERVED` in `MULTI_BOOL_ONE_FIELDS`
  in `WoWFunctions.lua`), caching the last-found marker
  position in `WowPlayer.MostRecentTargetMarkerX`/`MostRecentTargetMarkerY`.
  It reuses
  the marker scan it already does each iteration (for the bearing) for two
  drift checks, coarsest first: if the marker has flipped from in-front to
  behind since the last scan — at close range a small step forward swings the
  marker's angle around us fast enough that a single scan gap can jump
  straight over the cone check below — it stops walking, re-faces dead-on via
  `TurnToFaceTargetMarkerTask`, and resumes, rather than continuing to walk
  forward on a heading that's now backwards; otherwise, if it's merely
  drifted outside the finer `TARGET_FACING_CONE_DEGREES` cone (ordinary
  gradual drift), it re-turns via `TurnToFaceTargetMarkerTask` without
  interrupting the walk. None of this is tuned against live testing yet.
  `PathfindingLoopTask` also uses the marker while walking the waypoint route:
  once every `PATHFINDING_TARGET_MARKER_SCAN_INTERVAL_MILLIS` (1s — a full
  screen capture, so deliberately slower than the 200ms cadence above) it
  checks whether the marker is on screen while `CanEngageTarget()` is false
  but `WorldState.IsTargetAttackable` is true (i.e. we have a valid target,
  it's just out of range of our pull), and if so calls
  `WalkTowardsTargetMarkerTask`: stop route-walking, `TurnToFaceTargetMarkerTask`,
  then walk straight forward for at most `WALK_TOWARDS_TARGET_MARKER_MAX_MILLIS`
  (4s) or until `CanEngageTarget() || IsInCombat || LogoutTriggered` — the same
  early-exit the pathfinding loop itself uses, re-checked by the caller right
  after — or until `IsTargetAttackable` drops (e.g. someone else pulled it),
  which just hands back to route-walking. Unlike `WalkIntoMeleeRangeTask` it's a short nudge, not a commit; if
  the target's still out of range the next periodic scan just tries again.
  The addon never paints the marker on a target tapped by someone outside the
  group (`UnitIsTapDenied`, checked in `ShouldMarkCurrentTarget()` on
  target/nameplate events plus a 0.1s `OnUpdate` re-check for tags that land
  after targeting), specifically so this chase can't run after someone else's
  kill — a tapped mob is also "not engageable," which is indistinguishable
  from "out of range" on the C# side. The marker is its own `UIParent`-parented
  frame merely *anchored* to the plate (so it draws on top), which means it
  does **not** auto-hide with its plate — `UIFunctions.lua` tracks the plate
  it's on (`currentMarkerPlate`) and hides it on `NAME_PLATE_UNIT_REMOVED`,
  and the 0.1s `OnUpdate` reconcile re-checks that the marker sits on the
  target's *current* shown plate (or nowhere). Without that, a target walking
  out of nameplate range left the marker frozen on screen at the plate's last
  position.
  Per-route opt-out: `WowLocationConfiguration.ChaseOutOfRangeTargets`
  (default `true`, set in the constructor) — `false` skips the scan and the
  chase entirely, for routes where straying off the waypoints risks getting
  hung up on geometry or wandering into something dangerous (currently
  `LEVEL_53_NORTH_FELWOOD` and `LEVEL_34_SHIMMERING_FLATS`).
- **`Config/`** — per-location farming routes/waypoints
  (`WowLocationConfigs.cs` — also holds `ALL_LOCATIONS`, the explicit list
  `ResolveFarmingConfigurationTask()` auto-selects from; see "Automatic
  farming-config resolution" above) and per-resolution screen pixel maps
  (`WowScreenConfigs.cs`). There's no separate farming-profile config file
  anymore either — `WowFarmingConfigs.cs`/`CURRENT_CONFIG` were removed;
  `WowPlayer` holds `LocationConfiguration`/`CombatConfiguration`/
  `ScreenConfiguration` as its own properties instead, with the first two
  resolved at runtime, not set on any static instance. The
  `WowCombatConfiguration` enum lives in `Config/Definitions/WowCombatConfiguration.cs`.
  There's no management/alert-toggle config anymore either —
  `WowManagementConfiguration`/`WowManagementConfigs.cs` were removed;
  `AlertOnPotionUsed`/`AlertOnFullBags`/`AlertOnUnreadWhisper` always fired
  true in the only profile that was ever used, so those Slack alerts
  (`WowCommonCombatTasks.UseHealingPotionTask`,
  `WowManagementTasks.AlertOnUnseenWhisper`/`SetLogoutVariablesTask`) now
  just always fire unconditionally. Logout-on-low-dynamite/logout-on-full-bags
  never lived there to begin with — those are run-specific, toggled live via
  the addon's `/yyconfig` menu instead — see the `MultiBoolTwo` G1/G2 note in
  the color-encoding contract above. `Config/Definitions/` holds the POCOs
  these configs are instances of. `WowLevelingConfigs.cs` (instances of
  `WowLevelingConfiguration`) is a per-level leveling plan: the
  `LocationConfigs` acceptable for gaining XP/money at that level (one entry
  spans every starting zone — the goal logic picks by matching each
  location's `Route.Zone` to `WorldState.CurrentZone`) and `TrainerConfigs`
  (stubbed as plain name strings for now), looked up via
  `WowLevelingConfigs.GetFor(class, level)`. `Class` defaults to `null`
  (any class); a class-specific entry is possible and `GetFor` prefers it
  over an any-class one at the same level, but class differences should be
  pushed into the addon where possible (e.g. the per-class trainer tables
  behind G6/G7). Only levels 1-2 exist so far (both any-class), and nothing
  reads it yet — the goal-setting logic (train vs. grind
  for money vs. grind for XP, off the `MultiBoolTwo` G6/G7 bits) is still to
  be written. Each `WowLocationConfiguration` carries a
  `Route` (`WowRoute`, `Config/Definitions/WowRoute.cs`): the route's
  `Name` (human-readable, includes the minimum level — it used to be
  `WowLocationConfiguration.Title`), `Waypoints`, `TraversalMethod` (`WowRoute.WaypointTraversalMethod`,
  `CIRCULAR`/`LINEAR` — defaults to `LINEAR`, so loop routes must set
  `CIRCULAR` explicitly), `DistanceTolerance`, `MinimumLevel`, and `Zone` (`WowZone` enum,
  still declared in `WowLocationConfiguration.cs`) — see the zone ID
  note in the color-encoding contract above for how `Zone` ties to
  `WowWorldState.CurrentZone`. Every route lives in
  `Config/Data/WowRoutes.cs`, named after its location config plus `_ROUTE`
  (e.g. `LEVEL_53_NORTH_FELWOOD_ROUTE` for `LEVEL_53_NORTH_FELWOOD`). Each
  location config also has `ExpectedMobNames` and the
  `AllMobsInZoneAreNatureImmune()` helper built on it; see "Expected mob
  roster" above. `Config/Definitions/CreatureConfig.cs` is the C# mirror of
  the Lua addon's `CreatureConfig.lua` name lists that `ExpectedMobNames`
  checks against. `WowLocationConfiguration.MerchantConfig`
  (`WowMerchantConfiguration`, `Config/Definitions/WowMerchantConfiguration.cs`)
  is an optional "sell run" detour — default `null`, most routes just log out
  on full bags instead (`SetLogoutVariablesTask()`'s
  `LogoutOnFullBagsEnabled && BagsAreFull` check, unaffected by this feature).
  It's a `Name` (human-readable label only — **not** fed into any macro, see
  below) plus a `List<Vector2> Waypoints`: `Waypoints[0]` MUST equal (within
  `WowPlayerConstants.MERCHANT_BRANCH_POINT_EPSILON`) one of the owning
  route's own `Route.Waypoints` — that's the point the bot branches off from —
  and `Waypoints[^1]` is the merchant's exact standing spot. Enforced by
  `WoWHelperUnitTests/Tests/Pathfinding Tests/MerchantConfigTests.cs` rather
  than at runtime, same as the mob-roster/nature-immunity checks above.
  **Branch-off**: `PathfindingLoopTask`'s `MOVING_TOWARDS_WAYPOINT` case
  (`WowMovementTasks.cs`) checks, every time it arrives at a route waypoint,
  whether that waypoint's position matches `MerchantConfig.Waypoints[0]`
  (position, not index — it doesn't matter which index in the route reaches
  that point, or from which traversal direction) and `WorldState.BagsAreFull`
  is true; if so it sets `WowPlayer.IsOnMerchantRun = true`. **Execution**:
  once `IsOnMerchantRun` is true, `PathfindingLoopTask`'s loop skips its own
  TAB/macro target-finding and periodic jump (guarded with
  `!IsOnMerchantRun`) and instead calls `MerchantRunStepTask()` every tick,
  which drives its own small state machine
  (`WowPlayerStates.MerchantRunPhase`: `WALKING_TO_MERCHANT` →
  `INTERACTING_WITH_MERCHANT` → `WAITING_FOR_AUTO_SELL` →
  `WALKING_BACK_TO_ROUTE`) — walking leg-by-leg through `MerchantConfig.Waypoints`
  via `MoveTowardsPointTask` (the same rotate/strafe/walk logic
  `MoveTowardsWaypointTask` uses for the main route, extracted to take an
  arbitrary point/tolerance instead of always reading
  `LocationConfiguration.Route.Waypoints[CurrentWaypointIndex]`), with
  every leg but the final one using `MERCHANT_INTERMEDIATE_WAYPOINT_TOLERANCE`
  and the final approach using the much tighter
  `MERCHANT_FINAL_WAYPOINT_TOLERANCE` — the final waypoint is exactly where
  `INTERACTING_WITH_MERCHANT` presses `WowInput.CTRL_TARGET_MERCHANT` (via
  `PressKeyWithControl` — this key/macro already existed and, per live
  testing, its `/stopmacro [mod:shift]`/`/stopmacro [nomod]` lines mean a
  ctrl-press already falls through to its `/target` line with no macro
  changes needed) and right-clicks screen center
  (`ScreenConfiguration.LootDefaultX/Y`, the same point loot
  corpses are clicked at) to open the vendor, so it only works if the bot
  actually stopped right on top of the merchant. `WAITING_FOR_AUTO_SELL`
  then waits `MERCHANT_AUTO_SELL_WAIT_MILLIS` (15s) via the existing
  `WaitUnlessInCombatTask` for the addon's own `MERCHANT_SHOW` auto-sell
  handler (see the Lua addon section below) to empty the bags, before
  `WALKING_BACK_TO_ROUTE` retraces the same waypoints to index 0 and clears
  `IsOnMerchantRun`. The final approach uses the exact same
  rotate-to-heading/strafe-to-lane logic every other waypoint uses (no
  merchant-specific tolerance branching in `MoveTowardsPointTask` itself),
  with one difference: every merchant-run leg (both directions) passes
  `useMouseRotation: true`, so the rotate-to-heading step turns via
  `RotateToDirectionTaskWithMouse` (right-click camera drag, far more
  accurate) instead of the `RotateToDirectionTaskWithKeyboard` turn normal
  route-walking still uses —
  an earlier version special-cased a "precision approach" below a tolerance
  threshold (rotate only past a large fixed heading error, then strafe-only
  fine aiming) to stop the bot circling the merchant's exact spot, but that
  was reverted in favor of the plain shared logic pending live testing
  against `RotateToDirectionTask`'s own rewritten single-turn-then-verify
  logic (see the "C# bot architecture" section) — if `MERCHANT_FINAL_WAYPOINT_TOLERANCE`
  (0.02, still much tighter than any real route's own `DistanceTolerance`)
  still can't be reliably reached with the shared logic, revisit a
  merchant-specific approach then. **Stuck detection**: `PathfindingLoopTask`'s existing
  jump → wiggle-left → wiggle-right → give-up-and-logout escalation (the same
  one normal route-walking relies on to get unstuck from terrain, e.g. a
  fence) is a plain per-tick block keyed only off `WorldState.MapX/MapY`, not
  the current waypoint/target, so it runs unconditionally every tick
  regardless of `IsOnMerchantRun` — a merchant run stuck on the same kind of
  obstacle gets the same escalation instead of spinning in place forever.
  It's explicitly *disabled* (and the stuck anchor/clock kept continuously
  reset instead) during `INTERACTING_WITH_MERCHANT`/`WAITING_FOR_AUTO_SELL`
  specifically, since standing still there is intentional — otherwise the
  escalation would eventually back off/strafe away from the vendor
  mid-interaction, or (since the clock isn't paused, just not escalated
  against) immediately misfire once `WALKING_BACK_TO_ROUTE` starts, having
  gone stale during the ~15s+ interaction. The reset (`ResetStuckDetection()`,
  a local function in `PathfindingLoopTask`) runs both before *and after*
  `MerchantRunStepTask()` on those phases — the after-reset is the one that
  matters, since `WAITING_FOR_AUTO_SELL`'s step itself blocks for the full
  15s wait and flips straight to `WALKING_BACK_TO_ROUTE`; with only the
  before-reset, the first walk-back tick saw a 15s-stale clock and fired the
  jump/wiggle escalation (and eventually the stuck logout) — confirmed live.
  **Combat interruption**: `IsOnMerchantRun`/
  `CurrentMerchantRunPhase`/`CurrentMerchantWaypointIndex` are plain
  `WowPlayer` fields (declared next to `CurrentWaypointIndex`/
  `WaypointTraversalDirection`), so the existing combat short-circuit in
  `CoreGameplayLoopTask` (which unconditionally jumps to
  `IN_CORE_COMBAT_LOOP` from any state, with no save/restore of
  `PlayerState` — see "Automatic farming-config resolution" above for the
  same one-way-jump behavior elsewhere) leaves them untouched; once combat
  resolves and the state machine works its way back to
  `CHECK_FOR_VALID_TARGET` → `PathfindingLoopTask()` (the same path normal
  route-walking already relies on to resume), the very next tick sees
  `IsOnMerchantRun` still true and picks the trip back up from whatever
  phase/waypoint index it was on — no new `PlayerState` values or
  `CoreGameplayLoopTask` switch changes needed. If combat interrupts mid-sell
  (rare — would mean a mob reached the player at the vendor) the bot still
  walks back once the wait ends; bags may remain full, but the branch-off
  check above will simply retry on the route's next lap.
  **Timeout**: branch-off stamps `WowPlayer.MerchantRunStartTime`; if
  `IsOnMerchantRun` is still true `WowPlayerConstants.MERCHANT_RUN_TIMEOUT_MILLIS`
  (5 min, wall-clock from branch-off, combat time included) later,
  `PathfindingLoopTask` sets `LogoutTriggered`/`LogoutReason` (naming the
  phase it got stuck in) and returns, same as the stuck-on-terrain give-up.
  The check sits at the top of the `IsOnMerchantRun` block, so it's only
  evaluated while pathfinding runs — a timeout that elapses mid-combat fires
  on the first pathfinding tick after combat ends.
- **`Constants/`** — `WowInput.cs` maps logical actions to keybinds/macros the
  bot presses (expects specific in-game keybinds/macros to be set up to match),
  `WowPlayerConstants.cs` / `WowGameplayConstants.cs` hold thresholds/timings.
- **`Shared/`** — `KeyPoller` (global ESC-to-stop hotkey + cleanup),
  `GeneralHelpers` (stateless `ChangeStateBasedOnTaskResult`/`CurrentTimeInsideDuration`),
  `BitmapDifferenceVisualizer` (loot-heatmap detection by diffing frames to
  find where a loot corpse/sparkle is), `PathSubdivision` (splits long waypoint
  legs into shorter hops), `TessaractSingleton` (shared Tesseract OCR engine,
  used to read text like trade-partner names), `SlackFileUploadWorkaround`
  (see below — **temporary hack, not permanent architecture**).
- Input is sent through the `InputManager` DLL (`DLLs/InputManager.dll`);
  screen capture/image-matching and Slack notifications come from the
  `WindowsGameAutomationTools` NuGet package (external, not in this repo, and
  actually the user's own package — `github.com/yoyokazoo/WindowsGameAutomationTools`,
  matching the addon name). OCR uses `Tesseract` (`tessdata/eng.traineddata`).
- **`Shared/SlackFileUploadWorkaround.cs` is a temporary hack, meant to be
  deleted.** Slack deprecated the `files.upload` endpoint (now returns
  `"method_deprecated"`); `SlackHelper.UploadFile`/`SendScreenshotToChannel`
  and the `SlackAPI` package they wrap both still call it — confirmed via
  reflection, neither has been updated to Slack's replacement 3-step flow
  (`files.getUploadURLExternal` → upload bytes → `files.completeUploadExternal`).
  This file reimplements just that flow directly against Slack's HTTP API, as
  a stopgap until it's fixed upstream in `WindowsGameAutomationTools` and this
  project bumps to that package version — at which point delete this file and
  point call sites back at `SlackHelper`. Don't build on top of this as if
  it's permanent, and don't be surprised two file-upload paths exist side by
  side for now.

## Lua addon (`WoWHelper/Lua Addon/`, `YoyokazooUI`)

- **`YoyokazooUI.toc`** — addon manifest/load order. Loads
  `MathFunctions.lua` → `CreatureConfig.lua` → `WoWFunctions.lua` →
  `WarriorFunctions.lua` → `ShamanFunctions.lua` →
  `WarlockFunctions.lua` → `UIFunctions.lua` → `YoyokazooUI.lua`. Load order doesn't actually matter
  for correctness here (everything is a plain global function/table,
  resolved at call time, and nothing calls any of these before
  `PLAYER_ENTERING_WORLD`, well after every file has finished loading) —
  this ordering is just for readability.
- **`CreatureConfig.lua`** — every name-based special-case creature list
  (`CASTER_MOB_NAMES`, `RUNNER_MOB_NAMES`, `FIRE_IMMUNE_MOB_NAMES`, and
  wherever the next one like it gets added — e.g. a nature-immune list) in
  one file, so there's a single place to go update them. Classic has no
  reliable creature-ID API exposed to addons, so these all key off
  `UnitName("target")`. Plain global tables (not `local`) so
  `WoWFunctions.lua`'s `IsTargetXxx()` checks can read them — addon globals
  are one flat namespace, same as everything else here.
- **`WoWFunctions.lua`** — class-agnostic game-state queries (in melee range,
  in combat, should-attack-target checks, spell cooldown/range checks incl.
  GCD-aware cooldown detection, attacker counting, etc.) — the "is X true"
  logic layer (for the WoW Forever / Midnight-rules port, `IsAnySecretValue()`
  guards values that may come back secret; also, the
  top of the file has `Compat_*` shims for old globals modern clients removed
  — `GetSpellInfo`/`GetSpellCooldown`/`IsSpellInRange`/`IsUsableSpell`/
  `IsCurrentSpell`/`GetItemInfo`/`GetItemInfoInstant`/`GetItemCount`/
  `EquipItemByName`/`IsSpellKnown`/`UnitBuff`/`UnitDebuff`/`UnitAura` —
  which use the global if present, else the `C_Spell`/`C_Item`/`C_UnitAuras`
  replacement converted back to the old return shape; call those, not the
  bare globals. The aura shims swallow the in-combat "auras cannot be
  accessed when secret" error and return nil, so on Midnight-rules clients
  every aura-based flag reads "not present" during combat. Confirmed live via
  `/yybuff [name]` (`DebugBuffRead()`, probes every alternate aura API by name
  and by Battle Shout's rank spell IDs): in combat the index scan throws
  "Auras cannot be accessed when secret while tainted by 'YoyokazooUI'" and
  the by-name/by-ID lookups return nil — no in-combat aura read works.
  `HasBuffNamedOrRemembered()` is the workaround for buffs whose lifetime is
  just their duration: when the read is blind (`SafeAuraQuery` sets
  `lastAuraQueryWasBlind`) it answers from the expiration time remembered from
  the last read that worked, so a pre-pull buff stays true through combat.
  A buff cast *mid-combat* is invisible to it, so the C# side also remembers
  its own key press: `WarriorShouldCastBattleShout` skips recasting in combat
  within `BATTLE_SHOUT_DURATION_MILLIS` of `BattleShoutPressTime` (this used
  to recast Battle Shout every tick in combat). Only `HasBattleShout()` uses
  it so far; Demon Skin/Armor would fit too, but charge-consumed buffs
  (Lightning Shield) and procs (Clearcasting) would read true after they're
  gone. `BUFF_READ_DEBUG` (`WoWFunctions.lua`, off by default)
  prints each distinct `SafeAuraQuery` outcome plus each `HasBattleShout()`
  flip. Cooldowns are
  also secret in combat there — `startTime`/`duration`/`modRate` are secret,
  but `isEnabled`/`isActive` stay readable, and `isActive` is confirmed live
  to track the cooldown/GCD — so when the timings are secret,
  `Compat_GetSpellCooldown` answers from `isActive`: inactive → "ready"
  (0, 0), active → a stand-in "on cooldown" shape (`GetTime()`, 1s) that makes
  every caller's remaining-time math positive (real remaining time is
  unknown). Every cooldown check (`SpellIsCooledDown`,
  `IsGlobalCooldownCooledDown`, etc.) keeps working in combat this way; the
  one imprecision is `SpellIsCooledDownIgnoringGCD`, which can read a spell on
  a real cooldown as "ready apart from the GCD" while the GCD is running. If
  `isActive` is ever unreadable it falls back to "ready".
  `SECRET_COOLDOWN_DEBUG` (off) prints once per spell which cooldown fields
  were secret. `/yycd [spellId|off]`
  (`YoyokazooUI.lua`, watcher in `WoWFunctions.lua`) is a debug-only watcher
  that prints each `isActive` flip, with timestamp/combat/remaining time, plus
  every successful player cast, to confirm whether `isActive` tracks the
  cooldown in combat). On Midnight-rules clients (`issecretvalue` exists),
  `YoyokazooUI.lua` skips registering `COMBAT_LOG_EVENT_UNFILTERED`
  (forbidden there — raises the "blocked from an action" popup), so
  `TargetRecentlyEvaded` never fires and `IsCombatStalemate()` always
  returns false (gated on the `COMBAT_LOG_AVAILABLE` global). Also holds the
  class-agnostic pixel-row field lists (`MULTI_BOOL_ONE_FIELDS`/
  `MULTI_BOOL_TWO_FIELDS`, the `IsPlayerWarrior`/`Shaman`/`Warlock`
  class bits, and `GetPlayerClassFields()` — see "Class split" above). The
  `IsTargetCasterMob`/`IsTargetRunnerMob`/`IsTargetFireImmune` checks here
  read their name lists from `CreatureConfig.lua`. Also holds the
  merchant-auto-sell logic. `AUTO_SELL_MODE_CHOICES` defines three modes
  (picked via the `/yyconfig` "Auto-sell" selector, see below): `"off"`
  sells nothing; `"allowlist"` sells quality 0 (Poor/gray) junk plus any
  quality 1 (Common/white) item named in `AUTO_SELL_ALLOWLIST_ITEM_NAMES`
  (a short, manually-curated list of cooking/fishing byproducts and
  skinning/leatherworking materials); `"blocklist"` sells everything
  quality 0-2 (gray/white/green) *except* items named in
  `AUTO_SELL_BLOCKLIST_ITEM_NAMES` (changes often -- see the list itself),
  and skips any item whose name isn't cached yet since it can't be
  checked against the blocklist. Both lists match by name
  (`GetItemInfo(itemID)`) rather than item ID, unlike
  `DYNAMITE_ITEM_CHOICES`/`HEALING_POTION_ITEM_CHOICES` below, since they
  aren't runtime-selectable `/yyconfig` choices. `ShouldAutoSellItem(itemInfo, mode)`
  flags a single `C_Container.GetContainerItemInfo(bag, slot)` result under
  a mode, skipping anything `hasNoValue` (quest items, etc.) in every mode,
  and `FindAutoSellQueue(mode)` (mode defaults to the current `/yyconfig`
  one) scans bags 0-4 for everything it flags, returning a
  `{ bag, slot, quality }` list sorted grays first, then whites, then greens,
  which `YoyokazooUI.lua`'s `MERCHANT_SHOW` handler sells from in that order
  (see below).
- **`WarriorFunctions.lua`** / **`ShamanFunctions.lua`**
  / **`WarlockFunctions.lua`**
  — that class's specific checks (e.g. `TargetHasRend`, `CanCastMortalStrikeOrBloodthirst`,
  `TargetHasSunderArmor`/`KnowsSunderArmor` — ClassBoolOne G5/G6, feeding
  `WarriorShouldCastSunderArmor` in `WowWarriorTasks.cs`, which applies exactly
  one Sunder per target (any stack at all counts, it's a bool not a count)
  right after Battle Shout/Overpower/Execute in the rotation priority — for
  Warrior. `CanCastSweepingStrikes` — ClassBoolOne G8 (G7 reserved) — is a
  single bit covering both "trained" and "off cooldown," unlike every other
  `KnowsX`/`CanCastX` pair here: the trained-yet check
  (`IsSpellKnownByName("Sweeping Strikes")`) is folded directly into
  `CanCastSweepingStrikes()` (`WarriorFunctions.lua`) instead of getting its
  own bit, because nothing on the C# side ever reads "does the player know
  Sweeping Strikes" independently of "can I cast it right now" — unlike
  `KnowsCharge` (also gates engage-method logic) and
  `KnowsMortalStrikeOrBloodthirst` (also picks Heroic Strike's rage reserve),
  which stay split because they're each read from more than one place. The
  fold-in is required, not just tidier: `GetSpellCooldown()` reads `(0, 0)` —
  "ready" — for a spell ID the player has never trained at all (same
  false-ready quirk `KnowsMortalStrikeOrBloodthirst`'s own comment documents
  for Mortal Strike/Bloodthirst), so without the guard a Warrior without the
  talent would read "can cast" and, sitting second in `WarriorCombatLoopTask`'s
  priority chain, block every lower-priority ability every tick for an
  ability that was never actually going to fire. (This bit was also
  hardcoded to the wrong spell ID for a while — 12328, Death Wish, not
  Sweeping Strikes' actual 12292 — found live via a temporary
  `SWEEPING_STRIKES_DEBUG` switch in `WarriorFunctions.lua`; same
  manually-flipped debug-flag pattern as `COUNT_ATTACKERS_DEBUG`/
  `COMBAT_STALEMATE_DEBUG` elsewhere, defaults off now that it's fixed.)
  `CanCastSweepingStrikes()` also uses `SpellIsCooledDownIgnoringGCD()`
  rather than the plain `SpellIsCooledDown()` every other Warrior cooldown
  check here uses, since a ~30s-cooldown ability can still land on a tick
  where the shared GCD (from whatever else was just cast) makes
  `GetSpellCooldown()` read "on cooldown" a beat after the real cooldown
  cleared. `SpellIsCooledDownIgnoringGCD()` (`WoWFunctions.lua`) and
  `IsGlobalCooldownCooledDown()` (which decodes into `WorldState.GCDCooledDown`)
  both now probe the same class-appropriate real spell via a shared
  `GetGCDProbeSpell()` — Shaman: Lightning Bolt Rank 1 (confirmed live);
  Warrior: Battle Shout Rank 1 (6673 — known from level 1, triggers the GCD,
  no cooldown of its own; confirmed live on the Forever client. It replaced Rend Rank 1
  (772), which isn't trainable until level 4 and so gave no cooldown info
  at all below that; Rend in turn replaced an original pick of Heroic
  Strike, confirmed wrong outright — Heroic Strike doesn't trigger the GCD
  at all in Classic); Warlock: Shadow Bolt Rank 1 (still unconfirmed live).
  Before this, both functions separately probed the dedicated GCD spell ID
  (61304), confirmed via testing to never show a cooldown on Classic Era for
  any class (and on the Forever client it returns no cooldown info at all) — `SpellIsCooledDownIgnoringGCD()` was silently non-functional
  until this fix. Note the Warrior rotation's `WarriorShouldCastX` checks are
  game-state only (buff up? proc available? target already debuffed?) and
  deliberately don't look at rage: the `if/else if` chain in
  `WarriorCombatLoopTask` picks one ability by priority, then the chosen
  branch checks rage (`WorldState.ResourcePercent >= cost`) and does
  *nothing* that tick if it can't afford it — waiting for rage rather than falling
  through to a cheaper filler, so a 30-rage Mortal Strike/Bloodthirst isn't
  starved by 15-rage abilities. Cleave/Heroic Strike's required rage
  (`WarriorCleaveRageRequired`/`WarriorHeroicStrikeRageRequired`) is their
  own cost plus a reserve for the next MS/BT, if trained;
  `ShouldCastRockbiterWeapon`, `CanCastEarthShock` for Shaman;
  `ShouldCastDemonArmor`/`ShouldSummonPet`/`ShouldCastImmolate`/
  `ShouldCastCorruption` for Warlock) plus that class's
  `<CLASS>_CLASS_BOOL_ONE/TWO_FIELDS` lists for the Class pixel rows (and,
  for Warrior, the `HasBattleShout()` wrapper its list names). Split out of `WoWFunctions.lua` to keep class-specific
  logic physically separated as more classes/fields get added. (Mage support
  — `MageFunctions.lua`/`WowMageClassState.cs`/`WowMageTasks.cs` — was
  removed: the rotation never got working, and enough else has changed since
  that it wasn't worth carrying forward. Re-add if it gets revisited; nothing
  else was built to depend on Mage-specific behavior.)
  `WarlockFunctions.lua` is a work in progress — `WARLOCK_CLASS_BOOL_ONE_FIELDS`
  lists `CanSpellcastPullTarget()` (shared, from `WoWFunctions.lua`),
  `ShouldCastDemonArmor()` (checks `HasBuffNamed("Demon Skin")`/
  `HasBuffNamed("Demon Armor")` — only one is ever active — and
  `IsSpellKnownByName()` for either, the same name-matching pattern
  `CanCurePoison`/`CanCureDisease` use in `ShamanFunctions.lua`, since
  Demon Skin/Demon Armor are different spell IDs at different ranks),
  `ShouldSummonPet()` (`IsSpellKnownByName("Summon Imp")` gating whether a
  pet can be summoned at all yet, plus `UnitExists("pet")`/
  `UnitIsDeadOrGhost("pet")` to catch both "no pet out" and "pet died"), and
  `ShouldCastImmolate()`/`ShouldCastCorruption()` (each
  `IsSpellKnownByName()` plus `TargetHasDebuffSpellName()` — both
  `WoWFunctions.lua` — to check the player knows the spell and the target
  doesn't already have that DoT, same pattern
  `ShouldCastFlameShock()`/`TargetHasFlameShock()` use for Shaman);
  `WARLOCK_CLASS_BOOL_TWO_FIELDS` is still empty.
- **`UIFunctions.lua`** — builds the addon's on-screen frames:
  - `InitializePixelRow()` — **the one the C# bot actually reads**: the pixel
    rows described in the color-encoding contract above (status bars, the
    sentinel, and the bit rows, via `CreateStatusBarElement`/
    `CreateSolidSwatchElement`/`CreateBitRowElement`), all in one
    `TOOLTIP`-strata container frame, placed via the self-calibrating
    `GetPhysicalPixelsPerLocalUnit()` helper. No debug-only values live here.
  - `InitializeIndicators()` — the debug frame (`YoyokazooUIFrame`, toggled
    by `/yydebug`), showing map X/Y and facing degrees as labeled numbers
    (`CreateNumberIndicator`), for reading off coordinates by hand. Its size
    (`YoyokazooUI.lua`) fits exactly those three rows. The bot never reads it.
  - `CreateSettingsMenu()` — a small checkbox/selector dialog (not tied to
    any live game-state read/OnUpdate loop) toggled by `YoyokazooUI.lua`'s
    `/yyconfig` slash command, for run-specific settings like "log out on low
    dynamite"/"log out on full bags" — see the `MultiBoolTwo` G1/G2 note in
    the color-encoding contract above for how those reach the C# side.
  - The nameplate target marker (see the target-marker bullet in the C#
    architecture section).
- **`MathFunctions.lua`** — the pixel-row bit encodings (`EncodeBoolFields`/
  `EncodeBytes`/`EncodeFixedPoint`, `RESERVED`, `PIXEL_ROW_BITS`/
  `FIXED_POINT_FRACTION_BITS` — see the color-encoding contract above) plus
  `round2`.
- **`YoyokazooUI.lua`** — addon entry point/event wiring (login, XP/level-up
  tracking, whisper tracking for "unseen whisper" alerts) and indicator
  initialization. On the first `PLAYER_ENTERING_WORLD`, `InitializeIndicators()`
  (the human-only debug frame) and `InitializePixelRow()` (the only thing the
  C# bot actually reads) are each called through `pcall`, with their own
  success flag (`indicatorsInitialized`/`pixelRowInitialized`), rather than
  called back-to-back unguarded. This isn't defensive-for-its-own-sake: an
  uncaught error building the debug frame used to unwind straight out of that
  whole block, meaning `InitializePixelRow()` never even ran, silently
  freezing every decoded bot flag for the rest of the session — confirmed
  happening in testing (a bug in one of `MultiBoolOne`'s inputs, read by
  one of the debug frame's swatches, blocked the pixel row from ever being
  built). Now a failure in either one is isolated to that one, and (since its
  flag stays false) retries on the next `PLAYER_ENTERING_WORLD` (zone change,
  death+release, hearth, `/reload`) instead of being stuck all session.
  Every `PLAYER_ENTERING_WORLD` also sets the `cameraDistanceMaxZoomFactor`
  CVar to `CAMERA_MAX_ZOOM_FACTOR` (2.6) via `SetCVar` (pcall'd), the same as
  typing `/console cameraDistanceMaxZoomFactor 2.6`.
  Owns `YoyokazooUIDB` (the addon's `SavedVariablesPerCharacter`
  table) and every slash command built on it: `/yydebug` toggles the debug
  frame's visibility; `/yyconfig` opens/closes the `CreateSettingsMenu()`
  dialog (built lazily, on first use) for the run-specific settings above,
  each backed by its own `YoyokazooUIDB` field
  (`logoutOnLowDynamite`/`logoutOnFullBags`) read via
  `IsLogoutOnLowDynamiteEnabled()`/`IsLogoutOnFullBagsEnabled()` when the
  `MultiBoolTwo` pixel row is drawn; `/yyspells` (debug-only,
  `DumpSpellData()` in `WoWFunctions.lua`) dumps an open class trainer's
  rows, the class trainer table's spell IDs as the client sees them, and the
  spellbook into `YoyokazooUIDB.spellDump`, written to the SavedVariables
  file on `/reload`, for rebuilding a trainer table from real client data;
  `/yysell` is a debug-only dry run
  of the auto-sell bag scan (see the auto-sell paragraph below) that prints
  what it would sell without selling anything, and works with no merchant
  open. Also auto-confirms the bind-on-pickup loot popup: on
  `LOOT_BIND_CONFIRM` it hides Blizzard's `"LOOT_BIND"` StaticPopup (if
  already shown — our frame registers after the default UI's own handler)
  and calls `ConfirmLootSlot(lootSlot)` — the same action that popup's own
  `OnAccept` performs — deferred by one frame via `RunNextFrame`. The defer
  is required, not stylistic: calling `ConfirmLootSlot` synchronously inside
  the same `LOOT_BIND_CONFIRM` dispatch silently failed to confirm the loot
  in testing. The bot has no way to click a popup, and always wants "Yes"
  here, so there's no toggle/config for it — unlike the
  reference addon this was modeled on (KyrosKrane Sylvanblade's "Annoying
  Pop-up Remover"), which exposes it as a user-toggleable option.
  Also auto-sells to an open merchant, then auto-repairs: on
  `MERCHANT_SHOW`, if the `/yyconfig` "Auto-sell" selector
  (`YoyokazooUIDB.autoSellMode`, `GetAutoSellMode()`/`IsAutoSellEnabled()` —
  `"off"`/`"allowlist"`/`"blocklist"`, see `AUTO_SELL_MODE_CHOICES` above;
  defaults to **off**; it replaced an older `autoSellJunk` boolean, whose "on"
  behavior is now `"allowlist"` — the stale field is cleared on load, not
  migrated) isn't off, `WoWFunctions.lua`'s
  `FindAutoSellQueue()` builds the list of bag slots to sell (an empty list if
  it's off); `SellNextAutoSellQueueItem()` sells one slot every
  `AUTO_SELL_TICK_SECONDS` (0.2s, via chained `C_Timer.After` calls —
  `C_Container.UseContainerItem(bag, slot)` sells an item only while a
  merchant window is open). `MERCHANT_SHOW` fires before Blizzard's own
  handler has necessarily called `MerchantFrame:Show()` — confirmed live, the
  very first tick consistently saw `MerchantFrame` exists but not yet shown,
  which killed the chain before it ever sold anything — so each tick that
  finds `MerchantFrame` not shown retries on a short timer
  (`MERCHANT_NOT_SHOWN_RETRY_SECONDS`, 0.1s) rather than bailing outright,
  capped at `MERCHANT_NOT_SHOWN_MAX_RETRIES` (25, ~2.5s) so a merchant window
  that genuinely never shows doesn't retry forever. A generation counter
  (`autoSellGeneration`, bumped on every `MERCHANT_SHOW` and
  `MERCHANT_CLOSED`) is captured by each scheduled tick and checked before it
  fires, so a chain from an earlier merchant visit (or one interrupted by the
  window closing early) can never fire a stale sell on whatever's now open —
  `UseContainerItem` on a closed merchant would use/equip the item instead of
  selling it. This is Lua-only, like the "Dynamite item"/"Healing potion"
  selectors — the bot doesn't need to know it happened, so nothing here
  reaches the pixel row. `/yysell allowlist`/`/yysell blocklist` dry-runs a
  specific mode regardless of the saved one. The bag scan against this client's actual
  `C_Container.GetContainerItemInfo` table shape (`quality`/`hasNoValue`
  field names) is confirmed live (see the comment above
  `ShouldAutoSellItem()`); the root cause of auto-sell not doing anything was
  the `MERCHANT_SHOW`-fires-before-`MerchantFrame:Show()` race described
  above, not the bag scan itself, and the retry fix is now **confirmed
  working live**.

  Once the sell queue is exhausted (empty or not — see below),
  `FinishAutoSellVisit()` runs the repair step: if the `/yyconfig`
  "Auto-repair" checkbox (`YoyokazooUIDB.autoRepairEnabled`,
  `IsAutoRepairEnabled()` — defaults **on**, since repairing has no
  downside) is enabled and `CanMerchantRepair()` is true, it reads
  `GetRepairAllCost()` and calls `RepairAllItems()` only if that cost is
  affordable (`<= GetMoney()`) — a merchant with no repair vendor, or one the
  player can't afford full repairs at, is left alone rather than partially
  repairing. Auto-repair is independent of the auto-sell mode — it runs whatever
  that is set to, since a merchant with nothing in our bags worth
  selling might still be the one we need repairs from; `SellNextAutoSellQueueItem`
  is still always called on `MERCHANT_SHOW` (with an empty queue if
  auto-sell is off or nothing was queued) specifically so the repair check
  goes through the exact same `MerchantFrame`-shown wait/retry as selling,
  rather than adding a second, separately-timed path that risks hitting the
  same show-race on unverified ground — except when auto-sell and auto-repair are both off,
  which skips the wait/retry loop entirely since there'd be nothing for it to
  do. The merchant window is only auto-closed (`CloseMerchant()`) if
  something was actually sold or repaired this visit; if neither happened
  (both off, nothing queued and nothing needing repair, or repair
  unaffordable) the window is left open exactly as the player left it. The
  whole path stays instrumented, off by default: `AUTO_SELL_DEBUG` (a global
  in `WoWFunctions.lua`, currently **false**, shared by both files rather
  than a per-file local like `COMBAT_STALEMATE_DEBUG`) makes
  `AutoSellDebugPrint()` chat-print every step — each occupied bag slot's raw
  itemInfo dumped key-by-key via `AutoSellDescribeItemInfo()` (deliberately
  field-name-agnostic), each queue/skip decision and its reason, the
  `MERCHANT_SHOW` handler's view of both `/yyconfig` toggles and queue size,
  each sell tick including any not-shown retries or other guard it bailed on,
  and the repair cost/affordability decision — flip it back to `true` if
  auto-sell or auto-repair need debugging again. `GetRepairAllCost()`'s exact
  Classic Era return shape isn't verified against this client build from
  reading source alone (same caveat as every other WoW Lua API call in this
  addon, per the top of this file) — confirm live via `AUTO_SELL_DEBUG`
  rather than guessing if repair behaves unexpectedly.

  Also auto-equips armor upgrades from bags: `EquipBestUpgradeIfBetter()`
  (`YoyokazooUI.lua`) equips the single best upgrade found by
  `FindBestEquipmentUpgrade()` (`WoWFunctions.lua`) via `EquipItemByName`,
  gated by the `/yyconfig` "Auto-equip upgrades" checkbox
  (`YoyokazooUIDB.autoEquipUpgrades`, `IsAutoEquipUpgradesEnabled()`,
  defaults **on**). Only quality 0-2 (gray/white/green) —
  `AUTO_EQUIP_MAX_QUALITY` — never blues/purples; only the armor slots in
  `AUTO_EQUIP_SLOTS_BY_EQUIP_LOC` (head/neck/shoulder/chest/waist/legs/feet/
  wrist/hands/rings/cloak — weapons, shields, ranged and trinkets are
  deliberately left manual); required level must be met and the armor
  type's proficiency passive known (`ARMOR_PROFICIENCY_SPELL_BY_SUBCLASS`,
  via `IsPlayerSpell`). "Better" = strictly higher `GetItemEquipScore()`, a
  per-class weighted sum over `GetItemStats()` (`AUTO_EQUIP_STAT_WEIGHTS`);
  rings compare against whichever equipped ring scores worse. One item per
  call — the equip fires `BAG_UPDATE_DELAYED`, which (debounced 0.5s,
  `ScheduleAutoEquipScan()`) re-runs it, as do `PLAYER_REGEN_ENABLED` and
  `MERCHANT_CLOSED`. Skipped in combat, while dead, and **while a merchant
  is open** — the auto-sell queue is bag/slot positions, and equipping drops
  the old item into the upgrade's bag slot, which the sell chain could then
  sell. The bind-on-equip popup is auto-accepted
  (`EQUIP_BIND_CONFIRM` → `EquipPendingItem(slot)` — there's no
  `AUTOEQUIP_BIND_CONFIRM` on this client, registering it throws;
  deferred a frame like `ConfirmLootSlot`) only within 2s of an equip this
  code started, so manual equips still prompt. `GetItemStats()` key names
  and the bind-confirm event args aren't verified live yet —
  `AUTO_EQUIP_DEBUG` (`WoWFunctions.lua`, off by default) prints the raw
  stats/scores/skip reasons, and `/yyequip` is a dry run with it forced on.
  Lua-only; nothing reaches the pixel row.

  Also auto-picks a class trainer's training gossip option: on `GOSSIP_SHOW`,
  `FindTrainerGossipOption()` (`WoWFunctions.lua`) scans
  `C_GossipInfo.GetOptions()` for a name matching the Lua pattern
  `^i require .+ training%.?$` (lowercased), and the handler selects it a
  frame later via `C_GossipInfo.SelectOption(gossipOptionID)` (falling back
  to `SelectOptionByIndex`). Holding Shift when the gossip opens skips it
  (e.g. to unlearn talents). Confirmed working live. Lua-only.

  Then auto-trains: on `TRAINER_SHOW`, if the `/yyconfig` "Auto-train
  skills" checkbox (`YoyokazooUIDB.autoTrainSkills`,
  `IsAutoTrainSkillsEnabled()`, defaults **on**) is enabled, Shift isn't
  held, and it's not a profession trainer (`IsTradeskillTrainer()` — buying
  an apprentice profession would spend a profession slot),
  `TrainNextAffordableSkill()` (`YoyokazooUI.lua`) buys one service per
  `AUTO_TRAIN_TICK_SECONDS` (0.3s) via `BuyTrainerService`, re-scanning
  each tick with `FindNextAffordableTrainerService()` (`WoWFunctions.lua` —
  first row with category `"available"` and cost `<= GetMoney()`; forces
  the trainer's "available" filter on) since a purchase shifts the rows.
  `GetTrainerServiceInfo` returns `(name, rank, category)` on Classic Era but
  `(name, category, icon)` on the Forever client (no rank), so rows are read
  through `GetTrainerServiceNameRankCategory()`, which handles both; Forever
  also wants a real boolean for `SetTrainerServiceTypeFilter`'s on/off, not
  1/0.
  Same generation-counter guard as auto-sell (`autoTrainGeneration`, bumped
  on `TRAINER_SHOW`/`TRAINER_CLOSED`). The G6/G7 trainer-spell cache isn't
  invalidated, so those bits can lag up to 60s after training. Confirmed
  working live on Forever. Each tick runs through `pcall` and chat-prints
  any error (these run from `C_Timer` callbacks, where an error otherwise
  vanishes silently — that's how a nil `GetCoinText`, a Classic-only global,
  kept it from ever buying). `AUTO_TRAIN_DEBUG` (`WoWFunctions.lua`, off by
  default) prints every row scanned.

  Also auto-picks talents: if the `/yyconfig` "Auto-pick talents" checkbox
  (`YoyokazooUIDB.autoPickTalents`, `IsAutoPickTalentsEnabled()`, defaults
  **off**) is on, `LearnNextTalentIfAny()` (`YoyokazooUI.lua`) spends one
  free point (`UnitCharacterPoints`) via `LearnTalent(tab, index)`, picked by
  `FindNextTalentToLearn()` (`WoWFunctions.lua`) from the class's ordered
  build table — `WARRIOR_TALENT_BUILD` (`WarriorFunctions.lua`), entries
  `{ name, rank }` meaning "get this talent to rank N", matched by talent
  name (not tab/index). Warrior only — other classes have no build and do
  nothing. Only the first 11 points are stubbed in (Deflection 2, Improved
  Rend 3, Tactical Mastery 5, Anger Management 1). A build name that isn't
  found stops the scan rather than skipping it. Triggered (debounced 0.5s,
  `ScheduleAutoTalentPick()`) by `CHARACTER_POINTS_CHANGED` — registered
  through `pcall` since it isn't confirmed on this client — plus
  `PLAYER_LEVEL_UP`, `PLAYER_ENTERING_WORLD`, `PLAYER_REGEN_ENABLED`, and
  ticking the checkbox on. Each learned point fires `CHARACTER_POINTS_CHANGED`
  again, which spends the next one; a failed `LearnTalent` fires nothing, so
  it stops rather than looping. Skipped in combat. Since
  `PLAYER_REGEN_ENABLED` is a trigger, it re-runs the scan after every
  fight (harmless — no free point means nothing happens).
  `AUTO_TALENT_DEBUG` (`WoWFunctions.lua`, currently **off**) prints each
  build entry's current/target rank. Lua-only.

## Tests (`WoWHelperUnitTests/`)

MSTest project. Feeds real captured `Source Images/*.bmp` screenshots (trade
windows, HP bars, breath bar, login screens, etc.) through the
same `WowScreenConfiguration` matchers the live bot uses, so the pixel-match
logic can be verified without WoW running. Also covers `WowPathfinding` math
directly. When you change screen-position matchers or add new encoded state,
prefer adding/updating a bitmap-backed test here.

**Known issue:** most of these fixture bitmaps were captured against the old
calibrated debug-frame pixel layout, before the addon moved to the fixed
top-left pixel row (see the color-encoding contract above) — they're
currently expected to fail and aren't a signal about unrelated changes.
Fixing them for real means recapturing the `Source Images/*.bmp` fixtures
against the current pixel layout, a distinct task from whatever prompted
noticing them.

## Practical notes

- Windows-only, .NET Framework 4.7.2, built via `WoWHelper.sln`
  (Visual Studio / MSBuild — not `dotnet` SDK-style).
- The bot controls the real mouse/keyboard and expects the WoW window
  focused; it's meant to run against a real game client, not a simulator.
- Slack integration (`SlackAPI`) is used for out-of-band alerts (leveled up,
  unexpectedly disconnected, low on consumables, near-death "petri alt+F4").
- **Don't build to confirm every change.** The user builds and tests
  (including in-game) manually. Write correct code and move on rather than
  running a build after each edit as a verification step — only build if
  actually needed for your own work (e.g. checking a specific compile
  question), not as routine confirmation.

---
**Keep this file in sync:** whenever a change alters the color-encoding
contract (pixel positions, bit order, new encoded flags), the state machine
states/transitions, the folder/file layout, class rotations, or other
structural facts described above, update this file in the same commit/PR.
If a change would make something above inaccurate, treat updating this file
as part of finishing the change, not a follow-up.
