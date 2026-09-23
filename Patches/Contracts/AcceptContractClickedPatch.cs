using HarmonyLib;
#if MONO
using ScheduleOne.Economy;
#else
using Il2CppScheduleOne.Economy;
#endif
using HighBaller.Core;

namespace HighBaller.Patches.Contracts;

[HarmonyPatch(typeof(Customer), "AcceptContractClicked")]
public static class AcceptContractClickedPatch
{
    public static void Postfix(Customer __instance)
    {
        DealAutoScheduler.TryAutoScheduleDeal(__instance);
    }
}