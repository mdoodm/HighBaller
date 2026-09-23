using System;
using System.Reflection;
using HarmonyLib;
#if MONO
using ScheduleOne;
using ScheduleOne.Economy;
using ScheduleOne.Product;
using ScheduleOne.Quests;
#else
using Il2CppScheduleOne;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Quests;
#endif
using HighBaller.Automation;
using HighBaller.Core;
using HighBaller.Core.Models;

namespace HighBaller.Patches.Contracts;

[HarmonyPatch(typeof(Customer), "OfferContract")]
public static class OfferContractPatch
{
    public static void Postfix(ContractInfo info, Customer __instance, MethodBase __originalMethod)
    {
        if (__instance == null || info?.Products?.entries == null ||
            info.Products.entries.Count == 0)
        {
            Main.Log("OfferContractPatch skipped because the contract info is incomplete.", false, true);
        }
        else if (Config.GetCustomNpcMode(__instance.NPC) >= S1APINPCHandling.NoAutomation)
        {
            Main.Log("OfferContractPatch: skipping automation for custom NPC: " +
                     (__instance.customerData != null ? __instance.customerData.name : "unknown"));
        }
        else if (__instance.customerData == null || __instance.NPC == null)
        {
            Main.Log("OfferContractPatch skipped because customerData or NPC is null.", false, true);
        }
        else if (Config.AutoCounterOfferEnabled.Value)
        {
            Main.Log("Customer: " + (__instance.customerData != null ? __instance.customerData.name : "unknown") +
                     ", Contract: " + info.Products.entries[0].ProductID);
            var entry = info.Products.entries[0];
            bool MatchesEntry(ProductDefinition product) => product.ID.Equals(entry.ProductID);
            ProductDefinition offerProduct = null;
            foreach (var listedProduct in ProductManager.ListedProducts)
                if (MatchesEntry(listedProduct))
                {
                    offerProduct = listedProduct;
                    break;
                }

            if (offerProduct == null) offerProduct = Registry.GetItem<ProductDefinition>(entry.ProductID);
            if (offerProduct == null)
            {
                Main.Log("Auto counter-offer skipped because the offered product could not be resolved.", false, true);
                return;
            }

            var maxSpend = DealCalculator.CalculateSpendingLimits(__instance).maxSpend;
            if (info.Payment >= Math.Floor(maxSpend))
            {
                Main.Log("Skipping counter offer for " + __instance.customerData.name +
                         " because they are over their max spend");
                new DelayedCounterOffer(__instance, offerProduct, entry.Quantity, info.Payment, true).Start();
                return;
            }

            var initialOfferData = new InitialOfferData
            {
                ProductId = entry.ProductID,
                Quantity = entry.Quantity,
                Payment = info.Payment
            };
            var (quantity, counterOfferPrice) = DealCalculator.FindOptimalQuantity(__instance, offerProduct,
                entry.Quantity, info.Payment, initialOfferData);
            new DelayedCounterOffer(__instance, offerProduct, quantity, counterOfferPrice).Start();
        }
        else if (Config.AutoContractAcceptedEnabled.Value)
        {
            Main.Log("Auto-scheduling (without counter-offer) for " +
                     (__instance.customerData != null ? __instance.customerData.name : "unknown"));
            new DelayedCounterOffer(__instance, null, 0, 0f, true).Start();
        }
    }
}