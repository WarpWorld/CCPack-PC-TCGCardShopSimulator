# TCG Card Shop Simulator

This pack uses a BepInEx 5 plugin that communicates with Crowd Control through
the ConnectorLib.JSON protocol. The installable BepInEx layout is in `mod`;
the game-specific plugin source is in `src`.

## Requirements

- TCG Card Shop Simulator.
- BepInEx 5 (Mono).
- Crowd Control with the **TCG Card Shop Simulator** pack selected.

## Installation and setup

1. Close the game.
2. Overlay the contents of `mod` onto the game directory, preserving the
   `BepInEx` directory and loader files.
3. Confirm that the plugin files are under
   `BepInEx\plugins\CrowdControl`.
4. Start Crowd Control, select TCG Card Shop Simulator, then launch the game.
5. Load into normal shop gameplay before accepting effects.

## Connection behavior

The plugin connects to `127.0.0.1:51337` and maintains a background reconnect
loop. Effects run only while the game reports `InLevel`; menus, loading,
pauses, focus loss, hubs, dialogue, and unsafe player states defer effects.

Press **F8** to toggle the plugin overlay and **F9** to request a manual
reconnect.

## Troubleshooting

- **The game does not load the plugin:** verify that the BepInEx loader files
  and the `BepInEx\plugins\CrowdControl` directory were copied into the game
  folder.
- **No connection:** start the Crowd Control desktop app, select the matching
  pack, then press **F9** in game. Check that local port `51337` is free.
- **Effects retry:** leave menus, dialogue, pause, loading, or hub states and
  return to active shop gameplay.

## Development

Build with .NET SDK 8 or later:

```text
dotnet build src\CrowdControl.TCGCardShopSimulator.csproj -c Release
```

`src\README.md` documents the `GameBaseDir` override and the local fake
Crowd Control test server.
