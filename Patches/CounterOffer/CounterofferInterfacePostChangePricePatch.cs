using HarmonyLib;
#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.UI;
using ScheduleOne.UI.Phone.Messages;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone.Messages;
#endif
using HighBaller.Core;

namespace HighBaller.Patches.CounterOffer;

[HarmonyPatch(typeof(AmountSelector), "ChangeAmount")]
public static class CounterofferInterfacePostChangePricePatch
{
    private static void Postfix(AmountSelector __instance, float change)
    {
        var instance = MessagesApp.Instance;
        if (instance == null || instance.CounterofferInterface == null ||
            instance.CounterofferInterface.PriceSelector != __instance) return;
        if (Main.SuppressNextPriceEval)
            Main.SuppressNextPriceEval = false;
        else
            OfferEvaluator.OfferPriceChangeCheckNoLog();
    }
}