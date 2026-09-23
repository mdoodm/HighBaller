using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Core.Items.Framework;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Quests;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.UI.Phone.ContactsApp;
using Il2CppScheduleOne.UI.Phone.Messages;
using Il2CppScheduleOne.UI.Phone.ProductManagerApp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(HighBaller.Main), "HighBaller", "2.7.10", "zocke18r,SFox & fixed by Virtunerd", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace HighBaller;


public class Main : MelonMod
{
	public class InitialOfferData
	{
		public string ProductId { get; set; }

		public int Quantity { get; set; }

		public float Payment { get; set; }
	}

	public static class DealCalculator
	{
		public static float CalculateSuccessProbability(Customer customer, ProductDefinition product, int quantity, float price, InitialOfferData initialOfferData)
		{
			if (customer == null || product == null || initialOfferData == null || quantity <= 0 || initialOfferData.Quantity <= 0)
			{
				Log("Success probability calculation skipped because the input data is invalid.", error: false, warning: true);
				return 0f;
			}
			CustomerData customerData = customer.CustomerData;
			if (customerData == null)
			{
				Log("Success probability calculation skipped because customerData is null.", error: false, warning: true);
				return 0f;
			}
			ProductDefinition initialProduct = Registry.GetItem<ProductDefinition>(initialOfferData.ProductId);
			if (initialProduct == null)
			{
				Log("Success probability calculation skipped because the initial product could not be resolved.", error: false, warning: true);
				return 0f;
			}
			float valueProposition = Customer.GetValueProposition(initialProduct, initialOfferData.Payment / (float)initialOfferData.Quantity);
			float productEnjoyment = customer.GetProductEnjoyment(product, StandardsMethod.GetCorrespondingQuality(customerData.Standards));
			float enjoymentT = Mathf.InverseLerp(-1f, 1f, productEnjoyment);
			float valueProposition2 = Customer.GetValueProposition(product, price / (float)quantity);
			float quantityRatio = Mathf.Lerp(1f, 0f, Mathf.Abs(Mathf.Lerp(0f, 2f, Mathf.Pow((float)quantity / (float)initialOfferData.Quantity, 0.6f) * 0.5f) - 1f));
			if (valueProposition2 * quantityRatio > valueProposition)
			{
				return 1f;
			}
			if ((double)valueProposition2 < 0.11999999731779099)
			{
				return 0f;
			}
			float currentScore = productEnjoyment * valueProposition;
			float offeredScore = enjoymentT * quantityRatio * valueProposition2;
			return (offeredScore > currentScore) ? 1f : Mathf.Clamp01((float)((0.8999999761581421 - (double)(Mathf.Lerp(0f, 1f, (currentScore - offeredScore) / 0.2f) - Mathf.Lerp(0f, 0.2f, Mathf.Max(customer.CurrentAddiction, customer.NPC.RelationData.NormalizedRelationDelta)))) / 0.8999999761581421));
		}

		public static (float maxSpend, float avgDailySpend) CalculateSpendingLimits(Customer customer)
		{
			if (customer == null)
			{
				Log("Spending limit calculation skipped because the customer is null.", error: false, warning: true);
				return (0f, 0f);
			}
			CustomerData customerData = customer.CustomerData;
			if (customerData == null)
			{
				Log("Spending limit calculation skipped because customerData is null.", error: false, warning: true);
				return (0f, 0f);
			}
			Il2CppSystem.Collections.Generic.List<EDay> orderDays = new Il2CppSystem.Collections.Generic.List<EDay>();
			customerData.GetOrderDays(customer.CurrentAddiction, customer.NPC.RelationData.RelationDelta / 5f, orderDays);
			int orderDayCount = Math.Max(orderDays.Count, 1);
			float avgDailySpend = customerData.GetAdjustedWeeklySpend(customer.NPC.RelationData.RelationDelta / 5f) / (float)orderDayCount;
			return (avgDailySpend * 3f, avgDailySpend);
		}

