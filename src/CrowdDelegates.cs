using BepInEx;
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
using System.Net;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Core;
using System.Security.Cryptography;
using System.Text;
using static BepinControl.CrowdRequest.SourceDetails;


namespace BepinControl
{
    public delegate CrowdResponse CrowdDelegate(ControlClient client, CrowdRequest req);



    public class CrowdDelegates
    {
        public static System.Random rnd = new System.Random();
        public static int maxBoxCount = 100;

        public AssetBundle bundle; // Make sure this is assigned when the plugin loads

        private static GameObject breadPrefab;
        private static GameObject milkPrefab;
        private static GameObject trainPrefab;

        private static GameObject hypetrainPrefab;


        // Static flag to ensure assets are loaded only once
        private static bool loaded = false;

        // Load all assets from the bundle and store them
        public void LoadAssetsFromBundle()
        {
            if (loaded) return; // Only load once

            //TestMod.mls.LogDebug("PATH " + System.IO.Path.Combine(Paths.PluginPath, "CrowdControl", "food"));

            bundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(Paths.PluginPath, "CrowdControl", "food"));
            if (bundle == null)
            {
                Debug.LogError("Failed to load AssetBundle.");
                return;
            }

            milkPrefab = bundle.LoadAsset<GameObject>("MilkGroup");
            breadPrefab = bundle.LoadAsset<GameObject>("BreadGroup");

            HypeTrainBoxData boxData = new HypeTrainBoxData();// Do this to load the dll... maybe do something different, but this works for now
            bundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(Paths.PluginPath, "CrowdControl", "warpworld.hypetrain"));
            if (bundle == null)
            {
                Debug.LogError("Failed to load AssetBundle.");
                return;
            }

            hypetrainPrefab = bundle.LoadAsset<GameObject>("HypeTrain");

            if (hypetrainPrefab == null)
            {
                Debug.LogError("hypetrain prefab not found in AssetBundle.");
            }

            loaded = true; 
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

        public void Spawn_HypeTrain(Vector3 position, Quaternion rotation, CrowdRequest.SourceDetails sourceDetails)
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


                if (sourceDetails.top_contributions.Length == 0)
                {
                    TestMod.mls.LogInfo("No top_contributions?");
                    return;
                }

                HypeTrain hypeTrain = UnityEngine.Object.Instantiate(hypetrainPrefab, position, rotation).GetComponent<HypeTrain>();
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

                    foreach (var contribution in sourceDetails.top_contributions)
                    {
                        hypeTrainBoxDataList.Add(new HypeTrainBoxData()
                        {
                            name = contribution.user_name,
                            box_color = ConvertUserNameToColor(contribution.user_name),
                            bit_amount = contribution.type == "bits" ? contribution.total : 0 // Only set bit_amount if the contribution is bits
                        });
                    }

                    bool isLastContributionInTop = sourceDetails.last_contribution != null;

                    // Only add last train car if the last_contribution user_id is not in top_contributions
                    if (isLastContributionInTop)
                    {
                        hypeTrainBoxDataList.Add(new HypeTrainBoxData()
                        {
                            name = sourceDetails.last_contribution.user_name,
                            box_color = ConvertUserNameToColor(sourceDetails.last_contribution.user_name),
                            bit_amount = sourceDetails.last_contribution.type == "bits" ? sourceDetails.last_contribution.total : 0
                        });
                    }

