#nullable disable
using BepInEx;
using ConnectorLib.JSON;
using Newtonsoft.Json.Linq;
using CMF;
using HeathenEngineering.SteamworksIntegration;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TMPro;
using UnityEngine;
using System.Collections;


namespace CrowdControl
{



    public class GameActions
    {
        public static System.Random rnd = new System.Random();
        public static int maxBoxCount = 100;
        private const string FoodBundleName = "food";
        private const string FoodBundleMilkAssetName = "MilkGroup";
        private const string FoodBundleBreadAssetName = "BreadGroup";
        private const string HypeTrainBundleName = "warpworld.hypetrain";
        private const string HypeTrainAssetName = "HypeTrain";

        public AssetBundle bundle; // Make sure this is assigned when the plugin loads

        private static GameObject breadPrefab;
        private static GameObject milkPrefab;
        private static GameObject trainPrefab;

        private static GameObject hypetrainPrefab;


        // Static flag to ensure assets are loaded only once
        private static bool loaded = false;

        private static void LogInfo(string message)
        {
            CrowdControlMod.mls?.LogInfo("[GameActions] " + message);
        }

        private static void LogWarning(string message)
        {
            CrowdControlMod.mls?.LogWarning("[GameActions] " + message);
        }

        private static void LogError(string message)
        {
            CrowdControlMod.mls?.LogError("[GameActions] " + message);
        }

        private static string DescribePrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                return "null prefab";
            }

            int colliderCount = prefab.GetComponentsInChildren<Collider>(true).Length;
            int rigidbodyCount = prefab.GetComponentsInChildren<Rigidbody>(true).Length;
            int rendererCount = prefab.GetComponentsInChildren<Renderer>(true).Length;
            int audioSourceCount = prefab.GetComponentsInChildren<AudioSource>(true).Length;

