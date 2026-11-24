using System;
#if MONO
using ScheduleOne;
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.Product;
#else
using Il2CppScheduleOne;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Product;
#endif
using UnityEngine;
using HighBaller.Core.Models;

namespace HighBaller.Core;

public static class DealCalculator
{
    public static float CalculateSuccessProbability(Customer customer, ProductDefinition product, int quantity,
        float price, InitialOfferData initialOfferData)
    {
        if (customer == null ||
            product == null ||
            initialOfferData == null ||
            quantity <= 0 ||
            initialOfferData.Quantity <= 0)
        {
            Main.Log("Success probability calculation skipped because the input data is invalid.", false, true);
            return 0f;
        }

        var customerData = customer.CustomerData;
        if (customerData == null)
        {
            Main.Log("Success probability calculation skipped because customerData is null.", false, true);
            return 0f;
        }

        var initialProduct = Registry.GetItem<ProductDefinition>(initialOfferData.ProductId);
        if (initialProduct == null)
        {
            Main.Log("Success probability calculation skipped because the initial product could not be resolved.",
                false,
                true);
            return 0f;
        }

        var valueProposition =
            Customer.GetValueProposition(initialProduct, initialOfferData.Payment / initialOfferData.Quantity);
        // Must mirror Customer.EvaluateCounteroffer, which uses the quality-less overload
        // (different normalisation: max 0.7 instead of 1.0, no quality term).
        var productEnjoyment = customer.GetProductEnjoyment(product);
        var enjoymentT = Mathf.InverseLerp(-1f, 1f, productEnjoyment);
        var valueProposition2 = Customer.GetValueProposition(product, price / quantity);
        var quantityRatio = Mathf.Lerp(1f, 0f,
            Mathf.Abs(Mathf.Lerp(0f, 2f, Mathf.Pow(quantity / (float)initialOfferData.Quantity, 0.6f) * 0.5f) -
                      1f));
        if (valueProposition2 * quantityRatio > valueProposition) return 1f;
        if (valueProposition2 < 0.12f) return 0f;
        var currentScore = productEnjoyment * valueProposition;
        var offeredScore = enjoymentT * quantityRatio * valueProposition2;
        return offeredScore > currentScore
            ? 1f
            : Mathf.Clamp01(
                (0.9f -
                 (
                     Mathf.Lerp(0f, 1f, (currentScore - offeredScore) / 0.2f) -
                     Mathf.Lerp(0f, 0.2f,
                         Mathf.Max(customer.CurrentAddiction,
                             customer.NPC.RelationData.NormalizedRelationDelta)
                     )
                 )
                )
                / 0.9f
            );
    }

    public static (float maxSpend, float avgDailySpend) CalculateSpendingLimits(Customer customer)
    {
        if (customer == null)
        {
            Main.Log("Spending limit calculation skipped because the customer is null.", false, true);
            return (0f, 0f);
        }

        var customerData = customer.CustomerData;
        if (customerData == null)
        {
            Main.Log("Spending limit calculation skipped because customerData is null.", false, true);
            return (0f, 0f);
        }

#if MONO
        var orderDays = new List<EDay>();
#else
        var orderDays = new Il2CppSystem.Collections.Generic.List<EDay>();
#endif
        customerData.GetOrderDays(customer.CurrentAddiction, customer.NPC.RelationData.RelationDelta / 5f,
            orderDays);
        var orderDayCount = Math.Max(orderDays.Count, 1);
        var avgDailySpend = customerData.GetAdjustedWeeklySpend(customer.NPC.RelationData.RelationDelta / 5f) /
                            orderDayCount;
        return (avgDailySpend * 3f, avgDailySpend);
    }

    public static int FindOptimalPrice(Customer customer, ProductDefinition product, int quantity,
        float currentPrice, float maxSpend, float minSuccessProbability = -1f,
        InitialOfferData initialOfferData = null, bool log = false)
    {
        if (customer == null || product == null || quantity <= 0)
        {
            Main.Log("Optimal price calculation skipped because customer/product/quantity input is invalid.", false,
                true);
            return Math.Max(1, (int)currentPrice);
        }

        if (maxSpend <= 0f)
        {
            Main.Log("Optimal price calculation skipped because maxSpend is invalid.", false, true);
            return Math.Max(1, (int)currentPrice);
        }

        if (minSuccessProbability < 0.0) minSuccessProbability = Config.MinSuccessProbability.Value;
        var high = (int)Math.Floor(maxSpend - 0.01f);
        var low = Math.Min((int)currentPrice, high);
        var bestFailingPrice = (int)currentPrice;
        if (initialOfferData == null)
        {
            var offeredContractInfo = customer.OfferedContractInfo;
            if (offeredContractInfo is { Products.entries.Count: > 0 } &&
                offeredContractInfo.Products.entries[0].Quantity > 0)
            {
                initialOfferData = new InitialOfferData
                {
                    ProductId = offeredContractInfo.Products.entries[0].ProductID,
                    Quantity = offeredContractInfo.Products.entries[0].Quantity,
                    Payment = offeredContractInfo.Payment
                };
            }
            else
            {
                initialOfferData = new InitialOfferData
                {
                    ProductId = product.ID,
                    Quantity = Math.Max(quantity, 1),
                    Payment = Math.Max(currentPrice, 1f)
                };
                Main.Log(
                    "Optimal price calculation is using fallback initial offer data because offered contract data is unavailable.",
                    false, true);
            }
        }

        if (log)
        {
            Main.Log(
                $"Initial Offer Data: ProductId: {initialOfferData.ProductId}, Quantity: {initialOfferData.Quantity}, Payment: {initialOfferData.Payment}");
            Main.Log(
                $"Binary Search Start - Product: {product.ID}, Price: {currentPrice}, MaxSpend: {maxSpend}, Quantity: {quantity}, MinProbability: {minSuccessProbability}, Low: {low}, High: {high}");
        }

        const int maxIterations = 20;
        for (var i = 0; i < maxIterations; i++)
        {
            if (low >= high) break;
            var midPrice = (low + high) / 2;
            if (CalculateSuccessProbability(customer, product, quantity, midPrice, initialOfferData) >=
                minSuccessProbability)
            {
                low = midPrice + 1;
                if (low == high)
                {
                    bestFailingPrice =
                        CalculateSuccessProbability(customer, product, quantity, midPrice + 1, initialOfferData) >
                        minSuccessProbability
                            ? midPrice + 1
                            : midPrice;
                    break;
                }

                bestFailingPrice = midPrice;
            }
            else
            {
                high = midPrice;
            }
        }

        if (!log) return bestFailingPrice;
        Main.Log("Binary Search Complete:");
        Main.Log($"  Final bestFailingPrice: {bestFailingPrice}");
        Main.Log(
            $"  Final Probability: {CalculateSuccessProbability(customer, product, quantity, bestFailingPrice, initialOfferData)}");

        return bestFailingPrice;
    }

