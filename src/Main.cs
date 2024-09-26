using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Threading;
using UnityEngine.EventSystems;
using System.CodeDom;



namespace BepinControl
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class TestMod : BaseUnityPlugin
    {
        // Mod Details
        private const string modGUID = "WarpWorld.CrowdControl";
        private const string modName = "Crowd Control";
        private const string modVersion = "1.0.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static ManualLogSource mls;

        internal static TestMod Instance = null;
        private ControlClient client = null;
        public static bool isFocused = true;
        public static bool doneItems = false;
        public static bool ForceMath = false;
        public static bool WorkersFast = false;
        public static bool ForceUseCash = false;
        public static bool ForceUseCredit = false;
        public static bool isWarehouseUnlocked = false;
        public static int WareHouseRoomsUnlocked = 0;
        public static int ShopRoomUnlocked = 0;
        public static string NameOverride = "";

        void Awake()
        {


            Instance = this;
            mls = BepInEx.Logging.Logger.CreateLogSource("Crowd Control");

            mls.LogInfo($"Loaded {modGUID}. Patching.");
            harmony.PatchAll(typeof(TestMod));
            harmony.PatchAll();

            mls.LogInfo($"Initializing Crowd Control");

            try
            {
                client = new ControlClient();
                new Thread(new ThreadStart(client.NetworkLoop)).Start();
                new Thread(new ThreadStart(client.RequestLoop)).Start();
            }
            catch (Exception e)
            {
                mls.LogInfo($"CC Init Error: {e.ToString()}");
            }

            mls.LogInfo($"Crowd Control Initialized");


            mls = Logger;
        }


        public static Queue<Action> ActionQueue = new Queue<Action>();

        //attach this to some game class with a function that runs every frame like the player's Update()
        [HarmonyPatch(typeof(CGameManager), "Update")]
        [HarmonyPrefix]
        static void RunEffects()
        {
            if(CGameManager.Instance.m_IsGameLevel && !doneItems)//lets print all card arrays in the restock data, so we can use them
            {
                foreach (var cardPack in CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.ToArray())
                {
                    TestMod.mls.LogInfo(cardPack.name + " : Warehouse Rooms: "+ UnlockRoomManager.Instance.m_LockedRoomBlockerList.Count);

                }
                doneItems = true;
            }
            
            while (ActionQueue.Count > 0)
            {
                Action action = ActionQueue.Dequeue();
                action.Invoke();
            }

            lock (TimedThread.threads)
            {
                foreach (var thread in TimedThread.threads)
                {
                    if (!thread.paused)
                        thread.effect.tick();
                }
            }

        }


        [HarmonyPatch(typeof(EventSystem), "OnApplicationFocus")]
        public static class EventSystem_OnApplicationFocus_Patch
        {
            static void Postfix(bool hasFocus)
            {
                isFocused = hasFocus;
            }
        }

        [HarmonyPatch(typeof(Customer), "OnCashTaken")]
        public static class doPaymentPatch
        {
            public static void prefix(bool isCard)
            {
                if(ForceUseCash) { isCard = false; return; }
                if(ForceUseCredit) { isCard = true; return; }
            }
        }
            
    }

}
