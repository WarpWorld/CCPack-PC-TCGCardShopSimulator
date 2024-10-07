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
using Dummiesman;
using static UnityEngine.ImageConversion;
using System.Collections;
using System.Net;


namespace BepinControl
{
    public delegate CrowdResponse CrowdDelegate(ControlClient client, CrowdRequest req);



    public class CrowdDelegates
    {
        public static System.Random rnd = new System.Random();
        public static int maxBoxCount = 100;

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
                            setProperty(customer, "m_IsChattyCustomer ", true);
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
            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return new CrowdResponse(id: req.GetReqID(), status: CrowdResponse.Status.STATUS_RETRY, message: "Store is Closed");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    if (req.targets != null)
                    {
                        if (req.targets[0].service == "twitch") {
                            TestMod.twitchChannel = req.targets[0].name;
                        }
                    }
                    TestMod.NameOverride = req.viewer;
                    TestMod.isSmelly = false;
                    CustomerManager.Instance.m_CustomerCountMax = + 1;
                    callFunc(CustomerManager.Instance, "AddCustomerPrefab", null);
                    Customer newCustomer = CM.GetNewCustomer();

                    TestMod.NameOverride = "";
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
            CustomerManager CM = CustomerManager.Instance;
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
                    Customer Smelly = CM.GetNewCustomer();
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
            int workerCount =0;
            Worker workerid = null;
            List<Worker> m_WorkerList = WorkerManager.GetWorkerList();
            if (!found)
            {
                try
                {
                    workerCount = UnityEngine.Random.Range(0, m_WorkerList.Count);
                    if (!m_WorkerList[workerCount].isActiveAndEnabled)
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
                    workerCount = UnityEngine.Random.Range(0, m_WorkerList.Count);
                    if (m_WorkerList[workerCount].m_IsActive && !LightManager.GetHasDayEnded())
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
            foreach(Customer c in cust)
            {
                if(c.m_CustomerCash.gameObject.activeSelf == true)
                {
                    if(TestMod.ForceUseCredit) c.m_CustomerCash.m_IsCard = true;
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
                case "dutch":
                    {
                        newLanguage = "Dutch";
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

            if (CPlayerData.m_UnlockWarehouseRoomCount == 8 || CPlayerData.m_IsWarehouseRoomUnlocked == false) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Storage is already unlocked.");
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

            if (CPlayerData.m_UnlockRoomCount == 20) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Storage is already unlocked.");
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
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    if (item2.isBigBox) RestockManager.SpawnPackageBoxItem(item2.itemType, 64, item2.isBigBox);
                    else RestockManager.SpawnPackageBoxItem(item2.itemType, 32, item2.isBigBox);
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
            string[] enteredText = req.code.Split('_');
            if (enteredText.Length > 0)
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
            catch(Exception e)
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


        private static Texture2D LoadTexture(string path)
        {
            if (File.Exists(path))
            {
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2);  // Create a new Texture2D
                if (texture.LoadImage(fileData))  // Load the image from the file
                {
                    return texture;  // Return the loaded texture
                }
            }
            Debug.LogError("Texture not found or failed to load: " + path);
            return null;  // Return null if the texture couldn't be loaded
        }





public class InteractableObject2 : MonoBehaviour
    {
        private static InteractableObject2 currentlyHeldObject = null; 

        private bool isHeld = false;
        private Transform playerCamera;
        private Rigidbody rb;
        private Color originalColor;

        private Renderer renderer;
        private Collider breadCollider;  

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

                rb = GetComponent<Rigidbody>();


                renderer = GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    originalColor = renderer.material.color;
                }


                breadCollider = GetComponent<Collider>();
                if (breadCollider == null)
                {
                    breadCollider = gameObject.AddComponent<BoxCollider>();
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
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    EatObject();
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

        private void SmoothlyHoldObjectInFront()
        {
            rb.isKinematic = true; 
            breadCollider.enabled = false;

            Vector3 targetPosition = playerCamera.position + playerCamera.forward * 0.7f;
            targetPosition.y -= 0.2f; 

          
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            transform.rotation = Quaternion.LookRotation(playerCamera.forward);
            transform.Rotate(Vector3.right, 90.0f); 
        }

        private void TryPickupObjectUnderMouse()
        {
            Ray ray = playerCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, maxPickupDistance);

            bool breadHit = false;

            foreach (RaycastHit hit in hits)
            {
                Debug.Log("Raycast hit: " + hit.collider.gameObject.name);

                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    breadHit = true; 
                    Debug.Log("Bread object hit, attempting to pick it up.");
                    break; 
                }
            }

            if (breadHit)
            {
                PickUpObject(); 
            }
            else
            {
                Debug.Log("No bread object found by the raycast.");
            }
        }

        private void PickUpObject()
        {
            isHeld = true;
            currentlyHeldObject = this;  
            rb.isKinematic = true; 
            breadCollider.enabled = false;  
            Debug.Log("Bread object picked up.");
        }

        private void ReleaseObject()
        {
            Debug.Log("Attempting to release the bread object.");

            isHeld = false;
            currentlyHeldObject = null;  

           
            rb.isKinematic = false;
            breadCollider.enabled = true;

            Debug.Log("Bread object released: Kinematic = " + rb.isKinematic + ", Collider enabled = " + breadCollider.enabled);

            
            StartCoroutine(IgnorePlayerCollisionTemporarily());
        }

        private void ThrowObject()
        {
            Debug.Log("Throwing the bread object.");
            rb.isKinematic = false; 

            Vector3 throwForce = playerCamera.forward * 10f; 
            rb.AddForce(throwForce, ForceMode.Impulse);

         
            ReleaseObject();
        }

        private void EatObject()
        {

    

            transform.rotation = Quaternion.LookRotation(playerCamera.forward);
            transform.Rotate(Vector3.right, 90.0f); // Rotate to lay flat on the camera




            StartCoroutine(EatAndDestroy());
        }

        private IEnumerator EatAndDestroy()
        {
            float elapsedTime = 0f;
            float duration = 1.0f;

            Vector3 initialPosition = transform.position;

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
                Physics.IgnoreCollision(breadCollider, playerCollider, true);  // Ignore collisions

                yield return new WaitForSeconds(interactionDelay);

                Physics.IgnoreCollision(breadCollider, playerCollider, false);
                Debug.Log("Bread object interaction with player re-enabled.");
            }
            else
            {
                Debug.LogError("Player collider not found.");
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



                    /* bread test 
                    string objPath = Path.Combine(Paths.PluginPath, "CrowdControl", "Bread.obj");
                    string mtlPath = Path.Combine(Paths.PluginPath, "CrowdControl", "Bread.mtl");
                    string pngPath = Path.Combine(Paths.PluginPath, "CrowdControl", "Bread.png");

                    if (File.Exists(objPath))
                    {
                        GameObject originalBread = null;

                        // Load the OBJ file once to get the original object
                        using (FileStream objStream = new FileStream(objPath, FileMode.Open, FileAccess.Read))
                        using (FileStream mtlStream = new FileStream(mtlPath, FileMode.Open, FileAccess.Read))
                        {
                            originalBread = new OBJLoader().Load(objStream, mtlStream);

                            if (originalBread == null)
                            {
                                Debug.LogError("Failed to load object. The OBJLoader returned null.");
                                return;
                            }

                            // Set the scale of the original bread object
                            originalBread.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);  // Uniform scaling
                        }

                        // Now spawn 30 copies of the original bread object
                        for (int i = 0; i < 10; i++)  // Loop to spawn 30 breads
                        {
                            // Randomize height above the player, but keep X and Z the same as the player's position
                            float spawnHeightAbovePlayer = UnityEngine.Random.Range(5.0f, 10.0f);  // Bread spawns 5 to 10 units above the player
                            Vector3 spawnPosition = new Vector3(
                                pos.position.x,             // Player's X position
                                pos.position.y + spawnHeightAbovePlayer,  // Spawn above player
                                pos.position.z              // Player's Z position
                            );

                            // Clone the original bread object to create a new instance
                            GameObject breadInstance = UnityEngine.Object.Instantiate(originalBread, spawnPosition, Quaternion.identity);


                            Renderer renderer = breadInstance.GetComponent<Renderer>();

                            if (renderer != null)
                            {
                                if (renderer.material.shader.name != "Standard")
                                {
                                    renderer.material.shader = Shader.Find("Standard");  // Set the shader to Standard
                                    Debug.Log("Shader set to Standard.");
                                }


                                if (renderer.material.mainTexture == null)  // Check if the texture didn't load
                                {
                                    Debug.LogWarning("Applying default texture to bread.");
                                    Texture2D defaultTexture = LoadTexture(pngPath);
                                    renderer.material.mainTexture = defaultTexture;
                                }
                            }


                            // Add BoxCollider or MeshCollider to ensure the bread can collide with the floor
                            if (breadInstance.GetComponent<Collider>() == null)
                            {
                                // Option 1: Add a BoxCollider (simpler, usually works well)
                                BoxCollider boxCollider = breadInstance.AddComponent<BoxCollider>();

                                // Option 2: Use MeshCollider with convex (if you need complex collision)
                                // MeshCollider meshCollider = breadInstance.AddComponent<MeshCollider>();
                                // meshCollider.convex = true;  // Convex is required for dynamic objects
                            }

                            // Add Rigidbody to the bread instance for physics
                            Rigidbody rb = breadInstance.GetComponent<Rigidbody>();
                            if (rb == null)
                            {
                                rb = breadInstance.AddComponent<Rigidbody>();
                                rb.mass = 1.0f;
                                rb.useGravity = true;  // Enable gravity
                                rb.drag = 0.5f;        // Adjust drag to control movement
                                rb.angularDrag = 0.05f;  // Adjust angular drag to slow rotation
                            }

                            // Optional: Make the bread interactable if needed
                            breadInstance.AddComponent<InteractableObject2>();  // Script to handle interaction (optional)
                        }

                       
                    }
                    else
                    {
                        Debug.LogError("OBJ file not found: " + objPath);
                    }


                    */






                    

                    if (item2.isBigBox)
                    {


                        InteractablePackagingBox_Item interactablePackagingBox_Item = UnityEngine.Object.Instantiate<InteractablePackagingBox_Item>(CSingleton<RestockManager>.Instance.m_PackageBoxPrefab, new Vector3(position.x + 1.4f, position.y + 1.2f, position.z), rotation, CSingleton<RestockManager>.Instance.m_PackageBoxParentGrp);
                        interactablePackagingBox_Item.FillBoxWithItem(item2.itemType, 64);
                        interactablePackagingBox_Item.name = interactablePackagingBox_Item.m_ObjectType.ToString() + getProperty(CSingleton<RestockManager>.Instance, "m_SpawnedBoxCount");


                        /*
                         * auto open test
                        FieldInfo itemListField = typeof(ItemSpawnManager).GetField("m_ItemList", BindingFlags.NonPublic | BindingFlags.Instance);


                        if (itemListField != null)
                        {
                            List<Item> spawnedItems = (List<Item>)itemListField.GetValue(CSingleton<ItemSpawnManager>.Instance);




                            if (spawnedItems != null)
                            {
                                foreach (Item _item in spawnedItems)
                                {


                                    if (_item.GetItemType() == EItemType.RareCardPack)
                                    {
                                        CSingleton<CardOpeningSequence>.Instance.ReadyingCardPack(_item);
                                        interactablePackagingBox_Item.OnDestroyed();
                                        break;
                                    }
                                }
                            }
                        }
                        */

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
