using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Il2CppScheduleOne.Product;
using MelonLoader;

namespace HighBaller;

public class DelayedPriceCalculator
{
	private ProductDefinition product;

	private List<Main.CustomerAppeal> customerAppeals;

	private float initialPrice;

	private float minPrice;

	private float maxPrice;

	private Action<(float optimalPrice, float noLossPrice)> callback;

	private object coroutineToken;

	private bool isCalculating;

	private bool isCancelled;

	public DelayedPriceCalculator(ProductDefinition product, List<Main.CustomerAppeal> customerAppeals, float initialPrice, float minPrice, float maxPrice, Action<(float optimalPrice, float noLossPrice)> callback)
	{
		this.product = product;
		this.customerAppeals = customerAppeals;
		this.initialPrice = initialPrice;
		this.minPrice = minPrice;
		this.maxPrice = maxPrice;
		this.callback = callback;
	}

	public void Start()
	{
		if (!isCalculating)
		{
			isCalculating = true;
			isCancelled = false;
			coroutineToken = MelonCoroutines.Start(CalculatePriceCoroutine());
		}
	}

	public void Cancel()
	{
		isCancelled = true;
		if (coroutineToken != null)
		{
			MelonCoroutines.Stop(coroutineToken);
			coroutineToken = null;
		}
		isCalculating = false;
	}

	private IEnumerator CalculatePriceCoroutine()
	{
		float coarseStep = Math.Max((maxPrice - minPrice) / 30f, 1f);
		List<(float price, int count)> coarseResults = new List<(float price, int count)>();
		for (float p = minPrice; p <= maxPrice + 0.5f; p += coarseStep)
		{
			if (isCancelled)
			{
				yield break;
			}
			float clampedPrice = Math.Min(p, maxPrice);
			coarseResults.Add((clampedPrice, CountAt(clampedPrice)));
			if (coarseResults.Count % 5 == 0)
			{
				yield return null;
			}
		}
		yield return null;
		if (isCancelled)
		{
			yield break;
		}
		if (coarseResults.Count == 0)
		{
			callback((initialPrice, 0f));
			yield break;
		}
		int maxCount = coarseResults.Max(((float price, int count) r) => r.count);
		float coarseBestPrice = coarseResults.Where(((float price, int count) r) => r.count == maxCount).Max(((float price, int count) r) => r.price);
		float fineStartPrice = coarseBestPrice;
		float fineEnd = Math.Min(coarseBestPrice + coarseStep, maxPrice);
		List<(float price, int count)> fineResults = new List<(float price, int count)>();
		int fineCounter = 0;
		for (float p = fineStartPrice; p <= fineEnd; p++)
		{
			if (isCancelled)
			{
				yield break;
			}
			fineResults.Add((p, CountAt(p)));
			fineCounter++;
			if (fineCounter % 10 == 0)
			{
				yield return null;
			}
		}
		yield return null;
		if (isCancelled)
		{
			yield break;
		}
		isCalculating = false;
		List<(float price, int count)> allResults = coarseResults.Concat(fineResults).ToList();
		if (allResults.Count == 0)
		{
			callback((initialPrice, initialPrice));
			yield break;
		}
		int finalMaxCount = allResults.Max<(float price, int count)>(((float price, int count) r) => r.count);
		float noLossPrice = allResults.Where<(float price, int count)>(((float price, int count) r) => r.count == finalMaxCount).Max<(float price, int count)>(((float price, int count) r) => r.price);
		float optimalPrice = (from r in allResults
			orderby r.price * (float)r.count descending, r.price descending
			select r).First().price;
		callback((optimalPrice, noLossPrice));
	}

	private int CountAt(float price)
	{
		int qualifyingCount = 0;
		foreach (Main.CustomerAppeal customerAppeal in customerAppeals)
		{
			if (!((double)CustomerHelper.GetProductAppeal(product, customerAppeal.Customer, price) < 0.05))
			{
				PaymentQuantityResult paymentQuantityResult = CustomerHelper.CalculatePaymentQuantity(product, customerAppeal.Customer, price);
				if (paymentQuantityResult != null && paymentQuantityResult.Quantity > 0)
				{
					qualifyingCount++;
				}
			}
		}
		return qualifyingCount;
	}
}
