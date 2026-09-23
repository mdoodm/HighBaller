#if MONO
using ScheduleOne.ItemFramework;
#else
using Il2CppScheduleOne.ItemFramework;
#endif

namespace HighBaller.Core.Models;

public class ProductPackage<T>(int packageSize, T slot, int quantity, EQuality quality)
{
    public int PackageSize { get; } = packageSize;

    public T Slot { get; } = slot;

    public int AvailableQuantity { get; } = quantity;

    public EQuality Quality { get; } = quality;

    public int TotalUnits => PackageSize * AvailableQuantity;
}