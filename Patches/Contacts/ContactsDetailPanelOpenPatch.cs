using HarmonyLib;
#if MONO
using ScheduleOne.NPCs;
using ScheduleOne.UI.Phone.ContactsApp;
#else
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.UI.Phone.ContactsApp;
#endif
using HighBaller.Helpers;
using MelonLoader;

namespace HighBaller.Patches.Contacts;

[HarmonyPatch(typeof(ContactsDetailPanel), "Open")]
public static class ContactsDetailPanelOpenPatch
{
    [HarmonyPostfix]
    public static void Postfix(NPC npc, ContactsDetailPanel __instance)
    {
        if (npc == null) return;
        Main.Log(
            $"RelationCircle: {npc.ID}, Name: {npc.FullName}, RelationDelta: {npc.RelationData.RelationDelta}, Unlocked: {npc.RelationData.Unlocked}");
        var customerFromNpc = CustomerHelper.GetCustomerFromNpc(npc);
        if (customerFromNpc == null)
        {
            Main.Log("Not a customer");
            Melon<Main>.Instance.UIHandler.UpdateSalesInfoPanel(null);
        }
        else
        {
            Melon<Main>.Instance.UIHandler.UpdateContactInfoPanel(customerFromNpc);
        }
    }
}