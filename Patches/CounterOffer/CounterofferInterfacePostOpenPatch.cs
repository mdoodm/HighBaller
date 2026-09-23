using System;
using HarmonyLib;
#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Messaging;
using ScheduleOne.Product;
using ScheduleOne.UI.Phone;
using ScheduleOne.UI.Phone.Messages;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.UI.Phone.Messages;
#endif
using HighBaller.Core;
using HighBaller.Helpers;

namespace HighBaller.Patches.CounterOffer;

[HarmonyPatch(typeof(CounterofferInterface), "Open")]
public static class CounterofferInterfacePostOpenPatch
{
    private static void Postfix(ProductDefinition product, int quantity, float price, MSGConversation _conversation,
        Action<ProductDefinition, int, float> _orderConfirmedCallback)
    {
        var instance = MessagesApp.Instance;
        var customerFromConversation = CustomerHelper.GetCustomerFromConversation(_conversation);
        if (instance == null || product == null || customerFromConversation == null)
        {
            Main.Log("Counteroffer open automation skipped because the UI, product, or customer is unavailable.", false,
                true);
        }
        else if (Config.GetCustomNpcMode(customerFromConversation.NPC) < S1APINPCHandling.Exclude)
        {
            var (optimalQuantity, optimalPrice) =
                DealCalculator.FindOptimalQuantity(customerFromConversation, product, quantity, price);
            if (optimalPrice <= price) return;
            instance.CounterofferInterface.PriceSelector.ChangeAmount(optimalPrice - (int)price);
            instance.CounterofferInterface.ChangeQuantity(optimalQuantity - quantity);
        }
    }
}