		public static int FindOptimalPrice(Customer customer, ProductDefinition product, int quantity, float currentPrice, float maxSpend, float minSuccessProbability = -1f, InitialOfferData initialOfferData = null, bool log = false)
		{
			if (customer == null || product == null || quantity <= 0)
			{
				Log("Optimal price calculation skipped because customer/product/quantity input is invalid.", error: false, warning: true);
				return Math.Max(1, (int)currentPrice);
			}
			if (maxSpend <= 0f)
			{
				Log("Optimal price calculation skipped because maxSpend is invalid.", error: false, warning: true);
				return Math.Max(1, (int)currentPrice);
			}
			if ((double)minSuccessProbability < 0.0)
			{
				minSuccessProbability = Config.MinSuccessProbability.Value;
			}
			int high = (int)Math.Floor(maxSpend - 0.01f);
			int low = Math.Min((int)currentPrice, high);
			int bestFailingPrice = (int)currentPrice;
			int maxIterations = 20;
			int i = 0;
			if (initialOfferData == null)
			{
				ContractInfo offeredContractInfo = customer.OfferedContractInfo;
				if (offeredContractInfo != null && offeredContractInfo.Products != null && offeredContractInfo.Products.entries != null && offeredContractInfo.Products.entries.Count > 0 && offeredContractInfo.Products.entries[0].Quantity > 0)
				{
					initialOfferData = new InitialOfferData
					{
						ProductId = offeredContractInfo.Products.entries[0].ProductID,
						Quantity = offeredContractInfo.Products.entries[0].Quantity,
						Payment = offeredContractInfo.Payment
					};
				}
				else
				{
					initialOfferData = new InitialOfferData
					{
						ProductId = product.ID,
						Quantity = Math.Max(quantity, 1),
						Payment = Math.Max(currentPrice, 1f)
					};
					Log("Optimal price calculation is using fallback initial offer data because offered contract data is unavailable.", error: false, warning: true);
				}
			}
			if (log)
			{
				Log($"Initial Offer Data: ProductId: {initialOfferData.ProductId}, Quantity: {initialOfferData.Quantity}, Payment: {initialOfferData.Payment}");
				Log($"Binary Search Start - Product: {product.ID}, Price: {currentPrice}, MaxSpend: {maxSpend}, Quantity: {quantity}, MinProbability: {minSuccessProbability}, Low: {low}, High: {high}");
			}
			for (; i < maxIterations; i++)
			{
				if (low >= high)
				{
					break;
				}
				int midPrice = (low + high) / 2;
				if (CalculateSuccessProbability(customer, product, quantity, midPrice, initialOfferData) >= minSuccessProbability)
				{
					low = midPrice + 1;
					if (low == high)
					{
						bestFailingPrice = ((CalculateSuccessProbability(customer, product, quantity, midPrice + 1, initialOfferData) > minSuccessProbability) ? (midPrice + 1) : midPrice);
						break;
					}
					bestFailingPrice = midPrice;
				}
				else
				{
					high = midPrice;
				}
			}
			if (log)
			{
				Log("Binary Search Complete:");
				Log($"  Final bestFailingPrice: {bestFailingPrice}");
				Log($"  Final Probability: {CalculateSuccessProbability(customer, product, quantity, bestFailingPrice, initialOfferData)}");
			}
			return bestFailingPrice;
		}

		public static (int quantity, int price) FindOptimalQuantity(Customer customer, ProductDefinition product, int initialQuantity, float initialPrice, InitialOfferData initialOfferData = null)
		{
			if (customer == null || product == null || initialQuantity <= 0)
			{
				Log("Optimal quantity calculation skipped because customer/product/quantity input is invalid.", error: false, warning: true);
				return (Math.Max(initialQuantity, 1), Math.Max((int)initialPrice, 1));
			}
			float maxSpend = CalculateSpendingLimits(customer).maxSpend;
			(int quantity, int price) result;
			if (!Config.IncreaseQuantityEnabled.Value)
			{
				int optimalPrice = FindOptimalPrice(customer, product, initialQuantity, initialPrice, maxSpend, -1f, initialOfferData);
				result = (initialQuantity, optimalPrice);
			}
			else
			{
				int i = 0;
				int maxIterations = 20;
				int initialBestPrice = FindOptimalPrice(customer, product, initialQuantity, initialPrice, maxSpend, -1f, initialOfferData);
				int bestQuantity = initialQuantity;
				int bestPrice = initialBestPrice;
				if (bestPrice >= Math.Floor((double)maxSpend * 0.99f))
				{
					result = (bestQuantity, bestPrice);
				}
				else
				{
					int trialQuantity = initialQuantity;
					for (; i < maxIterations; i++)
					{
						trialQuantity++;
						int trialPrice = FindOptimalPrice(customer, product, trialQuantity, initialPrice, maxSpend, -1f, initialOfferData);
						if (trialPrice <= bestPrice)
						{
							break;
						}
						bestQuantity = trialQuantity;
						bestPrice = trialPrice;
						if (bestPrice >= Math.Floor((double)maxSpend * 0.99f))
						{
							break;
						}
					}
					result = (bestQuantity, bestPrice);
				}
			}
			return result;
		}
	}

	private static class DisplayHelper
	{
		public static void UpdateCounterOfferDisplayText(string text, string reasonText)
		{
			Instance.uiHandler.UpdateCounterOfferDisplayText(text, reasonText);
		}
	}

