# Crowd Control mod for TCG Card Shop Simulator

BepInEx 5 (mono) plugin built on the WarpWorld BepInEx example plugin architecture
(https://github.com/WarpWorld/BepinEx-Example-Plugin). Version 2.0.0 moved the mod from the legacy
TCP message format to the ConnectorLib.JSON protocol, so the game pack (`..\TCGCardShopSimulator.cs`)
no longer sets `MessageFormat = CrowdControlLegacy`.

## Building

Requirements: .NET SDK (8 or newer) and the game installed. Set `GameBaseDir` in
`CrowdControl.TCGCardShopSimulator.csproj` if the game is not at
`D:\SteamLibrary\steamapps\common\TCG Card Shop Simulator`.

```
dotnet build src\CrowdControl.TCGCardShopSimulator.csproj -c Release
```

Every build copies `CrowdControl.dll`, `ConnectorLib.JSON.dll` and `HypeTrain.dll` into the game's
`BepInEx\plugins\CrowdControl` folder and into `..\mod\BepInEx\plugins\CrowdControl` (the shipped
layout). Pass `-p:SkipDeploy=true` to build without copying. `..\ConnectorLib.JSON` is a vendored
copy of the WarpWorld ConnectorLib.JSON library (with the custom-effect message types the example
uses); it is built as a project reference.

The game ships Newtonsoft.Json 13, so it is not bundled. The community custom-effects compiler is
left out of the build (`IncludeCustomEffectCompiler` is false and `CUSTOM_EFFECTS_SUPPORTED` is false).

## Layout

| File / folder | What it is |
| --- | --- |
| `CrowdControlMod.cs` | Plugin entry point, ticks the scheduler from `Update()`. Generic, from the example. |
| `CrowdControlMod.TCG.cs` | Game-specific half of the plugin: effect state flags, action queue, Twitch chat listener, customer nameplates, on-screen text. |
| `GameStateManager.cs` | Reports Menu / Loading / Paused / NotFocused / InLevel to the Crowd Control app. Effects only run in `InLevel`. |
| `GameActions.cs` | The game logic behind every effect (spawning, items, hype train, food, reflection helpers). Instant effects return their result directly; timed effects only validate here. |
| `TimedEffects.cs` | Apply / revert / per-frame tick for each `TimedType` (FOV, payment forcing, speed, gravity, mute...). |
| `Delegates\Effects\Implementations\PackEffects.cs` | One `Effect` class per instant effect, listing the effect codes it answers. Generated from the old delegate map. |
| `Delegates\Effects\Implementations\PackTimedEffects.cs` | Timed effect classes: duration, conflicting codes, which `TimedType` to apply. |
| `Delegates\Effects\TimedGameEffect.cs` | Bridge between the scheduler's timed lifecycle and `TimedEffects.cs`. |
| `Harmony\Patches.cs` | Harmony patches (title screen version, nameplates, payment/exact change/large bills hooks, auto pack opener). |
| `UI\` | The Crowd Control overlay (F8 toggles, F9 reconnects). `EffectNames.cs` maps codes to display names. |
| `NetworkClient*.cs`, `Scheduler.cs`, `Delegates\Effects\*.cs`, `Delegates\Metadata\*`, `CustomEffects\*` | Unchanged framework code from the example. |
| `resources\` | `HypeTrain.dll` (prefab behaviour) plus the `food` and `warpworld.hypetrain` asset bundles. |

## Adding an effect

1. Add the menu entry to `..\TCGCardShopSimulator.cs` (the Crowd Control pack).
2. Implement it as a static method in `GameActions.cs` returning `EffectResponse`.
3. Instant effect: add a class to `PackEffects.cs` with `[Effect("code")]` whose `Start` calls the method.
   Timed effect: add a `TimedType` and its apply/revert in `TimedEffects.cs`, then a class in
   `PackTimedEffects.cs` deriving from `TimedGameEffect` with the duration and conflicts.
4. Add the display name to `UI\EffectNames.cs`.

Effect codes must match between the pack file and the `[Effect]` attributes. Timed effects pause
automatically while the game is not `InLevel` and count down in real time regardless of the
Slow Motion / Fast Forward effects.

## Testing

`..\tools\cc_test_server.py` is a fake Crowd Control app for exercising effects from the command line;
see the header of that file.
