using System;
using System.Collections.Generic;
using System.Linq;
#if MONO
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.Messaging;
using ScheduleOne.NPCs;
using ScheduleOne.Product;
using ScheduleOne.UI.Phone.Messages;
#else
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone.Messages;
#endif
using UnityEngine;
using HighBaller.Core.Models;

namespace HighBaller.Helpers;

public static class CustomerHelper
{
    public static Customer GetCustomerFromConversation(MSGConversation conversation)
    {
        if (conversation == null)
        {
            Main.Log("Could not resolve customer because the conversation is null.", false, true);
            return null;
        }

        var contactName = conversation.ContactName;
        foreach (var knownCustomer in GetKnownCustomers()
                     .Where(knownCustomer => knownCustomer != null && knownCustomer.NPC != null))
        {
            if (knownCustomer.NPC.MSGConversation == conversation ||
                !string.IsNullOrWhiteSpace(contactName) &&
                (
                    string.Equals(knownCustomer.NPC.FullName, contactName, StringComparison.OrdinalIgnoreCase) ||
                    (
                        knownCustomer.customerData != null &&
                        string.Equals(knownCustomer.customerData.name, contactName, StringComparison.OrdinalIgnoreCase)
                    )
                )) return knownCustomer;
        }

        Main.Log("Could not resolve customer from conversation: " + contactName, false, true);
        return null;
    }

    public static Customer GetCustomerFromMessagesApp(MessagesApp messagesApp)
    {
        if (messagesApp != null) return GetCustomerFromConversation(messagesApp.currentConversation);
        Main.Log("Could not resolve customer because MessagesApp is null.", false, true);
        return null;
    }

    public static float GetProductAppeal(ProductDefinition product, Customer customer, float? overridePrice = null)
    {
        if (product == null || customer == null || customer.customerData == null || product.MarketValue <= 0f)
        {
            Main.Log("Product appeal calculation skipped because the product or customer data is invalid.", false,
                true);
            return 0f;
        }

        var productEnjoyment =
            customer.GetProductEnjoyment(product, customer.customerData.Standards.GetCorrespondingQuality());
        var priceRatio = (overridePrice ?? product.Price) / product.MarketValue;
        var priceAdjustment = Mathf.Lerp(1f, -1f, priceRatio / 2f);
        return productEnjoyment + priceAdjustment;
    }

    public static PaymentQuantityResult CalculatePaymentQuantity(ProductDefinition product, Customer customer,
        float? overridePrice = null)
    {
        if (product == null || customer == null || customer.customerData == null)
        {
            Main.Log("Payment quantity calculation skipped because the product or customer is invalid.", false, true);
            return null;
        }

        var customerData = customer.customerData;
        var correspondingQuality = customerData.Standards.GetCorrespondingQuality();
        var unitPrice = overridePrice ?? product.Price;
        if (unitPrice <= 0f)
        {
            Main.Log("Payment quantity calculation skipped because the price is not greater than zero.", false, true);
            return new PaymentQuantityResult
            {
                Payment = 0f,
                Quantity = 0,
                Quality = correspondingQuality,
                Appeal = 0f
            };
        }

        var productAppeal = GetProductAppeal(product, customer, overridePrice);
        if (productAppeal < 0.05f)
            return new PaymentQuantityResult
            {
                Payment = 0f,
                Quantity = 0,
                Quality = correspondingQuality,
                Appeal = productAppeal
            };
        var productEnjoyment = customer.GetProductEnjoyment(product, correspondingQuality);
        var orderDays = 7;
        if (customer.AssignedDealer == null)
        {
#if MONO
            var orderDaysList = new List<EDay>();
#else
            var orderDaysList = new Il2CppSystem.Collections.Generic.List<EDay>();
#endif
            customerData.GetOrderDays(customer.CurrentAddiction, customer.NPC.RelationData.RelationDelta / 5f,
                orderDaysList);
            orderDays = Math.Max(orderDaysList.Count, 1);
        }

        var dailyBudget = customerData.GetAdjustedWeeklySpend(customer.NPC.RelationData.RelationDelta / 5f) /
            orderDays * Mathf.Lerp(0.66f, 1.5f, productEnjoyment);
        var enjoymentAdjustedPrice = unitPrice * Mathf.Lerp(0.66f, 1.5f, productEnjoyment);
        var quantity = Mathf.Clamp(Mathf.RoundToInt(dailyBudget / unitPrice), 1, 1000);
        if (quantity >= 14) quantity = Mathf.RoundToInt(quantity / 5) * 5;
        float payment = Mathf.RoundToInt(enjoymentAdjustedPrice * quantity / 5f) * 5;
        return new PaymentQuantityResult
        {
            Payment = payment,
            Quantity = quantity,
            Quality = correspondingQuality,
            Appeal = productAppeal
        };
    }

