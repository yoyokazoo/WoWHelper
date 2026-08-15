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

## The color-encoding contract (the critical coupling point)

`WoWFunctions.lua` / `UIFunctions.lua` compute game state and paint 1x1(ish)
colored squares at fixed screen positions. `WowWorldState.cs` reads the exact
same pixel positions (defined in `WowScreenConfigs.cs`, per-resolution) and
decodes them back into strongly-typed properties:

- **Floats** (map X/Y, facing degrees): `R*255 + G + B/255` → `GetFloatFromColor`.
- **Percents / small ints** (HP%, resource%, target HP%, attacker count, level):
  raw R/G/B channel value, 0-255.
- **Packed booleans**: a single pixel's R, G, and B bytes are each bit-packed
  (8 bools per channel = up to 24 bools per pixel) via `DecodeByte` /
  `MultiBoolOne` / `MultiBoolTwo`. Order of bits in the Lua encoder MUST match
  the order `WowWorldState.UpdateMultiBoolOne/Two` decode them in.
- **Text/UI state matched by exact pixel signature** (login screen, trade
  window open/accepted/confirmed, breath bar underwater, red error toast text
  like "facing wrong way" / "too far away" / "invalid target" / "out of
  range"): compared via `ImageMatchColorPositions.MatchesSourceImage` against
  known-good reference colors/positions, some validated against bitmaps in
  `WoWHelperUnitTests/Source Images/`.

Because both sides hardcode pixel coordinates and bit order, **the Lua encoder
and the C# decoder must be changed together** — a one-sided change silently
breaks the bot (wrong bit read as wrong flag, etc.), it won't fail loudly.
Screen positions are also resolution-specific (`WowScreenConfigs.cs` has
profiles for 1920x1080, 2560x1600, 3440x1440); the bot picks one based on
`Screen.PrimaryScreen.Bounds` in `WowFarmingConfiguration`.

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
- **`Gameplay/WowWorldState.cs`** — decodes one screen capture into all bot
  inputs (see color contract above); `WowPlayer` keeps a `PreviousWorldState`
  + `WorldState` pair each tick to detect edge-triggered events (leveled up,
  logged out unexpectedly, new whisper, etc.).
- **`Gameplay/Wow*Tasks.cs`** — behavior/task implementations grouped by
  concern: `WowMovementTasks` (pathfinding/turning/strafing/jumping),
  `WowCommonCombatTasks` (shared combat logic), `WowWarriorTasks` /
  `WowMageTasks` / `WowShamanTasks` (class-specific rotations, selected via
  `WowCombatConfiguration`), `WowManagementTasks` (logout conditions, low
  supplies, trade window handling, Slack alerts).
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

- **`YoyokazooUI.toc`** — addon manifest/load order.
- **`WoWFunctions.lua`** — game-state queries (in melee range, in combat,
  should-attack-target checks, spell cooldown/range checks incl. GCD-aware
  cooldown detection, attacker counting, etc.) — the "is X true" logic layer.
- **`UIFunctions.lua`** — builds the on-screen indicator frame/swatches and
  encodes values/booleans into the colors the C# side decodes
  (`EncodeFloatToColor` and friends).
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
