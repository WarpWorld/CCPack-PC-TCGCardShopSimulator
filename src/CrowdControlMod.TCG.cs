#nullable disable
using System.IO;
using System.Net.Sockets;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace CrowdControl;

/// <summary>
/// TCG Card Shop Simulator specific state and helpers for the mod. The generic plugin plumbing
/// (network client, scheduler, overlay) lives in CrowdControlMod.cs and is shared with the
/// BepInEx example project.
/// </summary>
public partial class CrowdControlMod
{
    /// <summary>Legacy log handle used throughout the game-specific code.</summary>
    public static BepInEx.Logging.ManualLogSource mls => Instance?.Logger;

    /// <summary>Set once the player controller is enabled, i.e. a save is loaded and the shop scene is live.</summary>
    public static bool loadedIntoWorld = false;

    //effect state flags read by the Harmony patches
    public static bool ForceMath = false;
    public static bool ForceUseCash = false;
    public static bool ForceUseCredit = false;
    public static bool ExactChange = false;
    public static bool LargeBills = false;

    public static bool isSmelly = false;

    public static Vector3 oldcashScale = new Vector3(6.408165f, 41.87513f, 8.071795f);//cash size fix
    public static Vector3 oldcashScaleOutline = new Vector3(6.598893f, 43.12251f, 8.312037f);//cash size fix
    public static Vector3 oldCardScale = new Vector3(5.760878f, 7.510158f, 3.336215f);//card size fix
    public static Vector3 oldCardScaleOutline = new Vector3(5.897457f, 8.062623f, 3.415312f);//card size fix

    public static bool isWarehouseUnlocked = false;

    /// <summary>The viewer name applied to the next customer that spawns (nameplate).</summary>
    public static string NameOverride = "";

    public static string OrgLanguage = "";
    public static string NewLanguage = "";

    // 0 = idle, 1 = auto-firing the pack opener, 2 = finished (patch resets the flags then goes back to 0)
    public static int autoOpenCards = 0;

    /// <summary>
    /// Work queued from background threads (Twitch chat) or deferred by effects, drained on the game thread every frame.
    /// Effects themselves already run on the game thread via the scheduler, so most of them could call the game directly;
    /// the queue is kept so the older effect code keeps working unchanged.
    /// </summary>
    public static readonly Queue<Action> ActionQueue = new();

    #region Twitch chat

    private static bool isChatConnected = false;
    private static bool isTwitchChatAllowed = true;
    private const string twitchServer = "irc.chat.twitch.tv";
    private const int twitchPort = 6667;
    private const string twitchUsername = "justinfan1337";
    public static string twitchChannel = "";
    private static TcpClient twitchTcpClient;
    private static NetworkStream twitchStream;
    private static StreamReader twitchReader;
    private static StreamWriter twitchWriter;

    private static readonly List<string> allowedUsernames = new() { "jaku", "s4turn", "crowdcontrol", "theunknowncod3r" };

    #endregion

    /// <summary>Game-specific initialisation, called from Awake after the Harmony attribute patches are applied.</summary>
    private void InitializeGame()
    {
        Harmony.CustomerManagerPatches.ApplyPatches(harmony);
    }

    /// <summary>Game-specific per-frame work, called from Update.</summary>
    private void UpdateGame()
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {
            isTwitchChatAllowed = !isTwitchChatAllowed;
            if (isChatConnected)
            {
                DisconnectFromTwitch();
                isChatConnected = false;
            }

            string status = isTwitchChatAllowed ? "Twitch Chat is enabled." : "Twitch Chat is disabled.";
            Logger.LogInfo(status);
            CreateChatStatusText(status);
        }

