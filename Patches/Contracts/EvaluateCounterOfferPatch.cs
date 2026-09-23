using HarmonyLib;
#if MONO
using ScheduleOne.Economy;
#else
using Il2CppScheduleOne.Economy;
#endif
using HighBaller.Core;

namespace HighBaller.Patches.Contracts;

[HarmonyPatch(typeof(Customer), "EvaluateCounteroffer")]
public static class EvaluateCounterOfferPatch
{
    public static bool Prefix(Customer __instance, ref bool __result)
    {
        if (!Config.AlwaysAcceptCounterOffersEnabled.Value) return true;
        if (__instance != null && Config.GetCustomNpcMode(__instance.NPC) >= S1APINPCHandling.NoAutomation) return true;
        __result = true;
        Main.Log("AlwaysAcceptCounterOffers: forcing accept for " +
                 (__instance != null && __instance.customerData != null ? __instance.customerData.name : "unknown"));
        return false;
    }

    public static void Postfix(Customer __instance, bool __result)
    {
        if (!__result)
            Main.Log("Counter failed", false, true);
        else
            DealAutoScheduler.TryAutoScheduleDeal(__instance);
    }
}