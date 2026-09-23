using HarmonyLib;
#if MONO
using ScheduleOne.UI.Handover;
#else
using Il2CppScheduleOne.UI.Handover;
#endif

namespace HighBaller.Patches.Handover;

[HarmonyPatch(typeof(HandoverScreenContractMode), "Open")]
public static class HandoverScreenOpenPatch
{
    public static bool Prefix(HandoverScreenContractMode __instance)
    {
        if (__instance.CurrentCustomer != null && __instance.CurrentCustomer.CustomerData != null) return true;
        Main.Log("HandoverScreen.Open blocked: customer or customerData is null (likely S1API custom NPC).", false,
            true);
        return false;
    }
}