    public static Customer GetCustomerFromNpc(NPC npc)
    {
        if (npc == null)
        {
            Main.Log("Could not resolve customer because the NPC is null.", false, true);
            return null;
        }

        foreach (var knownCustomer in GetKnownCustomers()
                     .Where(knownCustomer => knownCustomer != null &&
                                             knownCustomer.NPC != null &&
                                             (
                                                 knownCustomer.NPC.Equals(npc) ||
                                                 string.Equals(knownCustomer.NPC.FullName,
                                                     npc.FullName,
                                                     StringComparison.OrdinalIgnoreCase)
                                             )
                     ))
            return knownCustomer;

        Main.Log("Could not resolve customer from NPC: " + npc.FullName, false, true);
        return null;
    }

    public static IEnumerable<ProductOrders> GetProductsFromCustomer(Customer customer)
    {
        if (customer == null)
        {
            Main.Log("Could not get products because the customer is null.", false, true);
            return [];
        }

        var productOrders = new List<ProductOrders>();
        foreach (var orderableProduct in customer.GetOrderableProducts())
        {
            var paymentQuantityResult = CalculatePaymentQuantity(orderableProduct, customer);
            if (paymentQuantityResult is { Appeal: >= 0.05f })
                productOrders.Add(new ProductOrders
                {
                    Product = orderableProduct,
                    PaymentQuantity = paymentQuantityResult
                });
        }

        productOrders.Sort((a, b) => b.PaymentQuantity.Appeal.CompareTo(a.PaymentQuantity.Appeal));
        return productOrders.Take(4);
    }

    public static List<Customer> GetKnownCustomers()
    {
        var knownCustomers = new List<Customer>();
        try
        {
            var unlockedCustomers = Customer.UnlockedCustomers;
            if (unlockedCustomers != null)
                foreach (var unlockedCustomer in unlockedCustomers)
                    AddCustomer(knownCustomers, unlockedCustomer);
        }
        catch (Exception exUnlocked)
        {
            Main.Log("Error enumerating UnlockedCustomers: " + exUnlocked.Message, false, true);
        }

        try
        {
            var allCustomers = Resources.FindObjectsOfTypeAll<Customer>();
            if (allCustomers != null)
                foreach (var sceneCustomer in allCustomers)
                    AddCustomer(knownCustomers, sceneCustomer);
        }
        catch (Exception exScene)
        {
            Main.Log("Error enumerating all Customer objects: " + exScene.Message, false, true);
        }

        Main.Log("GetKnownCustomers found " + knownCustomers.Count + " customers (incl. S1API custom NPCs)");
        return knownCustomers;
    }

    private static void AddCustomer(List<Customer> customers, Customer customer)
    {
        if (customer == null) return;
        try
        {
            if (customer.NPC == null) return;
            if (customer.customerData == null)
            {
                if (Config.GetCustomNpcMode(customer.NPC) >= S1APINPCHandling.Exclude)
                {
                    Main.Log("Skipping custom NPC with missing customerData (Mode 2).");
                    return;
                }

                Main.Log("Adding custom NPC with missing customerData: " + (customer.NPC.FullName ?? "unknown"));
            }

            if (!customers.Any(existing => existing == customer ||
                                           (
                                               existing != null &&
                                               existing.NPC != null &&
                                               customer.NPC != null &&
                                               existing.NPC == customer.NPC)
                ))
                customers.Add(customer);
        }
        catch (Exception ex)
        {
            Main.Log("Error adding customer: " + ex.Message, false, true);
        }
    }

    public class ProductOrders
    {
        public ProductDefinition Product { get; set; }

        public PaymentQuantityResult PaymentQuantity { get; set; }
    }
}