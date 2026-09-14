# Crowd Control - TCG Card Shop Simulator

Crowd Control is an application that allows live streamers to enhance their gaming broadcasts by enabling real-time interaction between viewers and the game being played. Through Crowd Control, viewers can directly influence the gameplay experience, creating a dynamic and engaging environment that brings the audience closer to the action.

Crowd Control supports multiple platforms, such as Twitch, YouTube, Discord and more.

# Getting Started

To get started using this project you will need to check the ``readme.md`` in the src folder.

The mod (``src``) is built on the [WarpWorld BepInEx example plugin](https://github.com/WarpWorld/BepinEx-Example-Plugin)
and talks to the Crowd Control app with the ConnectorLib.JSON protocol (mod 2.0.0 and newer). Build it with
``dotnet build src\CrowdControl.TCGCardShopSimulator.csproj -c Release``; the build copies the plugin into the game's
BepInEx folder and into ``mod``. ``tools\cc_test_server.py`` is a fake Crowd Control app for testing effects locally.

You can load the ``TCGCardShopSimulator.cs`` in our SDK which can be found on our [Developer Page](https://developer.crowdcontrol.live/sdk/).

Follow instructions on that page to learn how to add effects to your CS file and how to activate them.

# Notes

Keep in mind updating your local CS file and mod will not make these effects live on the Crowd Control Interact/Twitch extension. If you add new effects and wish for them to get added to the existing pack on our service you will need to reach out in the #cc-developer channel in our [Discord](https://warp.world/discord).


## Links
[Crowd Control](https://crowdcontrol.live)

[Developer Page](https://developer.crowdcontrol.live/)
