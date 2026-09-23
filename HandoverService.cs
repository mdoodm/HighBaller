using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.Core.Items.Framework;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Product.Packaging;
using Il2CppScheduleOne.UI.Handover;
using UnityEngine;

namespace HighBaller;

[HarmonyPatch(typeof(Customer), "CompleteContractDialogueChosen")]
public static class HandoverService
{
	public static bool Prefix(Customer __instance)
	{
		if (!Config.AutoHandoverEnabled.Value)
		{
			return true;
		}
		if (Config.GetCustomNPCMode((__instance != null) ? __instance.NPC : null) >= 1)
		{
			Main.Log("Auto handover skipped for custom NPC: " + ((__instance.customerData != null) ? __instance.customerData.name : "unknown"));
			return true;
		}
		return doDeal(__instance);
	}

	private static bool doDeal(Customer customer)
	{
		if (customer == null || customer.NPC == null)
		{
			Main.Log("Auto handover skipped because the customer is invalid.", error: false, warning: true);
			return true;
		}
		if (customer.CurrentContract == null || customer.CurrentContract.ProductList == null || customer.CurrentContract.ProductList.entries == null || customer.CurrentContract.ProductList.entries.Count == 0)
		{
			Main.Log("Auto handover skipped because the current contract has no products.", error: false, warning: true);
			return true;
		}
		Main.Log("Customer: " + ((customer.NPC != null) ? customer.NPC.FullName : "unknown"));
		ProductList.Entry contractEntry = customer.CurrentContract.ProductList.entries[0];
		string productID = contractEntry.ProductID;
		int quantity = contractEntry.Quantity;
		if (quantity <= 0)
		{
			return true;
		}
		if (customer.CustomerData == null)
		{
			Main.Log("Auto handover skipped because customer data is null.", error: false, warning: true);
			return true;
		}
		EQuality minQuality = StandardsMethod.GetCorrespondingQuality(customer.CustomerData.Standards);
		List<ProductPackage<ItemSlot>> matchingPackages = new List<ProductPackage<ItemSlot>>();
		PlayerInventory instance = PlayerSingleton<PlayerInventory>.Instance;
		if (Player.Local == null || instance == null)
		{
			Main.Log("Auto handover skipped because Player.Local or Inventory is null.", error: false, warning: true);
			return true;
		}
		foreach (ItemSlot slot in instance.GetAllInventorySlots())
		{
			ItemInstance itemInstance = slot.ItemInstance;
			if (itemInstance != null && !(itemInstance.Definition.ID != productID) && itemInstance.Quantity > 0)
			{
				Main.Log($"Item: {itemInstance.Definition.ID} - {itemInstance.Quantity} - {itemInstance.Definition.Category}");
				ProductItemInstance productItem = itemInstance.TryCast<ProductItemInstance>();
				if (productItem != null && !(productItem.packaging == null) && productItem.Quality >= minQuality)
				{
					PackagingDefinition packaging = productItem.packaging;
					Main.Log($"ProductItem: {itemInstance.Definition.ID} - {itemInstance.Quantity} - {packaging.Name}");
					Main.Log($"Packaging: {packaging.Quantity} {packaging.ID}");
					matchingPackages.Add(new ProductPackage<ItemSlot>(packaging.Quantity, slot, itemInstance.Quantity, productItem.Quality));
				}
			}
		}
		List<ProductPackage<ItemSlot>> selectedPackages = new List<ProductPackage<ItemSlot>>();
		IEnumerable<EQuality> allQualities = Enum.GetValues(typeof(EQuality)).Cast<EQuality>();
		Func<EQuality, bool> meetsMinimumQuality = delegate(EQuality q)
		{
			return q >= minQuality;
		};
		foreach (EQuality qualityLevel in allQualities.Where(meetsMinimumQuality))
		{
			Main.Log($"Finding optimal packages for quality: {qualityLevel}");
			selectedPackages = FindOptimalPackages(matchingPackages.Where(delegate(ProductPackage<ItemSlot> p)
			{
				return p.Quality == qualityLevel;
			}).ToList(), quantity);
			if (selectedPackages.Count > 0)
			{
				Main.Log($"Found optimal packages for quality: {qualityLevel}");
				break;
			}
		}
		if (selectedPackages.Count == 0)
		{
			Main.Log("No suitable combination of packages found to meet target quantity");
			return true;
		}
		Il2CppSystem.Collections.Generic.List<ItemInstance> handoverItems = new Il2CppSystem.Collections.Generic.List<ItemInstance>();
		foreach (ProductPackage<ItemSlot> selectedPackage in selectedPackages)
		{
			Main.Log($"Selected package: {selectedPackage?.PackageSize} units, {selectedPackage?.AvailableQuantity} packages");
			ItemInstance copy = selectedPackage.Slot.ItemInstance.GetCopy(-1);
			if (selectedPackage.Slot.Quantity.Equals(selectedPackage.AvailableQuantity))
			{
				selectedPackage.Slot.ItemInstance.RequestClearSlot();
			}
			else
			{
				selectedPackage.Slot.ChangeQuantity(-selectedPackage.AvailableQuantity, false);
			}
			copy.Quantity = selectedPackage.AvailableQuantity;
			handoverItems.Add(copy);
		}
		customer.ProcessHandover(customer.CurrentContract, handoverItems, true, true);
		return false;
	}

