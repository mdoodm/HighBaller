using System.Collections;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Core.Items.Framework;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Quests;
using MelonLoader;
using UnityEngine;

namespace HighBaller;

public class DelayedCounterOffer
{
	private Customer customer;

	private ProductDefinition product;

	private int quantity;

	private float price;

	private object coroutineToken;

	private bool skipCounterOffer;

	public DelayedCounterOffer(Customer customer, ProductDefinition product, int quantity, float price, bool skipCounterOffer = false)
	{
		this.customer = customer;
		this.product = product;
		this.quantity = quantity;
		this.price = price;
		this.skipCounterOffer = skipCounterOffer;
	}

	public void Start()
	{
		coroutineToken = MelonCoroutines.Start(DelayedCounterOfferCoroutine());
	}

	private IEnumerator DelayedCounterOfferCoroutine()
	{
		yield return new WaitForSeconds(0.5f);
		float saveDelayElapsed = 0f;
		while (ShouldDelayForSave())
		{
			Main.Log("Delaying auto counter-offer because the game is loading.");
			yield return new WaitForSeconds(0.25f);
			saveDelayElapsed += 0.25f;
			if (saveDelayElapsed >= 30f)
			{
				Main.Log("Auto counter-offer aborted because save delay exceeded 30 seconds.", error: false, warning: true);
				coroutineToken = null;
				yield break;
			}
		}
		if (customer == null)
		{
			Main.Log("Auto counter-offer skipped because the customer is null.", error: false, warning: true);
			yield break;
		}
		ProductDefinition resolvedProduct = product;
		int resolvedQuantity = quantity;
		float payment = price;
		ContractInfo offeredContractInfo = customer.OfferedContractInfo;
		if (offeredContractInfo == null || offeredContractInfo.Products == null || offeredContractInfo.Products.entries == null || offeredContractInfo.Products.entries.Count == 0)
		{
			Main.Log("Auto counter-offer skipped because the offered contract is no longer available.", error: false, warning: true);
			yield break;
		}
		ProductList.Entry contractEntry = offeredContractInfo.Products.entries[0];
		if (resolvedProduct == null)
		{
			resolvedProduct = Registry.GetItem<ProductDefinition>(contractEntry.ProductID);
		}
		if (resolvedProduct == null)
		{
			Main.Log("Auto counter-offer skipped because the product could not be resolved.", error: false, warning: true);
			yield break;
		}
		if (resolvedQuantity <= 0)
		{
			resolvedQuantity = contractEntry.Quantity;
		}
		if (resolvedQuantity <= 0)
		{
			Main.Log("Auto counter-offer skipped because the quantity is invalid.", error: false, warning: true);
			yield break;
		}
		if (payment <= 0f)
		{
			payment = offeredContractInfo.Payment;
		}
		if (skipCounterOffer)
		{
			Main.Log("Directly accepting contract for " + customer.customerData.name);
			customer.AcceptContractClicked();
		}
		else
		{
			Main.Log("Sending auto counter-offer for " + customer.customerData.name + " with product " + resolvedProduct.ID + ", quantity " + resolvedQuantity + ", price " + payment);
			customer.SendCounteroffer(resolvedProduct, resolvedQuantity, payment);
		}
		coroutineToken = null;
	}

	private bool ShouldDelayForSave()
	{
		if (!Config.DelayAutoResponseDuringSave.Value)
		{
			return false;
		}
		LoadManager instance = Singleton<LoadManager>.Instance;
		if (instance != null)
		{
			if (instance.IsGameLoaded)
			{
				return instance.IsLoading;
			}
			return true;
		}
		return false;
	}
}
