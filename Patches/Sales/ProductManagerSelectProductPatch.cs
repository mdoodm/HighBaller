using HarmonyLib;
using HighBaller.UI;
using MelonLoader;
#if MONO
using ScheduleOne.Product;
using ScheduleOne.UI.Phone.ProductManagerApp;
#else
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone.ProductManagerApp;
#endif

namespace HighBaller.Patches.Sales;

[HarmonyPatch(typeof(ProductManagerApp), "SelectProduct")]
public static class ProductManagerSelectProductPatch
{
    public static void Prefix(ProductManagerApp __instance, ProductEntry entry)
    {
        UIHandler.ProductManagerApp = __instance;
    }

    private static void Postfix(ProductEntry entry)
    {
        Melon<Main>.Instance.UIHandler.UpdateSalesInfoPanel(entry);
    }
}
