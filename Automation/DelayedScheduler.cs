using System.Collections;
#if MONO
using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.Persistence;
#else
using Il2CppFishNet;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Persistence;
#endif
using MelonLoader;
using UnityEngine;

namespace HighBaller.Automation;

public class DelayedScheduler(Customer customer, EDealWindow window)
{
    private object _coroutineToken;

    public void Start()
    {
        if (_coroutineToken != null) MelonCoroutines.Stop(_coroutineToken);
        _coroutineToken = MelonCoroutines.Start(DelayedSchedulerCoroutine());
    }

    private IEnumerator DelayedSchedulerCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        var saveDelayElapsed = 0f;
        while (ShouldDelayForSave())
        {
            Main.Log("Delaying auto scheduling because the game is loading.");
            yield return new WaitForSeconds(0.25f);
            saveDelayElapsed += 0.25f;
            if (!(saveDelayElapsed >= 30f)) continue;
            Main.Log("Auto scheduling aborted because save delay exceeded 30 seconds.", false, true);
            _coroutineToken = null;
            yield break;
        }

        if (customer == null)
        {
            Main.Log("Auto scheduling skipped because the customer is null.", false, true);
            yield break;
        }

        Main.Log("DelayedScheduler checking contract state for " +
                 (customer.customerData != null ? customer.customerData.name : "unknown") + " - OfferedContractInfo: " +
                 (customer.OfferedContractInfo != null) + ", CurrentContract: " + (customer.CurrentContract != null));
        if (customer.OfferedContractInfo == null)
        {
            Main.Log("Auto scheduling skipped because there is no offered contract.", false, true);
            yield break;
        }

        var windowInfo = DealWindowInfo.GetWindowInfo(window);
        var offeredContractInfo = customer.OfferedContractInfo;
        if (offeredContractInfo.DeliveryWindow != null)
        {
            offeredContractInfo.DeliveryWindow.WindowStartTime = windowInfo.StartTime;
            offeredContractInfo.DeliveryWindow.WindowEndTime = windowInfo.EndTime;
        }

        Main.Log("Playing contract accepted reaction for " + customer.customerData.name);
        customer.PlayContractAcceptedReaction();
        Main.Log("Sending contract acceptance for " + customer.customerData.name + " in window " + window);
        customer.SendContractAccepted(window, true);
        if (!InstanceFinder.IsServer)
        {
            Main.Log("Clearing offered contract after acceptance for " + customer.customerData.name);
            customer.OfferedContractInfo = null;
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