            return $"{prefab.name} | children={prefab.transform.childCount}, colliders={colliderCount}, rigidbodies={rigidbodyCount}, renderers={rendererCount}, audioSources={audioSourceCount}";
        }

        private static void ValidateFoodPrefab(string label, GameObject prefab)
        {
            if (prefab == null)
            {
                LogError($"{label} prefab is null after bundle load.");
                return;
            }

            Collider[] colliders = prefab.GetComponentsInChildren<Collider>(true);
            Rigidbody[] rigidbodies = prefab.GetComponentsInChildren<Rigidbody>(true);

            LogInfo($"{label} prefab loaded: {DescribePrefab(prefab)}");

            if (colliders.Length == 0)
            {
                LogWarning($"{label} prefab has no colliders. Mouse pickup raycasts will never hit it.");
            }

            if (rigidbodies.Length == 0)
            {
                LogWarning($"{label} prefab has no rigidbody. Current pickup code expects RaycastHit.rigidbody.");
            }

            if (prefab.transform.childCount == 0)
            {
                LogWarning($"{label} prefab has no child transforms. Hold/eat animation code currently expects child index 0.");
            }
        }

        private static void ValidateSpawnedFoodInstance(string label, GameObject instance)
        {
            if (instance == null)
            {
                LogError($"{label} instance is null right after instantiate.");
                return;
            }

            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            Rigidbody[] rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);
            AudioSource[] audioSources = instance.GetComponentsInChildren<AudioSource>(true);

            LogInfo($"{label} spawned at {instance.transform.position}: {DescribePrefab(instance)}");

            if (colliders.Length == 0)
            {
                LogWarning($"{label} instance has no colliders.");
            }

            if (rigidbodies.Length == 0)
            {
                LogWarning($"{label} instance has no rigidbody; pickup will likely fail.");
            }

            if (audioSources.Length == 0)
            {
                LogWarning($"{label} instance has no AudioSource. Eat SFX will be skipped.");
            }
        }

        // Load all assets from the bundle and store them
        public void LoadAssetsFromBundle()
        {
            if (loaded && breadPrefab != null && milkPrefab != null && hypetrainPrefab != null)
            {
                return;
            }

            if (loaded)
            {
                LogWarning("Asset load was previously marked complete, but one or more cached prefabs are missing. Re-loading bundles.");
                loaded = false;
            }

            string pluginFolder = System.IO.Path.Combine(Paths.PluginPath, "CrowdControl");
            string foodBundlePath = System.IO.Path.Combine(pluginFolder, FoodBundleName);
            string hypeTrainBundlePath = System.IO.Path.Combine(pluginFolder, HypeTrainBundleName);

            LogInfo($"Loading food bundle from '{foodBundlePath}' (exists={File.Exists(foodBundlePath)})");
            bundle = AssetBundle.LoadFromFile(foodBundlePath);
            if (bundle == null)
            {
                LogError($"Failed to load food AssetBundle from '{foodBundlePath}'.");
                return;
            }

            LogInfo($"Loaded food bundle '{bundle.name}' with {bundle.GetAllAssetNames().Length} assets.");

            milkPrefab = bundle.LoadAsset<GameObject>(FoodBundleMilkAssetName);
            breadPrefab = bundle.LoadAsset<GameObject>(FoodBundleBreadAssetName);
            ValidateFoodPrefab("Milk", milkPrefab);
            ValidateFoodPrefab("Bread", breadPrefab);

            HypeTrainBoxData boxData = new HypeTrainBoxData();// Do this to load the dll... maybe do something different, but this works for now
            LogInfo($"Loading HypeTrain bundle from '{hypeTrainBundlePath}' (exists={File.Exists(hypeTrainBundlePath)})");
            bundle = AssetBundle.LoadFromFile(hypeTrainBundlePath);
            if (bundle == null)
            {
                LogError($"Failed to load HypeTrain AssetBundle from '{hypeTrainBundlePath}'.");
                return;
            }

            hypetrainPrefab = bundle.LoadAsset<GameObject>(HypeTrainAssetName);

            if (hypetrainPrefab == null)
            {
                LogError("HypeTrain prefab not found in AssetBundle.");
            }
            else
            {
                LogInfo($"HypeTrain prefab loaded: {DescribePrefab(hypetrainPrefab)}");
            }

            loaded = true; 
            LogInfo("Asset bundle load completed.");
        }

        public static Color ConvertUserNameToColor(string userName)
        {
            
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(userName));

                float r = hashBytes[0] / 255f;
                float g = hashBytes[1] / 255f;
                float b = hashBytes[2] / 255f;

                return new Color(r, g, b);
            }
        }

        private static TMP_FontAsset fallbackFontAsset;

        // Font assets serialized inside our asset bundles can come back with a null material after the game
        // upgrades its TextMeshPro package, which makes TMP throw NullReferenceException every frame while
        // rendering. Swap any broken text component over to a font asset the game itself is using.
        public static TMP_FontAsset GetGameFontAsset()
        {
            if (fallbackFontAsset != null && fallbackFontAsset.material != null) return fallbackFontAsset;

            TMP_FontAsset candidate = TMP_Settings.defaultFontAsset;
            if (candidate == null || candidate.material == null)
            {
                candidate = null;
                foreach (TMP_Text text in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
                {
                    if (text.font != null && text.font.material != null)
                    {
                        candidate = text.font;
                        break;
                    }
                }
            }

            fallbackFontAsset = candidate;
            return fallbackFontAsset;
        }

        public static void FixBundleTextFonts(GameObject instance, string label)
        {
            if (instance == null) return;
            try
            {
                TMP_Text[] texts = instance.GetComponentsInChildren<TMP_Text>(true);
                int fixedCount = 0;
                foreach (TMP_Text text in texts)
                {
                    bool broken = text.font == null || text.font.material == null || text.fontSharedMaterial == null;
                    if (!broken) continue;

                    TMP_FontAsset font = GetGameFontAsset();
                    if (font == null)
                    {
                        LogWarning($"{label}: text '{text.name}' has a broken font asset and no game font was found to replace it. Disabling the text.");
                        text.enabled = false;
                        continue;
                    }
                    text.font = font;
                    text.fontSharedMaterial = font.material;
                    fixedCount++;
                }
                if (fixedCount > 0)
                {
                    LogInfo($"{label}: replaced the font on {fixedCount}/{texts.Length} text component(s) with '{GetGameFontAsset()?.name}'.");
                }
            }
            catch (Exception e)
            {
                LogError($"{label}: failed to fix text fonts: {e}");
            }
        }

        public void Spawn_HypeTrain(Vector3 position, Quaternion rotation, HypeTrainSourceDetails sourceDetails)
        {


            if (hypetrainPrefab != null)
            {
                /*for (int i = 0; i < 32; ++i)
                {
                    try
                    {
                        Debug.Log($"Layer {i} is: {LayerMask.LayerToName(i)}");
						for (int j = 0; j < 32; ++j)
						{
							try
							{
                                if (i != j)
                                {
                                    Debug.Log($"Collide with  {LayerMask.LayerToName(j)}: {Physics.GetIgnoreLayerCollision(i, j)}");
                                }
							}
							catch { }
						}
					}
                    catch { }
                }*/


                if (sourceDetails == null || sourceDetails.TopContributions == null || sourceDetails.TopContributions.Count == 0)
                {
                    CrowdControlMod.mls.LogInfo("No top_contributions?");
                    return;
                }

                GameObject hypeTrainInstance = UnityEngine.Object.Instantiate(hypetrainPrefab, position, rotation);
                FixBundleTextFonts(hypeTrainInstance, "HypeTrain");
                HypeTrain hypeTrain = hypeTrainInstance.GetComponent<HypeTrain>();
                if (null == hypeTrain)
                {
                    Debug.LogError("No Train?");
                }
                else
                {
                    Vector3 initialStartOffset = new Vector3(-14.5f, 0.2f, 6.0f); // Further away by 2 units
                    Vector3 initialStopOffset = new Vector3(14.5f, 0.2f, 6.0f); // Further away by 2 units

                    Transform playerCamera = Camera.main?.transform;

                    if (playerCamera == null)
                    {
                        playerCamera = UnityEngine.Object.FindObjectOfType<Camera>()?.transform;
                        if (playerCamera == null)
                        {
                            return;
                        }
                    }

                    Transform playerTransform = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;

                    Vector3 startPos = playerTransform.position + playerCamera.TransformDirection(initialStartOffset);
                    startPos.y = playerTransform.position.y;

                    Vector3 stopPos = playerTransform.position + playerCamera.TransformDirection(initialStopOffset);
                    stopPos.y = playerTransform.position.y;

                    List<HypeTrainBoxData> hypeTrainBoxDataList = new List<HypeTrainBoxData>();

                    foreach (var contribution in sourceDetails.TopContributions)
                    {
                        hypeTrainBoxDataList.Add(new HypeTrainBoxData()
                        {
                            name = contribution.UserName,
                            box_color = ConvertUserNameToColor(contribution.UserName),
                            bit_amount = contribution.Type == "bits" ? contribution.Total : 0 // Only set bit_amount if the contribution is bits
                        });
                    }

                    bool isLastContributionInTop = sourceDetails.LastContribution != null;

                    // Only add last train car if the last_contribution user_id is not in top_contributions
                    if (isLastContributionInTop)
                    {
                        hypeTrainBoxDataList.Add(new HypeTrainBoxData()
                        {
                            name = sourceDetails.LastContribution.UserName,
                            box_color = ConvertUserNameToColor(sourceDetails.LastContribution.UserName),
                            bit_amount = sourceDetails.LastContribution.Type == "bits" ? sourceDetails.LastContribution.Total : 0
                        });
                    }

                    float defaultSpeed = 1f;
                    float speedIncrease = sourceDetails.Level * 0.1f;
                    float distance_per_second = Mathf.Min(defaultSpeed + speedIncrease, 10f);

                    // Now call StartHypeTrain with the generated hypeTrainBoxDataList
                    hypeTrain.StartHypeTrain(startPos, stopPos, hypeTrainBoxDataList.ToArray(), playerTransform,
                    new HypeTrainOptions()
                    {
                        train_layer = LayerMask.NameToLayer("Obstacles"),
                        max_bits_per_car = 100,
                        volume = SoundManager.SFXVolume,
                        distance_per_second = distance_per_second
                    });

                }
            }
            
        }

        // Method to spawn the bread asset
        public void Spawn_Bread(Vector3 position, Quaternion rotation)
        {
            if (breadPrefab == null)
            {
                LogError($"Bread prefab not loaded. Spawn request at {position} cannot continue.");
                return;
            }

            GameObject breadInstance = UnityEngine.Object.Instantiate(breadPrefab, position, rotation);
            InteractableObject2 interactableObject = breadInstance.GetComponent<InteractableObject2>();
            if (interactableObject == null)
            {
                interactableObject = breadInstance.AddComponent<InteractableObject2>();
            }

            interactableObject.SetDebugLabel("Bread");
            ValidateSpawnedFoodInstance("Bread", breadInstance);
        }


        // Method to spawn the milk asset
        public void Spawn_Milk(Vector3 position, Quaternion rotation)
        {
            if (milkPrefab == null)
            {
                LogError($"Milk prefab not loaded. Spawn request at {position} cannot continue.");
                return;
            }

            GameObject milkInstance = UnityEngine.Object.Instantiate(milkPrefab, position, rotation);
            InteractableObject2 interactableObject = milkInstance.GetComponent<InteractableObject2>();
            if (interactableObject == null)
            {
                interactableObject = milkInstance.AddComponent<InteractableObject2>();
            }

            interactableObject.SetDebugLabel("Milk");
            ValidateSpawnedFoodInstance("Milk", milkInstance);
        }


        public static EffectResponse ToggleLights(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";

            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    LightManager.Instance.ToggleShopLight();
                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse HeyOhh(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";

            List<Customer> customers = (List<Customer>)getProperty(CSingleton<CustomerManager>.Instance, "m_CustomerList");
            CustomerManager customerManager = CSingleton<CustomerManager>.Instance;
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {

                    if (customers == null)
                    {
                        CrowdControlMod.mls.LogInfo("Customer list not found.");
                        return;
                    }

                    // Loop through the customer list and add each customer to the smelly customer list
                    foreach (Customer customer in customers)
                    {
                        if (customer.isActiveAndEnabled)
                        {
                            List<string> textList = new List<string> { "heyooo" };
                            setProperty(customer, "m_IsChattyCustomer", true);
                            CSingleton<PricePopupSpawner>.Instance.ShowTextPopup(textList[0], 1.8f, customer.transform);
                        }
                    }

                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }

        /// <summary>
        /// True when a viewer-spawned customer should be refused. Counts customers the way the game does
        /// (those at the play table or in a tournament don't count) against the configurable cap; the game
        /// keeps its shop topped up to its own daily limit, so that limit is not used here.
        /// </summary>
        private static bool IsShopFullForSpawn()
        {
            int cap = UI.ModSettings.SpawnCustomerCap;
            if (cap <= 0) return false;
            CustomerManager cm = CustomerManager.Instance;
            int inShop = cm.m_TotalCurrentCustomerCount - cm.m_TotalPlaytableCustomerCount - cm.m_TotalTournamentCustomerCount;
            return inShop >= cap;
        }

        /// <summary>Picks up the streamer's Twitch channel from the request's target list, when the app sends one.</summary>
        private static bool loggedUnknownTargets = false;

        private static void ApplyTwitchChannel(EffectRequest req)
        {
            try
            {
                if (req.targets == null || req.targets.Count == 0) return;
                foreach (JToken target in req.targets)
                {
                    string service = (target?["profile"] ?? target?["service"] ?? target?["type"] ?? target?["platform"])?.ToString() ?? "";
                    if (!service.ToLowerInvariant().Contains("twitch")) continue;
                    string name = (target["name"] ?? target["login"] ?? target["channel"] ?? target["displayName"])?.ToString();
                    CrowdControlMod.SetTwitchChannel(name, "effect request");
                    return;
                }
                if (!loggedUnknownTargets)
                {
                    loggedUnknownTargets = true;
                    CrowdControlMod.mls?.LogInfo($"Effect targets carried no Twitch channel: {req.targets.ToString(Newtonsoft.Json.Formatting.None, Array.Empty<Newtonsoft.Json.JsonConverter>())}");
                }
            }
            catch (Exception e)
            {
                CrowdControlMod.mls?.LogDebug($"Could not read the Twitch channel from the request: {e.Message}");
            }
        }

        public static EffectResponse SpawnCustomer(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            CustomerManager CM = CustomerManager.Instance;
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            if (IsShopFullForSpawn()) return EffectResponse.Retry(req.ID, "Too Many Customers in Shop");
            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return EffectResponse.Retry(req.ID, "Store is Closed");
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    ApplyTwitchChannel(req);
                    string viewerName = req.GetViewerDisplayName();
                    CrowdControlMod.NameOverride = viewerName;
                    CrowdControlMod.isSmelly = false;
                    CustomerManager.Instance.m_CustomerCountMax += 1;
                    callFunc(CustomerManager.Instance, "AddCustomerPrefab", null);
                    Customer newCustomer = CM.GetNewCustomer(false);//spawn not smelly
                    CrowdControlMod.NameOverride = "";

                    if (newCustomer != null) newCustomer.name = viewerName;
                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse SpawnCustomerSmelly(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            CustomerManager CM = CustomerManager.Instance;
            if (IsShopFullForSpawn()) return EffectResponse.Retry(req.ID, "Too Many Customers in Shop");
            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return EffectResponse.Retry(req.ID, "Store is Closed");
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {

                    ApplyTwitchChannel(req);
                    string viewerName = req.GetViewerDisplayName();
                    CrowdControlMod.isSmelly = true;
                    CrowdControlMod.NameOverride = viewerName;
                    Customer Smelly = CM.GetNewCustomer(true);//Spawn him as Smelly
                    CrowdControlMod.NameOverride = "";
                    if (Smelly != null)
                    {
                        Smelly.SetSmelly();
                        CustomerManager.Instance.AddToSmellyCustomerList(Smelly);
                        Smelly.name = viewerName;
                    }
                    CrowdControlMod.isSmelly = false;


                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }
        public static EffectResponse HireWorker(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            bool found = false;
            int workerCount = 0;
            Worker workerid = null;
            List<Worker> m_WorkerList = WorkerManager.GetWorkerList();
            if (!found)
            {
                try
                {
                    Worker worker2 = m_WorkerList.Find(x => !x.m_IsActive);
                    workerCount = m_WorkerList.IndexOf(worker2);
                    if (worker2 != null)
                    {
                        workerid = m_WorkerList[workerCount];
                        found = true;
                    }
                }
                catch
                {
                    status = EffectStatus.Retry;
                }

            }
            if (!found || workerid == null) return EffectResponse.Retry(req.ID, "No Workers available");
            if (found)
            {
                try
                {
                    CrowdControlMod.ActionQueue.Enqueue(() =>
                    {
                        WorkerManager.Instance.ActivateWorker(workerCount,true);
                        workerid.m_IsActive = true;
                        //workerid.name = req.viewer; worker tags
                        workerid.gameObject.SetActive(true);
                        workerid.transform.position = InteractionPlayerController.Instance.m_WalkerCtrl.transform.position;
                        CPlayerData.SetIsWorkerHired(workerCount, true);
                    });
                }
                catch (Exception e)
                {
                    CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                    status = EffectStatus.Retry;
                }
            }
            return new EffectResponse(req.ID, status, message);
        }
        public static EffectResponse FireWorker(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            bool found = false;
            Worker workerid = null;
            int workerCount = 0;
            List<Worker> m_WorkerList = WorkerManager.GetWorkerList();
            if (!found)
            {
                try
                {
                    Worker worker2 = m_WorkerList.Find(x=>x.m_IsActive);
                    workerCount = m_WorkerList.IndexOf(worker2);
                    if (worker2 != null && !LightManager.GetHasDayEnded())
                    {
                        workerid = m_WorkerList[workerCount];
                        found = true;
                    }
                }
                catch
                {
                    status = EffectStatus.Retry;
                }

            }
            if (!found || workerid == null) return EffectResponse.Retry(req.ID, "No Workers available");
            if (found)
            {
                try
                {
                    CrowdControlMod.ActionQueue.Enqueue(() =>
                    {
                        workerid.FireWorker();
                        workerid.DeactivateWorker();
                        workerid.m_IsActive = false;
                        workerid.gameObject.SetActive(false);
                        CPlayerData.SetIsWorkerHired(workerCount, false);
                    });
                }
                catch (Exception e)
                {
                    CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                    status = EffectStatus.Retry;
                }
            }
            return new EffectResponse(req.ID, status, message);
        }
        public static EffectResponse LargeBills(EffectRequest req)
        {
            CrowdControlMod.mls.LogInfo($"running");

            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return EffectResponse.Retry(req.ID);
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            if (player.m_CurrentGameState != EGameState.CashCounterState) return EffectResponse.Retry(req.ID);//Better state check, still runs if the player leaves the checkout, but only starts if there


            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse AllSmellyCustomers(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return EffectResponse.Retry(req.ID);//Customers when store closed usually walk away, we should just reject these, and run when store is open
            List<Customer> customers = (List<Customer>)getProperty(CSingleton<CustomerManager>.Instance, "m_CustomerList");
            CustomerManager customerManager = CSingleton<CustomerManager>.Instance;
            CrowdControlMod.mls.LogInfo($"Customers?");
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {

                    if (customers == null)
                    {
                        CrowdControlMod.mls.LogInfo("Customer list not found.");
                        return;
                    }

                    // Loop through the customer list and add each customer to the smelly customer list
                    foreach (Customer customer in customers)
                    {
                        if (customer.isActiveAndEnabled)
                        {
                            CrowdControlMod.mls.LogInfo($"Customer?" + customer.name);
                            customerManager.AddToSmellyCustomerList(customer);
                            customer.SetSmelly();
                        }
                    }

                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse TeleportPlayer(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";

            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {


                    Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
                    CrowdControlMod.mls.LogInfo($"Player POS: {pos.position}");
                    Vector3 teleportPosition = new Vector3();

                    List<Vector3> possiblePositions = new List<Vector3>()
                    {
                        new Vector3(12.81f, -0.09f, -36.44f),
                        new Vector3(11.19f, -0.09f, 10.04f),
                        new Vector3(12.22f, -0.09f, 0.44f)
                    };

                    int randomIndex = UnityEngine.Random.Range(0, possiblePositions.Count);

                    teleportPosition = possiblePositions[randomIndex];

                    CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform.position = teleportPosition;


                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse ForceMath(EffectRequest req)
        {




            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return EffectResponse.Retry(req.ID);
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            if (player.m_CurrentGameState != EGameState.CashCounterState) return EffectResponse.Retry(req.ID);


            CrowdControlMod.ForceMath = true;

            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse ForcePaymentType(EffectRequest req)
        {



            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return EffectResponse.Retry(req.ID);
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            if (player.m_CurrentGameState != EGameState.CashCounterState) return EffectResponse.Retry(req.ID);//Better state check, still runs if the player leaves the checkout, but only starts if there

            List<Customer> cust = (List<Customer>)getProperty(CSingleton<CustomerManager>.Instance, "m_CustomerList");
            foreach (Customer c in cust)
            {
                if (c.m_CustomerCash.gameObject.activeSelf == true)
                {
                    if (CrowdControlMod.ForceUseCredit) c.m_CustomerCash.m_IsCard = true;
                    if (CrowdControlMod.ForceUseCash) c.m_CustomerCash.m_IsCard = false;
                }
            }


            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse InvertX(EffectRequest req)
        {
            CrowdControlMod.mls.LogInfo($"running");


            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse SetLanguage(EffectRequest req)
        {

            SettingScreen SS = CSingleton<SettingScreen>.Instance;
            string currentLanguage = LocalizationManager.CurrentLanguage;

            string language = req.code.Split('_')[1];
            string newLanguage = "";
            switch (language)
            {
                case "english":
                    {
                        newLanguage = "English";
                        break;
                    }
                case "french":
                    {
                        newLanguage = "France";
                        break;
                    }
                case "german":
                    {
                        newLanguage = "Germany";
                        break;
                    }
                case "italian":
                    {
                        newLanguage = "Italian";
                        break;
                    }
                case "spanish":
                    {
                        newLanguage = "Spanish";
                        break;
                    }
                case "chineset":
                    {
                        newLanguage = "ChineseT";
                        break;
                    }
                case "chineses":
                    {
                        newLanguage = "ChineseS";
                        break;
                    }
                case "korean":
                    {
                        newLanguage = "Korean";
                        break;
                    }
            };


            if (currentLanguage == newLanguage) return EffectResponse.Failure(req.ID);

            CrowdControlMod.NewLanguage = newLanguage;
            CrowdControlMod.OrgLanguage = currentLanguage;

            return EffectResponse.Success(req.ID);

        }

        public static EffectResponse InvertY(EffectRequest req)
        {


            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse HighFOV(EffectRequest req)
        {



            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse LowFOV(EffectRequest req)
        {
            

            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse GiveMoney(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            int amount = 0;
            string[] enteredText = req.code.Split('_');
            try
            {
                amount = int.Parse(enteredText[1]);
            }
            catch
            {
                return EffectResponse.Failure(req.ID, "Player has too much money?");

            }
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    CPlayerData.m_CoinAmount += amount;
                    CPlayerData.m_CoinAmountDouble += amount;//they added double at some point?
                    CSingleton<GameUIScreen>.Instance.AddCoin(amount, true);//Set as true to play Anim
                });

            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse TakeMoney(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            int amount = 0;
            string[] enteredText = req.code.Split('_');
            try
            {
                amount = int.Parse(enteredText[1]);
            }
            catch
            {
                return EffectResponse.Failure(req.ID, "Player has no money to take.");

            }
            if (CPlayerData.m_CoinAmount < amount) return EffectResponse.Retry(req.ID, "Player doesn't have enough money to take");//Negative Balance Fix
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    CPlayerData.m_CoinAmount -= amount;//this should be negative, silly
                    CPlayerData.m_CoinAmountDouble -= amount;//they added double at some point?
                    CSingleton<GameUIScreen>.Instance.ReduceCoin(amount, true);//Set as true to play Anim
                });

            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse ShopControls(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            string[] enteredText = req.code.Split('_');
            try
            {
                if (enteredText[0] == "close" && CPlayerData.m_IsShopOpen == true)
                {
                    CrowdControlMod.ActionQueue.Enqueue(() =>
                    {
                        CPlayerData.m_IsShopOpen = false;
                        InteractableOpenCloseSign.FindFirstObjectByType<InteractableOpenCloseSign>().m_CloseShopMesh.SetActive(true);
                        InteractableOpenCloseSign.FindFirstObjectByType<InteractableOpenCloseSign>().m_OpenShopMesh.SetActive(false);
                        CPlayerData.m_IsShopOnceOpen = false;
                    });
                }
                else if (enteredText[0] == "open" && CPlayerData.m_IsShopOpen == false)
                {
                    CrowdControlMod.ActionQueue.Enqueue(() =>
                    {
                        CPlayerData.m_IsShopOpen = true;
                        InteractableOpenCloseSign.FindFirstObjectByType<InteractableOpenCloseSign>().m_CloseShopMesh.SetActive(false);
                        InteractableOpenCloseSign.FindFirstObjectByType<InteractableOpenCloseSign>().m_OpenShopMesh.SetActive(true);
                        CPlayerData.m_IsShopOnceOpen = true;
                    });
                }
                else if (enteredText[0] == "renamestore")
                {
                    try
                    {
                        TutorialManager tutorialManager = UnityEngine.Object.FindObjectOfType<TutorialManager>();
                        if (tutorialManager == null)
                        {
                            CrowdControlMod.mls.LogInfo("Failed to find Tutorial Manager..");
                            return EffectResponse.Retry(req.ID, "No Shop To Rename? What?");
                        }
                        if (tutorialManager.m_ShopRenamer == null)
                        {
                            CrowdControlMod.mls.LogInfo("Failed to initialize ShopRenamer.");
                            return EffectResponse.Retry(req.ID, "No Shop To Rename? What?");
                        }
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            CSingleton<InteractionPlayerController>.Instance.EnterUIMode();
                            CSingleton<InteractionPlayerController>.Instance.EnterLockMoveMode();
                            tutorialManager.m_ShopRenamer.OnPressGoNextButton();
                            tutorialManager.m_ShopRenamer.m_SetNameInput.ActivateInputField();
                        });
                    }
                    catch (Exception e)
                    {
                        CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                        return EffectResponse.Retry(req.ID, "Failed to Rename Store");
                    }
                }
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse UpgradeWarehouse(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";

            if (CPlayerData.m_UnlockWarehouseRoomCount == UnlockRoomManager.Instance.m_LockedWarehouseRoomBlockerList.Count || CPlayerData.m_IsWarehouseRoomUnlocked == false) return EffectResponse.Retry(req.ID, "Storage is already unlocked.");//better fix for Warehouse Room Count
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    UnlockRoomManager.Instance.StartUnlockNextWarehouseRoom();
                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Failure;
            }
            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse UpgradeStore(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";

            if (CPlayerData.m_UnlockRoomCount == UnlockRoomManager.Instance.m_LockedRoomBlockerList.Count) return EffectResponse.Retry(req.ID, "Cannot upgrade Store any more");
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    UnlockRoomManager.Instance.StartUnlockNextRoom();
                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Failure;
            }
            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse UnlockWarehouse(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";

            if (CPlayerData.m_IsWarehouseRoomUnlocked == true) return EffectResponse.Retry(req.ID, "Storage is already unlocked.");
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    CrowdControlMod.isWarehouseUnlocked = true;
                    UnlockRoomManager.Instance.SetUnlockWarehouseRoom(true);
                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Failure;
            }
            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse GiveItem(EffectRequest req) //https://pastebin.com/BVEACvGA item list
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            var item = "";
            RestockData item2 = null;
            string[] enteredText = req.code.Split('_');
            if (enteredText.Length > 0)
            {
                try
                {
                    if (enteredText.Length == 5) { item = string.Join(" ", enteredText[1], enteredText[2], enteredText[3], enteredText[4]); }
                    else if (enteredText.Length == 4) { item = string.Join(" ", enteredText[1], enteredText[2], enteredText[3]); }//playmat, Plushie
                    else if (enteredText.Length == 3) { item = string.Join(" ", enteredText[1], enteredText[2]); }//single items like Freshener
                    else { item = enteredText[1]; }
                    item2 = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Find(z => z.name.ToLower() == item.ToLower());//Item bools
                }
                catch
                {
                    return EffectResponse.Failure(req.ID, "Unable to Find Item in Array.");
                }
            }
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    RestockManager.SpawnPackageBoxItem(item2.itemType, item2.amount, item2.isBigBox);
                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }
            return new EffectResponse(req.ID, status, message);
        }
        public static EffectResponse GiveEmpty(EffectRequest req) //https://pastebin.com/BVEACvGA item list
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            var item = "common pack (64)";
            RestockData item2 = null;
                try
                {
                    item2 = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Find(z => z.name.ToLower() == item.ToLower());//Find a random item to instantiate
                }
                catch
                {
                    return EffectResponse.Failure(req.ID, "Unable to Find Item in Array.");
                }
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    RestockManager.SpawnPackageBoxItem(item2.itemType, 0, true);//Have to call item2.itemType, 0 in box, true for bigbox.
                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }
            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse ThrowItem(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            bool isHoldingItem = (bool)getProperty(InteractionPlayerController.Instance, "m_IsHoldBoxMode");
            bool isMovingItem = (bool)getProperty(InteractionPlayerController.Instance, "m_IsMovingBoxMode");
            bool m_isBeingHold = (bool)getProperty(CSingleton<InteractablePackagingBox>.Instance, "m_IsBeingHold");
            List<Item> m_HoldItemList = (List<Item>)getProperty(InteractionPlayerController.Instance, "m_HoldItemList");
            //CrowdControlMod.mls.LogInfo("Is Holding Item: " + m_isBeingHold);//Comment out Logging info
            if (!m_isBeingHold || !isHoldingItem) { return EffectResponse.Retry(req.ID, "Player has no item in their hand"); }
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    CSingleton<InteractablePackagingBox>.Instance.ThrowBox(true);
                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse ExactChange(EffectRequest req)
        {

            if (!CPlayerData.m_IsShopOpen) return EffectResponse.Retry(req.ID);
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            if (player.m_CurrentGameState != EGameState.CashCounterState) return EffectResponse.Retry(req.ID);




            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse GiveItemFurniture(EffectRequest req) //https://pastebin.com/DjRnrjzi Furniture List
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            var item = "";
            FurniturePurchaseData item2 = null;
            string[] enteredText = req.code.Split('_');
            if (enteredText.Length > 0)
                try
                {
                    if (enteredText.Length == 4) { item = string.Join(" ", enteredText[1], enteredText[2], enteredText[3]); }//3 Word Items
                    else if (enteredText.Length == 3) { item = string.Join(" ", enteredText[1], enteredText[2]); }//2 word Items
                    else if (enteredText.Length == 2) { item = enteredText[1]; }//workbench fix, no need for string.join
                    item2 = CSingleton<InventoryBase>.Instance.m_ObjectData_SO.m_FurniturePurchaseDataList.Find(z => z.name.ToLower() == item.ToLower());//Item bools
                }
                catch
                {
                    return EffectResponse.Failure(req.ID, "Unable to Find Furniture Item in Array");
                }
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    Transform randomPackageSpawnPos = RestockManager.GetRandomPackageSpawnPos();
                    ShelfManager.SpawnInteractableObjectInPackageBox(item2.objectType, randomPackageSpawnPos.position, randomPackageSpawnPos.rotation);
                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }
            return new EffectResponse(req.ID, status, message);
        }



        public static EffectResponse OpenCardPack(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            string itemName = "";
            RestockData spawnItem = null;

            string[] codeParts = req.code.Split('_');

            // If this is 1, we're currently in the middle of opening packs. So retry it later. 
            //if (CrowdControlMod.autoOpenCards == 1) return EffectResponse.Retry(req.ID);



            if (codeParts.Length > 1)
            {
                try
                {
                    itemName = String.Join(" ", codeParts[1], codeParts[2]);
                    CrowdControlMod.mls.LogInfo(itemName);
                    spawnItem = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Find(z => z.name.ToLower().Contains(itemName.ToLower()));//Fix search Item Pack

                    if (spawnItem == null)
                    {
                        status = EffectStatus.Failure;
                        message = "Cannot find card pack to spawn.";
                        return new EffectResponse(req.ID, status, message);
                    }
                }
                catch (Exception ex)
                {
                    status = EffectStatus.Failure;
                    message = "Unable to spawn at player.";
                    return new EffectResponse(req.ID, status, message);
                }
            }

            InteractionPlayerController interactionPlayerController = CSingleton<InteractionPlayerController>.Instance;


            if (interactionPlayerController.m_CurrentGameState != EGameState.DefaultState)
            {

                status = EffectStatus.Retry;
                message = "";
                return new EffectResponse(req.ID, status, message);
            }

            try
            {


                //Debug.Log(interactionPlayerController.m_CurrentGameState);

                CrowdControlMod.ActionQueue.Enqueue(() =>
                {

                    Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
                    Vector3 position = pos.position;
                    Quaternion rotation = pos.rotation;

                    // Spawn the Item directly without using a box
                    // Get ItemMeshData for the card pack type
                    ItemMeshData itemMeshData = InventoryBase.GetItemMeshData(spawnItem.itemType);
                    
                    // Create a temporary parent GameObject for the item
                    GameObject tempParent = new GameObject("TempCardPackParent");
                    tempParent.transform.position = new Vector3(position.x + 1.4f, position.y + 1.2f, position.z);
                    tempParent.transform.rotation = rotation;
                    
                    // Get an Item from the ItemSpawnManager
                    Item cardPackItem = ItemSpawnManager.GetItem(tempParent.transform);
                    
                    // Configure the Item with the mesh data
                    cardPackItem.SetMesh(
                        itemMeshData.mesh, 
                        itemMeshData.material, 
                        spawnItem.itemType, 
                        itemMeshData.meshSecondary, 
                        itemMeshData.materialSecondary, 
                        itemMeshData.materialList
                    );
                    
                    // Set position and rotation (local to parent, which is already set correctly)
                    cardPackItem.transform.localPosition = Vector3.zero;
                    cardPackItem.transform.localRotation = Quaternion.identity;
                    cardPackItem.transform.localScale = Vector3.one;
                    
                    // Activate the item
                    cardPackItem.gameObject.SetActive(true);

                    CrowdControlMod.autoOpenCards = 1;
                    
                    // Now directly call ReadyingCardPack with the spawned item
                    CSingleton<CardOpeningSequence>.Instance.ReadyingCardPack(cardPackItem);

                });


            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                CrowdControlMod.autoOpenCards = 2;

                status = EffectStatus.Retry;
            }

            CrowdControlMod.autoOpenCards = 2;

            return EffectResponse.Success(req.ID);
        }


        public static EffectResponse SpawnHypeTrain(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";


            Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
            Vector3 position = pos.position;
            Quaternion rotation = pos.rotation;
            Transform playerCamera = Camera.main?.transform ?? UnityEngine.Object.FindObjectOfType<Camera>()?.transform;
            Vector3 forwardDirection = playerCamera.forward;

            if (!playerCamera) return EffectResponse.Failure(req.ID, "Unable to spawn item.");


            CrowdControlMod.ActionQueue.Enqueue(() =>
            {

                GameActions crowdDelegatesInstance = new GameActions();

                crowdDelegatesInstance.LoadAssetsFromBundle();

                for (int i = 0; i < 1; i++)
                {
                    float spawnDifference = UnityEngine.Random.Range(0.1f, 1.0f);
                    Vector3 spawnPosition = new Vector3(
                        playerCamera.position.x + forwardDirection.x * spawnDifference,
                        playerCamera.position.y + 1.0f,
                        playerCamera.position.z + forwardDirection.z * spawnDifference
                    );


                    crowdDelegatesInstance.Spawn_HypeTrain(spawnPosition, Quaternion.identity, req.sourceDetails as HypeTrainSourceDetails);

                }
            });

            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse SpawnBread(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";

            //CrowdControlMod.mls.LogMessage(req.sourceDetails);


            Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
            Vector3 position = pos.position;
            Quaternion rotation = pos.rotation;
            Transform playerCamera = CSingleton<InteractionPlayerController>.Instance.m_CameraController?.transform
                ?? CSingleton<InteractionPlayerController>.Instance.m_Cam?.transform
                ?? Camera.main?.transform
                ?? UnityEngine.Object.FindObjectOfType<Camera>()?.transform;
            if (!playerCamera)
            {
                LogError("SpawnBread failed: player camera was not found.");
                return EffectResponse.Failure(req.ID, "Unable to spawn item.");
            }

            Vector3 forwardDirection = playerCamera.forward;
            forwardDirection.y = 0f;
            if (forwardDirection.sqrMagnitude < 0.001f)
            {
                forwardDirection = pos.forward;
                forwardDirection.y = 0f;
            }
            forwardDirection.Normalize();
            LogInfo($"SpawnBread requested by '{req.viewer ?? "unknown"}' from playerPos={position}, cameraPos={playerCamera.position}, cameraForward={forwardDirection}");


            CrowdControlMod.ActionQueue.Enqueue(() =>
            {

                GameActions crowdDelegatesInstance = new GameActions();

                crowdDelegatesInstance.LoadAssetsFromBundle();

                for (int i = 0; i < 1; i++)
                {
                    float spawnDifference = UnityEngine.Random.Range(0.1f, 1.0f);
                    Vector3 spawnPosition = pos.position + forwardDirection * spawnDifference;
                    spawnPosition.y = pos.position.y + 1.0f;

                    LogInfo($"SpawnBread using spawnDifference={spawnDifference:F3}, spawnPosition={spawnPosition}");

                    crowdDelegatesInstance.Spawn_Bread(spawnPosition, Quaternion.identity);

                }
            });

            return new EffectResponse(req.ID, status, message);
        }

        

        public static EffectResponse SpawnMilk(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";


            Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
            Vector3 position = pos.position;
            Quaternion rotation = pos.rotation;
            Transform playerCamera = CSingleton<InteractionPlayerController>.Instance.m_CameraController?.transform
                ?? CSingleton<InteractionPlayerController>.Instance.m_Cam?.transform
                ?? Camera.main?.transform
                ?? UnityEngine.Object.FindObjectOfType<Camera>()?.transform;
            if (!playerCamera)
            {
                LogError("SpawnMilk failed: player camera was not found.");
                return EffectResponse.Failure(req.ID, "Unable to spawn item.");
            }

            Vector3 forwardDirection = playerCamera.forward;
            forwardDirection.y = 0f;
            if (forwardDirection.sqrMagnitude < 0.001f)
            {
                forwardDirection = pos.forward;
                forwardDirection.y = 0f;
            }
            forwardDirection.Normalize();
            LogInfo($"SpawnMilk requested by '{req.viewer ?? "unknown"}' from playerPos={position}, cameraPos={playerCamera.position}, cameraForward={forwardDirection}");


            CrowdControlMod.ActionQueue.Enqueue(() =>
            {

                GameActions crowdDelegatesInstance = new GameActions();

                crowdDelegatesInstance.LoadAssetsFromBundle();

                for (int i = 0; i < 1; i++)
                {
                    float spawnDifference = UnityEngine.Random.Range(0.1f, 1.0f);
                    Vector3 spawnPosition = pos.position + forwardDirection * spawnDifference;
                    spawnPosition.y = pos.position.y + 1.0f;

                    LogInfo($"SpawnMilk using spawnDifference={spawnDifference:F3}, spawnPosition={spawnPosition}");

                    crowdDelegatesInstance.Spawn_Milk(spawnPosition, Quaternion.identity);
                }
            });

            return new EffectResponse(req.ID, status, message);
        }


      

       

        public class InteractableObject2 : MonoBehaviour
        {
            private static InteractableObject2 currentlyHeldObject = null;
            private string debugLabel = "Food";

            private bool isHeld = false;
            private Camera playerCameraComponent;
            private Transform playerCamera;
            
            private Color originalColor;

            private Renderer renderer;
            

            private Transform playerTransform;

            private float maxPickupDistance = 5.0f;

            private float moveSpeed = 5.0f;

            private bool isHovered = false;



            private float interactionDelay = 1.0f;

            public void SetDebugLabel(string label)
            {
                if (!string.IsNullOrEmpty(label))
                {
                    debugLabel = label;
                }
            }

            private bool RefreshPlayerCamera()
            {
                playerCameraComponent = CSingleton<InteractionPlayerController>.Instance.m_Cam
                    ?? Camera.main
                    ?? FindObjectOfType<Camera>();

                if (playerCameraComponent == null)
                {
                    playerCamera = null;
                    return false;
                }

                playerCamera = playerCameraComponent.transform;
                return true;
            }

            void Start()
            {
                playerTransform = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;

                if (!RefreshPlayerCamera())
                {
                    LogError($"{debugLabel} interactable failed to initialize because no gameplay camera was found.");
                    return;
                }

                int colliderCount = GetComponentsInChildren<Collider>(true).Length;
                int rigidbodyCount = GetComponentsInChildren<Rigidbody>(true).Length;
                int audioSourceCount = GetComponentsInChildren<AudioSource>(true).Length;
                LogInfo($"{debugLabel} interactable initialized on '{gameObject.name}' at {transform.position}. colliders={colliderCount}, rigidbodies={rigidbodyCount}, childCount={transform.childCount}, audioSources={audioSourceCount}");

                if (rigidbodyCount == 0)
                {
                    LogWarning($"{debugLabel} interactable has no rigidbody; TryPickupObjectUnderMouse may never see it.");
                }

            }

            void OnGUI()
            {
                if (isHeld)
                {
                    // Create a style for the text
                    GUIStyle textStyle = new GUIStyle();
                    textStyle.fontSize = 24;
                    textStyle.normal.textColor = Color.white;
                    textStyle.alignment = TextAnchor.MiddleCenter; // Center the text
                    textStyle.richText = true; // Enable rich text formatting

                    // Create the text with different colors for F and E
                    string instructionText = "Press F to Drop\nPress E to Eat";

                    // Calculate the position for the text to be at the middle bottom of the screen
                    float width = 300;
                    float height = 100;
                    float xPos = (Screen.width - width) / 2; // Center horizontally
                    float yPos = Screen.height - height - 20; // Position slightly above the bottom

                    // Set the position and size of the text box
                    Rect rect = new Rect(xPos, yPos, width, height);

                    // Draw the instructions on the screen with rich text formatting
                    GUI.Label(rect, instructionText, textStyle);
                }
            }



            void Update()
            {

                if (isHeld)
                {
                    SmoothlyHoldObjectInFront();

                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        ThrowObject();
                        isHoldingObject = false;
                    }

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        EatObject();
                        isHoldingObject = false;
                    }
                }
                else
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (currentlyHeldObject == null)
                        {
                            TryPickupObjectUnderMouse();
                        }
                    }
                }
            }

            private bool isHoldingObject = false;  // Track whether the object is held
            private Vector3 relativePositionToCamera;  // The position relative to the camera/player
            private float heightOffset = 0.5f;  // Adjust this value for the height

            private void SmoothlyHoldObjectInFront()
            {
                // Get the player's transform and the camera's transform
                Transform playerTransform = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
                Transform cameraTransform = CSingleton<InteractionPlayerController>.Instance.m_CameraController.transform;

                // If the object is already being held, keep it in the same relative position
                if (isHoldingObject)
                {
                    // Calculate the new position based on the camera's current position and the saved relative position
                    Vector3 targetPosition2 = cameraTransform.position + cameraTransform.TransformDirection(relativePositionToCamera);
                    transform.position = targetPosition2;

                    // Ensure the object maintains the same orientation relative to the camera
                    transform.rotation = Quaternion.LookRotation(cameraTransform.forward);

                    return;  // Exit early since we're holding the object
                }

                // Make the object kinematic and disable the collider for holding
                

                // Calculate the target position in front of the player, relative to the camera's direction
                Vector3 targetPosition = playerTransform.position
                                         + cameraTransform.forward * 0.7f  // Slightly in front of the player
                                         + cameraTransform.right * 0.5f;   // Slightly to the right of the player

                // Adjust the Y position to lower the object based on the height offset
                targetPosition.y = playerTransform.position.y + heightOffset;  // Adjust height above the player (lower it)

                // Move the object smoothly towards the target position
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                // Check if the object is close enough to the target position to stop moving it
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)  // Close enough to target
                {
                    isHoldingObject = true;  // Mark the object as held
                    relativePositionToCamera = cameraTransform.InverseTransformDirection(transform.position - cameraTransform.position);  // Store the relative position to the camera

                    // Apply a random rotation of 0, 90, 180, or 270 degrees on the Y-axis
                    int randomAngleIndex = UnityEngine.Random.Range(0, 4);  // Generates 0, 1, 2, or 3
                    float randomAngle = randomAngleIndex * 90.0f;
                    if (transform.childCount > 0)
                    {
                        transform.GetChild(0).transform.Rotate(Vector3.up, randomAngle);
                    }
                    else
                    {
                        LogWarning($"{debugLabel} hold animation skipped child rotation because the instance has no children.");
                    }
                    //transform.Rotate(Vector3.up, randomAngle);  // Rotate the object on the Y-axis
                }

                // Make the object face the same direction as the camera
                transform.rotation = Quaternion.LookRotation(cameraTransform.forward);
            }


            private void TryPickupObjectUnderMouse()
            {
                if (!RefreshPlayerCamera())
                {
                    LogError($"{debugLabel} pickup failed because the gameplay camera is null.");
                    return;
                }

                Ray ray = playerCameraComponent.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(ray, maxPickupDistance);

                bool breadHit = false;

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null)
                    {
                        continue;
                    }

                    InteractableObject2 hitInteractable = hit.collider.GetComponentInParent<InteractableObject2>();
                    if (hitInteractable == this)
                    {
                        breadHit = true;
                        LogInfo($"{debugLabel} pickup matched collider '{hit.collider.name}' at distance {hit.distance:F3}.");
                        break;
                    }
                }
                
                if (breadHit)
                {
                    LogInfo($"{debugLabel} pickup raycast matched spawned object.");
                    PickUpObject();
                }
                else if (hits.Length > 0)
                {
                    LogWarning($"{debugLabel} pickup raycast hit {hits.Length} collider(s), but none resolved back to this object's rigidbody.");
                }
               
            }

            private void PickUpObject()
            {
                isHeld = true;
                currentlyHeldObject = this;
                LogInfo($"{debugLabel} picked up.");
                
                
            }

            private void ReleaseObject()
            {

                isHeld = false;
                currentlyHeldObject = null;
                LogInfo($"{debugLabel} released.");


                
                

                


                StartCoroutine(IgnorePlayerCollisionTemporarily());
            }

            private void ThrowObject()
            {
                

                Vector3 throwForce = playerCamera.forward * 10f;
                


                ReleaseObject();
            }

            private void EatObject()
            {
                LogInfo($"{debugLabel} eat started.");



                transform.rotation = Quaternion.LookRotation(playerCamera.forward);
                transform.Rotate(Vector3.right, 90.0f); 




                StartCoroutine(EatAndDestroy());
            }

            private IEnumerator EatAndDestroy()
            {
                float elapsedTime = 0f;
                float duration = 1.0f;

                Vector3 initialPosition = transform.position;

                AudioSource audioSource = null;
                if (transform.childCount > 0)
                {
                    audioSource = transform.GetChild(0).GetComponent<AudioSource>();
                }
                else
                {
                    LogWarning($"{debugLabel} eat animation has no child transform for audio lookup.");
                }

                if (audioSource != null)
                {
                    audioSource.Play();
                }
                else
                {
                    LogWarning($"{debugLabel} eat animation did not find an AudioSource.");
                }

                while (elapsedTime < duration)
                {
                    transform.position = Vector3.Lerp(initialPosition, playerCamera.position + playerCamera.forward * 0.5f, elapsedTime / duration);
                    transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.95f, transform.localScale.z); // Slightly reduce height
                    elapsedTime += Time.deltaTime;

                    yield return null;
                }


                LogInfo($"{debugLabel} eat finished. Destroying spawned object.");
                Destroy(gameObject);
            }

            private IEnumerator IgnorePlayerCollisionTemporarily()
            {
                Collider playerCollider = playerTransform.GetComponent<Collider>();

                if (playerCollider != null)
                {
                    

                    yield return new WaitForSeconds(interactionDelay);

                    
                }
               
            }
        }







        public static EffectResponse GiveItemAtPlayer(EffectRequest req) //https://pastebin.com/BVEACvGA item list
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            var item = "";
            RestockData item2 = null;



            string[] enteredText = req.code.Split('_');
            if (enteredText.Length > 0)
            {
                try
                {
                    if (enteredText.Length == 5) item = string.Join(" ", enteredText[1], enteredText[2], enteredText[3], enteredText[4]);
                    else if (enteredText.Length == 4) item = string.Join(" ", enteredText[1], enteredText[2], enteredText[3]);//playmat, Plushie
                    else if (enteredText.Length == 3) item = string.Join(" ", enteredText[1], enteredText[2]);//single items like Freshener
                    else item = enteredText[1];
                    item2 = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Find(z => z.name.ToLower() == item.ToLower());//Item database, make sure to search for item, in case name changes
                }
                catch
                {
                    return EffectResponse.Failure(req.ID, "Unable to spawn at player.");

                }
            }
            try
            {



                CrowdControlMod.ActionQueue.Enqueue(() =>
                {

                    Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
                    Vector3 position = pos.position;
                    Quaternion rotation = pos.rotation;



                    if (item2.isBigBox)
                    {


                        InteractablePackagingBox_Item interactablePackagingBox_Item = UnityEngine.Object.Instantiate<InteractablePackagingBox_Item>(CSingleton<RestockManager>.Instance.m_PackageBoxPrefab, new Vector3(position.x + 1.4f, position.y + 1.2f, position.z), rotation, CSingleton<RestockManager>.Instance.m_PackageBoxParentGrp);
                        interactablePackagingBox_Item.FillBoxWithItem(item2.itemType, item2.amount);
                        interactablePackagingBox_Item.name = interactablePackagingBox_Item.m_ObjectType.ToString() + getProperty(CSingleton<RestockManager>.Instance, "m_SpawnedBoxCount");

                    }
                    else
                    {
                        InteractablePackagingBox_Item interactablePackagingBox_Item2 = UnityEngine.Object.Instantiate<InteractablePackagingBox_Item>(CSingleton<RestockManager>.Instance.m_PackageBoxSmallPrefab, new Vector3(position.x + 1.4f, position.y + 1.2f, position.z), rotation, CSingleton<RestockManager>.Instance.m_PackageBoxParentGrp);
                        interactablePackagingBox_Item2.FillBoxWithItem(item2.itemType, 32);
                        interactablePackagingBox_Item2.name = interactablePackagingBox_Item2.m_ObjectType.ToString() + getProperty(CSingleton<RestockManager>.Instance, "m_SpawnedBoxCount");

                    }



                });


            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }
        public static EffectResponse SendHugeEmpty(EffectRequest req) //https://pastebin.com/BVEACvGA item list
        {
            EffectStatus status = EffectStatus.Success;
            string message = "";
            var item = "common pack (32)";
            RestockData item2 = null;



            string[] enteredText = req.code.Split('_');
            if (enteredText.Length > 0)
            {
                try
                {
                    item2 = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Find(z => z.name.ToLower() == item.ToLower());//Item database, make sure to search for item, in case name changes
                }
                catch
                {
                    return EffectResponse.Failure(req.ID, "Unable to spawn at player.");

                }
            }
            try
            {



                CrowdControlMod.ActionQueue.Enqueue(() =>
                {

                    Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
                    Vector3 position = pos.position;
                    Quaternion rotation = pos.rotation;
                    
                    InteractablePackagingBox_Item interactablePackagingBox_Item2 = UnityEngine.Object.Instantiate<InteractablePackagingBox_Item>(CSingleton<RestockManager>.Instance.m_PackageBoxSmallPrefab, new Vector3(position.x + 1.4f, position.y + 1.2f, position.z), rotation, CSingleton<RestockManager>.Instance.m_PackageBoxParentGrp);
                    interactablePackagingBox_Item2.FillBoxWithItem(item2.itemType, 0); 
                    interactablePackagingBox_Item2.transform.localScale = new Vector3(UnityEngine.Random.Range(5f, 15f), UnityEngine.Random.Range(5f,15f), UnityEngine.Random.Range(5f, 15f));
                    interactablePackagingBox_Item2.name = interactablePackagingBox_Item2.m_ObjectType.ToString() + getProperty(CSingleton<RestockManager>.Instance, "m_SpawnedBoxCount");



                });


            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }

            return new EffectResponse(req.ID, status, message);
        }

        public static EffectResponse PlayerSpeed(EffectRequest req)
        {
            if (CSingleton<InteractionPlayerController>.Instance?.m_WalkerCtrl == null) return EffectResponse.Retry(req.ID);
            TimedType type = req.code == "player_fast" ? TimedType.PLAYER_FAST : TimedType.PLAYER_SLOW;
            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse LowGravity(EffectRequest req)
        {
            if (CSingleton<InteractionPlayerController>.Instance?.m_WalkerCtrl == null) return EffectResponse.Retry(req.ID);
            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse GameSpeed(EffectRequest req)
        {
            if (Timed.IsGamePaused()) return EffectResponse.Retry(req.ID);
            TimedType type = req.code == "slowmo" ? TimedType.GAME_SLOW : TimedType.GAME_FAST;
            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse HyperCustomers(EffectRequest req)
        {
            if (!CSingleton<CustomerManager>.Instance.HasCustomerInShop()) return EffectResponse.Retry(req.ID, "No customers in the shop");
            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse MuteAudio(EffectRequest req)
        {
            return EffectResponse.Success(req.ID);
        }

        public static EffectResponse LeaveReview(EffectRequest req)
        {
            EffectStatus status = EffectStatus.Success;
            bool isBad = req.code == "badreview";
            try
            {
                CrowdControlMod.ActionQueue.Enqueue(() =>
                {
                    // goodBadLevel 0 = 1 star text, 2 = good text (4 stars). higherStarChanceAdd pushes the +1 star roll to never/always.
                    CustomerReviewManager.AddCustomerReview(ECustomerReviewType.StoreGeneric, EItemType.None, isBad ? 0 : 2, isBad ? -100 : 100);
                });
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = EffectStatus.Retry;
            }
            return new EffectResponse(req.ID, status, "");
        }

        public static void setProperty(System.Object a, string prop, System.Object val)
        {
            var f = a.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (f == null)
            {
                CrowdControlMod.mls.LogInfo($"Field {prop} not found in {a.GetType()}");
                return;
            }

            f.SetValue(a, val);
        }

        public static System.Object getProperty(System.Object a, string prop)
        {
            var f = a.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (f == null)
            {
                CrowdControlMod.mls.LogInfo($"Field {prop} not found in {a.GetType()}");
                return null;
            }

            return f.GetValue(a);
        }

        public static void setSubProperty(System.Object a, string prop, string prop2, System.Object val)
        {
            var f = a.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic);
            var f2 = f.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic);
            f2.SetValue(f, val);
        }

        public static void callSubFunc(System.Object a, string prop, string func, System.Object val)
        {
            callSubFunc(a, prop, func, new object[] { val });
        }

        public static void callSubFunc(System.Object a, string prop, string func, System.Object[] vals)
        {
            var f = a.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic);


            var p = f.GetType().GetMethod(func, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            p.Invoke(f, vals);

        }

        public static void callFunc(System.Object a, string func, System.Object val)
        {
            callFunc(a, func, new object[] { val });
        }

        public static void callFunc(System.Object a, string func, System.Object[] vals)
        {
            var p = a.GetType().GetMethod(func, BindingFlags.Instance | BindingFlags.NonPublic);
            p.Invoke(a, vals);

        }

        public static System.Object callAndReturnFunc(System.Object a, string func, System.Object val)
        {
            return callAndReturnFunc(a, func, new object[] { val });
        }

        public static System.Object callAndReturnFunc(System.Object a, string func, System.Object[] vals)
        {
            var p = a.GetType().GetMethod(func, BindingFlags.Instance | BindingFlags.NonPublic);
            return p.Invoke(a, vals);

        }

    }
}
