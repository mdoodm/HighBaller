using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone.Messages;
using UnityEngine;

namespace HighBaller;

public static class CustomerHelper
{
	public class ProductOrders
	{
		public ProductDefinition Product { get; set; }

		public PaymentQuantityResult PaymentQuantity { get; set; }
	}

	public static Customer GetCustomerFromConversation(MSGConversation conversation)
	{
		if (conversation == null)
		{
			Main.Log("Could not resolve customer because the conversation is null.", error: false, warning: true);
			return null;
		}
		string contactName = conversation.ContactName;
		foreach (Customer knownCustomer in GetKnownCustomers())
		{
			if (knownCustomer != null && knownCustomer.NPC != null)
			{
				if (knownCustomer.NPC.MSGConversation == conversation)
				{
					return knownCustomer;
				}
				if (!string.IsNullOrWhiteSpace(contactName) && (string.Equals(knownCustomer.NPC.FullName, contactName, StringComparison.OrdinalIgnoreCase) || (knownCustomer.customerData != null && string.Equals(knownCustomer.customerData.name, contactName, StringComparison.OrdinalIgnoreCase))))
				{
					return knownCustomer;
				}
			}
		}
		Main.Log("Could not resolve customer from conversation: " + contactName, error: false, warning: true);
		return null;
	}

	public static Customer GetCustomerFromMessagesApp(MessagesApp messagesApp)
	{
		if (messagesApp == null)
		{
			Main.Log("Could not resolve customer because MessagesApp is null.", error: false, warning: true);
			return null;
		}
		return GetCustomerFromConversation(messagesApp.currentConversation);
	}

	public static float GetProductAppeal(ProductDefinition product, Customer customer, float? overridePrice = null)
	{
		if (product == null || customer == null || customer.customerData == null || product.MarketValue <= 0f)
		{
			Main.Log("Product appeal calculation skipped because the product or customer data is invalid.", error: false, warning: true);
			return 0f;
		}
		float productEnjoyment = customer.GetProductEnjoyment(product, StandardsMethod.GetCorrespondingQuality(customer.customerData.Standards));
		float priceRatio = (overridePrice ?? product.Price) / product.MarketValue;
		float priceAdjustment = Mathf.Lerp(1f, -1f, priceRatio / 2f);
		return productEnjoyment + priceAdjustment;
	}

	public static PaymentQuantityResult CalculatePaymentQuantity(ProductDefinition product, Customer customer, float? overridePrice = null)
	{
		if (product == null || customer == null || customer.customerData == null)
		{
			Main.Log("Payment quantity calculation skipped because the product or customer is invalid.", error: false, warning: true);
			return null;
		}
		CustomerData customerData = customer.customerData;
		EQuality correspondingQuality = StandardsMethod.GetCorrespondingQuality(customerData.Standards);
		float unitPrice = overridePrice ?? product.Price;
		if (unitPrice <= 0f)
		{
			Main.Log("Payment quantity calculation skipped because the price is not greater than zero.", error: false, warning: true);
			return new PaymentQuantityResult
			{
				Payment = 0f,
				Quantity = 0,
				Quality = correspondingQuality,
				Appeal = 0f
			};
		}
		float productAppeal = GetProductAppeal(product, customer, overridePrice);
		if ((double)productAppeal < 0.05000000074505806)
		{
			return new PaymentQuantityResult
			{
				Payment = 0f,
				Quantity = 0,
				Quality = correspondingQuality,
				Appeal = productAppeal
			};
		}
		float productEnjoyment = customer.GetProductEnjoyment(product, correspondingQuality);
		int orderDays = 7;
		if (customer.AssignedDealer == null)
		{
			Il2CppSystem.Collections.Generic.List<EDay> orderDaysList = new Il2CppSystem.Collections.Generic.List<EDay>();
			customerData.GetOrderDays(customer.CurrentAddiction, customer.NPC.RelationData.RelationDelta / 5f, orderDaysList);
			orderDays = Math.Max(orderDaysList.Count, 1);
		}
		double dailyBudget = (double)customerData.GetAdjustedWeeklySpend(customer.NPC.RelationData.RelationDelta / 5f) / (double)orderDays * (double)Mathf.Lerp(0.66f, 1.5f, productEnjoyment);
		float enjoymentAdjustedPrice = unitPrice * Mathf.Lerp(0.66f, 1.5f, productEnjoyment);
		double unitPriceDouble = unitPrice;
		int quantity = Mathf.Clamp(Mathf.RoundToInt((float)(dailyBudget / unitPriceDouble)), 1, 1000);
		if (quantity >= 14)
		{
			quantity = Mathf.RoundToInt((float)(quantity / 5)) * 5;
		}
		float payment = Mathf.RoundToInt((float)((double)enjoymentAdjustedPrice * (double)quantity / 5.0)) * 5;
		return new PaymentQuantityResult
		{
			Payment = payment,
			Quantity = quantity,
			Quality = correspondingQuality,
			Appeal = productAppeal
		};
	}

