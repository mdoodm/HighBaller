using System.Collections;
#if MONO
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.Persistence;
using ScheduleOne.Product;
#else
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Product;
#endif
using MelonLoader;
using UnityEngine;

namespace HighBaller.Automation;

public class DelayedCounterOffer(
    Customer customer,
    ProductDefinition product,
    int quantity,
    float price,
    bool skipCounterOffer = false)
{
    private object _coroutineToken;

    public void Start()
    {
        if (_coroutineToken != null) MelonCoroutines.Stop(_coroutineToken);
        _coroutineToken = MelonCoroutines.Start(DelayedCounterOfferCoroutine());
    }

    private IEnumerator DelayedCounterOfferCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        var saveDelayElapsed = 0f;
        while (ShouldDelayForSave())
        {
            Main.Log("Delaying auto counter-offer because the game is loading.");
            yield return new WaitForSeconds(0.25f);
            saveDelayElapsed += 0.25f;
            if (!(saveDelayElapsed >= 30f)) continue;
            Main.Log("Auto counter-offer aborted because save delay exceeded 30 seconds.", false, true);
            _coroutineToken = null;
            yield break;
        }

        if (customer == null)
        {
            Main.Log("Auto counter-offer skipped because the customer is null.", false, true);
            yield break;
        }

        var resolvedProduct = product;
        var resolvedQuantity = quantity;
        var payment = price;
        var offeredContractInfo = customer.OfferedContractInfo;
        if (offeredContractInfo?.Products?.entries == null || offeredContractInfo.Products.entries.Count == 0)
        {
            Main.Log("Auto counter-offer skipped because the offered contract is no longer available.", false, true);
            yield break;
        }

        var contractEntry = offeredContractInfo.Products.entries[0];
        if (resolvedProduct == null) resolvedProduct = Registry.GetItem<ProductDefinition>(contractEntry.ProductID);
        if (resolvedProduct == null)
        {
            Main.Log("Auto counter-offer skipped because the product could not be resolved.", false, true);
            yield break;
        }

        if (resolvedQuantity <= 0) resolvedQuantity = contractEntry.Quantity;
        if (resolvedQuantity <= 0)
        {
            Main.Log("Auto counter-offer skipped because the quantity is invalid.", false, true);
            yield break;
        }

        if (payment <= 0f) payment = offeredContractInfo.Payment;
        if (skipCounterOffer)
        {
            Main.Log("Directly accepting contract for " + customer.customerData.name);
            customer.AcceptContractClicked();
        }
        else
        {
            Main.Log("Sending auto counter-offer for " + customer.customerData.name + " with product " +
                     resolvedProduct.ID + ", quantity " + resolvedQuantity + ", price " + payment);
            customer.SendCounteroffer(resolvedProduct, resolvedQuantity, payment);
        }

        _coroutineToken = null;
    }

    private static bool ShouldDelayForSave()
    {
        if (!Config.DelayAutoResponseDuringSave.Value) return false;
        var instance = LoadManager.Instance;
        if (instance == null) return false;
        return !instance.IsGameLoaded || instance.IsLoading;
    }
}