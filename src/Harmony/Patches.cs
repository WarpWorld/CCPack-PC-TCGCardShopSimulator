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
        if (__result == null) return;
        try
        {
            CrowdControlMod.AddNamePlateToCustomer(__result);
        }
        catch (Exception e)
        {
            CrowdControlMod.mls?.LogWarning($"Could not add a nameplate to the customer: {e}");
        }
        try
        {
            CrowdControlMod.ConnectToTwitchChat();
        }
        catch (Exception e)
        {
            CrowdControlMod.mls?.LogWarning($"Could not start the Twitch chat listener: {e.Message}");
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

// The game (since the 2026-09-16 update) ticks PlayCardSetUI before its play-card set is assigned, which
// throws a NullReferenceException twice per frame per card set and floods the log badly enough to lag the
// game. Skip those ticks until the UI has been initialised; the game's own code does nothing useful before then.
[HarmonyPatch(typeof(PlayCardSetUI), "Update")]
public static class PlayCardSetUI_Update_Guard
{
    static bool Prefix(PlayCardSet ___m_PlayCardSet) => ___m_PlayCardSet != null && ___m_PlayCardSet.m_PlayTableGame != null;
}

[HarmonyPatch(typeof(PlayCardSetUI), "LateUpdate")]
public static class PlayCardSetUI_LateUpdate_Guard
{
    static bool Prefix(PlayCardSet ___m_PlayCardSet) => ___m_PlayCardSet != null && ___m_PlayCardSet.m_PlayTableGame != null;
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