	public static Customer GetCustomerFromNPC(NPC npc)
	{
		if (npc == null)
		{
			Main.Log("Could not resolve customer because the NPC is null.", error: false, warning: true);
			return null;
		}
		foreach (Customer knownCustomer in GetKnownCustomers())
		{
			if (knownCustomer != null && knownCustomer.NPC != null && (knownCustomer.NPC.Equals(npc) || string.Equals(knownCustomer.NPC.FullName, npc.FullName, StringComparison.OrdinalIgnoreCase)))
			{
				return knownCustomer;
			}
		}
		Main.Log("Could not resolve customer from NPC: " + npc.FullName, error: false, warning: true);
		return null;
	}

	public static IEnumerable<ProductOrders> GetProductsFromCustomer(Customer customer)
	{
		if (customer == null)
		{
			Main.Log("Could not get products because the customer is null.", error: false, warning: true);
			return Enumerable.Empty<ProductOrders>();
		}
		List<ProductOrders> productOrders = new List<ProductOrders>();
		foreach (ProductDefinition orderableProduct in customer.GetOrderableProducts((Dealer)null))
		{
			PaymentQuantityResult paymentQuantityResult = CalculatePaymentQuantity(orderableProduct, customer);
			if (paymentQuantityResult != null && (double)paymentQuantityResult.Appeal >= 0.05000000074505806)
			{
				productOrders.Add(new ProductOrders
				{
					Product = orderableProduct,
					PaymentQuantity = paymentQuantityResult
				});
			}
		}
		productOrders.Sort((ProductOrders a, ProductOrders b) => b.PaymentQuantity.Appeal.CompareTo(a.PaymentQuantity.Appeal));
		return productOrders.Take(4);
	}

	public static List<Customer> GetKnownCustomers()
	{
		List<Customer> knownCustomers = new List<Customer>();
		try
		{
			Il2CppSystem.Collections.Generic.List<Customer> unlockedCustomers = Customer.UnlockedCustomers;
			if (unlockedCustomers != null)
			{
				foreach (Customer unlockedCustomer in unlockedCustomers)
				{
					AddCustomer(knownCustomers, unlockedCustomer);
				}
			}
		}
		catch (Exception exUnlocked)
		{
			Main.Log("Error enumerating UnlockedCustomers: " + exUnlocked.Message, error: false, warning: true);
		}
		try
		{
			Il2CppArrayBase<Customer> allCustomers = Resources.FindObjectsOfTypeAll<Customer>();
			if (allCustomers != null)
			{
				foreach (Customer sceneCustomer in allCustomers)
				{
					AddCustomer(knownCustomers, sceneCustomer);
				}
			}
		}
		catch (Exception exScene)
		{
			Main.Log("Error enumerating all Customer objects: " + exScene.Message, error: false, warning: true);
		}
		Main.Log("GetKnownCustomers found " + knownCustomers.Count + " customers (incl. S1API custom NPCs)");
		return knownCustomers;
	}

	private static void AddCustomer(List<Customer> customers, Customer customer)
	{
		if (customer == null)
		{
			return;
		}
		try
		{
			if (customer.NPC == null)
			{
				return;
			}
			if (customer.customerData == null)
			{
				if (Config.GetCustomNPCMode(customer.NPC) >= 2)
				{
					Main.Log("Skipping custom NPC with missing customerData (Mode 2).");
					return;
				}
				Main.Log("Adding custom NPC with missing customerData: " + (customer.NPC.FullName ?? "unknown"));
			}
			if (!customers.Any((Customer existing) => existing == customer || (existing != null && existing.NPC != null && customer.NPC != null && existing.NPC == customer.NPC)))
			{
				customers.Add(customer);
			}
		}
		catch (Exception ex)
		{
			Main.Log("Error adding customer: " + ex.Message, error: false, warning: true);
		}
	}
}