	[HarmonyPatch(typeof(CounterofferInterface), "SetProduct")]
	private static class CounterofferInterfaceChangeProductPatch
	{
		private static void Postfix(ProductDefinition newProduct)
		{
			MessagesApp instance = PlayerSingleton<MessagesApp>.Instance;
			if (instance == null || newProduct == null)
			{
				Log("Counteroffer product change skipped because the UI or product is unavailable.", error: false, warning: true);
				return;
			}
			Log("CounterofferInterfaceChangeProductPatch: " + newProduct.Name);
			OfferData offerData = GetOfferData();
			if (offerData != null)
			{
				Customer customer = offerData.Customer;
				int quantity = offerData.Quantity;
				float price = offerData.Price;
				(int quantity, int price) optimal = DealCalculator.FindOptimalQuantity(customer, newProduct, quantity, price * 0.25f);
				int newQuantity = optimal.quantity;
				int newPrice = optimal.price;
				int currentQuantity = quantity;
				if ((float)newPrice != price)
				{
					instance.CounterofferInterface.PriceSelector.ChangeAmount((float)(newPrice - (int)price));
					instance.CounterofferInterface.ChangeQuantity((float)(newQuantity - currentQuantity));
				}
			}
		}
	}

	[HarmonyPatch(typeof(CounterofferInterface), "Open")]
	private static class CounterofferInterfacePostOpenPatch
	{
		private static void Postfix(ProductDefinition product, int quantity, float price, MSGConversation _conversation, Action<ProductDefinition, int, float> _orderConfirmedCallback)
		{
			MessagesApp instance = PlayerSingleton<MessagesApp>.Instance;
			Customer customerFromConversation = CustomerHelper.GetCustomerFromConversation(_conversation);
			if (instance == null || product == null || customerFromConversation == null)
			{
				Log("Counteroffer open automation skipped because the UI, product, or customer is unavailable.", error: false, warning: true);
			}
			else if (Config.GetCustomNPCMode(customerFromConversation.NPC) < 2)
			{
				var (optimalQuantity, optimalPrice) = DealCalculator.FindOptimalQuantity(customerFromConversation, product, quantity, price);
				if (!((float)optimalPrice <= price))
				{
					instance.CounterofferInterface.PriceSelector.ChangeAmount((float)(optimalPrice - (int)price));
					instance.CounterofferInterface.ChangeQuantity((float)(optimalQuantity - quantity));
				}
			}
		}
	}

	[HarmonyPatch(typeof(AmountSelector), "ChangeAmount")]
	private static class CounterofferInterfacePostChangePricePatch
	{
		private static void Postfix(AmountSelector __instance, float change)
		{
			MessagesApp instance = PlayerSingleton<MessagesApp>.Instance;
			if (!(instance == null) && !(instance.CounterofferInterface == null) && !(instance.CounterofferInterface.PriceSelector != __instance))
			{
				if (suppressNextPriceEval)
				{
					suppressNextPriceEval = false;
				}
				else
				{
					OfferPriceChangeCheckNoLog();
				}
			}
		}
	}

	[HarmonyPatch(typeof(CounterofferInterface), "ChangeQuantity", new Type[] { typeof(float) })]
	private static class CounterofferInterfacePostChangeQuantityPatch
	{
		private static void Postfix(float change)
		{
			MessagesApp instance = PlayerSingleton<MessagesApp>.Instance;
			if (instance == null || !instance.CounterofferInterface.IsOpen)
			{
				return;
			}
			OfferData offerData = GetOfferData();
			if (offerData == null)
			{
				return;
			}
			Customer customer = offerData.Customer;
			float maxSpend = DealCalculator.CalculateSpendingLimits(customer).maxSpend;
			CounterofferInterface counterofferInterface = instance.CounterofferInterface;
			ProductDefinition product = counterofferInterface.selectedProduct ?? offerData.Product;
			int quantity = offerData.Quantity;
			float price = offerData.Price;
			int optimalPrice = DealCalculator.FindOptimalPrice(customer, product, quantity, 1f, maxSpend, -1f, null, log: true);
			if (optimalPrice <= 1)
			{
				ContractInfo offeredContractInfo = customer.OfferedContractInfo;
				if (offeredContractInfo != null)
				{
					ProductList products = offeredContractInfo.Products;
					if (products != null && products.entries?.Count > 0)
					{
						int quantity2 = offeredContractInfo.Products.entries[0].Quantity;
						if (quantity2 > 0)
						{
							optimalPrice = Math.Min((int)(offeredContractInfo.Payment / (float)quantity2 * (float)quantity), (int)maxSpend - 1);
							Log($"FindOptimalPrice fallback: proportional price {optimalPrice} for {quantity} units");
						}
					}
				}
			}
			if (!Mathf.Approximately((float)optimalPrice, price))
			{
				suppressNextPriceEval = true;
				counterofferInterface.PriceSelector.ChangeAmount((float)(optimalPrice - (int)price));
			}
			OfferPriceChangeCheckNoLog();
		}
	}

	private class OfferData
	{
		public Customer Customer { get; }

		public ProductDefinition Product { get; }

		public int Quantity { get; }

		public float Price { get; }

		public OfferData(Customer customer, ProductDefinition product, int quantity, float price)
		{
			Customer = customer;
			Product = product;
			Quantity = quantity;
			Price = price;
		}
	}

