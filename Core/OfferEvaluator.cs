using System;
using System.Globalization;
#if MONO
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Product;
using ScheduleOne.UI.Phone.Messages;
#else
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone.Messages;
#endif
using HighBaller.Core.Models;
using HighBaller.Helpers;

namespace HighBaller.Core;

public static class OfferEvaluator
{
    public static OfferData GetOfferData()
    {
        var instance = MessagesApp.Instance;
        if (instance == null || instance.currentConversation == null || instance.CounterofferInterface == null ||
            instance.CounterofferInterface.ProductLabel == null || instance.CounterofferInterface.PriceSelector == null)
        {
            Main.Log("Counter-offer data could not be read because the UI state is incomplete.", false, true);
            return null;
        }

        var customerFromMessagesApp = CustomerHelper.GetCustomerFromMessagesApp(instance);
        if (customerFromMessagesApp == null || customerFromMessagesApp.OfferedContractInfo?.Products?.entries == null ||
            customerFromMessagesApp.OfferedContractInfo.Products.entries.Count == 0)
        {
            Main.Log("Counter-offer data could not be read because the customer contract state is incomplete.", false,
                true);
            return null;
        }

        var contractEntry = customerFromMessagesApp.OfferedContractInfo.Products.entries[0];
        var product = Registry.GetItem<ProductDefinition>(contractEntry.ProductID);
        if (product == null)
        {
            Main.Log("Counter-offer data could not be read because the offered product could not be resolved.", false,
                true);
            return null;
        }

        var quantityParts =
            (instance.CounterofferInterface.ProductLabel.text ?? string.Empty).Split(['x'],
                StringSplitOptions.RemoveEmptyEntries);
        if (quantityParts.Length == 0 || !int.TryParse(quantityParts[0].Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var parsedQuantity) ||
            parsedQuantity <= 0) parsedQuantity = contractEntry.Quantity;
        if (parsedQuantity <= 0)
        {
            Main.Log("Counter-offer data could not be read because the quantity is invalid.", false, true);
            return null;
        }

        var priceText =
            instance.CounterofferInterface.PriceSelector.SelectedAmount.ToString(CultureInfo.InvariantCulture);
        var parsedPrice = 0f;
        if (string.IsNullOrWhiteSpace(priceText) ||
            float.TryParse(priceText, NumberStyles.Float, CultureInfo.CurrentCulture, out parsedPrice) ||
            float.TryParse(priceText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedPrice))
            return new OfferData(customerFromMessagesApp, product, parsedQuantity, parsedPrice);
        Main.Log("Counter-offer data could not be read because the entered price is invalid.", false, true);
        return null;
    }

    public static void OfferPriceChangeCheckNoLog()
    {
        var offerData = GetOfferData();
        if (offerData != null) EvaluateCounterOffer(offerData);
    }

    public static void OfferPriceChangeCheckWithLog()
    {
        var offerData = GetOfferData();
        if (offerData == null) return;
        var summary =
            $"\nCustomer Name: {offerData.Customer.name}\nProduct: {offerData.Product.ID}\nQuantity: {offerData.Quantity}\nPrice: {offerData.Price}\n";
        EvaluateCounterOffer(offerData, log: true);
        Main.Log(summary);
    }

    private static void EvaluateCounterOffer(OfferData offerData, bool log = false)
    {
        if (offerData == null || offerData.Customer == null || offerData.Product == null || offerData.Quantity <= 0)
        {
            Main.Log(log
                ? "Detailed counter-offer evaluation skipped because the offer data is incomplete."
                : "Counter-offer evaluation skipped because the offer data is incomplete.", false, true);
            return;
        }

        var limits = DealCalculator.CalculateSpendingLimits(offerData.Customer);
        var maxSpend = limits.maxSpend;
        var avgDailySpend = limits.avgDailySpend;
        var maxSpendRounded = Math.Round((decimal)maxSpend, 2);

        var report = "";
        if (log)
        {
            var weeklySpend = offerData.Customer.CustomerData.GetAdjustedWeeklySpend(
                offerData.Customer.NPC.RelationData.RelationDelta / 5f);
#if MONO
            var orderDays = new List<EDay>();
#else
            var orderDays = new Il2CppSystem.Collections.Generic.List<EDay>();
#endif
            offerData.Customer.CustomerData.GetOrderDays(offerData.Customer.CurrentAddiction,
                offerData.Customer.NPC.RelationData.RelationDelta / 5f, orderDays);
            report =
                $"\nAdjusted Weekly Spend: {weeklySpend}\n" +
                $"Order Days: {orderDays.Count}\n" +
                $"Average Daily Spend: {avgDailySpend}\n" +
                $"Daily Spend Threshold (3x Avg): {maxSpendRounded}\n" +
                $"Min Weekly Spend: {offerData.Customer.CustomerData.MinWeeklySpend}\n" +
                $"Max Weekly Spend: {offerData.Customer.CustomerData.MaxWeeklySpend}\n";
            var noDealer = offerData.Customer.AssignedDealer == null;
            var orderableList = "";
            foreach (var orderableProduct in
                     offerData.Customer.GetOrderableProducts(noDealer ? null : offerData.Customer.AssignedDealer))
                orderableList +=
                    $"[Name: {orderableProduct.Name}, Price: {orderableProduct.Price}, Market: {orderableProduct.MarketValue}],\n";
            report += $"Orderable Products: {orderableList}\n";
        }

        if (offerData.Price >= maxSpend)
        {
            DisplayHelper.UpdateCounterOfferDisplayText("Guaranteed Failure",
                $"Exceeded Max Spend ({maxSpendRounded})");
            return;
        }

        var product = offerData.Product;
        if (!log)
        {
            var instance = MessagesApp.Instance;
            if (instance != null && instance.CounterofferInterface != null &&
                instance.CounterofferInterface.selectedProduct != null)
                product = instance.CounterofferInterface.selectedProduct;
        }

        var initialOfferData = new InitialOfferData
        {
            ProductId = offerData.Customer.OfferedContractInfo.Products.entries[0].ProductID,
            Quantity = offerData.Customer.OfferedContractInfo.Products.entries[0].Quantity,
            Payment = offerData.Customer.OfferedContractInfo.Payment
        };
        var probabilityPct =
            Math.Round(
                (decimal)(DealCalculator.CalculateSuccessProbability(offerData.Customer, product,
                    offerData.Quantity, offerData.Price, initialOfferData) * 100f), 3);

        if (log)
        {
            report += $"\nProbability of success: {probabilityPct}\n";
            DisplayHelper.UpdateCounterOfferDisplayText($"Probability of success: {probabilityPct}", "");
            Main.Log(report);
            return;
        }

        DisplayHelper.UpdateCounterOfferDisplayText($"Probability of success: {probabilityPct}%",
            $"Price per unit: {offerData.Price / offerData.Quantity}\nMax Spend: {maxSpendRounded}");
    }
}