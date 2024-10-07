using I2.Loc;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TMPro;
using UnityEngine;


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
                        if (req.targets[0].service == "twitch")
                        {
                            TestMod.twitchChannel = req.targets[0].name;
                        }
                    }
                    TestMod.NameOverride = req.viewer;
                    TestMod.isSmelly = false;
                    CustomerManager.Instance.m_CustomerCountMax = +1;
                    callFunc(CustomerManager.Instance, "AddCustomerPrefab", null);
                    Customer newCustomer = CM.GetNewCustomer(false);

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
        public static CrowdResponse EmptyCleansers(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            InteractableAutoCleanser IAC = CSingleton<InteractableAutoCleanser>.Instance;
            if (IAC.m_StoredItemList.Count == 0 || IAC.m_PosList.Count == 0) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "No Cleanser, or no Cleaners");
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            if (!CPlayerData.m_IsShopOpen || LightManager.GetHasDayEnded()) return new CrowdResponse(id: req.GetReqID(), status: CrowdResponse.Status.STATUS_RETRY, message: "Store is Closed");
            try
            {
                foreach (var cleanser in IAC.GetStoredItemList())
                {
                    if (cleanser != null)
                    {
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            cleanser.SetContentFill(0f);
                        });
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
                    Customer Smelly = CM.GetNewCustomer(true);
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
                    if (!m_WorkerList[workerCount].IsActive())
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
                    if (m_WorkerList[workerCount].IsActive() && !LightManager.GetHasDayEnded())
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

            bool m_IsCreditCardMode = (bool)getProperty(CSingleton<UI_CreditCardScreen>.Instance, "m_IsCreditCardMode");
            if (m_IsCreditCardMode) { TestMod.mls.LogInfo("Tried to Teleport in Card reader"); return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Player is in card Machine"); }
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
            if (CSingleton<CollectionBinderUI>.Instance.m_CollectionAlbum.gameObject.activeSelf) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Player is in Binder");
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
                        InteractablePackagingBox_Item interactablePackagingBox_Item = UnityEngine.Object.Instantiate<InteractablePackagingBox_Item>(CSingleton<RestockManager>.Instance.m_PackageBoxPrefab, new Vector3(position.x + 1.4f, position.y+ 1.2f, position.z), rotation, CSingleton<RestockManager>.Instance.m_PackageBoxParentGrp);
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
