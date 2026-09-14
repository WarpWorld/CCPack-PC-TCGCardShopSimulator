#nullable disable
using CMF;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace CrowdControl
{


    public enum TimedType
    {
        GAME_ULTRA_SLOW,
        GAME_SLOW,
        GAME_FAST,
        GAME_ULTRA_FAST,
        HIGH_FOV,
        LOW_FOV,
        SET_LANGUAGE,
        FORCE_CARD,
        FORCE_CASH,
        FORCE_MATH,
        INVERT_X,
        INVERT_Y,
        FORCE_EXACT_CHANGE,
        FORCE_REQUIRE_CHANGE,
        FORCE_LARGE_BILLS,
        ALLOW_MISCHARGE,
        WORKERS_FAST,
        HUGE_BOXES,
        OPENING_PACK,
        PLAYER_FAST,
        PLAYER_SLOW,
        LOW_GRAVITY,
        HYPER_CUSTOMERS,
        MUTE_AUDIO
    }


    public class Timed
    {
        public TimedType type;
        public static float org_FOV = 80f;
        public static float org_MoveSpeed = 7f;
        public static float org_Gravity = 30f;
        public static float org_JumpSpeed = 10f;
        public const float SLOWMO_SCALE = 0.5f;
        public const float FASTFORWARD_SCALE = 2f;
        public const float HYPER_CUSTOMER_MULTIPLIER = 4f;

        /// <summary>Flips the payment type of customers already standing at the counter so the effect is visible immediately.</summary>
        public static void ApplyPaymentTypeToWaitingCustomers()
        {
            try
            {
                List<Customer> customers = CSingleton<CustomerManager>.Instance.GetCustomerList();
                if (customers == null) return;
                foreach (Customer c in customers)
                {
                    if (c == null || c.m_CustomerCash == null || !c.m_CustomerCash.gameObject.activeSelf) continue;
                    if (CrowdControlMod.ForceUseCredit) c.m_CustomerCash.m_IsCard = true;
                    if (CrowdControlMod.ForceUseCash) c.m_CustomerCash.m_IsCard = false;
                }
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo(e.ToString());
            }
        }

        public static bool IsGamePaused()
        {
            try
            {
                return PauseScreen.Instance != null && PauseScreen.Instance.m_ScreenGrp != null && PauseScreen.Instance.m_ScreenGrp.activeSelf;
            }
            catch
            {
                return false;
            }
        }

        public static void SetTimeScale(float scale)
        {
            if (IsGamePaused()) return;
            Time.timeScale = scale;
        }

        public static void ApplyCustomerSpeedMultiplier(float multiplier)
        {
            try
            {
                List<Customer> customers = CSingleton<CustomerManager>.Instance.GetCustomerList();
                if (customers == null) return;
                foreach (Customer customer in customers)
                {
                    if (customer == null || !customer.isActiveAndEnabled) continue;
                    if (multiplier <= 1f) customer.ResetExtraSpeedMultiplier();
                    else customer.SetExtraSpeedMultiplier(multiplier);
                }
            }
            catch (Exception e)
            {
                CrowdControlMod.mls.LogInfo(e.ToString());
            }
        }
        float old;
        

        private static Dictionary<string, object> customVariables = new Dictionary<string, object>();

        public static T GetCustomVariable<T>(string key)
        {
            if (customVariables.TryGetValue(key, out object value))
            {
                return (T)value;
            }

            throw new KeyNotFoundException($"Custom variable with key '{key}' not found.");
        }

        public void SetCustomVariables(Dictionary<string, object> variables)
        {
            customVariables = variables;
        }

        public Timed(TimedType t) { 
            type = t;
        }

        public void addEffect()
        {
            switch (type)
            {
                case TimedType.GAME_SLOW:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() => { SetTimeScale(SLOWMO_SCALE); });
                        break;
                    }
                case TimedType.GAME_FAST:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() => { SetTimeScale(FASTFORWARD_SCALE); });
                        break;
                    }
                case TimedType.PLAYER_FAST:
                case TimedType.PLAYER_SLOW:
                    {
                        bool fast = type == TimedType.PLAYER_FAST;
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            AdvancedWalkerController walker = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl;
                            org_MoveSpeed = walker.movementSpeed;
                            walker.movementSpeed = org_MoveSpeed * (fast ? 2.5f : 0.35f);
                        });
                        break;
                    }
                case TimedType.LOW_GRAVITY:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            AdvancedWalkerController walker = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl;
                            org_Gravity = walker.gravity;
                            org_JumpSpeed = walker.jumpSpeed;
                            walker.gravity = org_Gravity * 0.2f;
                            walker.jumpSpeed = org_JumpSpeed * 1.5f;
                        });
                        break;
                    }
                case TimedType.HYPER_CUSTOMERS:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() => { ApplyCustomerSpeedMultiplier(HYPER_CUSTOMER_MULTIPLIER); });
                        break;
                    }
                case TimedType.MUTE_AUDIO:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() => { SoundManager.MuteAllSound(); });
                        break;
                    }
                case TimedType.SET_LANGUAGE:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            string newLang = CrowdControlMod.NewLanguage;
                            SettingScreen.Instance.OnPressLanguageSelect(newLang);
                        });
                        break;
                    }
                case TimedType.FORCE_MATH:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            CrowdControlMod.ForceMath = true;
                        });
                        break;
                    }
                //case TimedType.GAME_ULTRA_SLOW://Something to look at, altering Timescale in game
                   // {
                        //CrowdControlMod.ActionQueue.Enqueue(() =>
                        //{
                           // CGameManager.Instance.m_TimeScale = (int)0.1;
                       // });
                        //break;
                   // }
                case TimedType.FORCE_CASH:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            CrowdControlMod.ForceUseCredit = false;
                            CrowdControlMod.ForceUseCash = true;
                            ApplyPaymentTypeToWaitingCustomers();
                        });
                        break;
                    }
                case TimedType.FORCE_CARD:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            CrowdControlMod.ForceUseCash = false;
                            CrowdControlMod.ForceUseCredit = true;
                            ApplyPaymentTypeToWaitingCustomers();
                        });
                        break;
                    }
                case TimedType.HIGH_FOV:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            CameraFOVControl camera = CSingleton<CameraFOVControl>.Instance;
                            org_FOV = (float)GameActions.getProperty(camera, "m_CurrentFOV");
                            camera.UpdateFOV(140f);
                        });
                        break;
                    }
                case TimedType.LOW_FOV:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            CameraFOVControl camera = CSingleton<CameraFOVControl>.Instance;
                            org_FOV = (float)GameActions.getProperty(camera, "m_CurrentFOV");
                            camera.UpdateFOV(10f);
                        });
                        break;
                    }
                case TimedType.INVERT_X:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            InteractionPlayerController IPC = CSingleton<InteractionPlayerController>.Instance;
                            IPC.m_CameraMouseInput.invertHorizontalInput = !IPC.m_CameraMouseInput.invertHorizontalInput;
                        });
                        break;
                    }
                case TimedType.INVERT_Y:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            InteractionPlayerController IPC = CSingleton<InteractionPlayerController>.Instance;
                            IPC.m_CameraMouseInput.invertVerticalInput = !IPC.m_CameraMouseInput.invertVerticalInput;

                        });
                        break;
                    }
                case TimedType.FORCE_EXACT_CHANGE:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            CrowdControlMod.ExactChange = true;
                        });
                        break;
                    }
                case TimedType.FORCE_LARGE_BILLS:
                    {
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            CrowdControlMod.LargeBills = true;
                        });
                        break;
                    }
                case TimedType.OPENING_PACK:
                    {
                        /*
                        CrowdControlMod.ActionQueue.Enqueue(() =>
                        {
                            CrowdControlMod.autoOpenPacks = true;
                            FieldInfo itemListField = typeof(ItemSpawnManager).GetField("m_ItemList", BindingFlags.NonPublic | BindingFlags.Instance);

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
                                        if (_item.GetItemType() == CrowdControlMod.spawnItem.itemType)
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
                                        CrowdControlMod.cardPack.OnDestroyed();
                                    }
                                }
                            }
                        });
                        */
                        break;
                    }
            }
        }

    
        public static bool removeEffect(TimedType etype)
        {
            try
            {
                switch(etype)
                {
                    case TimedType.GAME_SLOW:
                    case TimedType.GAME_FAST:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() => { SetTimeScale(1f); });
                            break;
                        }
                    case TimedType.PLAYER_FAST:
                    case TimedType.PLAYER_SLOW:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.movementSpeed = org_MoveSpeed;
                            });
                            break;
                        }
                    case TimedType.LOW_GRAVITY:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                AdvancedWalkerController walker = CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl;
                                walker.gravity = org_Gravity;
                                walker.jumpSpeed = org_JumpSpeed;
                            });
                            break;
                        }
                    case TimedType.HYPER_CUSTOMERS:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() => { ApplyCustomerSpeedMultiplier(1f); });
                            break;
                        }
                    case TimedType.MUTE_AUDIO:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() => { SoundManager.UnMuteAllSound(); });
                            break;
                        }
                    case TimedType.FORCE_CASH:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                try
                                {
                                    CrowdControlMod.ForceUseCredit = false;
                                    CrowdControlMod.ForceUseCash = false;
                                }
                                catch (Exception e)
                                {
                                    CrowdControlMod.mls.LogInfo(e.ToString());
                                    Timed.removeEffect(etype);
                                }
                            });
                            break;
                        }
                    case TimedType.FORCE_CARD:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                try
                                {
                                    CrowdControlMod.ForceUseCash = false;
                                    CrowdControlMod.ForceUseCredit = false;
                                }
                                catch (Exception e)
                                {
                                    CrowdControlMod.mls.LogInfo(e.ToString());
                                    Timed.removeEffect(etype);
                                }
                            });
                            break;
                        }
                    case TimedType.FORCE_MATH:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                try
                                {
                                    CrowdControlMod.ForceMath = false;
                                }
                                catch (Exception e)
                                {
                                    CrowdControlMod.mls.LogInfo(e.ToString());
                                    Timed.removeEffect(etype);
                                }
                            });
                            break;
                        }
                    case TimedType.HIGH_FOV:
                    case TimedType.LOW_FOV:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                CameraFOVControl camera = CSingleton<CameraFOVControl>.Instance;
                                camera.UpdateFOV(org_FOV);
                            });
                            break;
                        }
                    case TimedType.INVERT_X:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                InteractionPlayerController IPC = CSingleton<InteractionPlayerController>.Instance;
                                IPC.m_CameraMouseInput.invertHorizontalInput = !IPC.m_CameraMouseInput.invertHorizontalInput;
                            });
                            break;
                        }
                    case TimedType.INVERT_Y:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                InteractionPlayerController IPC = CSingleton<InteractionPlayerController>.Instance;
                                IPC.m_CameraMouseInput.invertVerticalInput = !IPC.m_CameraMouseInput.invertVerticalInput;
                            });
                            break;
                        }
                    case TimedType.SET_LANGUAGE:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                string oldLang = CrowdControlMod.OrgLanguage;
                                SettingScreen.Instance.OnPressLanguageSelect(oldLang);
                            });
                            break;
                        }
                    case TimedType.FORCE_EXACT_CHANGE:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                CrowdControlMod.ExactChange = false;
                            });
                            break;
                        }
                    case TimedType.FORCE_LARGE_BILLS:
                        {
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                CrowdControlMod.LargeBills = false;
                            });
                            break;
                        }
                        case TimedType.OPENING_PACK:
                        {
                            /*
                            CrowdControlMod.ActionQueue.Enqueue(() =>
                            {
                                CrowdControlMod.autoOpenPacks = false;
                                GameActions.setProperty(CardOpeningSequence.Instance, "m_IsAutoFire", false);
                                GameActions.setProperty(CardOpeningSequence.Instance, "m_IsAutoFireKeydown", false);
                            });
                            */
                            break;
                        }
                }
            } catch(Exception e)
            {
                CrowdControlMod.mls.LogInfo(e.ToString());
                return false;
            }
            return true;
        }

        static int frames = 0;

        public void tick()
        {
            frames++;
            switch (type)
            {
                case TimedType.GAME_SLOW:
                    SetTimeScale(SLOWMO_SCALE);//PauseScreen resets Time.timeScale to 1 on unpause, so keep re-applying
                    break;
                case TimedType.GAME_FAST:
                    SetTimeScale(FASTFORWARD_SCALE);
                    break;
                case TimedType.HYPER_CUSTOMERS:
                    if (frames % 60 == 0) ApplyCustomerSpeedMultiplier(HYPER_CUSTOMER_MULTIPLIER);//catch customers that spawned after the effect started
                    break;
            }
        }
    }
}
