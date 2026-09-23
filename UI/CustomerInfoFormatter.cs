using System.Collections.Generic;
using System.Linq;
using System.Text;
using HighBaller.Helpers;
#if MONO
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.NPCs.Relation;

#else
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.NPCs.Relation;
#endif

namespace HighBaller.UI;

internal static class CustomerInfoFormatter
{
    private static string FormatCurrency(float amount)
    {
        return $"${amount:F2}";
    }

    private static List<EDay> GetSortedOrderDays(List<EDay> orderDays, EDay currentDay)
    {
        return
        [
            .. orderDays
                .OrderBy(day =>
                {
                    var daysFromCurrent = day - currentDay;
                    if (daysFromCurrent < 0) daysFromCurrent += 7;
                    return daysFromCurrent;
                })
        ];
    }

    internal static string FormatCustomerInfo(Customer customer, CustomerData customerData,
        NPCRelationData relationData, EDay currentDay)
    {
        var html = new StringBuilder();
        html.Append("<section>Customer Details</section>\n<label>Name:</label> <value>").Append(customer.NPC.FullName)
            .Append("</value>\n");
        if (customer.AssignedDealer != null)
            html.Append("<label>Assigned Dealer:</label> <value>").Append(customer.AssignedDealer.FirstName).Append(' ')
                .Append(customer.AssignedDealer.LastName).Append("</value>\n");

#if MONO
        var rawOrderDays = new List<EDay>();
#else
        var rawOrderDays = new Il2CppSystem.Collections.Generic.List<EDay>();
#endif
        customerData.GetOrderDays(customer.CurrentAddiction, relationData.RelationDelta / 5f, rawOrderDays);
        var orderDays = new List<EDay>();
        foreach (var day in rawOrderDays)
            orderDays.Add(day);

        var relationAdjustedSpend = customerData.GetAdjustedWeeklySpend(relationData.RelationDelta / 5f);
        var amount = relationAdjustedSpend * 3f / orderDays.Count;
        var pctOfMax = relationAdjustedSpend / customerData.GetAdjustedWeeklySpend(1f) * 100f;
        html.Append("<section>Financial Information</section>\n")
            .Append($"<label>Max Spend:</label> <value>{FormatCurrency(customerData.MaxWeeklySpend)}</value>\n")
            .Append($"<label>Min Spend:</label> <value>{FormatCurrency(customerData.MinWeeklySpend)}</value>\n")
            .Append(
                $"<label>Daily Spend Limit:</label> <value>{FormatCurrency(amount)} ({pctOfMax:F2}% of Max Daily)</value>\n");

        var sortedOrderDays = GetSortedOrderDays(orderDays, currentDay);
        html.Append("<section>Schedule Information</section>\n<label>Next Order Days:</label>\n");
        for (var j = 0; j < sortedOrderDays.Count; j++)
            html.Append($"<value>  {j + 1}. {sortedOrderDays[j]}</value>\n");

        html.Append("<section>Product Preferences</section>\n");
        var products = CustomerHelper.GetProductsFromCustomer(customer).ToList();
        if (products.Count == 0)
            html.Append(customer.AssignedDealer != null
                ? "<value>Dealer has no appealing products for sale</value>\n"
                : "<value>Won't buy anything</value>\n");

        foreach (var product in products)
        {
            var paymentQuantity = product.PaymentQuantity;
            html.Append(
                $"<value>{product.Product.Name} - ${paymentQuantity.Payment:F2} × {paymentQuantity.Quantity} (Appeal: {paymentQuantity.Appeal:F3})</value>\n");
        }

        return html.ToString();
    }
}