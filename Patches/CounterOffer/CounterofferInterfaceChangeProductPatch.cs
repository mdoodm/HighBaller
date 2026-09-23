using HarmonyLib;
#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Product;
using ScheduleOne.UI.Phone;
using ScheduleOne.UI.Phone.Messages;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.UI.Phone.Messages;
#endif
using HighBaller.Core;
using UnityEngine;

namespace HighBaller.Patches.CounterOffer;

[HarmonyPatch(typeof(CounterofferInterface), "SetProduct")]
public static class CounterofferInterfaceChangeProductPatch
{
    private static void Postfix(ProductDefinition newProduct)
    {
        var instance = MessagesApp.Instance;
        if (instance == null || newProduct == null)
        {
            Main.Log("Counteroffer product change skipped because the UI or product is unavailable.", false, true);
            return;
        }

        Main.Log("CounterofferInterfaceChangeProductPatch: " + newProduct.Name);
        var offerData = OfferEvaluator.GetOfferData();
        if (offerData == null) return;
        var customer = offerData.Customer;
        var quantity = offerData.Quantity;
        var price = offerData.Price;
        var (newQuantity, newPrice) = DealCalculator.FindOptimalQuantity(customer, newProduct, quantity, price * 0.25f);
        if (Mathf.Approximately(newPrice, price)) return;
        instance.CounterofferInterface.PriceSelector.ChangeAmount(newPrice - (int)price);
        instance.CounterofferInterface.ChangeQuantity(newQuantity - quantity);
    }
}