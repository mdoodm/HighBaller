using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HighBaller.Core.Models;
#if MONO
using ScheduleOne.Product;
#else
using Il2CppScheduleOne.Product;
#endif
using MelonLoader;
using HighBaller.Helpers;

namespace HighBaller.Automation;

public class DelayedPriceCalculator(
    ProductDefinition product,
    List<CustomerAppeal> customerAppeals,
    float initialPrice,
    float minPrice,
    float maxPrice,
    Action<(float optimalPrice, float noLossPrice)> callback)
{
    private readonly struct PriceCandidate(float price, int count)
    {
        public float Price { get; } = price;

        public int Count { get; } = count;

        public float Revenue => Price * Count;
    }

    private object _coroutineToken;

    private bool _isCalculating;

    private bool _isCancelled;

    public void Start()
    {
        if (_isCalculating) return;
        _isCalculating = true;
        _isCancelled = false;
        _coroutineToken = MelonCoroutines.Start(CalculatePriceCoroutine());
    }

    public void Cancel()
    {
        _isCancelled = true;
        if (_coroutineToken != null)
        {
            MelonCoroutines.Stop(_coroutineToken);
            _coroutineToken = null;
        }

        _isCalculating = false;
    }

    private IEnumerator CalculatePriceCoroutine()
    {
        var coarseStep = Math.Max((maxPrice - minPrice) / 30f, 1f);
        var coarseResults = new List<PriceCandidate>();
        for (var p = minPrice; p <= maxPrice + 0.5f; p += coarseStep)
        {
            if (_isCancelled) yield break;
            var clampedPrice = Math.Min(p, maxPrice);
            coarseResults.Add(new PriceCandidate(clampedPrice, CountAt(clampedPrice)));
            if (coarseResults.Count % 5 == 0) yield return null;
        }

        yield return null;
        if (_isCancelled) yield break;
        if (coarseResults.Count == 0)
        {
            callback((initialPrice, 0f));
            yield break;
        }

        var maxCount = coarseResults.Max(r => r.Count);
        var coarseBestPrice = coarseResults.Where(r => r.Count == maxCount).Max(r => r.Price);
        var fineEnd = Math.Min(coarseBestPrice + coarseStep, maxPrice);
        var fineResults = new List<PriceCandidate>();
        var fineCounter = 0;
        for (var p = coarseBestPrice; p <= fineEnd; p++)
        {
            if (_isCancelled) yield break;
            fineResults.Add(new PriceCandidate(p, CountAt(p)));
            fineCounter++;
            if (fineCounter % 10 == 0) yield return null;
        }

        yield return null;
        if (_isCancelled) yield break;
        _isCalculating = false;
        var allResults = new List<PriceCandidate>(coarseResults);
        allResults.AddRange(fineResults);
        if (allResults.Count == 0)
        {
            callback((initialPrice, initialPrice));
            yield break;
        }

        var finalMaxCount = allResults.Max(r => r.Count);
        var noLossPrice = allResults.Where(r => r.Count == finalMaxCount).Max(r => r.Price);
        var optimalPrice = allResults
            .OrderByDescending(r => r.Revenue)
            .ThenByDescending(r => r.Price)
            .First().Price;
        callback((optimalPrice, noLossPrice));
    }

    private int CountAt(float price)
    {
        return customerAppeals
            .Where(customerAppeal =>
                !(CustomerHelper.GetProductAppeal(product, customerAppeal.Customer, price) < 0.05f))
            .Select(customerAppeal =>
                CustomerHelper.CalculatePaymentQuantity(product, customerAppeal.Customer, price))
            .Count(paymentQuantityResult => paymentQuantityResult is { Quantity: > 0 });
    }
}