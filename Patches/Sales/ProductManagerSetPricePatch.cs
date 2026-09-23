using HarmonyLib;
#if MONO
using ScheduleOne.Product;
#else
using Il2CppScheduleOne.Product;
#endif
using HighBaller.Core;
using HighBaller.UI;
using MelonLoader;

namespace HighBaller.Patches.Sales;

[HarmonyPatch(typeof(ProductManager), "SendPrice")]
public static class ProductManagerSetPricePatch
{
    private static void Postfix(string productID, float value)
    {
        Main.Log($"ProductManager: Set price: {value} for {productID}", false, false, true);
        if (UIHandler.SelectedProduct == null || UIHandler.SelectedProduct.Definition.ID != productID) return;
        Melon<Main>.Instance.UIHandler.UpdateSalesInfoPanel(UIHandler.SelectedProduct);
    }
}