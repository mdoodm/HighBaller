using System.Collections;
using Il2CppFishNet;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Quests;
using MelonLoader;
using UnityEngine;

namespace HighBaller;

public class DelayedScheduler
{
	private Customer customer;

	private EDealWindow window;

	private object coroutineToken;

	public DelayedScheduler(Customer customer, EDealWindow window)
	{
		this.customer = customer;
		this.window = window;
	}

	public void Start()
	{
		coroutineToken = MelonCoroutines.Start(DelayedSchedulerCoroutine());
	}

	private IEnumerator DelayedSchedulerCoroutine()
	{
		yield return new WaitForSeconds(0.5f);
		float saveDelayElapsed = 0f;
		while (ShouldDelayForSave())
		{
			Main.Log("Delaying auto scheduling because the game is loading.");
			yield return new WaitForSeconds(0.25f);
			saveDelayElapsed += 0.25f;
			if (saveDelayElapsed >= 30f)
			{
				Main.Log("Auto scheduling aborted because save delay exceeded 30 seconds.", error: false, warning: true);
				coroutineToken = null;
				yield break;
			}
		}
		if (customer == null)
		{
			Main.Log("Auto scheduling skipped because the customer is null.", error: false, warning: true);
			yield break;
		}
		Main.Log("DelayedScheduler checking contract state for " + ((customer.customerData != null) ? customer.customerData.name : "unknown") + " - OfferedContractInfo: " + (customer.OfferedContractInfo != null) + ", CurrentContract: " + (customer.CurrentContract != null));
		if (customer.OfferedContractInfo == null)
		{
			Main.Log("Auto scheduling skipped because there is no offered contract.", error: false, warning: true);
			yield break;
		}
		DealWindowInfo windowInfo = DealWindowInfo.GetWindowInfo(window);
		ContractInfo offeredContractInfo = customer.OfferedContractInfo;
		if (offeredContractInfo.DeliveryWindow != null)
		{
			offeredContractInfo.DeliveryWindow.WindowStartTime = windowInfo.StartTime;
			offeredContractInfo.DeliveryWindow.WindowEndTime = windowInfo.EndTime;
		}
		Main.Log("Playing contract accepted reaction for " + customer.customerData.name);
		customer.PlayContractAcceptedReaction();
		Main.Log("Sending contract acceptance for " + customer.customerData.name + " in window " + window.ToString());
		customer.SendContractAccepted(window, true);
		if (!InstanceFinder.IsServer)
		{
			Main.Log("Clearing offered contract after acceptance for " + customer.customerData.name);
			customer.OfferedContractInfo = null;
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
