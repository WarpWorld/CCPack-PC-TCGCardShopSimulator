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

        Camera cam = FindObjectOfType<Camera>();
        if (cam == null) return;

        currentTextObject = new GameObject("ChatStatusText");
        TextMeshPro chatStatusText = currentTextObject.AddComponent<TextMeshPro>();

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
            mainCamera = Camera.main ?? FindObjectOfType<Camera>();
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

    public static void AddNamePlateToCustomer(Customer customer)
    {
        if (customer.transform.Find("NamePlate") != null)
        {
            return; // Return if the nameplate already exists
        }

        string chatterName = NameOverride;

        if (string.IsNullOrEmpty(chatterName)) return;

        GameObject namePlate = new GameObject("NamePlate");
        namePlate.transform.SetParent(customer.transform);
        namePlate.transform.localPosition = Vector3.up * 1.9f;

        TextMeshPro tmp = namePlate.AddComponent<TextMeshPro>();
        tmp.enabled = true;
        tmp.text = $"<b>{chatterName}</b>";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 1;
        tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
        tmp.outlineColor = Color.black;
        tmp.outlineWidth = 0.2f;

        // Set color based on the smelly condition
        tmp.color = isSmelly ? new Color(0.0f, 1.0f, 0.0f) : Color.white;

        // Attach the NamePlateController component
        namePlate.AddComponent<NamePlateController>();
    }

    public class NamePlateController : MonoBehaviour
    {
        private Camera mainCamera;
        public Transform target;
        public float distanceThreshold = 5f;
        private TextMeshPro tmp;
        private Customer customer;

        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }

            customer = GetComponentInParent<Customer>();
            if (customer == null) return;

            // Get the TextMeshPro component from the customer's children
            tmp = customer.GetComponentInChildren<TextMeshPro>();
        }

        void LateUpdate()
        {
            if (!tmp || !customer || !mainCamera) return;

            float distance = Vector3.Distance(mainCamera.transform.position, customer.transform.position);

            if (distance <= distanceThreshold)
            {
                tmp.enabled = true;
                Vector3 directionToCamera = mainCamera.transform.position - transform.position;
                directionToCamera.y = 0;
                Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
                transform.rotation = lookRotation * Quaternion.Euler(0, 180, 0);

                tmp.color = customer.IsSmelly() ? new Color(0.0f, 1.0f, 0.0f) : Color.white;
            }
            else
            {
                tmp.enabled = false;
            }
        }
    }

    #endregion

    #region Twitch chat listener

    public static void ConnectToTwitchChat()
    {
        if (!isTwitchChatAllowed) return;
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
