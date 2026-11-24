using HarmonyLib;
#if MONO
using ScheduleOne.Economy;
using ScheduleOne.Quests;
using ScheduleOne.UI.Handover;
#else
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Quests;
using Il2CppScheduleOne.UI.Handover;
#endif

namespace HighBaller.Patches.Handover;

[HarmonyPatch(typeof(HandoverScreenContractMode), "Open")]
public static class HandoverScreenOpenPatch
{
    // CurrentCustomer is derived from CurrentContract, which Open() itself sets (and OnClose clears),
    // so it is always null in a prefix. Inspect the contract being opened instead.
    public static bool Prefix(Contract contract)
    {
        var customer = contract != null && contract.Customer != null
            ? contract.Customer.GetComponent<Customer>()
            : null;
        if (customer != null && customer.CustomerData != null) return true;
        Main.Log("HandoverScreen.Open blocked: customer or customerData is null (likely S1API custom NPC).", false,
            true);
        return false;
    }
}