	[HarmonyPatch(typeof(ProductManager), "SendPrice")]
	private static class ProductManagerSetPricePatch
	{
		private static void Postfix(string productID, float value)
		{
			Log($"ProductManager: Set price: {value} for {productID}", error: false, warning: false, bypassConfig: true);
			if (UIHandler.SelectedProduct != null && UIHandler.SelectedProduct.Definition.ID == productID)
			{
				optimalPriceCache.Remove(productID);
				Instance.uiHandler.UpdateSalesInfoPanel(UIHandler.SelectedProduct, value);
			}
		}
	}

	[HarmonyPatch(typeof(ProductManagerApp), "SelectProduct")]
	private static class ProductManagerSelectProductPatch
	{
		public static void Prefix(ProductManagerApp __instance, ProductEntry entry)
		{
			productManagerApp = __instance;
		}

		private static void Postfix(ProductEntry entry)
		{
			Instance.uiHandler.UpdateSalesInfoPanel(entry);
		}
	}

	[HarmonyPatch(typeof(ContactsDetailPanel), "Open")]
	public static class ContactsDetailPanelOpenPatch
	{
		[HarmonyPostfix]
		public static void Postfix(NPC npc, ContactsDetailPanel __instance)
		{
			if (!(npc == null))
			{
				Log($"RelationCircle: {npc.ID}, Name: {npc.FullName}, RelationDelta: {npc.RelationData.RelationDelta}, Unlocked: {npc.RelationData.Unlocked}");
				Customer customerFromNPC = CustomerHelper.GetCustomerFromNPC(npc);
				if (customerFromNPC == null)
				{
					Log("Not a customer");
					Instance.uiHandler.UpdateContactInfoPanel(null);
				}
				else
				{
					Instance.uiHandler.UpdateContactInfoPanel(customerFromNPC);
				}
			}
		}
	}

	public class CustomerAppeal
	{
		public Customer Customer { get; set; }

		public float Appeal { get; set; }

		public PaymentQuantityResult PaymentQuantity { get; set; }
	}

	[HarmonyPatch(typeof(Customer), "OfferContract")]
	public static class OfferContractPatch
	{
		public static void Postfix(ContractInfo info, Customer __instance, MethodBase __originalMethod)
		{
			if (__instance == null || info == null || info.Products == null || info.Products.entries == null || info.Products.entries.Count == 0)
			{
				Log("OfferContractPatch skipped because the contract info is incomplete.", error: false, warning: true);
			}
			else if (Config.GetCustomNPCMode(__instance.NPC) >= 1)
			{
				Log("OfferContractPatch: skipping automation for custom NPC: " + ((__instance.customerData != null) ? __instance.customerData.name : "unknown"));
			}
			else if (__instance.customerData == null || __instance.NPC == null)
			{
				Log("OfferContractPatch skipped because customerData or NPC is null.", error: false, warning: true);
			}
			else if (Config.AutoCounterOfferEnabled.Value)
			{
				Log("Customer: " + ((__instance.customerData != null) ? __instance.customerData.name : "unknown") + ", Contract: " + info.Products.entries[0].ProductID);
				ProductList.Entry entry = info.Products.entries[0];
				Func<ProductDefinition, bool> matchesEntry = (ProductDefinition product) => product.ID.Equals(entry.ProductID);
				ProductDefinition offerProduct = null;
				foreach (ProductDefinition listedProduct in ProductManager.ListedProducts)
				{
					if (matchesEntry(listedProduct))
					{
						offerProduct = listedProduct;
						break;
					}
				}
				if (offerProduct == null)
				{
					offerProduct = Registry.GetItem<ProductDefinition>(entry.ProductID);
				}
				if (offerProduct == null)
				{
					Log("Auto counter-offer skipped because the offered product could not be resolved.", error: false, warning: true);
					return;
				}
				float maxSpend = DealCalculator.CalculateSpendingLimits(__instance).maxSpend;
				if ((double)info.Payment >= Math.Floor(maxSpend))
				{
					Log("Skipping counter offer for " + __instance.customerData.name + " because they are over their max spend");
					new DelayedCounterOffer(__instance, offerProduct, entry.Quantity, info.Payment, skipCounterOffer: true).Start();
					return;
				}
				InitialOfferData initialOfferData = new InitialOfferData
				{
					ProductId = entry.ProductID,
					Quantity = entry.Quantity,
					Payment = info.Payment
				};
				var (quantity, counterOfferPrice) = DealCalculator.FindOptimalQuantity(__instance, offerProduct, entry.Quantity, info.Payment, initialOfferData);
				new DelayedCounterOffer(__instance, offerProduct, quantity, counterOfferPrice).Start();
			}
			else if (Config.AutoContractAcceptedEnabled.Value)
			{
				Log("Auto-scheduling (without counter-offer) for " + ((__instance.customerData != null) ? __instance.customerData.name : "unknown"));
				new DelayedCounterOffer(__instance, null, 0, 0f, skipCounterOffer: true).Start();
			}
		}
	}

