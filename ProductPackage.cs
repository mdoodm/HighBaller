using Il2CppScheduleOne.ItemFramework;

namespace HighBaller;

public class ProductPackage<T>
{
	public int PackageSize { get; }

	public T Slot { get; }

	public int AvailableQuantity { get; }

	public EQuality Quality { get; }

	public int TotalUnits => PackageSize * AvailableQuantity;

	public ProductPackage(int packageSize, T slot, int quantity, EQuality quality)
	{
		PackageSize = packageSize;
		Slot = slot;
		AvailableQuantity = quantity;
		Quality = quality;
	}
}
