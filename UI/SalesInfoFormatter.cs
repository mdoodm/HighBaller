using System;
using System.Linq;
using System.Text;
using HighBaller.Automation;
using HighBaller.Core;
using HighBaller.Core.Models;
using HighBaller.Helpers;
#if MONO
using ScheduleOne.Product;
#else
using Il2CppScheduleOne.Product;
#endif
using UnityEngine;

namespace HighBaller.UI;

internal class SalesInfoFormatter
{
    private readonly PanelState _salesPanel;

    private DelayedPriceCalculator _currentPriceCalculator;

    internal SalesInfoFormatter(PanelState salesPanel)
    {
        _salesPanel = salesPanel;
    }

    internal void CancelPendingCalculation()
    {
        if (_currentPriceCalculator == null) return;
        _currentPriceCalculator.Cancel();
        _currentPriceCalculator = null;
    }

    private static string FormatCurrency(float amount)
    {
        return $"${amount:F2}";
    }

    internal string FormatSalesInfo(ProductEntry entry, float? overridePrice = null)
    {
        if (entry == null || entry.Definition == null)
        {
            Main.Log("Sales info formatting skipped because entry or definition is null.", false, true);
            return "";
        }

        var html = new StringBuilder();
        html.Append("<section>Product Details</section>\n")
            .Append($"<label>Product:</label> <value>{entry.Definition.Name}</value>\n")
            .Append($"<label>Base Price:</label> <value>{FormatCurrency(entry.Definition.Price)}</value>\n")
            .Append($"<label>Market Value:</label> <value>{FormatCurrency(entry.Definition.MarketValue)}</value>\n");
        if (overridePrice.HasValue && !Mathf.Approximately(overridePrice.Value, entry.Definition.Price))
            html.Append($"<label>Current Price:</label> <value>{FormatCurrency(overridePrice.Value)}</value>\n");

        var knownCustomers = CustomerHelper.GetKnownCustomers();
        var appeals = knownCustomers.Select(knownCustomer => new CustomerAppeal
        {
            Customer = knownCustomer,
            Appeal = CustomerHelper.GetProductAppeal(entry.Definition, knownCustomer, overridePrice),
            PaymentQuantity = CustomerHelper.CalculatePaymentQuantity(entry.Definition, knownCustomer, overridePrice)
        }).ToList();

        var sortedAppeals = appeals.OrderByDescending(ca => ca.Appeal).ToList();
        var qualifyingAppeals = sortedAppeals.FindAll(ca => ca.Appeal > 0.05f);
        var maxSpenderScore =
            PriceOptimizer.EvaluatePriceAcceptance(entry.Definition, sortedAppeals,
                overridePrice ?? entry.Definition.Price);

        var avgOrderSize = qualifyingAppeals.Count > 0
            ? qualifyingAppeals.Average(ca => ca.PaymentQuantity.Quantity)
            : 0.0;
        var avgPayment = qualifyingAppeals.Count > 0
            ? FormatCurrency(qualifyingAppeals.Average(ca => ca.PaymentQuantity.Payment))
            : FormatCurrency(0f);
        var perUnitPrice = qualifyingAppeals.Count > 0
            ? FormatCurrency(qualifyingAppeals.Average(ca => ca.PaymentQuantity.Payment / ca.PaymentQuantity.Quantity))
            : FormatCurrency(0f);
        html.Append("<section>Market Analysis</section>\n")
            .Append($"<label>Potential Customers:</label> <value-good>{qualifyingAppeals.Count}</value-good>\n")
            .Append($"<label>Average Order Size:</label> <value>{avgOrderSize:F1}</value>\n")
            .Append($"<label>Average Payment:</label> <value>{avgPayment}</value>\n")
            .Append($"<label>Per-Unit Price:</label> <value>{perUnitPrice}</value>\n")
            .Append($"<label>Max Spender Score:</label> <value>{maxSpenderScore:F2}</value>\n");

        var avgAppeal = qualifyingAppeals.Count > 0 ? qualifyingAppeals.Average(ca => ca.Appeal) : 0f;
        var minAppeal = qualifyingAppeals.Count > 0 ? qualifyingAppeals.Min(ca => ca.Appeal) : 0f;
        var maxAppeal = qualifyingAppeals.Count > 0 ? qualifyingAppeals.Max(ca => ca.Appeal) : 0f;
        var overallAvgAppeal = sortedAppeals.Count > 0 ? sortedAppeals.Average(ca => ca.Appeal) : 0f;
        var overallMinAppeal = sortedAppeals.Count > 0 ? sortedAppeals.Min(ca => ca.Appeal) : 0f;
        html.Append("<section>Appeal Statistics</section>\n<label>Customer Appeal:</label>\n")
            .Append($"<value>Average: {avgAppeal:F2}</value>\n")
            .Append($"<value>Min: {minAppeal:F2}</value>\n")
            .Append($"<value>Max: {maxAppeal:F2}</value>\n")
            .Append(
                $"<label>Overall Appeal:</label>\n<value>Average: {overallAvgAppeal:F2}</value>\n<value>Min: {overallMinAppeal:F2}</value>\n");

        if (!Config.PriceOptimizationEnabled.Value) return html.ToString();

        html.Append("<section>Price Optimization</section>\n")
            .Append($"<label>Original Price:</label> <value>{FormatCurrency(entry.Definition.Price)}</value>\n")
            .Append("<label>Optimal Price:</label> <value>Calculating...</value>\n")
            .Append("<label>No-Loss Price:</label> <value>Calculating...</value>\n")
            .Append("<label>Max Markup:</label> <value>Calculating...</value>\n");

        var initialPrice = overridePrice ?? entry.Definition.Price;
        const float minPrice = 1f;
        var maxPrice =
            Math.Max(
                (sortedAppeals.Count > 0
                    ? sortedAppeals.Max(ca => DealCalculator.CalculateSpendingLimits(ca.Customer).maxSpend)
                    : entry.Definition.MarketValue * 3f) - 1f, entry.Definition.MarketValue * 2f);
        _currentPriceCalculator = new DelayedPriceCalculator(entry.Definition, sortedAppeals, initialPrice,
            minPrice,
            maxPrice, result =>
            {
                var optimalPrice = result.optimalPrice;
                var noLossPrice = result.noLossPrice;
                var contentLines = _salesPanel.Content.Split(['\n'], StringSplitOptions.None);
                for (var j = 0; j < contentLines.Length; j++)
                    if (contentLines[j].Contains("Optimal Price:"))
                        contentLines[j] = "<label>Optimal Price:</label> <value>" + FormatCurrency(optimalPrice) +
                                          "</value>\n";
                    else if (contentLines[j].Contains("No-Loss Price:"))
                        contentLines[j] = "<label>No-Loss Price:</label> <value>" + FormatCurrency(noLossPrice) +
                                          "</value>\n";
                    else if (contentLines[j].Contains("Max Markup:"))
                        contentLines[j] =
                            $"<label>Max Markup:</label> <value>{(optimalPrice - entry.Definition.Price) / entry.Definition.Price * 100f:F1}%</value>\n";

                _salesPanel.Content = string.Join("\n", contentLines);
            });
        _currentPriceCalculator.Start();


        return html.ToString();
    }
}