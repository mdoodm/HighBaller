#if MONO
using ScheduleOne.Economy;
#else
using Il2CppScheduleOne.Economy;
#endif

namespace HighBaller.Core.Models;

public class CustomerAppeal
{
    public Customer Customer { get; set; }

    public float Appeal { get; set; }

    public PaymentQuantityResult PaymentQuantity { get; set; }
}