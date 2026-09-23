using System;
using HarmonyLib;
#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone;
using ScheduleOne.UI.Phone.Messages;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.UI.Phone.Messages;
#endif
using UnityEngine;
using HighBaller.Core;

namespace HighBaller.Patches.CounterOffer;

[HarmonyPatch(typeof(CounterofferInterface), "ChangeQuantity", typeof(float))]
public static class CounterofferInterfacePostChangeQuantityPatch
{
    private static void Postfix(float change)
    {
        var instance = MessagesApp.Instance;
        if (instance == null || !instance.CounterofferInterface.IsOpen) return;
        var offerData = OfferEvaluator.GetOfferData();
        if (offerData == null) return;
        var customer = offerData.Customer;
        var maxSpend = DealCalculator.CalculateSpendingLimits(customer).maxSpend;
        var counterofferInterface = instance.CounterofferInterface;
        var product = counterofferInterface.selectedProduct ?? offerData.Product;
        var quantity = offerData.Quantity;
        var price = offerData.Price;
        var optimalPrice =
            DealCalculator.FindOptimalPrice(customer, product, quantity, 1f, maxSpend, -1f, null, true);
        if (optimalPrice <= 1)
        {
            var offeredContractInfo = customer.OfferedContractInfo;
            var products = offeredContractInfo?.Products;
            if (products is { entries.Count: > 0 })
            {
                var quantity2 = offeredContractInfo.Products.entries[0].Quantity;
                if (quantity2 > 0)
                {
                    optimalPrice = Math.Min((int)(offeredContractInfo.Payment / quantity2 * quantity),
                        (int)maxSpend - 1);
                    Main.Log($"FindOptimalPrice fallback: proportional price {optimalPrice} for {quantity} units");
                }
            }
        }

        if (!Mathf.Approximately(optimalPrice, price))
        {
            Main.SuppressNextPriceEval = true;
            counterofferInterface.PriceSelector.ChangeAmount(optimalPrice - (int)price);
        }

        OfferEvaluator.OfferPriceChangeCheckNoLog();
    }
}