    public static (int quantity, int price) FindOptimalQuantity(Customer customer, ProductDefinition product,
        int initialQuantity, float initialPrice, InitialOfferData initialOfferData = null)
    {
        if (customer == null || product == null || initialQuantity <= 0)
        {
            Main.Log("Optimal quantity calculation skipped because customer/product/quantity input is invalid.", false,
                true);
            return (Math.Max(initialQuantity, 1), Math.Max((int)initialPrice, 1));
        }

        var maxSpend = CalculateSpendingLimits(customer).maxSpend;
        (int quantity, int price) result;
        if (!Config.IncreaseQuantityEnabled.Value)
        {
            var optimalPrice = FindOptimalPrice(customer, product, initialQuantity, initialPrice, maxSpend, -1f,
                initialOfferData);
            result = (initialQuantity, optimalPrice);
        }
        else
        {
            var initialBestPrice = FindOptimalPrice(customer, product, initialQuantity, initialPrice, maxSpend, -1f,
                initialOfferData);
            var bestQuantity = initialQuantity;
            var bestPrice = initialBestPrice;
            if (bestPrice >= Math.Floor(maxSpend * 0.99f))
            {
            }
            else
            {
                var trialQuantity = initialQuantity;
                const int maxIterations = 20;
                for (var i = 0; i < maxIterations; i++)
                {
                    trialQuantity++;
                    var trialPrice = FindOptimalPrice(customer, product, trialQuantity, initialPrice, maxSpend, -1f,
                        initialOfferData);
                    if (trialPrice <= bestPrice) break;
                    bestQuantity = trialQuantity;
                    bestPrice = trialPrice;
                    if (bestPrice >= Math.Floor(maxSpend * 0.99f)) break;
                }
            }

            result = (bestQuantity, bestPrice);
        }

        if (Config.RoundQuantityToNearest5.Value)
            result = RoundQuantityToFive(customer, product, result, maxSpend, initialOfferData);

        return result;
    }

    // Rounds the quantity to the nearest multiple of 5 (minimum 5) and re-prices it. Keeps the
    // unrounded result if the rounded quantity can't reach the minimum success probability.
    private static (int quantity, int price) RoundQuantityToFive(Customer customer, ProductDefinition product,
        (int quantity, int price) result, float maxSpend, InitialOfferData initialOfferData)
    {
        var rounded = Math.Max(5, (int)Math.Round(result.quantity / 5.0, MidpointRounding.AwayFromZero) * 5);
        if (rounded == result.quantity) return result;

        if (initialOfferData == null)
        {
            var offered = customer.OfferedContractInfo;
            if (offered is { Products.entries.Count: > 0 } && offered.Products.entries[0].Quantity > 0)
                initialOfferData = new InitialOfferData
                {
                    ProductId = offered.Products.entries[0].ProductID,
                    Quantity = offered.Products.entries[0].Quantity,
                    Payment = offered.Payment
                };
        }

        if (initialOfferData == null)
        {
            Main.Log("Quantity rounding skipped because the original offer is unavailable.", false, true);
            return result;
        }

        // Search from $1 so rounding down never inherits a price that was only valid for more units.
        var price = FindOptimalPrice(customer, product, rounded, 1f, maxSpend, -1f, initialOfferData);
        var probability = CalculateSuccessProbability(customer, product, rounded, price, initialOfferData);
        if (price <= 1 || probability < Config.MinSuccessProbability.Value)
        {
            Main.Log($"Quantity rounding {result.quantity} -> {rounded} rejected (price {price}, probability {probability:F3}); keeping {result.quantity}");
            return result;
        }

        Main.Log($"Quantity rounded {result.quantity} -> {rounded}, price {result.price} -> {price} (probability {probability:F3})");
        return (rounded, price);
    }
}