        while (ActionQueue.Count > 0)
        {
            Action action = ActionQueue.Dequeue();
            try { action.Invoke(); }
            catch (Exception e) { Logger.LogError($"Crowd Control action failed: {e}"); }
        }
    }

    private void ShutdownGame()
    {
        try { DisconnectFromTwitch(); }
        catch {/**/}
    }

    #region On-screen text

    public static GameObject currentTextObject = null;

    /// <summary>Shows a short purple message in front of the camera (in addition to the mod overlay).</summary>
    public static void CreateChatStatusText(string message)
    {
        if (!loadedIntoWorld) return;

        if (currentTextObject != null)
        {
            UnityEngine.Object.Destroy(currentTextObject);
        }

        Camera cam = GetPlayerCameraTransform()?.GetComponent<Camera>();
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return;

        currentTextObject = new GameObject("ChatStatusText");
        TextMeshPro chatStatusText = currentTextObject.AddComponent<TextMeshPro>();
        if (!ApplyGameFont(chatStatusText))
        {
            UnityEngine.Object.Destroy(currentTextObject);
            currentTextObject = null;
            return;
        }

        chatStatusText.fontSize = 0.05f;
        chatStatusText.color = new Color(0.5f, 0, 1);
        chatStatusText.alignment = TextAlignmentOptions.Center;
        chatStatusText.text = message;
        chatStatusText.lineSpacing = 1.2f;

        Vector3 screenCenterPosition = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.6f, 0.15f));
        currentTextObject.transform.position = screenCenterPosition;

        currentTextObject.transform.SetParent(cam.transform, true);
        currentTextObject.AddComponent<FaceCamera>();

        UnityEngine.Object.Destroy(currentTextObject, 3f);
    }

    public class FaceCamera : MonoBehaviour
    {
        private Camera mainCamera;

        void Start()
        {
            mainCamera = GetPlayerCameraTransform()?.GetComponent<Camera>() ?? Camera.main ?? FindObjectOfType<Camera>();
        }

        void LateUpdate()
        {
            if (mainCamera == null) return;

            Vector3 directionToCamera = mainCamera.transform.position - transform.position;
            directionToCamera.y = 0;
            directionToCamera.Normalize();

            Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
            transform.rotation = lookRotation * Quaternion.Euler(0, 180, 0);
        }
    }

    #endregion

    #region Customer nameplates

    public static void MakeCustomerSmellyTemporarily(Customer customer, float duration)
    {
        if (!customer.enabled) return;
        if (!customer.IsInsideShop()) return;
        customer.SetSmelly();

        Timer timer = new Timer(_ => ActionQueue.Enqueue(() => ClearSmellyStatus(customer)), null, (int)(duration * 1000), Timeout.Infinite);
    }

    private static void ClearSmellyStatus(Customer customer)
    {
        customer.m_SmellyFX.SetActive(false);
        customer.m_CleanFX.SetActive(true);
        GameActions.setProperty(customer, "m_IsSmelly", false);
        CSingleton<CustomerManager>.Instance.RemoveFromSmellyCustomerList(customer);
    }

    /// <summary>
    /// Gives a freshly created TextMeshPro component a font the game is actually using.
    /// </summary>
    /// <remarks>
    /// Since the game upgraded its TextMeshPro package, <c>TMP_Settings.defaultFontAsset</c> is broken
    /// (null material), so a text component created from code has no usable font and touching
    /// <c>fontMaterial</c> throws. Returns false when no game font could be found.
    /// </remarks>
    private static bool ApplyGameFont(TMP_Text text)
    {
        try
        {
            if (text.font != null && text.font.material != null && text.fontSharedMaterial != null) return true;

            TMP_FontAsset font = GameActions.GetGameFontAsset();
            if (font == null)
            {
                mls?.LogWarning("No usable TextMeshPro font found in the game; skipping on-screen text.");
                return false;
            }
            text.font = font;
            text.fontSharedMaterial = font.material;
            return true;
        }
        catch (Exception e)
        {
            mls?.LogWarning($"Could not assign a font to on-screen text: {e.Message}");
            return false;
        }
    }

    private static bool loggedNamePlateLayout = false;

    public static void AddNamePlateToCustomer(Customer customer)
    {
        if (customer.GetComponent<NamePlateOwner>() != null)
        {
            return; // Return if the nameplate already exists
        }

        string chatterName = NameOverride;

        if (string.IsNullOrEmpty(chatterName)) return;

        Color color = isSmelly ? new Color(0.0f, 1.0f, 0.0f) : Color.white;

        // Preferred: clone one of the game's own text popups (the "chat bubble" objects PricePopupSpawner
        // floats above customers) and drive it the way the game drives the original - left in the same
        // hierarchy, moved to the customer every frame and turned to face the camera. That is the one
        // world-space text setup this game is known to render, and after the TextMeshPro upgrade a text
        // component built from code has not been.
        GameObject namePlate = TryCloneGamePopup(customer, chatterName, color);

        if (namePlate == null)
        {
            namePlate = new GameObject("NamePlate");
            namePlate.transform.SetParent(customer.transform);
            namePlate.transform.localPosition = Vector3.up * 1.9f;
            namePlate.transform.localRotation = Quaternion.identity;

            TextMeshPro tmp = namePlate.AddComponent<TextMeshPro>();
            if (!ApplyGameFont(tmp))
            {
                UnityEngine.Object.Destroy(namePlate);
                return;
            }

            tmp.enabled = true;
            tmp.richText = false;
            tmp.fontStyle = FontStyles.Bold;
            tmp.text = chatterName;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 1;

            try
            {
                // the outline lives on a per-instance material; not every game font shader supports it
                tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
                tmp.outlineColor = Color.black;
                tmp.outlineWidth = 0.2f;
            }
            catch (Exception e)
            {
                mls?.LogDebug($"Nameplate outline not applied: {e.Message}");
            }

            tmp.color = color;
            NamePlateController fallbackController = namePlate.AddComponent<NamePlateController>();
            fallbackController.mimicGamePopup = false;
            fallbackController.customer = customer;
            customer.gameObject.AddComponent<NamePlateOwner>().namePlate = namePlate;
            mls?.LogInfo($"Nameplate '{chatterName}' attached to customer (fallback 3D text, font '{tmp.font?.name}', shader '{tmp.fontSharedMaterial?.shader?.name}').");
            return;
        }

        NamePlateController controller = namePlate.AddComponent<NamePlateController>();
        controller.mimicGamePopup = true;
        controller.customer = customer;
        customer.gameObject.AddComponent<NamePlateOwner>().namePlate = namePlate;
        mls?.LogInfo($"Nameplate '{chatterName}' attached to customer.");
    }

    /// <summary>Builds a nameplate by cloning one of the game's floating text popups. Null if that isn't possible.</summary>
    private static GameObject TryCloneGamePopup(Customer customer, string chatterName, Color color)
    {
        try
        {
            PricePopupSpawner spawner = CSingleton<PricePopupSpawner>.Instance;
            PricePopupUI template = null;
            if (spawner != null && spawner.m_PricePopupList != null)
            {
                foreach (PricePopupUI popup in spawner.m_PricePopupList)
                {
                    if (popup != null && popup.m_Text != null) { template = popup; break; }
                }
            }
            if (template == null) return null;

            // same parent as the real popups, so the clone inherits exactly the same canvas/scale/layer context
            GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent, false);
            clone.name = "NamePlate";
            clone.SetActive(false);
            clone.transform.localScale = template.transform.localScale;

            // strip every script the popup carries (PricePopupUI, and DeactivateAfterTime which would switch
            // the plate off again a moment later) plus anything that would animate it away; keep only the text
            foreach (MonoBehaviour c in clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (c is TMP_Text) continue;
                UnityEngine.Object.DestroyImmediate(c);
            }
            foreach (Animator c in clone.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(c);
            foreach (Animation c in clone.GetComponentsInChildren<Animation>(true)) UnityEngine.Object.DestroyImmediate(c);
            foreach (CanvasGroup g in clone.GetComponentsInChildren<CanvasGroup>(true)) { g.alpha = 1f; g.enabled = true; }

            TMP_Text text = clone.GetComponentInChildren<TMP_Text>(true);
            if (text == null) { UnityEngine.Object.Destroy(clone); return null; }
            // the game's popup text has rich text turned off, so tags would show literally - use the style flag
            text.richText = false;
            text.fontStyle |= FontStyles.Bold;
            text.text = chatterName;
            text.color = color;
            text.enabled = true;

            clone.transform.position = customer.transform.position + Vector3.up * NamePlateController.HEIGHT;
            clone.SetActive(true);

            if (!loggedNamePlateLayout)
            {
                loggedNamePlateLayout = true;
                string comps = string.Join(", ", clone.GetComponentsInChildren<Component>(true).Select(c => c.GetType().Name).Distinct());
                mls?.LogInfo($"Nameplate cloned from popup '{template.name}' (parent '{template.transform.parent?.name}'): lossyScale={clone.transform.lossyScale}, layer={clone.layer}, font='{text.font?.name}', shader='{text.fontSharedMaterial?.shader?.name}', components=[{comps}]");
            }
            return clone;
        }
        catch (Exception e)
        {
            mls?.LogWarning($"Could not clone a game popup for the nameplate, using fallback text: {e.Message}");
            return null;
        }
    }

    /// <summary>Marks a customer that already has a nameplate, and takes the plate down when the customer goes away.</summary>
    public class NamePlateOwner : MonoBehaviour
    {
        public GameObject namePlate;

        void OnDisable()
        {
            // customers are pooled: a deactivated customer is a despawned one, and the plate must not outlive it
            if (namePlate != null) UnityEngine.Object.Destroy(namePlate);
            UnityEngine.Object.Destroy(this);
        }

        void OnDestroy()
        {
            if (namePlate != null) UnityEngine.Object.Destroy(namePlate);
        }
    }

    /// <summary>
    /// The transform of the camera the player actually looks through. The game's popup spawner keeps a
    /// reference to it; <c>Camera.main</c> is not reliable here because the game has several cameras
    /// (the card battle feature added its own) and whichever one Unity returns may sit far from the shop.
    /// </summary>
    public static Transform GetPlayerCameraTransform()
    {
        try
        {
            PricePopupSpawner spawner = CSingleton<PricePopupSpawner>.Instance;
            if (spawner != null && spawner.m_Cam != null) return spawner.m_Cam;
        }
        catch {/* fall through */}
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        return cam != null ? cam.transform : null;
    }

    public class NamePlateController : MonoBehaviour
    {
        public const float HEIGHT = 1.9f;

        private Transform cameraTransform;
        /// <summary>Metres from the player camera beyond which the plate is hidden. The shop floor is roughly this deep.</summary>
        public float distanceThreshold = 15f;
        public bool mimicGamePopup = true;
        public Customer customer;
        private TMP_Text tmp;
        private static bool loggedFirstFrame = false;

        void Start()
        {
            cameraTransform = GetPlayerCameraTransform();
            if (customer == null) customer = GetComponentInParent<Customer>();

            // the nameplate's own text (customers can carry other text children)
            tmp = GetComponentInChildren<TMP_Text>(true);
        }

        void LateUpdate()
        {
            if (customer == null || !customer.gameObject.activeInHierarchy)
            {
                UnityEngine.Object.Destroy(gameObject);
                return;
            }
            if (!tmp) return;
            if (cameraTransform == null)
            {
                cameraTransform = GetPlayerCameraTransform();
                if (cameraTransform == null) return;
            }

            float distance = Vector3.Distance(cameraTransform.position, customer.transform.position);
            bool visible = distance <= distanceThreshold;
            tmp.enabled = visible;

            if (visible)
            {
                if (mimicGamePopup)
                {
                    // exactly what PricePopupSpawner does with its popups every frame
                    transform.position = customer.transform.position + Vector3.up * HEIGHT;
                    transform.LookAt(cameraTransform.position);
                }
                else
                {
                    Vector3 directionToCamera = cameraTransform.position - transform.position;
                    directionToCamera.y = 0;
                    Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
                    transform.rotation = lookRotation * Quaternion.Euler(0, 180, 0);
                }

                tmp.color = customer.IsSmelly() ? new Color(0.0f, 1.0f, 0.0f) : Color.white;
            }

            if (!loggedFirstFrame)
            {
                loggedFirstFrame = true;
                mls?.LogInfo($"Nameplate first frame: camera '{cameraTransform.name}' at {cameraTransform.position}, customer at {customer.transform.position}, distance {distance:F1} m, visible={visible}, textActive={tmp.isActiveAndEnabled}, plateActive={gameObject.activeInHierarchy}, text='{tmp.text}'");
            }
        }
    }

    #endregion

    #region Twitch chat listener

    /// <summary>
    /// Records the Twitch channel to listen to. The first non-empty name wins while the listener is
    /// running, so a config override is not replaced by what an effect request carries.
    /// </summary>
    public static void SetTwitchChannel(string channel, string source)
    {
        channel = (channel ?? "").Trim().TrimStart('#').ToLowerInvariant();
        if (channel.Length == 0 || twitchChannel == channel) return;
        if (twitchChannel.Length > 0 && isChatConnected) return;
        twitchChannel = channel;
        mls?.LogInfo($"Twitch channel set to '{channel}' (from {source}).");
    }

    public static void ConnectToTwitchChat()
    {
        if (!isTwitchChatAllowed) return;
        if (twitchChannel.Length == 0) SetTwitchChannel(UI.ModSettings.TwitchChannel, "config");
        if (!isChatConnected && twitchChannel.Length >= 1)
        {
            new Thread(new ThreadStart(StartTwitchChatListener)) { IsBackground = true, Name = "CrowdControl-TwitchChat" }.Start();
            isChatConnected = true;
        }
    }

    public static void StartTwitchChatListener()
    {
        try
        {
            twitchTcpClient = new TcpClient(twitchServer, twitchPort);
            twitchStream = twitchTcpClient.GetStream();
            twitchReader = new StreamReader(twitchStream);
            twitchWriter = new StreamWriter(twitchStream);

            // Request membership and tags capabilities from Twitch
            twitchWriter.WriteLine("CAP REQ :twitch.tv/membership twitch.tv/tags");

            twitchWriter.WriteLine($"NICK {twitchUsername}");
            twitchWriter.WriteLine($"JOIN #{twitchChannel}");
            twitchWriter.Flush();

            mls.LogInfo($"Connected to Twitch channel: {twitchChannel}");

            while (true)
            {
                if (twitchStream.DataAvailable)
                {
                    var message = twitchReader.ReadLine();
                    if (message != null)
                    {
                        if (message.StartsWith("PING"))
                        {
                            twitchWriter.WriteLine("PONG :tmi.twitch.tv");
                            twitchWriter.Flush();
                        }
                        else if (message.Contains("PRIVMSG"))
                        {
                            var messageParts = message.Split(new[] { ' ' }, 4);
                            if (messageParts.Length >= 4)
                            {
                                var rawUsername = messageParts[1];
                                string username = rawUsername.Substring(1, rawUsername.IndexOf('!') - 1);
                                int messageStartIndex = message.IndexOf("PRIVMSG");
                                if (messageStartIndex >= 0)
                                {
                                    string chatMessage = messageParts[3].Substring(1);
                                    string[] chatParts = chatMessage.Split(new[] { " :" }, 2, StringSplitOptions.None);
                                    chatMessage = chatParts[1];

                                    var badges = ParseBadges(messageParts[0]);

                                    string badgeDisplay = "";
                                    if (badges.Contains("broadcaster")) badgeDisplay = "[BROADCASTER]";
                                    else if (badges.Contains("moderator")) badgeDisplay = "[MODERATOR]";
                                    else if (badges.Contains("vip")) badgeDisplay = "[VIP]";
                                    else if (badges.Contains("subscriber")) badgeDisplay = "[SUBSCRIBER]";

                                    string[] triggerWords = { "fart", "gas", "burp", "smell", "shit", "poo", "stank", "nasty" };

                                    if (!string.IsNullOrEmpty(badgeDisplay) || allowedUsernames.Any(name => name.ToLower().Equals(username, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        ActionQueue.Enqueue(() =>
                                        {
                                            try
                                            {
                                                List<Customer> customers = CSingleton<CustomerManager>.Instance.GetCustomerList();

                                                if (customers != null && customers.Count >= 1)
                                                {
                                                    foreach (Customer customer in customers)
                                                    {
                                                        if (customer.isActiveAndEnabled && customer.name.ToLower() == username.ToLower())
                                                        {
                                                            string lowerChatMessage = chatMessage.ToLower();
                                                            if (triggerWords.Any(word => lowerChatMessage.Contains(word)))
                                                            {
                                                                if (!customer.IsSmelly())
                                                                {
                                                                    MakeCustomerSmellyTemporarily(customer, 5f);
                                                                }
                                                            }
                                                            CSingleton<PricePopupSpawner>.Instance.ShowTextPopup(EffectRequestEx.SanitizeDisplayName(chatMessage), 1.8f, customer.transform);
                                                        }
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                                //the customer may have despawned
                                            }
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(50);
            }
        }
        catch (Exception e) when (e is ObjectDisposedException || e is IOException || e is SocketException)
        {
            mls?.LogInfo("Twitch chat listener stopped.");
        }
        catch (Exception e)
        {
            mls?.LogInfo($"Twitch Chat Listener Error: {e}");
        }
    }

    public static void DisconnectFromTwitch()
    {
        try
        {
            if (twitchWriter != null && twitchChannel.Length >= 1)
            {
                twitchWriter.WriteLine("PART #" + twitchChannel);
                twitchWriter.Flush();
                twitchWriter.Close();
            }

            twitchReader?.Close();
            twitchStream?.Close();
            twitchTcpClient?.Close();

            mls?.LogInfo("Disconnected from Twitch chat.");
        }
        catch (Exception e)
        {
            mls?.LogError($"Error disconnecting from Twitch: {e.Message}");
        }
    }

    public static HashSet<string> ParseBadges(string tagsPart)
    {
        var badgesSet = new HashSet<string>();
        var tags = tagsPart.Split(';');

        foreach (var tag in tags)
        {
            if (tag.StartsWith("badges="))
            {
                var badges = tag.Substring("badges=".Length).Split(',');
                foreach (var badge in badges)
                {
                    var badgeType = badge.Split('/')[0];
                    badgesSet.Add(badgeType);
                }
            }
        }

        return badgesSet;
    }

    #endregion
}
