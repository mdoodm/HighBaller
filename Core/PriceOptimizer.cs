using System;
using System.Collections.Generic;
using System.Linq;
using HighBaller.Automation;
using HighBaller.Core.Models;
#if MONO
using ScheduleOne.Product;
#else
using Il2CppScheduleOne.Product;
#endif

namespace HighBaller.Core;

public static class PriceOptimizer
{
    public static float EvaluatePriceAcceptance(ProductDefinition product, List<CustomerAppeal> customerAppeals,
        float price)
    {
        var qualifyingCount = customerAppeals.Count(customerAppeal =>
            !(customerAppeal.Appeal < 0.05f) &&
            customerAppeal.PaymentQuantity is { Quantity: > 0 });

        return price * qualifyingCount;
    }
}