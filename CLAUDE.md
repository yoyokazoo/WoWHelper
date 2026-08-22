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

`UIFunctions.lua`'s `InitializePixelRow()` paints a row of `PIXEL_SIZE` x
`PIXEL_SIZE` (currently 3x3) swatches pinned to the screen's literal top-left
corner — fixed and resolution-independent (no per-resolution calibration
needed). **The row is deliberately condensed to exactly the pixels the C#
bot actually reads, in the order it reads them** — nothing decorative or
debug-only lives here (that's what the separate `InitializeIndicators()`
frame is for, see the Lua addon section below). Swatches are placed by
converting desired physical-pixel x/y/size into UIParent's local coordinate
units via `GetPhysicalPixelsPerLocalUnit()`, a **self-calibrating** ratio
computed as `GetPhysicalScreenSize() / UIParent:GetWidth()` — NOT
`UIParent:GetEffectiveScale()` and NOT Blizzard's `PixelUtil` library, both
of which were tried first and gave wrong/inconsistent results. On the dev
machine (Classic Era 1.15.9, post Edit-Mode UI update), `GetEffectiveScale()`
read 0.9, but the real measured ratio was 1.6875 — confirmed by matching four
independently-measured rendered sizes exactly, none of which matched any
formula built from 0.9. Comparing `GetPhysicalScreenSize()` against
`UIParent`'s own reported size sidesteps needing to know *why*
`GetEffectiveScale()` disagrees with it, so it should keep working even if
this particular quirk changes or gets patched later. `WowWorldState.cs`
reads back the **center pixel** of each swatch (`PixelSize/2` in from its
top-left corner) for margin against any residual edge blur — coordinates are
computed via `PixelRowPoint(index)` on `WowScreenConfiguration.cs`, not
hardcoded per property. `PixelSize` must be kept equal on both sides (Lua's
`PIXEL_SIZE` local in `InitializePixelRow()`, C#'s
`WowScreenConfiguration.PixelSize` const).

Current row, index → content (both sides MUST stay in this exact order —
see "Adding a new pixel" below):