                    float defaultSpeed = 1f;
                    float speedIncrease = sourceDetails.level * 0.1f;
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
            if (breadPrefab != null)
            {
                GameObject breadInstance = UnityEngine.Object.Instantiate(breadPrefab, position, rotation);
                breadInstance.AddComponent<InteractableObject2>();
            }

        }


        // Method to spawn the milk asset
        public void Spawn_Milk(Vector3 position, Quaternion rotation)
        {
            if (milkPrefab != null)
            {
                GameObject milkInstance = UnityEngine.Object.Instantiate(milkPrefab, position, rotation);
                milkInstance.AddComponent<InteractableObject2>();
            }
            else
            {
                Debug.LogError("Milk prefab not loaded.");
            }
        }


        public static CrowdResponse ToggleLights(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    LightManager.Instance.ToggleShopLight();
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse HeyOhh(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            List<Customer> customers = (List<Customer>)getProperty(CSingleton<CustomerManager>.Instance, "m_CustomerList");
            CustomerManager customerManager = CSingleton<CustomerManager>.Instance;
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {

                    if (customers == null)
                    {
                        TestMod.mls.LogInfo("Customer list not found.");
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
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse SpawnCustomer(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            CustomerManager CM = CustomerManager.Instance;
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            int CustomerTotal = CustomerManager.Instance.m_TotalCurrentCustomerCount;//make sure we have < 28 Customers when we spawn
            if (CustomerTotal == CustomerManager.Instance.m_CustomerCountMax) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Too Many Customers in Shop");//Too many Customers?
            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return new CrowdResponse(id: req.GetReqID(), status: CrowdResponse.Status.STATUS_RETRY, message: "Store is Closed");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    if (req.targets != null)
                    {
                        if (req.targets[0].service == "twitch")
                        {
                            TestMod.twitchChannel = req.targets[0].name;
                        }
                    }
                    TestMod.NameOverride = req.viewer;
                    TestMod.isSmelly = false;
                    CustomerManager.Instance.m_CustomerCountMax += 1;
                    callFunc(CustomerManager.Instance, "AddCustomerPrefab", null);
                    Customer newCustomer = CM.GetNewCustomer(false);//spawn not smelly

                    newCustomer.name = req.viewer;
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse SpawnCustomerSmelly(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            CustomerManager CM = CustomerManager.Instance; int CustomerTotal = CustomerManager.Instance.m_TotalCurrentCustomerCount;//make sure we have < 28 Customers when we spawn
            if (CustomerTotal == CustomerManager.Instance.m_CustomerCountMax) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Too Many Customers in Shop");//Too many Customers?
            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return new CrowdResponse(id: req.GetReqID(), status: CrowdResponse.Status.STATUS_RETRY, message: "Store is Closed");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {

                    if (req.targets != null)
                    {

                        if (req.targets[0].service == "twitch")
                        {
                            TestMod.twitchChannel = req.targets[0].name;
                        }
                    }
                    TestMod.isSmelly = true;
                    TestMod.NameOverride = req.viewer;
                    Customer Smelly = CM.GetNewCustomer(true);//Spawn him as Smelly
                    if (Smelly != null)
                    {
                        Smelly.SetSmelly();
                        CustomerManager.Instance.AddToSmellyCustomerList(Smelly);
                        Smelly.name = req.viewer;
                    }
                    TestMod.isSmelly = false;


                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse HireWorker(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            bool found = false;
            int workerCount = 0;
            Worker workerid = null;
            List<Worker> m_WorkerList = WorkerManager.GetWorkerList();
            if (!found)
            {
                try
                {
                    Worker worker2 = m_WorkerList.Find(x => x.m_IsActive == false);
                    workerCount = m_WorkerList.IndexOf(worker2);
                    if (worker2 != null)
                    {
                        workerid = m_WorkerList[workerCount];
                        found = true;
                    }
                }
                catch
                {
                    status = CrowdResponse.Status.STATUS_RETRY;
                }

            }
            if (!found || workerid == null) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "No Workers available");
            if (found)
            {
                try
                {
                    TestMod.ActionQueue.Enqueue(() =>
                    {

                        workerid.ActivateWorker(true);
                        workerid.m_IsActive = true;
                        //workerid.name = req.viewer; worker tags
                        workerid.gameObject.SetActive(true);
                        workerid.transform.position = InteractionPlayerController.Instance.m_WalkerCtrl.transform.position;
                        CPlayerData.SetIsWorkerHired(workerCount, true);

                    });
                }
                catch (Exception e)
                {
                    TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                    status = CrowdResponse.Status.STATUS_RETRY;
                }
            }
            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse FireWorker(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
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
                    status = CrowdResponse.Status.STATUS_RETRY;
                }

            }
            if (!found || workerid == null) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "No Workers available");
            if (found)
            {
                try
                {
                    TestMod.ActionQueue.Enqueue(() =>
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
                    TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                    status = CrowdResponse.Status.STATUS_RETRY;
                }
            }
            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse LargeBills(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;
            TestMod.mls.LogInfo($"running");

            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            if (player.m_CurrentGameState != EGameState.CashCounterState) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");//Better state check, still runs if the player leaves the checkout, but only starts if there

            if (TimedThread.isRunning(TimedType.FORCE_LARGE_BILLS)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            //if (TimedThread.isRunning(TimedType.FORCE_CARD)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");//Comment out, since cards now work

            new Thread(new TimedThread(req.GetReqID(), TimedType.FORCE_LARGE_BILLS, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse AllSmellyCustomers(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");//Customers when store closed usually walk away, we should just reject these, and run when store is open
            List<Customer> customers = (List<Customer>)getProperty(CSingleton<CustomerManager>.Instance, "m_CustomerList");
            CustomerManager customerManager = CSingleton<CustomerManager>.Instance;
            TestMod.mls.LogInfo($"Customers?");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {

                    if (customers == null)
                    {
                        TestMod.mls.LogInfo("Customer list not found.");
                        return;
                    }

                    // Loop through the customer list and add each customer to the smelly customer list
                    foreach (Customer customer in customers)
                    {
                        if (customer.isActiveAndEnabled)
                        {
                            TestMod.mls.LogInfo($"Customer?" + customer.name);
                            customerManager.AddToSmellyCustomerList(customer);
                            customer.SetSmelly();
                        }
                    }

                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse TeleportPlayer(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {


                    Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
                    TestMod.mls.LogInfo($"Player POS: {pos.position}");
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
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse ForceMath(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;


            if (TestMod.ForceMath || TimedThread.isRunning(TimedType.FORCE_MATH)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");


            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            if (player.m_CurrentGameState != EGameState.CashCounterState) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");


            TestMod.ForceMath = true;

            new Thread(new TimedThread(req.GetReqID(), TimedType.FORCE_MATH, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse ForcePaymentType(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;


            if (TimedThread.isRunning(TimedType.FORCE_CASH)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            if (TimedThread.isRunning(TimedType.FORCE_CARD)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            if (player.m_CurrentGameState != EGameState.CashCounterState) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");//Better state check, still runs if the player leaves the checkout, but only starts if there

            List<Customer> cust = (List<Customer>)getProperty(CSingleton<CustomerManager>.Instance, "m_CustomerList");
            foreach (Customer c in cust)
            {
                if (c.m_CustomerCash.gameObject.activeSelf == true)
                {
                    if (TestMod.ForceUseCredit) c.m_CustomerCash.m_IsCard = true;
                    if (TestMod.ForceUseCash) c.m_CustomerCash.m_IsCard = false;
                }
            }
            string paymentType = req.code.Split('_')[1];

            if (paymentType == "cash") new Thread(new TimedThread(req.GetReqID(), TimedType.FORCE_CASH, dur * 1000).Run).Start();
            if (paymentType == "card") new Thread(new TimedThread(req.GetReqID(), TimedType.FORCE_CARD, dur * 1000).Run).Start();

            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse InvertX(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;
            TestMod.mls.LogInfo($"running");

            if (TimedThread.isRunning(TimedType.INVERT_X)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            new Thread(new TimedThread(req.GetReqID(), TimedType.INVERT_X, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse SetLanguage(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;

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


            if (currentLanguage == newLanguage) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "");
            if (TimedThread.isRunning(TimedType.SET_LANGUAGE)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            TestMod.NewLanguage = newLanguage;
            TestMod.OrgLanguage = currentLanguage;

            new Thread(new TimedThread(req.GetReqID(), TimedType.SET_LANGUAGE, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);

        }

        public static CrowdResponse InvertY(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;

            if (TimedThread.isRunning(TimedType.INVERT_Y)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            new Thread(new TimedThread(req.GetReqID(), TimedType.INVERT_Y, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse HighFOV(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;


            if (TimedThread.isRunning(TimedType.HIGH_FOV)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            if (TimedThread.isRunning(TimedType.LOW_FOV)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            new Thread(new TimedThread(req.GetReqID(), TimedType.HIGH_FOV, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse LowFOV(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;
            
            if (TimedThread.isRunning(TimedType.HIGH_FOV)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            if (TimedThread.isRunning(TimedType.LOW_FOV)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            new Thread(new TimedThread(req.GetReqID(), TimedType.LOW_FOV, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse GiveMoney(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            int amount = 0;
            string[] enteredText = req.code.Split('_');
            try
            {
                amount = int.Parse(enteredText[1]);
            }
            catch
            {
                return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "Player has too much money?");

            }
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    CPlayerData.m_CoinAmount += amount;
                    CSingleton<GameUIScreen>.Instance.AddCoin(amount, true);//Set as true to play Anim
                });

            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse TakeMoney(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            int amount = 0;
            string[] enteredText = req.code.Split('_');
            try
            {
                amount = int.Parse(enteredText[1]);
            }
            catch
            {
                return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "Player has no money to take.");

            }
            if (CPlayerData.m_CoinAmount < amount) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Player doesn't have enough money to take");//Negative Balance Fix
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    CPlayerData.m_CoinAmount -= amount;//this should be negative, silly
                    CSingleton<GameUIScreen>.Instance.ReduceCoin(amount, true);//Set as true to play Anim
                });

            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse ShopControls(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            string[] enteredText = req.code.Split('_');
            try
            {
                if (enteredText[0] == "close" && CPlayerData.m_IsShopOpen == true)
                {
                    TestMod.ActionQueue.Enqueue(() =>
                    {
                        CPlayerData.m_IsShopOpen = false;
                        InteractableOpenCloseSign.FindFirstObjectByType<InteractableOpenCloseSign>().m_CloseShopMesh.SetActive(true);
                        InteractableOpenCloseSign.FindFirstObjectByType<InteractableOpenCloseSign>().m_OpenShopMesh.SetActive(false);
                        CPlayerData.m_IsShopOnceOpen = false;
                    });
                }
                else if (enteredText[0] == "open" && CPlayerData.m_IsShopOpen == false)
                {
                    TestMod.ActionQueue.Enqueue(() =>
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
                            TestMod.mls.LogInfo("Failed to find Tutorial Manager..");
                            return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "No Shop To Rename? What?");
                        }
                        if (tutorialManager.m_ShopRenamer == null)
                        {
                            TestMod.mls.LogInfo("Failed to initialize ShopRenamer.");
                            return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "No Shop To Rename? What?");
                        }
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            CSingleton<InteractionPlayerController>.Instance.EnterUIMode();
                            CSingleton<InteractionPlayerController>.Instance.EnterLockMoveMode();
                            tutorialManager.m_ShopRenamer.OnPressGoNextButton();
                            tutorialManager.m_ShopRenamer.m_SetNameInput.ActivateInputField();
                        });
                    }
                    catch (Exception e)
                    {
                        TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                        return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Failed to Rename Store");
                    }
                }
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse UpgradeWarehouse(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            if (CPlayerData.m_UnlockWarehouseRoomCount < UnlockRoomManager.Instance.m_LockedWarehouseRoomBlockerList.Count || CPlayerData.m_IsWarehouseRoomUnlocked == false) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Storage is already unlocked.");//better fix for Warehouse Room Count
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    UnlockRoomManager.Instance.StartUnlockNextWarehouseRoom();
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_FAILURE;
            }
            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse UpgradeStore(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            if (CPlayerData.m_UnlockRoomCount < UnlockRoomManager.Instance.m_LockedRoomBlockerList.Count) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Cannot upgrade Store any more");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    UnlockRoomManager.Instance.StartUnlockNextRoom();
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_FAILURE;
            }
            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse UnlockWarehouse(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            if (CPlayerData.m_IsWarehouseRoomUnlocked == true) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Storage is already unlocked.");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    TestMod.isWarehouseUnlocked = true;
                    UnlockRoomManager.Instance.SetUnlockWarehouseRoom(true);
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_FAILURE;
            }
            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse GiveItem(ControlClient client, CrowdRequest req) //https://pastebin.com/BVEACvGA item list
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
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
                    return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "Unable to Find Item in Array.");
                }
            }
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    if (item2.isBigBox) RestockManager.SpawnPackageBoxItem(item2.itemType, item2.amount, item2.isBigBox);
                    else RestockManager.SpawnPackageBoxItem(item2.itemType, item2.amount, item2.isBigBox);
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }
            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse GiveEmpty(ControlClient client, CrowdRequest req) //https://pastebin.com/BVEACvGA item list
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            var item = "common pack (64)";
            RestockData item2 = null;
                try
                {
                    item2 = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Find(z => z.name.ToLower() == item.ToLower());//Find a random item to instantiate
                }
                catch
                {
                    return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "Unable to Find Item in Array.");
                }
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    RestockManager.SpawnPackageBoxItem(item2.itemType, 0, true);//Have to call item2.itemType, 0 in box, true for bigbox.
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }
            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse ThrowItem(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            bool isHoldingItem = (bool)getProperty(InteractionPlayerController.Instance, "m_IsHoldBoxMode");
            bool isMovingItem = (bool)getProperty(InteractionPlayerController.Instance, "m_IsMovingBoxMode");
            bool m_isBeingHold = (bool)getProperty(CSingleton<InteractablePackagingBox>.Instance, "m_IsBeingHold");
            List<Item> m_HoldItemList = (List<Item>)getProperty(InteractionPlayerController.Instance, "m_HoldItemList");
            //TestMod.mls.LogInfo("Is Holding Item: " + m_isBeingHold);//Comment out Logging info
            if (!m_isBeingHold || !isHoldingItem) { return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Player has no item in their hand"); }
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    CSingleton<InteractablePackagingBox>.Instance.ThrowBox(true);
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse ExactChange(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;

            if (!CPlayerData.m_IsShopOpen) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            if (player.m_CurrentGameState != EGameState.CashCounterState) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            if (TimedThread.isRunning(TimedType.ALLOW_MISCHARGE)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            if (TimedThread.isRunning(TimedType.FORCE_EXACT_CHANGE)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            if (TimedThread.isRunning(TimedType.FORCE_CASH)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");



            new Thread(new TimedThread(req.GetReqID(), TimedType.FORCE_EXACT_CHANGE, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse GiveItemFurniture(ControlClient client, CrowdRequest req) //https://pastebin.com/DjRnrjzi Furniture List
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
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
                    return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "Unable to Find Furniture Item in Array");
                }
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    Transform randomPackageSpawnPos = RestockManager.GetRandomPackageSpawnPos();
                    ShelfManager.SpawnInteractableObjectInPackageBox(item2.objectType, randomPackageSpawnPos.position, randomPackageSpawnPos.rotation);
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }
            return new CrowdResponse(req.GetReqID(), status, message);
        }



        public static CrowdResponse OpenCardPack(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            string itemName = "";
            RestockData spawnItem = null;

            string[] codeParts = req.code.Split('_');

            // If this is 1, we're currently in the middle of opening packs. So retry it later. 
            //if (TestMod.autoOpenCards == 1) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            if (TimedThread.isRunning(TimedType.OPENING_PACK)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");


            if (codeParts.Length > 1)
            {
                try
                {
                    itemName = String.Join(" ", codeParts[1], codeParts[2]);
                    TestMod.mls.LogInfo(itemName);
                    spawnItem = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Find(z => z.name.ToLower().Contains(itemName.ToLower()));//Fix search Item Pack

                    if (spawnItem == null)
                    {
                        status = CrowdResponse.Status.STATUS_FAILURE;
                        message = "Cannot find card pack to spawn.";
                        return new CrowdResponse(req.GetReqID(), status, message);
                    }
                }
                catch (Exception ex)
                {
                    status = CrowdResponse.Status.STATUS_FAILURE;
                    message = "Unable to spawn at player.";
                    return new CrowdResponse(req.GetReqID(), status, message);
                }
            }

            InteractionPlayerController interactionPlayerController = CSingleton<InteractionPlayerController>.Instance;


            if (interactionPlayerController.m_CurrentGameState != EGameState.DefaultState)
            {

                status = CrowdResponse.Status.STATUS_RETRY;
                message = "";
                return new CrowdResponse(req.GetReqID(), status, message);
            }

            try
            {


                //Debug.Log(interactionPlayerController.m_CurrentGameState);
                new Thread(new TimedThread(req.GetReqID(), TimedType.OPENING_PACK, 6 * 1000).Run).Start();

                TestMod.ActionQueue.Enqueue(() =>
                {

                    Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
                    Vector3 position = pos.position;
                    Quaternion rotation = pos.rotation;


                    InteractablePackagingBox_Item interactablePackagingBox_Item = UnityEngine.Object.Instantiate<InteractablePackagingBox_Item>(CSingleton<RestockManager>.Instance.m_PackageBoxPrefab, new Vector3(position.x + 1.4f, position.y + 1.2f, position.z), rotation, CSingleton<RestockManager>.Instance.m_PackageBoxParentGrp);
                    interactablePackagingBox_Item.FillBoxWithItem(spawnItem.itemType, 1);
                    interactablePackagingBox_Item.name = interactablePackagingBox_Item.m_ObjectType.ToString() + getProperty(CSingleton<RestockManager>.Instance, "m_SpawnedBoxCount");

                    FieldInfo itemListField = typeof(ItemSpawnManager).GetField("m_ItemList", BindingFlags.NonPublic | BindingFlags.Instance);

                    TestMod.autoOpenCards = 1;
                    if (itemListField != null)
                    {
                        List<Item> spawnedItems = (List<Item>)itemListField.GetValue(CSingleton<ItemSpawnManager>.Instance);

                        if (spawnedItems != null)
                        {
                            Transform playerTransform = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;

                            Item closestItem = null;
                            float shortestDistance = float.MaxValue;

                            foreach (Item _item in spawnedItems)
                            {
                                if (_item.GetItemType() == spawnItem.itemType)
                                {
                                    float distanceToPlayer = Vector3.Distance(playerTransform.position, _item.transform.position);

                                    if (distanceToPlayer < shortestDistance)
                                    {
                                        shortestDistance = distanceToPlayer;
                                        closestItem = _item;
                                    }
                                }
                            }

                            if (closestItem != null)
                            {
                                CSingleton<CardOpeningSequence>.Instance.ReadyingCardPack(closestItem);
                                interactablePackagingBox_Item.OnDestroyed();
                            }
                            else
                            {
                                status = CrowdResponse.Status.STATUS_FAILURE;
                                message = "Unable to activate pack opening.";
                            }
                        }
                    }

                });


            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                TestMod.autoOpenCards = 2;

                status = CrowdResponse.Status.STATUS_RETRY;
            }

            TestMod.autoOpenCards = 2;

            return new TimedResponse(req.GetReqID(), 6 * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }


        public static CrowdResponse SpawnHypeTrain(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";


            Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
            Vector3 position = pos.position;
            Quaternion rotation = pos.rotation;
            Transform playerCamera = Camera.main?.transform ?? UnityEngine.Object.FindObjectOfType<Camera>()?.transform;
            Vector3 forwardDirection = playerCamera.forward;

            if (!playerCamera) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "Unable to spawn item.");


            TestMod.ActionQueue.Enqueue(() =>
            {

                CrowdDelegates crowdDelegatesInstance = new CrowdDelegates();

                crowdDelegatesInstance.LoadAssetsFromBundle();

                for (int i = 0; i < 1; i++)
                {
                    float spawnDifference = UnityEngine.Random.Range(0.1f, 1.0f);
                    Vector3 spawnPosition = new Vector3(
                        playerCamera.position.x + forwardDirection.x * spawnDifference,
                        playerCamera.position.y + 1.0f,
                        playerCamera.position.z + forwardDirection.z * spawnDifference
                    );


                    crowdDelegatesInstance.Spawn_HypeTrain(spawnPosition, Quaternion.identity, req.sourceDetails);

                }
            });

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse SpawnBread(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            //TestMod.mls.LogMessage(req.sourceDetails);


            Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
            Vector3 position = pos.position;
            Quaternion rotation = pos.rotation;
            Transform playerCamera = Camera.main?.transform ?? UnityEngine.Object.FindObjectOfType<Camera>()?.transform;
            Vector3 forwardDirection = playerCamera.forward;

            if (!playerCamera) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "Unable to spawn item.");


            TestMod.ActionQueue.Enqueue(() =>
            {

                CrowdDelegates crowdDelegatesInstance = new CrowdDelegates();

                crowdDelegatesInstance.LoadAssetsFromBundle();

                for (int i = 0; i < 1; i++)
                {
                    float spawnDifference = UnityEngine.Random.Range(0.1f, 1.0f);
                    Vector3 spawnPosition = new Vector3(
                        playerCamera.position.x + forwardDirection.x * spawnDifference,
                        playerCamera.position.y + 1.0f,
                        playerCamera.position.z + forwardDirection.z * spawnDifference
                    );

                    crowdDelegatesInstance.Spawn_Bread(spawnPosition, Quaternion.identity);

                }
            });

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        

        public static CrowdResponse SpawnMilk(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";


            Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
            Vector3 position = pos.position;
            Quaternion rotation = pos.rotation;
            Transform playerCamera = Camera.main?.transform ?? UnityEngine.Object.FindObjectOfType<Camera>()?.transform;
            Vector3 forwardDirection = playerCamera.forward;

            if (!playerCamera) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "Unable to spawn item.");


            TestMod.ActionQueue.Enqueue(() =>
            {

                CrowdDelegates crowdDelegatesInstance = new CrowdDelegates();

                crowdDelegatesInstance.LoadAssetsFromBundle();

                for (int i = 0; i < 1; i++)
                {
                    float spawnDifference = UnityEngine.Random.Range(0.1f, 1.0f);
                    Vector3 spawnPosition = new Vector3(
                        playerCamera.position.x + forwardDirection.x * spawnDifference,
                        playerCamera.position.y + 1.0f,
                        playerCamera.position.z + forwardDirection.z * spawnDifference
                    );

                    crowdDelegatesInstance.Spawn_Milk(spawnPosition, Quaternion.identity);
                }
            });

            return new CrowdResponse(req.GetReqID(), status, message);
        }


      

       

        public class InteractableObject2 : MonoBehaviour
        {
            private static InteractableObject2 currentlyHeldObject = null;

            private bool isHeld = false;
            private Transform playerCamera;
            
            private Color originalColor;

            private Renderer renderer;
            

            private Transform playerTransform;

            private float maxPickupDistance = 3.5f;

            private float moveSpeed = 5.0f;

            private bool isHovered = false;



            private float interactionDelay = 1.0f;

            void Start()
            {
                playerTransform = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;

                playerCamera = Camera.main?.transform;

                if (playerCamera == null)
                {
                    playerCamera = FindObjectOfType<Camera>()?.transform;
                    if (playerCamera == null)
                    {
                        return;
                    }
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
                    transform.GetChild(0).transform.Rotate(Vector3.up, randomAngle);
                    //transform.Rotate(Vector3.up, randomAngle);  // Rotate the object on the Y-axis
                }

                // Make the object face the same direction as the camera
                transform.rotation = Quaternion.LookRotation(cameraTransform.forward);
            }


            private void TryPickupObjectUnderMouse()
            {
                Ray ray = playerCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(ray, maxPickupDistance);

                bool breadHit = false;

                foreach (RaycastHit hit in hits)
                {

                    if(!hit.rigidbody)
                    {
                        continue;
                    }


                    if (hit.rigidbody != null && hit.rigidbody.gameObject == gameObject)
                    {
                        breadHit = true;
                        break;
                    }
                }
                
                if (breadHit)
                {
                    PickUpObject();
                }
               
            }

            private void PickUpObject()
            {
                isHeld = true;
                currentlyHeldObject = this;
                
                
            }

            private void ReleaseObject()
            {

                isHeld = false;
                currentlyHeldObject = null;


                
                

                


                StartCoroutine(IgnorePlayerCollisionTemporarily());
            }

            private void ThrowObject()
            {
                

                Vector3 throwForce = playerCamera.forward * 10f;
                


                ReleaseObject();
            }

            private void EatObject()
            {



                transform.rotation = Quaternion.LookRotation(playerCamera.forward);
                transform.Rotate(Vector3.right, 90.0f); 




                StartCoroutine(EatAndDestroy());
            }

            private IEnumerator EatAndDestroy()
            {
                float elapsedTime = 0f;
                float duration = 1.0f;

                Vector3 initialPosition = transform.position;

                AudioSource audioSource = transform.GetChild(0).GetComponent<AudioSource>();

                if (audioSource != null)
                {
                    audioSource.Play();
                }

                while (elapsedTime < duration)
                {
                    transform.position = Vector3.Lerp(initialPosition, playerCamera.position + playerCamera.forward * 0.5f, elapsedTime / duration);
                    transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.95f, transform.localScale.z); // Slightly reduce height
                    elapsedTime += Time.deltaTime;

                    yield return null;
                }


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







        public static CrowdResponse GiveItemAtPlayer(ControlClient client, CrowdRequest req) //https://pastebin.com/BVEACvGA item list
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
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
                    return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "Unable to spawn at player.");

                }
            }
            try
            {



                TestMod.ActionQueue.Enqueue(() =>
                {

                    Transform pos = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform;
                    Vector3 position = pos.position;
                    Quaternion rotation = pos.rotation;



                    if (item2.isBigBox)
                    {


                        InteractablePackagingBox_Item interactablePackagingBox_Item = UnityEngine.Object.Instantiate<InteractablePackagingBox_Item>(CSingleton<RestockManager>.Instance.m_PackageBoxPrefab, new Vector3(position.x + 1.4f, position.y + 1.2f, position.z), rotation, CSingleton<RestockManager>.Instance.m_PackageBoxParentGrp);
                        interactablePackagingBox_Item.FillBoxWithItem(item2.itemType, 64);
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
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse SendHugeEmpty(ControlClient client, CrowdRequest req) //https://pastebin.com/BVEACvGA item list
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
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
                    return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "Unable to spawn at player.");

                }
            }
            try
            {



                TestMod.ActionQueue.Enqueue(() =>
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
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static void setProperty(System.Object a, string prop, System.Object val)
        {
            var f = a.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (f == null)
            {
                TestMod.mls.LogInfo($"Field {prop} not found in {a.GetType()}");
                return;
            }

            f.SetValue(a, val);
        }

        public static System.Object getProperty(System.Object a, string prop)
        {
            var f = a.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (f == null)
            {
                TestMod.mls.LogInfo($"Field {prop} not found in {a.GetType()}");
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