	[HarmonyPatch(typeof(Customer), "EvaluateCounteroffer")]
	public static class EvaluateCounterOfferPatch
	{
		public static bool Prefix(Customer __instance, ref bool __result)
		{
			if (!Config.AlwaysAcceptCounterOffersEnabled.Value)
			{
				return true;
			}
			if (__instance != null && Config.GetCustomNPCMode(__instance.NPC) >= 1)
			{
				return true;
			}
			__result = true;
			Log("AlwaysAcceptCounterOffers: forcing accept for " + ((__instance != null && __instance.customerData != null) ? __instance.customerData.name : "unknown"));
			return false;
		}

		public static void Postfix(Customer __instance, bool __result)
		{
			if (!__result)
			{
				Log("Counter failed", error: false, warning: true);
			}
			else
			{
				TryAutoScheduleDeal(__instance);
			}
		}
	}

	[HarmonyPatch(typeof(Customer), "AcceptContractClicked")]
	public static class AcceptContractClickedPatch
	{
		public static void Postfix(Customer __instance)
		{
			TryAutoScheduleDeal(__instance);
		}
	}

	[HarmonyPatch(typeof(LevelManager), "AddXP")]
	public static class LevelManagerLoggingPatch
	{
		public static void Postfix(int xp)
		{
			Log($"LevelManager: AddXP: {xp}");
		}
	}

	private bool listening;

	private string lastCounterofferPriceText;

	internal static bool suppressNextPriceEval;

	private UIHandler uiHandler;

	private static Dictionary<string, (float optimalPrice, float noLossPrice)> optimalPriceCache = new Dictionary<string, (float optimalPrice, float noLossPrice)>();

	public static ProductManager productManager;

	public static ProductManagerApp productManagerApp;

	public static Main Instance { get; private set; }

	public static void Log(string message, bool error = false, bool warning = false, bool bypassConfig = false)
	{
		if (Config.DebugModeEnabled.Value || bypassConfig)
		{
			if (error)
			{
				Melon<Main>.Logger.Error(message);
			}
			else if (warning)
			{
				Melon<Main>.Logger.Warning(message);
			}
			else
			{
				Melon<Main>.Logger.Msg(message);
			}
		}
	}

	private static OfferData GetOfferData()
	{
		MessagesApp instance = PlayerSingleton<MessagesApp>.Instance;
		if (instance == null || instance.currentConversation == null || instance.CounterofferInterface == null || instance.CounterofferInterface.ProductLabel == null || instance.CounterofferInterface.PriceSelector == null)
		{
			Log("Counter-offer data could not be read because the UI state is incomplete.", error: false, warning: true);
			return null;
		}
		Customer customerFromMessagesApp = CustomerHelper.GetCustomerFromMessagesApp(instance);
		if (customerFromMessagesApp == null || customerFromMessagesApp.OfferedContractInfo == null || customerFromMessagesApp.OfferedContractInfo.Products == null || customerFromMessagesApp.OfferedContractInfo.Products.entries == null || customerFromMessagesApp.OfferedContractInfo.Products.entries.Count == 0)
		{
			Log("Counter-offer data could not be read because the customer contract state is incomplete.", error: false, warning: true);
			return null;
		}
		ProductList.Entry contractEntry = customerFromMessagesApp.OfferedContractInfo.Products.entries[0];
		ProductDefinition product = Registry.GetItem<ProductDefinition>(contractEntry.ProductID);
		if (product == null)
		{
			Log("Counter-offer data could not be read because the offered product could not be resolved.", error: false, warning: true);
			return null;
		}
		string[] quantityParts = (instance.CounterofferInterface.ProductLabel.text ?? string.Empty).Split(new char[1] { 'x' }, StringSplitOptions.RemoveEmptyEntries);
		if (quantityParts.Length == 0 || !int.TryParse(quantityParts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedQuantity) || parsedQuantity <= 0)
		{
			parsedQuantity = contractEntry.Quantity;
		}
		if (parsedQuantity <= 0)
		{
			Log("Counter-offer data could not be read because the quantity is invalid.", error: false, warning: true);
			return null;
		}
		string priceText = instance.CounterofferInterface.PriceSelector.SelectedAmount.ToString(CultureInfo.InvariantCulture);
		float parsedPrice = 0f;
		if (!string.IsNullOrWhiteSpace(priceText) && !float.TryParse(priceText, NumberStyles.Float, CultureInfo.CurrentCulture, out parsedPrice) && !float.TryParse(priceText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedPrice))
		{
			Log("Counter-offer data could not be read because the entered price is invalid.", error: false, warning: true);
			return null;
		}
		return new OfferData(customerFromMessagesApp, product, parsedQuantity, parsedPrice);
	}

