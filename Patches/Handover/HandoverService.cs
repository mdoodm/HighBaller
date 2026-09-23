using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Product;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Product;
#endif
using HighBaller.Core.Models;

namespace HighBaller.Patches.Handover;

[HarmonyPatch(typeof(Customer), "CompleteContractDialogueChosen")]
public static class HandoverService
{
    public static bool Prefix(Customer __instance)
    {
        if (!Config.AutoHandoverEnabled.Value) return true;
        if (Config.GetCustomNpcMode(__instance != null ? __instance.NPC : null) < S1APINPCHandling.NoAutomation)
            return DoDeal(__instance);
        Main.Log("Auto handover skipped for custom NPC: " +
                 (__instance.customerData != null ? __instance.customerData.name : "unknown"));
        return true;
    }

    private static bool DoDeal(Customer customer)
    {
        if (customer == null || customer.NPC == null)
        {
            Main.Log("Auto handover skipped because the customer is invalid.", false, true);
            return true;
        }

        if (customer.CurrentContract == null || customer.CurrentContract.ProductList == null ||
            customer.CurrentContract.ProductList.entries == null ||
            customer.CurrentContract.ProductList.entries.Count == 0)
        {
            Main.Log("Auto handover skipped because the current contract has no products.", false, true);
            return true;
        }

        Main.Log("Customer: " + (customer.NPC != null ? customer.NPC.FullName : "unknown"));
        var contractEntry = customer.CurrentContract.ProductList.entries[0];
        var productID = contractEntry.ProductID;
        var quantity = contractEntry.Quantity;
        if (quantity <= 0) return true;
        if (customer.CustomerData == null)
        {
            Main.Log("Auto handover skipped because customer data is null.", false, true);
            return true;
        }

        var minQuality = customer.CustomerData.Standards.GetCorrespondingQuality();
        var matchingPackages = new List<ProductPackage<ItemSlot>>();
        var instance = PlayerInventory.Instance;
        if (Player.Local == null || instance == null)
        {
            Main.Log("Auto handover skipped because Player.Local or Inventory is null.", false, true);
            return true;
        }

        foreach (var slot in instance.GetAllInventorySlots())
        {
            var itemInstance = slot.ItemInstance;
            if (itemInstance == null || itemInstance.Definition.ID != productID || itemInstance.Quantity <= 0) continue;
            Main.Log(
                $"Item: {itemInstance.Definition.ID} - {itemInstance.Quantity} - {itemInstance.Definition.Category}");
#if MONO
            var productItem = itemInstance as ProductItemInstance;
#else
            var productItem = itemInstance.TryCast<ProductItemInstance>();
#endif
            if (productItem == null || productItem.packaging == null || productItem.Quality < minQuality) continue;
            var packaging = productItem.packaging;
            Main.Log($"ProductItem: {itemInstance.Definition.ID} - {itemInstance.Quantity} - {packaging.Name}");
            Main.Log($"Packaging: {packaging.Quantity} {packaging.ID}");
            matchingPackages.Add(new ProductPackage<ItemSlot>(packaging.Quantity, slot, itemInstance.Quantity,
                productItem.Quality));
        }

        var selectedPackages = new List<ProductPackage<ItemSlot>>();
        var allQualities = Enum.GetValues(typeof(EQuality)).Cast<EQuality>();
        var meetsMinimumQuality = (EQuality q) => q >= minQuality;
        foreach (var qualityLevel in allQualities.Where(meetsMinimumQuality))
        {
            Main.Log($"Finding optimal packages for quality: {qualityLevel}");
            selectedPackages =
                HandoverPackageSelector.FindOptimalPackages(
                    [.. matchingPackages.Where(p => p.Quality == qualityLevel)], quantity);
            if (selectedPackages.Count <= 0) continue;
            Main.Log($"Found optimal packages for quality: {qualityLevel}");
            break;
        }

        if (selectedPackages.Count == 0)
        {
            Main.Log("No suitable combination of packages found to meet target quantity");
            return true;
        }

#if MONO
        var handoverItems = new List<ItemInstance>();
#else
        var handoverItems = new Il2CppSystem.Collections.Generic.List<ItemInstance>();
#endif
        foreach (var selectedPackage in selectedPackages)
        {
            Main.Log(
                $"Selected package: {selectedPackage?.PackageSize} units, {selectedPackage?.AvailableQuantity} packages");
            if (selectedPackage == null)
            {
                Main.Log("Selected package is null", false, true);
                continue;
            }

            var copy = selectedPackage.Slot.ItemInstance.GetCopy();
            if (selectedPackage.Slot.Quantity.Equals(selectedPackage.AvailableQuantity))
                selectedPackage.Slot.ItemInstance.RequestClearSlot();
            else
                selectedPackage.Slot.ChangeQuantity(-selectedPackage.AvailableQuantity);
            copy.Quantity = selectedPackage.AvailableQuantity;
            handoverItems.Add(copy);
        }

        customer.ProcessHandover(customer.CurrentContract, handoverItems, true);
        return false;
    }
}