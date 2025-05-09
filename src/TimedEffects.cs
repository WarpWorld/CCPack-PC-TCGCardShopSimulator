﻿using CMF;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine.Localization.Pseudo;
using UnityEngine;

namespace BepinControl
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
        OPENING_PACK
    }


    public class Timed
    {
        public TimedType type;
        public static float org_FOV = 80f;
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
                case TimedType.SET_LANGUAGE:
                    {
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            string newLang = TestMod.NewLanguage;
                            SettingScreen.Instance.OnPressLanguageSelect(newLang);
                        });
                        break;
                    }
                case TimedType.FORCE_MATH:
                    {
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            TestMod.ForceMath = true;
                        });
                        break;
                    }
                //case TimedType.GAME_ULTRA_SLOW://Something to look at, altering Timescale in game
                   // {
                        //TestMod.ActionQueue.Enqueue(() =>
                        //{
                           // CGameManager.Instance.m_TimeScale = (int)0.1;
                       // });
                        //break;
                   // }
                case TimedType.FORCE_CASH:
                    {
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            TestMod.ForceUseCredit = false;
                            TestMod.ForceUseCash = true;
                        });
                        break;
                    }
                case TimedType.FORCE_CARD:
                    {
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            TestMod.ForceUseCash = false;
                            TestMod.ForceUseCredit = true;
                        });
                        break;
                    }
                case TimedType.HIGH_FOV:
                    {
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            CameraFOVControl camera = CSingleton<CameraFOVControl>.Instance;
                            org_FOV = (float)CrowdDelegates.getProperty(camera, "m_CurrentFOV");
                            camera.UpdateFOV(140f);
                        });
                        break;
                    }
                case TimedType.LOW_FOV:
                    {
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            CameraFOVControl camera = CSingleton<CameraFOVControl>.Instance;
                            org_FOV = (float)CrowdDelegates.getProperty(camera, "m_CurrentFOV");
                            camera.UpdateFOV(10f);
                        });
                        break;
                    }
                case TimedType.INVERT_X:
                    {
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            InteractionPlayerController IPC = CSingleton<InteractionPlayerController>.Instance;
                            IPC.m_CameraMouseInput.invertHorizontalInput = !IPC.m_CameraMouseInput.invertHorizontalInput;
                        });
                        break;
                    }
                case TimedType.INVERT_Y:
                    {
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            InteractionPlayerController IPC = CSingleton<InteractionPlayerController>.Instance;
                            IPC.m_CameraMouseInput.invertVerticalInput = !IPC.m_CameraMouseInput.invertVerticalInput;

                        });
                        break;
                    }
                case TimedType.FORCE_EXACT_CHANGE:
                    {
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            TestMod.ExactChange = true;
                        });
                        break;
                    }
                case TimedType.FORCE_LARGE_BILLS:
                    {
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            TestMod.LargeBills = true;
                        });
                        break;
                    }
                case TimedType.OPENING_PACK:
                    {
                        /*
                        TestMod.ActionQueue.Enqueue(() =>
                        {
                            TestMod.autoOpenPacks = true;
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
                                        if (_item.GetItemType() == TestMod.spawnItem.itemType)
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
                                        TestMod.cardPack.OnDestroyed();
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
                    case TimedType.FORCE_CASH:
                        {
                            TestMod.ActionQueue.Enqueue(() =>
                            {
                                try
                                {
                                    TestMod.ForceUseCredit = false;
                                    TestMod.ForceUseCash = false;
                                }
                                catch (Exception e)
                                {
                                    TestMod.mls.LogInfo(e.ToString());
                                    Timed.removeEffect(etype);
                                }
                            });
                            break;
                        }
                    case TimedType.FORCE_CARD:
                        {
                            TestMod.ActionQueue.Enqueue(() =>
                            {
                                try
                                {
                                    TestMod.ForceUseCash = false;
                                    TestMod.ForceUseCredit = false;
                                }
                                catch (Exception e)
                                {
                                    TestMod.mls.LogInfo(e.ToString());
                                    Timed.removeEffect(etype);
                                }
                            });
                            break;
                        }
                    case TimedType.FORCE_MATH:
                        {
                            TestMod.ActionQueue.Enqueue(() =>
                            {
                                try
                                {
                                    TestMod.ForceMath = false;
                                }
                                catch (Exception e)
                                {
                                    TestMod.mls.LogInfo(e.ToString());
                                    Timed.removeEffect(etype);
                                }
                            });
                            break;
                        }
                    case TimedType.HIGH_FOV:
                    case TimedType.LOW_FOV:
                        {
                            TestMod.ActionQueue.Enqueue(() =>
                            {
                                CameraFOVControl camera = CSingleton<CameraFOVControl>.Instance;
                                camera.UpdateFOV(org_FOV);
                            });
                            break;
                        }
                    case TimedType.INVERT_X:
                        {
                            TestMod.ActionQueue.Enqueue(() =>
                            {
                                InteractionPlayerController IPC = CSingleton<InteractionPlayerController>.Instance;
                                IPC.m_CameraMouseInput.invertHorizontalInput = !IPC.m_CameraMouseInput.invertHorizontalInput;
                            });
                            break;
                        }
                    case TimedType.INVERT_Y:
                        {
                            TestMod.ActionQueue.Enqueue(() =>
                            {
                                InteractionPlayerController IPC = CSingleton<InteractionPlayerController>.Instance;
                                IPC.m_CameraMouseInput.invertVerticalInput = !IPC.m_CameraMouseInput.invertVerticalInput;
                            });
                            break;
                        }
                    case TimedType.SET_LANGUAGE:
                        {
                            TestMod.ActionQueue.Enqueue(() =>
                            {
                                string oldLang = TestMod.OrgLanguage;
                                SettingScreen.Instance.OnPressLanguageSelect(oldLang);
                            });
                            break;
                        }
                    case TimedType.FORCE_EXACT_CHANGE:
                        {
                            TestMod.ActionQueue.Enqueue(() =>
                            {
                                TestMod.ExactChange = false;
                            });
                            break;
                        }
                    case TimedType.FORCE_LARGE_BILLS:
                        {
                            TestMod.ActionQueue.Enqueue(() =>
                            {
                                TestMod.LargeBills = false;
                            });
                            break;
                        }
                        case TimedType.OPENING_PACK:
                        {
                            /*
                            TestMod.ActionQueue.Enqueue(() =>
                            {
                                TestMod.autoOpenPacks = false;
                                CrowdDelegates.setProperty(CardOpeningSequence.Instance, "m_IsAutoFire", false);
                                CrowdDelegates.setProperty(CardOpeningSequence.Instance, "m_IsAutoFireKeydown", false);
                            });
                            */
                            break;
                        }
                }
            } catch(Exception e)
            {
                TestMod.mls.LogInfo(e.ToString());
                return false;
            }
            return true;
        }

        static int frames = 0;

        public void tick()
        {
            frames++;
        }
    }
    public class TimedThread
    {
        public static List<TimedThread> threads = new List<TimedThread>();

        public readonly Timed effect;
        public int duration;
        public int remain;
        public int id;
        public bool paused;

        public static bool isRunning(TimedType t)
        {
            foreach (var thread in threads)
            {
                if (thread.effect.type == t) return true;
            }
            return false;
        }


        public static void tick()
        {
            foreach (var thread in threads)
            {
                if (!thread.paused)
                {
                   thread.effect.tick();
                }
            }
        }
        public static void addTime(int duration)
        {
            try
            {
                lock (threads)
                {
                    foreach (var thread in threads)
                    {
                        Interlocked.Add(ref thread.duration, duration+5);
                        if (!thread.paused)
                        {
                            int time = Volatile.Read(ref thread.remain);
                            new TimedResponse(thread.id, time, CrowdResponse.Status.STATUS_PAUSE).Send(ControlClient.Socket);
                            thread.paused = true;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                TestMod.mls.LogInfo(e.ToString());
            }
        }

        public static void tickTime(int duration)
        {
            try
            {
                lock (threads)
                {
                    foreach (var thread in threads)
                    {
                        int time = Volatile.Read(ref thread.remain);
                        time -= duration;
                        if (time < 0) time = 0;
                        Volatile.Write(ref thread.remain, time);
                    }
                }
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo(e.ToString());
            }
        }

        public static void unPause()
        {
            try
            {
                lock (threads)
                {
                    foreach (var thread in threads)
                    {
                        if (thread.paused)
                        {
                            int time = Volatile.Read(ref thread.remain);
                            new TimedResponse(thread.id, time, CrowdResponse.Status.STATUS_RESUME).Send(ControlClient.Socket);
                            thread.paused = false;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                TestMod.mls.LogInfo(e.ToString());
            }
        }

        public TimedThread(int id, TimedType type, int duration, Dictionary<string, object> customVariables = null)
        {
            this.effect = new Timed(type);
            this.duration = duration;
            this.remain = duration;
            this.id = id;
            paused = false;

            if (customVariables == null)
            {
                customVariables = new Dictionary<string, object>();
            }

            this.effect.SetCustomVariables(customVariables);
            try
            {
                lock (threads)
                {
                    threads.Add(this);
                }
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo(e.ToString());
            }
        }

        public void Run()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            effect.addEffect();
            bool error = false;
            try
            {
                do
                {
                    error = false;
                    int time = Volatile.Read(ref duration); ;
                    while (time > 0)
                    {
                        Interlocked.Add(ref duration, -time);
                        Thread.Sleep(time);

                        time = Volatile.Read(ref duration);
                    }
                    if (Timed.removeEffect(effect.type))
                    {
                        lock (threads)
                        {
                            threads.Remove(this);
                        }
                        new TimedResponse(id, 0, CrowdResponse.Status.STATUS_STOP).Send(ControlClient.Socket);
                    }
                    else error = true;
                } while (error);
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo(e.ToString());
            }
        }
    }
}