	public static void OfferPriceChangeCheckNoLog()
	{
		OfferData offerData = GetOfferData();
		if (offerData != null)
		{
			EvaluateCounterOffer(offerData);
		}
	}

	private void OfferPriceChangeCheckWithLog()
	{
		OfferData offerData = GetOfferData();
		if (offerData != null)
		{
			string summary = $"\nCustomer Name: {offerData.Customer.name}\nProduct: {offerData.Product.ID}\nQuantity: {offerData.Quantity}\nPrice: {offerData.Price}\n";
			EvaluateCounterOffer(summary, offerData);
			Log(summary.ToString());
		}
	}

	private static void EvaluateCounterOffer(OfferData offerData)
	{
		if (offerData == null || offerData.Customer == null || offerData.Product == null || offerData.Quantity <= 0)
		{
			Log("Counter-offer evaluation skipped because the offer data is incomplete.", error: false, warning: true);
			return;
		}
		float maxSpend = DealCalculator.CalculateSpendingLimits(offerData.Customer).maxSpend;
		decimal maxSpendRounded = Math.Round((decimal)maxSpend, 2);
		if (offerData.Price >= maxSpend)
		{
			DisplayHelper.UpdateCounterOfferDisplayText("Guaranteed Failure", $"Exceeded Max Spend ({maxSpendRounded})");
			return;
		}
		MessagesApp instance = PlayerSingleton<MessagesApp>.Instance;
		ProductDefinition product = ((instance != null && instance.CounterofferInterface != null && instance.CounterofferInterface.selectedProduct != null) ? instance.CounterofferInterface.selectedProduct : offerData.Product);
		InitialOfferData initialOfferData = new InitialOfferData
		{
			ProductId = offerData.Customer.OfferedContractInfo.Products.entries[0].ProductID,
			Quantity = offerData.Customer.OfferedContractInfo.Products.entries[0].Quantity,
			Payment = offerData.Customer.OfferedContractInfo.Payment
		};
		DisplayHelper.UpdateCounterOfferDisplayText($"Probability of success: {Math.Round((decimal)(DealCalculator.CalculateSuccessProbability(offerData.Customer, product, offerData.Quantity, offerData.Price, initialOfferData) * 100f), 3)}%", $"Price per unit: {offerData.Price / (float)offerData.Quantity}\nMax Spend: {maxSpendRounded}");
	}

	private void EvaluateCounterOffer(string stringBuilder, OfferData offerData)
	{
		if (offerData == null || offerData.Customer == null || offerData.Product == null || offerData.Quantity <= 0)
		{
			Log("Detailed counter-offer evaluation skipped because the offer data is incomplete.", error: false, warning: true);
			return;
		}
		(float maxSpend, float avgDailySpend) limits = DealCalculator.CalculateSpendingLimits(offerData.Customer);
		float maxSpend = limits.maxSpend;
		float avgDailySpend = limits.avgDailySpend;
		decimal maxSpendRounded = Math.Round((decimal)maxSpend, 2);
		string[] parts = new string[12]
		{
			"\nAdjusted Weekly Spend: ", null, null, null, null, null, null, null, null, null,
			null, null
		};
		int weeklySpendIdx = 1;
		parts[weeklySpendIdx] = $"{offerData.Customer.CustomerData.GetAdjustedWeeklySpend(offerData.Customer.NPC.RelationData.RelationDelta / 5f)}\n";
		parts[2] = "Order Days: ";
		int orderDaysIdx = 3;
		Il2CppSystem.Collections.Generic.List<EDay> orderDays = new Il2CppSystem.Collections.Generic.List<EDay>();
		offerData.Customer.CustomerData.GetOrderDays(offerData.Customer.CurrentAddiction, offerData.Customer.NPC.RelationData.RelationDelta / 5f, orderDays);
		parts[orderDaysIdx] = $"{orderDays.Count}\n";
		parts[4] = "Average Daily Spend: ";
		int avgDailySpendIdx = 5;
		parts[avgDailySpendIdx] = $"{avgDailySpend}\n";
		parts[6] = "Daily Spend Threshold (3x Avg): ";
		int thresholdIdx = 7;
		parts[thresholdIdx] = $"{maxSpendRounded}\n";
		parts[8] = "Min Weekly Spend: ";
		int minWeeklySpendIdx = 9;
		parts[minWeeklySpendIdx] = $"{offerData.Customer.CustomerData.MinWeeklySpend}\n";
		parts[10] = "Max Weekly Spend: ";
		int maxWeeklySpendIdx = 11;
		parts[maxWeeklySpendIdx] = $"{offerData.Customer.CustomerData.MaxWeeklySpend}\n";
		string report = string.Concat(parts);
		bool noDealer = offerData.Customer.AssignedDealer == null;
		string orderableList = "";
		foreach (ProductDefinition orderableProduct in offerData.Customer.GetOrderableProducts(noDealer ? null : offerData.Customer.AssignedDealer))
		{
			orderableList += $"[Name: {orderableProduct.Name}, Price: {orderableProduct.Price}, Market: {orderableProduct.MarketValue}],\n";
		}
		report = report + "Orderable Products: " + orderableList + "\n";
		if (offerData.Price >= maxSpend)
		{
			report += "\nGuaranteed Failure - order must be less than 3x average daily spend\n";
			string reasonText = $"Exceeded Max Spend ({maxSpendRounded})";
			DisplayHelper.UpdateCounterOfferDisplayText("Guaranteed Failure", reasonText);
			return;
		}
		InitialOfferData initialOfferData = new InitialOfferData
		{
			ProductId = offerData.Customer.OfferedContractInfo.Products.entries[0].ProductID,
			Quantity = offerData.Customer.OfferedContractInfo.Products.entries[0].Quantity,
			Payment = offerData.Customer.OfferedContractInfo.Payment
		};
		decimal probabilityPct = Math.Round((decimal)(DealCalculator.CalculateSuccessProbability(offerData.Customer, offerData.Product, offerData.Quantity, offerData.Price, initialOfferData) * 100f), 3);
		string priorReport = report;
		string probabilityPrefix = "\nProbability of success: ";
		report = priorReport + probabilityPrefix + $"{probabilityPct}\n";
		DisplayHelper.UpdateCounterOfferDisplayText($"Probability of success: {probabilityPct}", "");
		Log(report);
	}