	public static List<ProductPackage<T>> FindOptimalPackages<T>(List<ProductPackage<T>> availablePackages, int targetQuantity)
	{
		if (targetQuantity <= 0)
		{
			return new List<ProductPackage<T>>();
		}
		Func<ProductPackage<T>, int> totalUnitsSelector = (ProductPackage<T> p) => p.TotalUnits;
		if (availablePackages.Sum(totalUnitsSelector) < targetQuantity)
		{
			return new List<ProductPackage<T>>();
		}
		List<ProductPackage<T>> selectedPackages = new List<ProductPackage<T>>();
		int unitsRemaining = targetQuantity;
		Func<ProductPackage<T>, int> packageSizeSelector = (ProductPackage<T> p) => p.PackageSize;
		List<ProductPackage<T>> orderedPackages = availablePackages.OrderByDescending(packageSizeSelector).ToList();
		foreach (ProductPackage<T> candidate in orderedPackages)
		{
			if (unitsRemaining <= 0)
			{
				break;
			}
			int packageCount = Math.Min(candidate.AvailableQuantity, unitsRemaining / candidate.PackageSize);
			if (packageCount > 0)
			{
				selectedPackages.Add(new ProductPackage<T>(candidate.PackageSize, candidate.Slot, packageCount, candidate.Quality));
				unitsRemaining -= packageCount * candidate.PackageSize;
			}
		}
		if (unitsRemaining > 0)
		{
			List<ProductPackage<T>> exactMatch = FindExactMatch(orderedPackages.Where((ProductPackage<T> p) => !selectedPackages.Any((ProductPackage<T> sp) => sp.Slot.Equals(p.Slot))).ToList(), unitsRemaining);
			if (exactMatch == null)
			{
				return new List<ProductPackage<T>>();
			}
			selectedPackages.AddRange(exactMatch);
		}
		return selectedPackages;
	}

	private static List<ProductPackage<T>> FindExactMatch<T>(List<ProductPackage<T>> packages, int target)
	{
		if (target == 0)
		{
			return new List<ProductPackage<T>>();
		}
		if (packages.Count == 0 || target < 0)
		{
			return null;
		}
		ProductPackage<T> productPackage = packages[0];
		List<ProductPackage<T>> remainingPackages = packages.Skip(1).ToList();
		int maxUseCount = Math.Min(productPackage.AvailableQuantity, target / productPackage.PackageSize);
		for (int i = 0; i <= maxUseCount; i++)
		{
			int remainingTarget = target - i * productPackage.PackageSize;
			List<ProductPackage<T>> match = FindExactMatch(remainingPackages, remainingTarget);
			if (match != null)
			{
				if (i > 0)
				{
					match.Add(new ProductPackage<T>(productPackage.PackageSize, productPackage.Slot, i, productPackage.Quality));
				}
				return match;
			}
		}
		return null;
	}
}
