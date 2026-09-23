using HarmonyLib;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.UI.Handover;
using UnityEngine;

namespace HighBaller;

[HarmonyPatch(typeof(HandoverScreen), "Open")]
public static class HandoverScreenOpenGuard
{
	public static bool Prefix(Customer customer)
	{
		if (customer == null || customer.CustomerData == null)
		{
			Main.Log("HandoverScreen.Open blocked: customer or customerData is null (likely S1API custom NPC).", error: false, warning: true);
			return false;
		}
		return true;
	}
}