	public override void OnInitializeMelon()
	{
		Instance = this;
		uiHandler = new UIHandler();
		Config.Initialize();
		Log("Initialized HighBaller Fixed v" + this.Info.Version, error: false, warning: false, bypassConfig: true);
		Log("DebugMode is " + (Config.DebugModeEnabled.Value ? "enabled" : "disabled") + ", logs will show if it is enabled.", error: false, warning: false, bypassConfig: true);
	}

	public override void OnGUI()
	{
		uiHandler.OnGUI();
	}

	public override void OnSceneWasLoaded(int buildIndex, string sceneName)
	{
		if (sceneName == "Main")
		{
			try
			{
				Il2CppArrayBase<ProductManager> foundManagers = Object.FindObjectsOfType<ProductManager>();
				if (foundManagers != null && foundManagers.Length > 0)
				{
					productManager = foundManagers[0];
					Log($"ProductManager: {productManager}");
				}
				else
				{
					Log("No ProductManager found in scene.", error: false, warning: true);
				}
			}
			catch (Exception ex)
			{
				Log("Error finding ProductManager: " + ex.Message, error: false, warning: false, bypassConfig: true);
			}
		}
	}

	public static float EvaluatePriceAcceptance(ProductDefinition product, List<CustomerAppeal> customerAppeals, float price)
	{
		int qualifyingCount = 0;
		foreach (CustomerAppeal customerAppeal in customerAppeals)
		{
			if (!((double)customerAppeal.Appeal < 0.05000000074505806) && customerAppeal.PaymentQuantity != null && customerAppeal.PaymentQuantity.Quantity > 0)
			{
				qualifyingCount++;
			}
		}
		return price * (float)qualifyingCount;
	}

	public static (float optimalPrice, float noLossPrice) GetOptimalPrice(ProductDefinition product, List<CustomerAppeal> customerAppeals, float initialPrice, float minPrice, float maxPrice, Action<(float optimalPrice, float noLossPrice)> callback = null)
	{
		Log($"Getting optimal price for {product.Name} at {initialPrice}");
		if (optimalPriceCache.TryGetValue(product.ID, out var cached))
		{
			callback?.Invoke(cached);
			return cached;
		}
		new DelayedPriceCalculator(product, customerAppeals, initialPrice, minPrice, maxPrice, delegate((float optimalPrice, float noLossPrice) result)
		{
			optimalPriceCache[product.ID] = result;
			callback?.Invoke(result);
		}).Start();
		return (initialPrice, 0f);
	}

	public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
	{
		if (listening)
		{
			listening = false;
		}
		lastCounterofferPriceText = null;
	}

	public override void OnUpdate()
	{
		Scene activeScene = SceneManager.GetActiveScene();
		bool isMainScene = activeScene.name == "Main";
		MessagesApp instance = PlayerSingleton<MessagesApp>.Instance;
		if (isMainScene && !listening && instance != null)
		{
			Subscribe();
		}
		if (isMainScene && instance != null && instance.CounterofferInterface != null && instance.CounterofferInterface.PriceSelector != null && instance.CounterofferInterface.IsOpen)
		{
			string currentPriceText = instance.CounterofferInterface.PriceSelector.SelectedAmount.ToString(CultureInfo.InvariantCulture);
			if (!string.Equals(lastCounterofferPriceText, currentPriceText, StringComparison.Ordinal))
			{
				lastCounterofferPriceText = currentPriceText;
				OfferPriceChangeCheckWithLog();
			}
		}
		else
		{
			lastCounterofferPriceText = null;
		}
	}

