using System;
using System.Collections.Generic;
using System.Linq;
using HighBaller.Core.Models;

namespace HighBaller.Patches.Handover;

public static class HandoverPackageSelector
{
    public static List<ProductPackage<T>> FindOptimalPackages<T>(List<ProductPackage<T>> availablePackages,
        int targetQuantity)
    {
        if (targetQuantity <= 0 ||
            availablePackages.Sum((Func<ProductPackage<T>, int>)TotalUnitsSelector) < targetQuantity) return [];
        var selectedPackages = new List<ProductPackage<T>>();
        var unitsRemaining = targetQuantity;
        var orderedPackages = availablePackages.OrderByDescending((Func<ProductPackage<T>, int>)PackageSizeSelector)
            .ToList();
        foreach (var candidate in orderedPackages)
        {
            if (unitsRemaining <= 0) break;
            var packageCount = Math.Min(candidate.AvailableQuantity, unitsRemaining / candidate.PackageSize);
            if (packageCount <= 0) continue;
            selectedPackages.Add(new ProductPackage<T>(candidate.PackageSize, candidate.Slot, packageCount,
                candidate.Quality));
            unitsRemaining -= packageCount * candidate.PackageSize;
        }

        if (unitsRemaining <= 0) return selectedPackages;

        var exactMatch =
            FindExactMatch([.. orderedPackages.Where(p => !selectedPackages.Any(sp => sp.Slot.Equals(p.Slot)))],
                unitsRemaining);
        if (exactMatch == null) return [];
        selectedPackages.AddRange(exactMatch);


        return selectedPackages;
        int PackageSizeSelector(ProductPackage<T> p) => p.PackageSize;
        int TotalUnitsSelector(ProductPackage<T> p) => p.TotalUnits;
    }

    private static List<ProductPackage<T>> FindExactMatch<T>(List<ProductPackage<T>> packages, int target)
    {
        if (target == 0) return [];
        if (packages.Count == 0 || target < 0) return null;
        var productPackage = packages[0];
        var remainingPackages = packages.Skip(1).ToList();
        var maxUseCount = Math.Min(productPackage.AvailableQuantity, target / productPackage.PackageSize);
        for (var i = 0; i <= maxUseCount; i++)
        {
            var remainingTarget = target - i * productPackage.PackageSize;
            var match = FindExactMatch(remainingPackages, remainingTarget);
            if (match == null) continue;
            if (i > 0)
                match.Add(new ProductPackage<T>(productPackage.PackageSize, productPackage.Slot, i,
                    productPackage.Quality));
            return match;
        }

        return null;
    }
}