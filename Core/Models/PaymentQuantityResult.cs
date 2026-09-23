#if MONO
using ScheduleOne.ItemFramework;
#else
using Il2CppScheduleOne.ItemFramework;
#endif

namespace HighBaller.Core.Models;

public class PaymentQuantityResult
{
    public float Payment { get; set; }

    public int Quantity { get; set; }

    public EQuality Quality { get; set; }

    public float Appeal { get; set; }

    public override string ToString()
    {
        return $"[Payment: {Payment}, Quantity: {Quantity}, Quality: {Quality}, Appeal: {Appeal}]";
    }
}