| Index | Content | Decoded into |
|---|---|---|
| 0 | Fixed sentinel, exactly `ADDON_LOADED_COLOR` (96, 255, 117) | `WowWorldState.OnLoginScreen` (inverted — see below) |
| 1 | Map X (float) | `WowWorldState.MapX` |
| 2 | Map Y (float) | `WowWorldState.MapY` |
| 3 | Facing degrees (float) | `WowWorldState.FacingDegrees` |
| 4 | `MultiBoolOne` (packed bools, class-agnostic) | various `WowWorldState` bools |
| 5 | `MultiIntOne` (packed R/G/B percents) | `PlayerHpPercent`/`ResourcePercent`/`TargetHpPercent` |
| 6 | `MultiIntTwo` (packed R/G/B) | `AttackerCount`/`PlayerLevel`/`CurrentZone` |
| 7 | `ClassBoolOne` (packed bools, class-specific) | a `WowClassState` subtype (see C# architecture section) |

Decode schemes: floats use `R*255 + G + B/255` (`GetFloatFromColor`,
matching Lua's `EncodeFloatToColor`); packed bools bit-pack 8 flags per
channel (`DecodeByte`, order MUST match between Lua's `EncodeBooleansToByte`
call and the corresponding C# `Update...` method); packed ints use a raw
0-255 value per channel. Index 0 is the one exception — a plain exact-color
match (`WowScreenConfiguration.ADDON_LOADED_COLOR`), not one of the schemes
above: if the pixel is exactly that color, the addon is loaded and rendering
the row (so the rest of it is meaningful) and `OnLoginScreen` is false;
*any* other color — including whatever's actually on-screen at that position
when the addon isn't loaded — means `OnLoginScreen` is true. This replaced an
older, unrelated mechanism (a multi-point text/UI pixel-signature match
against login-screen-specific colors) since folding it into the row is more
reliable than matching login-screen chrome. Separately, **text/UI state
matched by exact pixel signature** (trade window, breath bar, red error toast
text) is still its own unrelated mechanism — resolution-specific coordinates
in `WowScreenConfigs.cs`, compared via `ImageMatchColorPositions.MatchesSourceImage`,
some validated against bitmaps in `WoWHelperUnitTests/Source Images/`.

**Zone ID (`MultiIntTwo`'s B channel, `WowWorldState.CurrentZone`):** a
WowHelper-defined numeric zone ID, NOT Blizzard's internal map ID (which
doesn't fit in a single byte channel). Lua's `GetCurrentZoneId()`
(`WoWFunctions.lua`) looks up `GetRealZoneText()` against a
`ZONE_NAME_TO_ID` table (255 = not a known zone); the numeric values there
MUST stay in sync with the `WowZone` enum in `WowLocationConfiguration.cs`
(`Unknown = 255`) — two independent hardcoded tables that have to agree,
same class of coupling as everything else in this contract. Each
`WowLocationConfiguration` in `WowLocationConfigs.cs` also carries a `Zone`
(plus `Title` and `MinimumLevel`), for validating the character is in the
right place/level before a farming route starts — `CurrentZone` is the
runtime half of that check. That validation (plus a third check: is the
player near *any* of the route's own waypoints, via
`WowPathfinding.GetDistanceToClosestWaypoint` against the single global
`WowPlayerConstants.MAX_DISTANCE_FROM_ROUTE_WAYPOINT` threshold — deliberately
one constant sized off the loosest route's own largest adjacent-waypoint gap,
not a per-config value) is wired into `WowManagementTasks.SetLogoutVariablesTask()`,
checked first so a bad start gives the clearest possible logout reason.

**Automatic farming-config resolution:** `WowFarmingConfigs.CURRENT_CONFIG`
only sets `ManagementConfiguration` now — `LocationConfiguration` and
`CombatConfiguration` are no longer hardcoded there. Instead,
`WowPlayer.ResolveFarmingConfigurationTask()` (`WowConfigResolutionTasks.cs`)
runs once at startup, in a new `PlayerState.RESOLVE_FARMING_CONFIGURATION`
state right after the window is focused and before anything else touches
`ClassState` or `FarmingConfig.LocationConfiguration`/`CombatConfiguration`
(both start `null`/`WowCombatConfiguration.Unknown` — see
`WowFarmingConfiguration`'s constructor):
- `CombatConfiguration` is set straight from `WowWorldState.PlayerClass` (see
  the `MultiBoolOne` B-byte bits 2-4 note below). If it's `null` (unsupported
  class, or the addon isn't rendering a real row yet), resolution fails.
- `LocationConfiguration` is picked from `WowLocationConfigs.ALL_LOCATIONS` —
  an explicit list (deliberately not reflection over the class's static
  fields, so a route can be pulled out of auto-selection without deleting
  it) — by filtering to configs the player currently satisfies: the same
  three checks `SetLogoutVariablesTask()` uses to keep validating an
  already-running route (level, zone, waypoint proximity via
  `WowPathfinding.GetDistanceToClosestWaypoint`/
  `WowPlayerConstants.MAX_DISTANCE_FROM_ROUTE_WAYPOINT`), just run once
  up front instead of every tick. Exactly one match wins; zero or more than
  one means there's no safe automatic choice.

On either failure, `ResolveFarmingConfigurationTask()` sends a Slack alert via
`SlackHelper.SendMessageToChannel()` explaining why, returns `false`, and
`CoreGameplayLoopTask` sends the state machine straight to
`EXITING_CORE_GAMEPLAY_LOOP` (which exits the process) rather than guessing.
Because resolution now happens after construction, `WowPlayer.ClassState`
starts `null` (`UpdateWorldStateAsync`/`UpdateWorldState`/`UpdateFromBitmap`
all null-guard the per-tick `ClassState.UpdateFromBitmap` call) until
`ResolveFarmingConfigurationTask()` builds the concrete subtype; nothing
reads `ClassState` before that state runs. `WowManagementTasks.
EveryWorldStateUpdateTasks()` runs every tick including the handful before
resolution completes, so its level-up-alert block is guarded on
`FarmingConfig.LocationConfiguration != null` for the same reason.

**`MultiBoolOne`'s B byte:** bit 1 carries `TargetRecentlyEvaded` — true for a
few seconds after the player's own attack drew an `EVADE` combat-log miss
against the current target (i.e. the target is stuck evading, e.g. leashed on
the far side of terrain it can't path across). Detected via
`COMBAT_LOG_EVENT_UNFILTERED` in `YoyokazooUI.lua` (`HasRecentTargetEvade()`,
same sticky-timestamp pattern as `HasUnseenWhisper()`), latched for
`EVADE_WINDOW_SECONDS` (3s) rather than requiring the bot's poll to land on
the exact tick the miss fired. Consumed by `MeleeMakeSureWeAreAttackingEnemyTask`
(`WowCommonCombatTasks.cs`) as part of its "this target is stuck, back off/clear
it" checks.

Bits 2-4 carry which of the three bot-supported classes the player is playing
— exactly one of `PlayerIsWarrior`/`PlayerIsMage`/`PlayerIsShaman` is true,
from a plain `UnitClass("player")` check in `GetMultiBoolOne()`
(`WoWFunctions.lua`). C# decodes these into `WowWorldState.PlayerClass`
(nullable `WowCombatConfiguration` — null if none of the three bits are set,
i.e. an unsupported class or the addon isn't rendering a real row yet), which
`WowPlayer.ResolveFarmingConfigurationTask` uses to set
`FarmingConfig.CombatConfiguration` automatically at startup — see "Automatic
farming-config resolution" below.

Bits 5-8 carry `IsPlayerPoisoned`/`IsPlayerDiseased` (from `PlayerHasDebuffType()`
in `WoWFunctions.lua`, keyed off `UnitDebuff("player", i)`'s dispel-type return
value) and `IsTargetNatureImmune` (name-based, per `NATURE_IMMUNE_MOB_NAMES` in
`CreatureConfig.lua` — same pattern as `IsTargetFireImmune`)/`IsTargetCasting`
(`UnitCastingInfo`/`UnitChannelInfo` against `"target"`, the target-side
counterpart to the already-existing `IsPlayerCasting`, which packs into
`MultiBoolOne`'s G byte as `WowWorldState.IsCurrentlyCasting`) — this fully
packs the byte.

**Reserved-but-not-in-the-row:** all of `MultiBoolTwo` is reserved for the
next class-agnostic bool (see `GetMultiBoolOne/Two` in `WoWFunctions.lua`);
`ClassBoolTwo`/`ClassIntOne` are reserved for the next class-specific field.
Neither has a pixel in the row or a `Point` on `WowScreenConfiguration` right
now, **on purpose** — the row only grows when a field actually needs to go
in it.

**Adding a new pixel:** append a new `AddSwatch(N, ...)` call in
`InitializePixelRow()` (Lua) AND a new `PixelRowPoint(N)`-based property on
`WowScreenConfiguration.cs` (C#), keeping index `N` identical on both sides.
Do this together — a one-sided change silently breaks the bot (wrong pixel
read as wrong flag), it won't fail loudly.

**Class split:** class-specific flags (Battle Shout, Rend, Frost Armor,
Rockbiter, etc.) live only in `ClassBoolOne`, never in `MultiBoolOne/Two`.
`GetClassBoolOne/Two`/`GetClassIntOne` (in `WoWFunctions.lua`) check
`UnitClass("player")` and delegate to that class's own populate function
(`GetWarriorClassBoolOne` in `WarriorFunctions.lua`, `GetMageClassBoolOne` in
`MageFunctions.lua`, `GetShamanClassBoolOne` in `ShamanFunctions.lua`) — so
the *same* pixel/bit position means something different depending on which
class is playing. On the C# side, `WowPlayer.ClassState` (a `WowClassState`
subtype — see the C# architecture section) decodes the matching bits,
selected once from `FarmingConfig.CombatConfiguration` — itself auto-set at
startup from the player's detected class (see "Automatic farming-config
resolution" below), not hardcoded. Note
`CanSpellcastPullTarget()` stays in `WoWFunctions.lua` rather than being
split into the class files — it's shared between Mage and Shaman under the
same name, and duplicating that name into both class files would collide
(last-loaded file wins silently, since addon globals are one flat namespace).
The C#-side mirror of that same constraint: `CanEngageTarget()` in
`WowPlayerCombatConfig.cs` is a thin class-dispatching wrapper for
class-agnostic callers (e.g. `WowMovementTasks.PathfindingLoopTask`), while
`WarriorCanEngageTarget`/`MageCanEngageTarget`/`ShamanCanEngageTarget` (one
per `Wow*Tasks.cs`) hold the real per-class logic and take that class's
typed `ClassState` directly.

The `Screen.PrimaryScreen.Bounds`-based per-resolution config in
`WowFarmingConfiguration` still selects a `WowScreenConfiguration`, but that
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
- **`Gameplay/WowClassState.cs`** (abstract) / **`WowWarriorClassState.cs`** /
  **`WowMageClassState.cs`** / **`WowShamanClassState.cs`** — the
  class-specific counterpart to `WowWorldState`. `ClassBool`/`ClassInt` pixels
  mean something different per class, so rather than one flat object with
  every class's fields (where nothing would stop e.g. Mage code from reading
  a Warrior-only field and silently getting stale data), each class gets its
  own concrete subtype exposing *only* its own fields — a wrong-class field
  reference is a compile error, not a runtime surprise. `WowPlayer.ClassState`
  holds the one built for `FarmingConfig.CombatConfiguration` — `null` until
  `ResolveFarmingConfigurationTask()` sets that from the player's detected
  class and builds the matching subtype (see "Automatic farming-config
  resolution" above) — and updates it (in place, same instance — no
  `PreviousClassState` exists, nothing has needed one yet) every tick
  alongside `WorldState`, off the same captured bitmap.
- **`Gameplay/Wow*Tasks.cs`** — behavior/task implementations grouped by
  concern: `WowMovementTasks` (pathfinding/turning/strafing/jumping),
  `WowCommonCombatTasks` (shared combat logic), `WowWarriorTasks` /
  `WowMageTasks` / `WowShamanTasks` (class-specific rotations, selected via
  `WowCombatConfiguration`), `WowManagementTasks` (logout conditions, low
  supplies, trade window handling, Slack alerts). The class-specific task
  files' entry points (dispatched from `WowPlayerCombatConfig.cs`) take their
  own class's `WowClassState` subtype as a **method parameter**, not read off
  `this` — so a Mage-only field is unreachable from inside a Warrior method's
  scope, not just absent on some shared type. `WowPlayerCombatConfig.cs`
  casts `ClassState` to the right concrete type at each dispatch call site;
  if that cast ever fails, `ClassState` and `CombatConfiguration` have gone
  out of sync, and throwing immediately there is intentional — silently
  reading the wrong class's state would be worse.
- **`Gameplay/WowPathfinding.cs`** — pure-math helpers for waypoint following
  (facing/turn-direction math, angle tolerance that tightens near a waypoint,
  lateral-distance-from-path calc). No side effects, unit-testable.
- **`Config/`** — per-location farming routes/waypoints
  (`WowLocationConfigs.cs` — also holds `ALL_LOCATIONS`, the explicit list
  `ResolveFarmingConfigurationTask()` auto-selects from; see "Automatic
  farming-config resolution" above), per-resolution screen pixel maps
  (`WowScreenConfigs.cs`), management/alert toggles
  (`WowManagementConfigs.cs`), the farming profile
  (`WowFarmingConfigs.cs` — now only `ManagementConfiguration`;
  `LocationConfiguration`/`CombatConfiguration` are resolved at runtime, not
  set here). `Config/Definitions/` holds the POCOs these configs are
  instances of. Each `WowLocationConfiguration` carries a `Title`
  (human-readable, includes the minimum level), `MinimumLevel`, and `Zone`
  (`WowZone` enum, `WowLocationConfiguration.cs`) — see the zone ID
  note in the color-encoding contract above for how `Zone` ties to
  `WowWorldState.CurrentZone`.
- **`Constants/`** — `WowInput.cs` maps logical actions to keybinds/macros the
  bot presses (expects specific in-game keybinds/macros to be set up to match),
  `WowPlayerConstants.cs` / `WowGameplayConstants.cs` hold thresholds/timings.
- **`Shared/`** — `KeyPoller` (global ESC-to-stop hotkey + cleanup),
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
  `WarriorFunctions.lua` → `MageFunctions.lua` → `ShamanFunctions.lua` →
  `UIFunctions.lua` → `YoyokazooUI.lua`. Load order doesn't actually matter
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
  logic layer, plus `GetMultiBoolOne/Two`/`GetMultiIntOne/Two` (shared-state
  pixel populate functions) and `GetClassBoolOne/Two`/`GetClassIntOne`
  (class-specific dispatchers — see "Class split" above). The
  `IsTargetCasterMob`/`IsTargetRunnerMob`/`IsTargetFireImmune` checks here
  read their name lists from `CreatureConfig.lua`.
- **`WarriorFunctions.lua`** / **`MageFunctions.lua`** / **`ShamanFunctions.lua`**
  — that class's specific checks (e.g. `TargetHasRend`, `CanCastWhirlwind` for
  Warrior; `ShouldWeSummonWater`, `IsFireblastCooledDown` for Mage;
  `ShouldCastRockbiterWeapon`, `CanCastEarthShock` for Shaman) plus a
  `GetXClassBoolOne/Two`/`GetXClassIntOne` set that packs that class's state
  into the ClassBool/ClassInt pixels. Split out of `WoWFunctions.lua` to keep
  class-specific logic physically separated as more classes/fields get added.
- **`UIFunctions.lua`** — builds the on-screen indicator frame/swatches and
  encodes values/booleans into the colors the C# side decodes
  (`EncodeFloatToColor` and friends). Two rendering paths coexist, both fed
  by the same underlying value/color functions:
  - `InitializePixelRow()` — **the one the C# bot actually reads**, condensed
    to exactly the 7 pixels it consumes (see the table in the color-encoding
    contract above) — no debug-only or not-yet-decoded values live here.
    `PIXEL_SIZE`-square swatches pinned to the screen's literal top-left
    corner, placed via the self-calibrating `GetPhysicalPixelsPerLocalUnit()`
    helper, matching the center-pixel `Point`s computed on
    `WowScreenConfiguration`. Needs no per-resolution calibration.
  - `InitializeIndicators()` — the original 20x20-box draggable debug frame
    (`YoyokazooUIFrame`), showing every value (including ones the bot doesn't
    consume) legibly with labels, for human debugging. The bot never reads
    its position; kept purely as a human-facing debug display.
- **`MathFunctions.lua`** — small numeric helpers shared by the above.
- **`YoyokazooUI.lua`** — addon entry point/event wiring (login, XP/level-up
  tracking, whisper tracking for "unseen whisper" alerts) and indicator
  initialization.

## Tests (`WoWHelperUnitTests/`)

MSTest project. Feeds real captured `Source Images/*.bmp` screenshots (trade
windows, HP bars, breath bar, error text, login screens, etc.) through the
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
