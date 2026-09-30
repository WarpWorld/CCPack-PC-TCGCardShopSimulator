using BepInEx;
using BepInEx.Logging;
using CrowdControl.Delegates.Effects;
using System.Reflection;
using UnityEngine;

namespace CrowdControl;

/// <summary>
/// The main Crowd Control mod class.
/// </summary>
[BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
public partial class CrowdControlMod : BaseUnityPlugin
{
    // Mod Details - ALWAYS SET THESE FOR A NEW GAME!
    // (these must be compile-time constants for the [BepInPlugin] attribute, so they can't live in the
    // csproj - the DLL name is set separately via the GameName property in BepinExExample.csproj)
    public const string MOD_GUID = "WarpWorld.CrowdControl"; //unique BepInEx plugin ID - fine to leave as-is since only one Crowd Control mod is installed per game
    public const string MOD_NAME = "Crowd Control"; //display name shown in the BepInEx log
    public const string MOD_VERSION = "1.2.2"; //bump this with each release of your mod (the ccver file next to the DLL wins when present)

    /// <summary>Whether this mod supports community-written effects loaded from disk.</summary>
    /// <remarks>
    /// Opt-in per game, and deliberately a compile-time constant rather than a setting: a streamer
    /// cannot turn this on, so a mod that has not adopted custom effects will never compile or run
    /// code from the custom effects folder no matter what ends up in it or in the config file.
    /// <para>
    /// Before setting this to true, make sure the game's Crowd Control pack has
    /// <c>allowCustomEffects</c> set - without it the app rejects the effects the mod registers, and
    /// nothing a viewer can buy will appear. See CustomEffect.md.
    /// </para>
    /// </remarks>
    public const bool CUSTOM_EFFECTS_SUPPORTED = false;

    
    /// <summary>The real-time duration of the current tick, used to advance timed effect countdowns.</summary>
    /// <remarks>
    /// Dividing by the time scale makes timed effects count down in real time even during slow motion.
    /// The time scale is checked to avoid Infinity/NaN corrupting effect timers in games that
    /// run FixedUpdate with the time scale at (or below) zero.
    /// Change this to use Time.deltaTime if ticking from Update instead of FixedUpdate.
    /// </remarks>
    //TCG Card Shop Simulator pauses by setting Time.timeScale to 0 (which stops FixedUpdate entirely), and the
    //Slow Motion / Fast Forward effects change the time scale too, so the scheduler is ticked from Update()
    //using unscaled real time - timed effects always count down in real seconds.
    public static float DeltaTime => Time.unscaledDeltaTime;

    private readonly HarmonyLib.Harmony harmony = new(MOD_GUID);

    /// <summary>The logger for the mod.</summary>
    public new ManualLogSource Logger => base.Logger;

    /// <summary>The singleton instance of the game mod.</summary>
    internal static CrowdControlMod Instance { get; private set; } = null!;

    /// <summary>The game state manager object.</summary>
    public GameStateManager GameStateManager { get; private set; } = null!;
    
    /// <summary>The effect class loader.</summary>
    public EffectLoader EffectLoader { get; private set; } = null!;

    /// <summary>The loader for community-written effects dropped into the custom effects folder.</summary>
    public CustomEffects.CustomEffectManager CustomEffects { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the client is connected.
    /// </summary>
    public bool ClientConnected => Client.Connected;

    public NetworkClient Client { get; private set; } = null!;
    
    public Scheduler Scheduler { get; private set; } = null!;

    private const double MANUAL_RECONNECT_COOLDOWN_SECONDS = 5.0;
    private DateTime m_nextManualReconnectAllowedUtc = DateTime.MinValue;

    /// <summary>
    /// Called when the mod is awakened.
    /// </summary>
    void Awake()
    {
        Instance = this;

        Logger.LogInfo($"Loaded {MOD_GUID}. Patching.");
        harmony.PatchAll();
        InitializeGame();

        UI.ModSettings.Initialize(Config);

        Logger.LogInfo("Initializing Crowd Control");

        try
        {
            GameStateManager = new(this);
            Client = new(this);
            EffectLoader = new(this, Client);
            Logger.LogInfo($"Registered {EffectLoader.Effects.Count} effect codes.");
            Scheduler = new(this, Client);

            //after the scheduler, so a custom effect that fires on load has somewhere to go
            CustomEffects = new(this, Client);
            CustomEffects.LoadAll();
        }
        catch (Exception e)
        {
            Logger.LogError($"Crowd Control Init Error: {e}");
        }

        Logger.LogInfo("Crowd Control Initialized");
    }

    void OnApplicationQuit()
    {
        ShutdownGame();
        try
        {
            CustomEffects?.Dispose();
            Client?.Stop();
            Client?.Dispose();
        }
        catch {/**/}
    }

    void OnDestroy()
    {
        try
        {
            CustomEffects?.Dispose();
            Client?.Stop();
            Client?.Dispose();
        }
        catch {/**/}
    }

    /// <summary>Called every rendered frame.</summary>
    /// <remarks>
    /// This game pauses with Time.timeScale = 0, which stops FixedUpdate, so the scheduler is ticked from here
    /// (see <see cref="DeltaTime"/>). This function is called on the main game thread. Blocking here may cause
    /// lag or crash the game entirely.
    /// </remarks>
    void Update()
    {
        if (GameStateManager == null) return; //initialization failed - do nothing rather than throw every tick

        //recompute the game state once per tick (everything else this tick reads the cached value)
        //and report it if it changed - state changes reach the Crowd Control client within one tick
        GameStateManager.InvalidateStateCache();
        GameStateManager.UpdateGameState();

        Scheduler?.Tick();

        //custom effect loading touches Unity through effect constructors, so it happens here rather
        //than on the file watcher's thread
        CustomEffects?.Tick();

        UpdateGame();

        UpdateClientPresence();
        HandleOverlayToggleHotkey();
        HandleManualReconnectHotkey();
    }

    /// <summary>True if the Crowd Control app appears to be running on this machine.</summary>
    /// <remarks>
    /// Checked on a timer rather than every frame - it opens a named semaphore and can fall back to
    /// a process scan, which is far too heavy for OnGUI.
    /// </remarks>
    private bool m_clientPresent;
    private float m_nextClientCheck;

    private const float CLIENT_CHECK_INTERVAL = 5f;
    private System.Threading.Tasks.Task<bool>? m_clientCheckTask;

    /// <summary>Refreshes whether the Crowd Control app is running, at a sane interval.</summary>
    /// <remarks>
    /// While connected the answer is obviously yes. Otherwise the probe (a named semaphore, falling back
    /// to a full process scan) runs on a worker thread - a process scan on the game thread is a visible hitch.
    /// </remarks>
    private void UpdateClientPresence()
    {
        if (ClientConnected)
        {
            m_clientPresent = true;
            return;
        }

        if (m_clientCheckTask != null)
        {
            if (!m_clientCheckTask.IsCompleted) return;
            m_clientPresent = m_clientCheckTask.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && m_clientCheckTask.Result;
            m_clientCheckTask = null;
        }

        float now = Time.realtimeSinceStartup;
        if (now < m_nextClientCheck) return;
        m_nextClientCheck = now + CLIENT_CHECK_INTERVAL;

        NetworkClient? client = Client;
        if (client == null) { m_clientPresent = false; return; }
        m_clientCheckTask = System.Threading.Tasks.Task.Run(() => client.CrowdControlClientFound);
    }

    /// <summary>Draws the connection indicator and any active timed effects.</summary>
    void OnGUI()
    {
        //only show the overlay while actually playing - never over the title screen, loading, or menus.
        //an alt-tabbed game (NotFocused) still shows it, so the streamer can see paused effects from another window.
        try
        {
            if (!loadedIntoWorld || (CGameManager.Instance == null) || !CGameManager.Instance.m_IsGameLevel) return;
            ConnectorLib.JSON.GameState state = GameStateManager?.CurrentState ?? ConnectorLib.JSON.GameState.Unknown;
            if (state is not (ConnectorLib.JSON.GameState.InLevel or ConnectorLib.JSON.GameState.NotFocused)) return;
        }
        catch { return; }

        try { UI.Overlay.Draw(ClientConnected, m_clientPresent); }
        catch {/* never let drawing break the frame */}
    }

    /// <summary>F8 hides or shows the mod's on-screen display.</summary>
    private void HandleOverlayToggleHotkey()
    {
        if (!Input.GetKeyDown(KeyCode.F8)) return;

        bool visible = UI.Overlay.Toggle();
        Logger.LogInfo($"F8 pressed - overlay {(visible ? "shown" : "hidden")}.");

        //forced, so the confirmation is visible even though we may have just turned the UI off
        if (visible) UI.Overlay.Show("Crowd Control display on (F8)", force: true);
    }

    private void HandleManualReconnectHotkey()
    {
        if (!Input.GetKeyDown(KeyCode.F9))
            return;

        DateTime now = DateTime.UtcNow;
        if (now < m_nextManualReconnectAllowedUtc)
            return;

        m_nextManualReconnectAllowedUtc = now.AddSeconds(MANUAL_RECONNECT_COOLDOWN_SECONDS);
        Logger.LogInfo("F9 pressed - manual Crowd Control reconnect requested.");

        if (Client?.RequestReconnect() == true)
        {
            UI.Overlay.Show("Reconnecting to Crowd Control...", force: true);
            Logger.LogInfo("Manual Crowd Control reconnect queued.");
        }
        else
        {
            UI.Overlay.Show("Crowd Control client not found.", force: true);
            Logger.LogInfo("Manual Crowd Control reconnect skipped because the Crowd Control client was not found.");
        }
    }

    /// <summary>
    /// Displays a message to the player using the game's UI/toast system.
    /// </summary>
    /// <remarks>
    /// Goes to the mod's own overlay (see <see cref="UI.Overlay"/>), which works in any game and
    /// respects the streamer's opt-out. If your game has a toast, subtitle, or HUD message system of
    /// its own, call it here as well - a native-looking line in the middle of the screen is easier to
    /// notice than a corner panel, and the two complement each other.
    /// </remarks>
    public void ShowGameUiMessage(string message)
    {
        UI.Overlay.Show(message);
        CreateChatStatusText(message);
    }

    /// <summary>Called by Unity when the application gains or loses focus.</summary>
    /// <remarks>
    /// This pushes a game state update immediately rather than waiting for the next polling interval,
    /// which matters because FixedUpdate may stop running entirely while the game is unfocused.
    /// </remarks>
    void OnApplicationFocus(bool hasFocus)
    {
        try
        {
            GameStateManager?.InvalidateStateCache(); //the cached state predates the focus change
            GameStateManager?.UpdateGameState();
        }
        catch {/**/}
    }

    /// <summary>Called by Unity when the application is paused or resumed by the OS.</summary>
    /// <remarks><inheritdoc cref="OnApplicationFocus" path="/remarks"/></remarks>
    void OnApplicationPause(bool isPaused)
    {
        try
        {
            GameStateManager?.InvalidateStateCache(); //the cached state predates the pause change
            GameStateManager?.UpdateGameState();
        }
        catch {/**/}
    }

    /***** == ONLY USE THIS IF FixedUpdate() ISN'T ALREADY BEING CALLED EVERY TICK == *****/
    //attach this to some game class with a function that runs every frame like the player's Update()
    //[HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.FixedUpdate))]
    //private class PlayerMovement_FixedUpdate { static void Prefix() => Instance.FixedUpdate(); }

    private string? _modVersion = null;
    public string Version
    {
        get
        {
            try
            {
                if (!string.IsNullOrEmpty(_modVersion)) return _modVersion;

                string ccver = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ccver"));
                if (string.IsNullOrEmpty(ccver))
                    _modVersion = MOD_VERSION;
                else
                    _modVersion = ccver;
                return _modVersion;
            }
            catch (Exception e)
            {
                Logger.LogInfo($"Error retrieving mod version: {e}");
                return "0";
            }
        }
    }
}
