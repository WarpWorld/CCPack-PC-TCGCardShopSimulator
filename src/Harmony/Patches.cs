#nullable disable
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace CrowdControl.Harmony;

// All patches here are applied by harmony.PatchAll() in CrowdControlMod.Awake, except
// CustomerManagerPatches which is applied manually because its target has an overload.

[HarmonyPatch(typeof(TitleScreen), "Start")]
public static class TitleScreenPatch
{
    public static void Postfix(ref TextMeshProUGUI ___m_VersionText)
    {
        ___m_VersionText.text += "\nCrowd Control version: v" + CrowdControlMod.Instance.Version;
        ___m_VersionText.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
    }
}

public static class CustomerManagerPatches
{
    public static void ApplyPatches(HarmonyLib.Harmony harmonyInstance)
    {
        var original = typeof(CustomerManager).GetMethod("GetNewCustomer", new Type[] { typeof(bool) });
        var postfix = new HarmonyMethod(typeof(CustomerManagerPatches).GetMethod(nameof(GetNewCustomerPostfix), BindingFlags.Static | BindingFlags.NonPublic));
        harmonyInstance.Patch(original, null, postfix);
    }

    private static void GetNewCustomerPostfix(Customer __result)
    {
        if (__result != null)
        {
            CrowdControlMod.AddNamePlateToCustomer(__result);
            CrowdControlMod.ConnectToTwitchChat();
        }
    }
}

[HarmonyPatch(typeof(InteractionPlayerController), "OnEnable")]
public static class Patch_OnEnable
{
    static void Postfix()
    {
        CrowdControlMod.loadedIntoWorld = true;
    }
}

[HarmonyPatch(typeof(UI_CashCounterScreen), "UpdateMoneyChangeAmount")]
public static class UI_CashCounterScreen_Patch
{
    static void Postfix(UI_CashCounterScreen __instance)
    {
        if (!CrowdControlMod.ForceMath) return;
        TextMeshProUGUI text = __instance.m_ChangeToGiveAmountText;

        if (text != null)
        {
            text.text = "DO THE MATH";
        }
    }
}

[HarmonyPatch(typeof(InteractableCustomerCash), "SetIsCard")]
public static class SetIsCardPatch
{
    public static void Prefix(ref bool isCard)
    {
        if (CrowdControlMod.ForceUseCash)
        {
            isCard = false;
            return;
        }
        if (CrowdControlMod.ForceUseCredit)
        {
            isCard = true;
        }
    }
}

[HarmonyPatch(typeof(InteractableCashierCounter), "StartGivingChange")]
public static class StartGivingChangePatch
{
    public static void Prefix(InteractableCashierCounter __instance, ref bool ___m_IsUsingCard)
    {
        if (CrowdControlMod.ForceUseCash)
        {
            ___m_IsUsingCard = false;
        }

        if (CrowdControlMod.ForceUseCredit)
        {
            ___m_IsUsingCard = true;
        }
    }
}

[HarmonyPatch(typeof(Customer), "EvaluateFinishScanItem")]
public static class LargeBillsPatch
{
    public static void Postfix(ref InteractableCustomerCash ___m_CustomerCash)
    {
        if (CrowdControlMod.LargeBills && ___m_CustomerCash.m_IsCard == false)//only trigger on cash effects,
        {
            float size = UnityEngine.Random.Range(26.0f, 42.0f);//match cash to cards, to make it more noticeable.
            ___m_CustomerCash.m_CashModel.transform.localScale = new Vector3(size, size, size);
            ___m_CustomerCash.m_CashOutlineModel.transform.localScale = new Vector3(size, size, size);
        }
        else if (CrowdControlMod.LargeBills && ___m_CustomerCash.m_IsCard == true)//Trigger on Card Payments too
        {
            float size = UnityEngine.Random.Range(26.0f, 42.0f);//make cards more noticeable
            ___m_CustomerCash.m_CardModel.transform.localScale = new Vector3(size, size, size);
            ___m_CustomerCash.m_CardOutlineModel.transform.localScale = new Vector3(size, size, size);
        }
        else if (!CrowdControlMod.LargeBills)//revert to default sizes
        {
            ___m_CustomerCash.m_CardModel.transform.localScale = CrowdControlMod.oldCardScale;
            ___m_CustomerCash.m_CardOutlineModel.transform.localScale = CrowdControlMod.oldCardScaleOutline;
            ___m_CustomerCash.m_CashModel.transform.localScale = CrowdControlMod.oldcashScale;
            ___m_CustomerCash.m_CashOutlineModel.transform.localScale = CrowdControlMod.oldcashScaleOutline;
        }
    }
}

[HarmonyPatch(typeof(CardOpeningSequence), "Update")]
public static class Patch_CardOpeningSequence_Update
{
    static readonly FieldInfo autoFireField = typeof(CardOpeningSequence).GetField("m_IsAutoFire", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo autoFireKeydownField = typeof(CardOpeningSequence).GetField("m_IsAutoFireKeydown", BindingFlags.NonPublic | BindingFlags.Instance);

    static void Postfix(CardOpeningSequence __instance)
    {
        if (CrowdControlMod.autoOpenCards == 0) return;
        bool autoOpen = CrowdControlMod.autoOpenCards == 1;
        autoFireField.SetValue(__instance, autoOpen);
        autoFireKeydownField.SetValue(__instance, autoOpen);

        if (CrowdControlMod.autoOpenCards == 2) CrowdControlMod.autoOpenCards = 0;
    }
}

[HarmonyPatch(typeof(CustomerManager), "PlayerFinishOpenCardPack")]
public static class Patch_PlayerFinishOpenCardPack
{
    static void Postfix(CustomerManager __instance)
    {
        if (CrowdControlMod.autoOpenCards == 1)
        {
            CrowdControlMod.autoOpenCards = 2;
        }
    }
}

[HarmonyPatch(typeof(CustomerManager), "GetCustomerExactChangeChance")]
public static class HarmonyPatch_CustomerManager_GetCustomerExactChangeChance
{
    private static bool Prefix(ref int __result)
    {
        if (CrowdControlMod.ExactChange)
        {
            __result = 100;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(Customer), "GetRandomPayAmount")]
public static class HarmonyPatch_Customer_GetRandomPayAmount
{
    private static bool Prefix(double limit, ref double __result)
    {
        if (CrowdControlMod.ExactChange)
        {
            __result = limit;
            return false;
        }
        return true;
    }
}