	private void Subscribe()
	{
		MessagesApp instance = PlayerSingleton<MessagesApp>.Instance;
		if (instance == null || instance.CounterofferInterface == null || instance.CounterofferInterface.PriceSelector == null)
		{
			Log("Listener attachment skipped because the counter-offer UI is not ready.", error: false, warning: true);
			return;
		}
		Log("Counter-offer polling initialized");
		listening = true;
	}

	private static int ResolveCurrentWindowOption()
	{
		try
		{
			TimeManager instance = NetworkSingleton<TimeManager>.Instance;
			if (instance == null)
			{
				Log("Window 5: TimeManager unavailable, defaulting to Late Night.", error: false, warning: true);
				return 4;
			}
			int currentTime = instance.CurrentTime;
			int minSumFrom24HourTime = TimeManager.GetMinSumFrom24HourTime(currentTime);
			int cutoffMins = DealWindowSelector.WINDOW_CUTOFF_MINS;
			if (cutoffMins <= 0)
			{
				cutoffMins = 60;
			}
			for (int i = 0; i < 4; i++)
			{
				EDealWindow window = (EDealWindow)i;
				DealWindowInfo windowInfo = DealWindowInfo.GetWindowInfo(window);
				int minSumFrom24HourTime2 = TimeManager.GetMinSumFrom24HourTime(windowInfo.StartTime);
				int windowEnd = TimeManager.GetMinSumFrom24HourTime(windowInfo.EndTime);
				if (windowEnd <= minSumFrom24HourTime2)
				{
					windowEnd += 1440;
				}
				int currentMin = minSumFrom24HourTime;
				if (currentMin < minSumFrom24HourTime2)
				{
					currentMin += 1440;
				}
				if (currentMin >= minSumFrom24HourTime2 && currentMin < windowEnd)
				{
					if (windowEnd - currentMin > cutoffMins)
					{
						Log($"Window 5: current time {currentTime} -> window {window} (option {i + 1}).");
						return i + 1;
					}
					int fallbackIndex = (i + 1) % 4;
					Log($"Window 5: current window {window} too close to cutoff (remaining {windowEnd - currentMin} mins), falling back to {(EDealWindow)fallbackIndex} (option {fallbackIndex + 1}).");
					return fallbackIndex + 1;
				}
			}
			Log($"Window 5: current time {currentTime} did not match any window, defaulting to Morning.", error: false, warning: true);
			return 1;
		}
		catch (Exception ex)
		{
			Log("Window 5 resolution error: " + ex.Message + " - defaulting to Late Night.", error: false, warning: true);
			return 4;
		}
	}

	private static void TryAutoScheduleDeal(Customer customer)
	{
		if (!Config.AutoContractAcceptedEnabled.Value)
		{
			return;
		}
		if (customer != null && Config.GetCustomNPCMode(customer.NPC) >= 1)
		{
			object npcName;
			if (!(customer.customerData != null))
			{
				NPC resolvedNpc = customer.NPC;
				npcName = ((resolvedNpc != null) ? resolvedNpc.FullName : null) ?? "unknown";
			}
			else
			{
				npcName = customer.customerData.name;
			}
			Log("Auto scheduling skipped for custom NPC: " + (string)npcName);
			return;
		}
		if (customer == null || customer.OfferedContractInfo == null)
		{
			Log("Auto scheduling skipped because the customer or offered contract is null.", error: false, warning: true);
			return;
		}
		Log("Auto scheduling for " + ((customer.customerData != null) ? customer.customerData.name : "unknown"));
		int windowOption = Config.AutoContractAcceptedWindow.Value;
		if (customer.NPC == null || customer.NPC.MSGConversation == null)
		{
			Log("Auto scheduling skipped because NPC or conversation is null.", error: false, warning: true);
			return;
		}
		if (windowOption == 5)
		{
			windowOption = ResolveCurrentWindowOption();
		}
		EDealWindow window;
		string windowName;
		switch (windowOption)
		{
		case 1:
			window = EDealWindow.Morning;
			windowName = "Morning";
			break;
		case 2:
			window = EDealWindow.Afternoon;
			windowName = "Afternoon";
			break;
		case 3:
			window = EDealWindow.Night;
			windowName = "Night";
			break;
		case 4:
			window = EDealWindow.LateNight;
			windowName = "Late Night";
			break;
		default:
			window = EDealWindow.LateNight;
			windowName = "Late Night";
			Log("AutoContractAcceptedWindow is not correctly set!\nDefaulting to Late Night \nCorrect Values: 1 = Morning, 2 = Afternoon, 3 = Night, 4 = Late Night, 5 = Current", error: false, warning: true);
			break;
		}
		customer.NPC.MSGConversation.SendMessage(new Message(windowName, Message.ESenderType.Player, true, -1), true, true);
		customer.NPC.MSGConversation.ClearResponses(true);
		new DelayedScheduler(customer, window).Start();
	}
}
