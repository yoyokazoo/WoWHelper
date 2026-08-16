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

`UIFunctions.lua`'s `InitializePixelRow()` paints a row of 18 `PIXEL_SIZE` x
`PIXEL_SIZE` (currently 3x3) swatches pinned to the screen's literal top-left
corner — one field/slot per swatch, fixed and resolution-independent (no
per-resolution calibration needed). Swatches are placed by converting desired
physical-pixel x/y/size into UIParent's local coordinate units via
`GetPhysicalPixelsPerLocalUnit()`, a **self-calibrating** ratio computed as
`GetPhysicalScreenSize() / UIParent:GetWidth()` — NOT `UIParent:GetEffectiveScale()`
and NOT Blizzard's `PixelUtil` library, both of which were tried first and
gave wrong/inconsistent results. On the dev machine (Classic Era 1.15.9, post
Edit-Mode UI update), `GetEffectiveScale()` read 0.9, but the real measured
ratio was 1.6875 — confirmed by matching four independently-measured rendered
sizes exactly, none of which matched any formula built from 0.9. Comparing
`GetPhysicalScreenSize()` against `UIParent`'s own reported size sidesteps
needing to know *why* `GetEffectiveScale()` disagrees with it, so it should
keep working even if this particular quirk changes or gets patched later.
`WowWorldState.cs` reads back the **center pixel** of each swatch
(`PixelSize/2` in from its top-left corner) for margin against any residual
edge blur — coordinates are computed via `PixelRowPoint(index)` on
`WowScreenConfiguration.cs`, not hardcoded per property. `PixelSize` must be
kept equal on both sides (Lua's `PIXEL_SIZE` local in `InitializePixelRow()`,
C#'s `WowScreenConfiguration.PixelSize` const). This scale-correctness fix is
still being iterated on/verified in-game as of this writing — check for a
more recent note here before assuming it's fully settled.

`WowWorldState.cs` decodes those swatches into strongly-typed properties:

- **Floats** (map X/Y, facing degrees — pixels 3, 4, 5): `R*255 + G + B/255`
  → `GetFloatFromColor`, matching Lua's `EncodeFloatToColor`.
- **Packed booleans** (pixels 11, 12 — `MultiBoolOne`/`MultiBoolTwo`): a
  single pixel's R/G/B bytes are each bit-packed (8 bools per channel) via
  `DecodeByte`. Order of bits in the Lua encoder MUST match the order
  `WowWorldState.UpdateMultiBoolOne/Two` decode them in. All 12 remaining
  class-agnostic bools are packed into `MultiBoolOne`'s R+G bytes alone;
  `MultiBoolOne`'s B byte and all of `MultiBoolTwo` are reserved/unused —
  intentionally, as clean room for the next class-agnostic field, rather
  than scattering a few bits across multiple pixels.
- **Packed percents/small ints** (pixels 13, 14 — `MultiIntOne`/`MultiIntTwo`):
  raw R/G/B channel value, 0-255 (HP%, resource%, target HP% / attacker
  count, level).
- **Class-specific packed state** (pixels 15, 16, 17 — `ClassBool`/`ClassInt`,
  see the "Class split" note below) — same bit-packing/raw-byte schemes as
  above, but the *meaning* of a given bit depends on which class is
  currently playing. Decoded into a `WowClassState` subtype, not
  `WowWorldState` — see the C# architecture section below.
- **Text/UI state matched by exact pixel signature** (login screen, trade
  window open/accepted/confirmed, breath bar underwater, red error toast text
  like "facing wrong way" / "too far away" / "invalid target" / "out of
  range"): a separate mechanism, unrelated to the pixel row above — compared
  via `ImageMatchColorPositions.MatchesSourceImage` against known-good
  reference colors/positions at resolution-specific coordinates
  (`WowScreenConfigs.cs`), some validated against bitmaps in
  `WoWHelperUnitTests/Source Images/`.

Pixels 0-2 and 6-10 of the row (individual HP%/resource%/target HP%/attacker
count and the single-bool `InRange`/`InCombat`/`CanChargeTarget`/`HeroicQueued`
slots) are drawn by Lua but **not yet read by C#** — placeholders for a
future move away from bit-packing. Pixels 15-17 (`ClassBool`/`ClassInt`) ARE
now read (see below).

**Class split:** class-specific flags (Battle Shout, Rend, Frost Armor,
Rockbiter, etc.) have been fully stripped out of `GetMultiBoolOne/Two` in
`WoWFunctions.lua` — those pixels now carry only class-agnostic state (see
above). `GetClassBoolOne/Two`/`GetClassIntOne` (also in `WoWFunctions.lua`) check
`UnitClass("player")` and delegate to that class's own populate function
(`GetWarriorClassBoolOne` in `WarriorFunctions.lua`, `GetMageClassBoolOne` in
`MageFunctions.lua`, `GetShamanClassBoolOne` in `ShamanFunctions.lua`) — so
the *same* pixel/bit position means something different depending on which
class is playing. On the C# side, `WowPlayer.ClassState` (a `WowClassState`
subtype — see the C# architecture section) decodes the matching bits,
selected once from `FarmingConfig.CombatConfiguration`. Note
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

Because both sides hardcode pixel coordinates and bit order, **the Lua encoder
and the C# decoder must be changed together** — a one-sided change silently
breaks the bot (wrong bit read as wrong flag, etc.), it won't fail loudly.
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
  focus window → check logout conditions → recover to "battle ready" → find/
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
  holds the one built for `FarmingConfig.CombatConfiguration` at construction
  time and updates it (in place, same instance — no `PreviousClassState`
  exists, nothing has needed one yet) every tick alongside `WorldState`, off
  the same captured bitmap.
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
  (`WowLocationConfigs.cs`), per-resolution screen pixel maps
  (`WowScreenConfigs.cs`), management/alert toggles
  (`WowManagementConfigs.cs`), farming profile selection
  (`WowFarmingConfigs.cs`). `Config/Definitions/` holds the POCOs these
  configs are instances of.
- **`Constants/`** — `WowInput.cs` maps logical actions to keybinds/macros the
  bot presses (expects specific in-game keybinds/macros to be set up to match),
  `WowPlayerConstants.cs` / `WowGameplayConstants.cs` hold thresholds/timings.
- **`Shared/`** — `KeyPoller` (global ESC-to-stop hotkey + cleanup),
  `BitmapDifferenceVisualizer` (loot-heatmap detection by diffing frames to
  find where a loot corpse/sparkle is), `PathSubdivision` (splits long waypoint
  legs into shorter hops), `TessaractSingleton` (shared Tesseract OCR engine,
  used to read text like trade-partner names).
- Input is sent through the `InputManager` DLL (`DLLs/InputManager.dll`);
  screen capture/image-matching and Slack notifications come from the
  `WindowsGameAutomationTools` NuGet package (external, not in this repo).
  OCR uses `Tesseract` (`tessdata/eng.traineddata`).

## Lua addon (`WoWHelper/Lua Addon/`, `YoyokazooUI`)

- **`YoyokazooUI.toc`** — addon manifest/load order. Loads
  `MathFunctions.lua` → `WoWFunctions.lua` → `WarriorFunctions.lua` →
  `MageFunctions.lua` → `ShamanFunctions.lua` → `UIFunctions.lua` →
  `YoyokazooUI.lua`. Load order doesn't actually matter for correctness here
  (everything is a plain global function, resolved at call time, and nothing
  calls any of these before `PLAYER_ENTERING_WORLD`, well after every file
  has finished loading) — this ordering is just for readability.
- **`WoWFunctions.lua`** — class-agnostic game-state queries (in melee range,
  in combat, should-attack-target checks, spell cooldown/range checks incl.
  GCD-aware cooldown detection, attacker counting, etc.) — the "is X true"
  logic layer, plus `GetMultiBoolOne/Two`/`GetMultiIntOne/Two` (shared-state
  pixel populate functions) and `GetClassBoolOne/Two`/`GetClassIntOne`
  (class-specific dispatchers — see "Class split" above).
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
  - `InitializePixelRow()` — **the one the C# bot actually reads** (mostly —
    pixels 0-2/6-10 are drawn but not yet decoded, see above). 18
    `PIXEL_SIZE`-square swatches pinned to the screen's literal top-left
    corner, placed via the self-calibrating `GetPhysicalPixelsPerLocalUnit()`
    helper, matching the center-pixel `Point`s computed on
    `WowScreenConfiguration` (see color-encoding contract above). Needs no
    per-resolution calibration.
  - `InitializeIndicators()` — the original 20x20-box draggable debug frame
    (`YoyokazooUIFrame`). Purely a human-readable visual now; the bot no
    longer reads its position, but it's left in place as a human-facing
    debug display since it shows the same 18 values legibly.
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

---
**Keep this file in sync:** whenever a change alters the color-encoding
contract (pixel positions, bit order, new encoded flags), the state machine
states/transitions, the folder/file layout, class rotations, or other
structural facts described above, update this file in the same commit/PR.
If a change would make something above inaccurate, treat updating this file
as part of finishing the change, not a follow-up.
