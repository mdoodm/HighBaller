using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppScheduleOne.Core.Items.Framework;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Relation;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.UI.Phone.ContactsApp;
using Il2CppScheduleOne.UI.Phone.Messages;
using Il2CppScheduleOne.UI.Phone.ProductManagerApp;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighBaller;

public class UIHandler
{
	private class PanelState
	{
		public bool IsVisible;

		public bool IsDragging;

		public Vector2 Position;

		public Vector2 DragOffset;

		public Vector2 ScrollPosition;

		public string Content;

		public string Title;

		public PanelState(string title)
		{
			IsVisible = false;
			IsDragging = false;
			Position = new Vector2(20f, 20f);
			DragOffset = Vector2.zero;
			ScrollPosition = Vector2.zero;
			Content = "";
			Title = title;
		}
	}

	private GUIStyle displayTextStyle;

	private static string counterOfferDisplayText = "";

	private static GUIStyle salesPanelStyle;

	private static GUIStyle salesHeaderStyle;

	private static GUIStyle salesTextStyle;

	private GUIStyle sectionHeaderStyle;

	private GUIStyle infoLabelStyle;

	private GUIStyle infoValueStyle;

	private GUIStyle infoValueHighlightStyle;

	private PanelState salesPanel;

	private PanelState contactPanel;

	private DelayedPriceCalculator currentPriceCalculator;

	private Texture2D cachedPanelBackground;

	private static int guiScale = 1;

	private const int MaxGuiScale = 10;

	public static ProductEntry SelectedProduct { get; set; } = null;

	public static Customer SelectedCustomer { get; set; } = null;

	public UIHandler()
	{
		InitializeStyles();
		salesPanel = new PanelState("Sales Information");
		contactPanel = new PanelState("Contact Information");
	}

	private void InitializeStyles()
	{
		displayTextStyle = new GUIStyle((GUIStyle)"label");
		displayTextStyle.fontSize = 18;
		displayTextStyle.normal.background = Texture2D.whiteTexture;
		displayTextStyle.alignment = TextAnchor.MiddleCenter;
		salesPanelStyle = new GUIStyle((GUIStyle)"box");
		salesPanelStyle.normal.background = Texture2D.whiteTexture;
		salesPanelStyle.normal.textColor = Color.white;
		salesPanelStyle.padding = new RectOffset(10, 10, 10, 10);
		salesHeaderStyle = new GUIStyle((GUIStyle)"label");
		salesHeaderStyle.fontSize = 20;
		salesHeaderStyle.fontStyle = FontStyle.Bold;
		salesHeaderStyle.normal.textColor = Color.white;
		salesHeaderStyle.alignment = TextAnchor.MiddleLeft;
		salesHeaderStyle.padding = new RectOffset(10, 0, 0, 0);
		salesTextStyle = new GUIStyle((GUIStyle)"label");
		salesTextStyle.fontSize = 14;
		salesTextStyle.normal.textColor = Color.white;
		salesTextStyle.wordWrap = true;
		salesTextStyle.padding = new RectOffset(10, 0, 0, 0);
		sectionHeaderStyle = new GUIStyle((GUIStyle)"label");
		sectionHeaderStyle.fontSize = 16;
		sectionHeaderStyle.fontStyle = FontStyle.Bold;
		sectionHeaderStyle.normal.textColor = new Color(0.9f, 0.9f, 1f);
		sectionHeaderStyle.padding = new RectOffset(0, 0, 10, 5);
		infoLabelStyle = new GUIStyle((GUIStyle)"label");
		infoLabelStyle.fontSize = 14;
		infoLabelStyle.fontStyle = FontStyle.Bold;
		infoLabelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
		infoLabelStyle.padding = new RectOffset(10, 0, 2, 2);
		infoValueStyle = new GUIStyle((GUIStyle)"label");
		infoValueStyle.fontSize = 14;
		infoValueStyle.normal.textColor = new Color(1f, 0.95f, 0.8f);
		infoValueStyle.padding = new RectOffset(10, 0, 2, 8);
		infoValueHighlightStyle = new GUIStyle((GUIStyle)"label");
		infoValueHighlightStyle.fontSize = 14;
		infoValueHighlightStyle.fontStyle = FontStyle.Bold;
		infoValueHighlightStyle.normal.textColor = new Color(0.3f, 1f, 0.4f);
		infoValueHighlightStyle.padding = new RectOffset(10, 0, 2, 8);
	}

	private string FormatCurrency(float amount)
	{
		return $"${amount:F2}";
	}

	public void UpdateCounterOfferDisplayText(string text, string reasonText)
	{
		counterOfferDisplayText = text + "\n" + reasonText;
	}

	private string FormatSalesInfo(ProductEntry entry, float? overridePrice = null)
	{
		if (entry == null || entry.Definition == null)
		{
			Main.Log("Sales info formatting skipped because entry or definition is null.", error: false, warning: true);
			return "";
		}
		string html = "<section>Product Details</section>\n<label>Product:</label> <value>" + entry.Definition.Name + "</value>\n<label>Base Price:</label> <value>" + FormatCurrency(entry.Definition.Price) + "</value>\n<label>Market Value:</label> <value>" + FormatCurrency(entry.Definition.MarketValue) + "</value>\n";
		if (overridePrice.HasValue && overridePrice.Value != entry.Definition.Price)
		{
			html = html + "<label>Current Price:</label> <value>" + FormatCurrency(overridePrice.Value) + "</value>\n";
		}
		List<Customer> knownCustomers = CustomerHelper.GetKnownCustomers();
		List<Main.CustomerAppeal> appeals = new List<Main.CustomerAppeal>();
		for (int i = 0; i < knownCustomers.Count; i++)
		{
			appeals.Add(new Main.CustomerAppeal
			{
				Customer = knownCustomers[i],
				Appeal = CustomerHelper.GetProductAppeal(entry.Definition, knownCustomers[i], overridePrice),
				PaymentQuantity = CustomerHelper.CalculatePaymentQuantity(entry.Definition, knownCustomers[i], overridePrice)
			});
		}
		List<Main.CustomerAppeal> sortedAppeals = appeals.OrderByDescending((Main.CustomerAppeal ca) => ca.Appeal).ToList();
		List<Main.CustomerAppeal> qualifyingAppeals = sortedAppeals.FindAll((Main.CustomerAppeal ca) => (double)ca.Appeal > 0.05000000074505806);
		float maxSpenderScore = Main.EvaluatePriceAcceptance(entry.Definition, sortedAppeals, overridePrice ?? entry.Definition.Price);
		string[] marketParts = new string[12]
		{
			html, "<section>Market Analysis</section>\n<label>Potential Customers:</label> <value-good>", null, null, null, null, null, null, null, null,
			null, null
		};
		int potentialCustomersIdx = 2;
		marketParts[potentialCustomersIdx] = $"{qualifyingAppeals.Count}";
		marketParts[3] = "</value-good>\n<label>Average Order Size:</label> <value>";
		int avgOrderSizeIdx = 4;
		double avgOrderSize = ((qualifyingAppeals.Count > 0) ? qualifyingAppeals.Average((Main.CustomerAppeal ca) => ca.PaymentQuantity.Quantity) : 0.0);
		marketParts[avgOrderSizeIdx] = $"{avgOrderSize:F1}";
		marketParts[5] = "</value>\n<label>Average Payment:</label> <value>";
		int avgPaymentIdx = 6;
		string avgPayment = ((qualifyingAppeals.Count > 0) ? FormatCurrency(qualifyingAppeals.Average((Main.CustomerAppeal ca) => ca.PaymentQuantity.Payment)) : FormatCurrency(0f));
		marketParts[avgPaymentIdx] = avgPayment;
		marketParts[7] = "</value>\n<label>Per-Unit Price:</label> <value>";
		int perUnitPriceIdx = 8;
		string perUnitPrice = ((qualifyingAppeals.Count > 0) ? FormatCurrency(qualifyingAppeals.Average((Main.CustomerAppeal ca) => ca.PaymentQuantity.Payment / (float)ca.PaymentQuantity.Quantity)) : FormatCurrency(0f));
		marketParts[perUnitPriceIdx] = perUnitPrice;
		marketParts[9] = "</value>\n<label>Max Spender Score:</label> <value>";
		int maxSpenderScoreIdx = 10;
		marketParts[maxSpenderScoreIdx] = $"{maxSpenderScore:F2}";
		marketParts[11] = "</value>\n";
		html = string.Concat(marketParts);
		string[] appealParts = new string[10] { html, "<section>Appeal Statistics</section>\n<label>Customer Appeal:</label>\n", null, null, null, null, null, null, null, null };
		int avgAppealIdx = 2;
		float avgAppeal = ((qualifyingAppeals.Count > 0) ? qualifyingAppeals.Average((Main.CustomerAppeal ca) => ca.Appeal) : 0f);
		appealParts[avgAppealIdx] = $"<value>Average: {avgAppeal:F2}</value>\n";
		int minAppealIdx = 3;
		float minAppeal = ((qualifyingAppeals.Count > 0) ? qualifyingAppeals.Min((Main.CustomerAppeal ca) => ca.Appeal) : 0f);
		appealParts[minAppealIdx] = $"<value>Min: {minAppeal:F2}</value>\n";
		int maxAppealIdx = 4;
		float maxAppeal = ((qualifyingAppeals.Count > 0) ? qualifyingAppeals.Max((Main.CustomerAppeal ca) => ca.Appeal) : 0f);
		appealParts[maxAppealIdx] = $"<value>Max: {maxAppeal:F2}</value>\n";
		appealParts[5] = "<label>Overall Appeal:</label>\n<value>Average: ";
		int overallAvgIdx = 6;
		float overallAvgAppeal = ((sortedAppeals.Count > 0) ? sortedAppeals.Average((Main.CustomerAppeal ca) => ca.Appeal) : 0f);
		appealParts[overallAvgIdx] = $"{overallAvgAppeal:F2}";
		appealParts[7] = "</value>\n<value>Min: ";
		int overallMinIdx = 8;
		float overallMinAppeal = ((sortedAppeals.Count > 0) ? sortedAppeals.Min((Main.CustomerAppeal ca) => ca.Appeal) : 0f);
		appealParts[overallMinIdx] = $"{overallMinAppeal:F2}";
		appealParts[9] = "</value>\n";
		html = string.Concat(appealParts);
		if (Config.PriceOptimizationEnabled.Value)
		{
			html = html + "<section>Price Optimization</section>\n<label>Original Price:</label> <value>" + FormatCurrency(entry.Definition.Price) + "</value>\n<label>Optimal Price:</label> <value>Calculating...</value>\n<label>No-Loss Price:</label> <value>Calculating...</value>\n<label>Max Markup:</label> <value>Calculating...</value>\n";
			float initialPrice = overridePrice ?? entry.Definition.Price;
			float minPrice = 1f;
			float maxPrice = Math.Max(((sortedAppeals.Count > 0) ? sortedAppeals.Max((Main.CustomerAppeal ca) => Main.DealCalculator.CalculateSpendingLimits(ca.Customer).maxSpend) : (entry.Definition.MarketValue * 3f)) - 1f, entry.Definition.MarketValue * 2f);
			currentPriceCalculator = new DelayedPriceCalculator(entry.Definition, sortedAppeals, initialPrice, minPrice, maxPrice, delegate((float optimalPrice, float noLossPrice) result)
			{
				float optimalPrice = result.optimalPrice;
				float noLossPrice = result.noLossPrice;
				string[] contentLines = salesPanel.Content.Split(new char[1] { '\n' }, StringSplitOptions.None);
				for (int j = 0; j < contentLines.Length; j++)
				{
					if (contentLines[j].Contains("Optimal Price:"))
					{
						contentLines[j] = "<label>Optimal Price:</label> <value>" + FormatCurrency(optimalPrice) + "</value>\n";
					}
					else if (contentLines[j].Contains("No-Loss Price:"))
					{
						contentLines[j] = "<label>No-Loss Price:</label> <value>" + FormatCurrency(noLossPrice) + "</value>\n";
					}
					else if (contentLines[j].Contains("Max Markup:"))
					{
						contentLines[j] = $"<label>Max Markup:</label> <value>{(float)(((double)optimalPrice - (double)entry.Definition.Price) / (double)entry.Definition.Price * 100.0):F1}%</value>\n";
					}
				}
				salesPanel.Content = string.Join("\n", contentLines);
			});
			currentPriceCalculator.Start();
		}
		return html;
	}

	private List<EDay> GetSortedOrderDays(List<EDay> orderDays, EDay currentDay)
	{
		List<EDay> sortedDays = new List<EDay>();
		for (int i = 0; i < orderDays.Count; i++)
		{
			sortedDays.Add(orderDays[i]);
		}
		Func<EDay, EDay, int> comparison = delegate(EDay a, EDay b)
		{
			int daysFromA = a - currentDay;
			int daysFromB = b - currentDay;
			if (daysFromA < 0)
			{
				daysFromA += 7;
			}
			if (daysFromB < 0)
			{
				daysFromB += 7;
			}
			return daysFromA.CompareTo(daysFromB);
		};
		sortedDays.Sort(comparison.Invoke);
		return sortedDays;
	}

	private string FormatCustomerInfo(Customer customer, CustomerData customerData, NPCRelationData relationData, EDay currentDay)
	{
		string html = "<section>Customer Details</section>\n<label>Name:</label> <value>" + customer.NPC.FullName + "</value>\n";
		if (customer.AssignedDealer != null)
		{
			html = html + "<label>Assigned Dealer:</label> <value>" + customer.AssignedDealer.FirstName + " " + customer.AssignedDealer.LastName + "</value>\n";
		}
		Il2CppSystem.Collections.Generic.List<EDay> rawOrderDays = new Il2CppSystem.Collections.Generic.List<EDay>();
		customerData.GetOrderDays(customer.CurrentAddiction, relationData.RelationDelta / 5f, rawOrderDays);
		List<EDay> orderDays = new List<EDay>();
		for (int i = 0; i < rawOrderDays.Count; i++)
		{
			orderDays.Add(rawOrderDays[i]);
		}
		float amount = customerData.GetAdjustedWeeklySpend(relationData.RelationDelta / 5f) * 3f / (float)orderDays.Count;
		string[] financialParts = new string[10]
		{
			html,
			"<section>Financial Information</section>\n<label>Max Spend:</label> <value>",
			FormatCurrency(customerData.MaxWeeklySpend),
			"</value>\n<label>Min Spend:</label> <value>",
			FormatCurrency(customerData.MinWeeklySpend),
			"</value>\n<label>Daily Spend Limit:</label> <value>",
			FormatCurrency(amount),
			" (",
			null,
			null
		};
		int pctOfMaxIdx = 8;
		financialParts[pctOfMaxIdx] = $"{(double)customerData.GetAdjustedWeeklySpend(relationData.RelationDelta / 5f) / (double)customerData.GetAdjustedWeeklySpend(1f) * 100.0:F2}";
		financialParts[9] = "% of Max Daily)</value>\n";
		html = string.Concat(financialParts);
		List<EDay> sortedOrderDays = GetSortedOrderDays(orderDays, currentDay);
		html += "<section>Schedule Information</section>\n<label>Next Order Days:</label>\n";
		for (int j = 0; j < sortedOrderDays.Count; j++)
		{
			string priorHtml = html;
			string linePrefix = "<value>  ";
			html = priorHtml + linePrefix + $"{j + 1}. {sortedOrderDays[j]}" + "</value>\n";
		}
		html += "<section>Product Preferences</section>\n";
		var indexedProducts = CustomerHelper.GetProductsFromCustomer(customer).Select((CustomerHelper.ProductOrders p, int index) => new
		{
			ProductInfo = p,
			Index = index
		}).ToList();
		if (indexedProducts.Count == 0)
		{
			html = ((!(customer.AssignedDealer == null)) ? (html + "<value>Dealer has no appealing products for sale</value>\n") : (html + "<value>Won't buy anything</value>\n"));
		}
		foreach (var indexedProduct in indexedProducts)
		{
			string[] productParts = new string[10]
			{
				html,
				"<value>",
				indexedProduct.ProductInfo.Product.Name,
				" - $",
				null,
				null,
				null,
				null,
				null,
				null
			};
			int paymentIdx = 4;
			productParts[paymentIdx] = $"{indexedProduct.ProductInfo.PaymentQuantity.Payment:F2}";
			productParts[5] = " × ";
			int quantityIdx = 6;
			productParts[quantityIdx] = $"{indexedProduct.ProductInfo.PaymentQuantity.Quantity}";
			productParts[7] = " (Appeal: ";
			int appealIdx = 8;
			productParts[appealIdx] = $"{indexedProduct.ProductInfo.PaymentQuantity.Appeal:F3}";
			productParts[9] = ")</value>\n";
			html = string.Concat(productParts);
		}
		return html;
	}

	private void HandlePanelDragging(PanelState panel, Rect dragRect, Event currentEvent)
	{
		if (currentEvent.type == EventType.MouseDown && dragRect.Contains(currentEvent.mousePosition))
		{
			panel.IsDragging = true;
			panel.DragOffset = currentEvent.mousePosition - panel.Position;
			currentEvent.Use();
		}
		else if (currentEvent.type == EventType.MouseUp)
		{
			panel.IsDragging = false;
		}
		if (panel.IsDragging && currentEvent.type == EventType.MouseDrag)
		{
			panel.Position = currentEvent.mousePosition - panel.DragOffset;
			currentEvent.Use();
		}
	}

	private void DrawPanel(PanelState panel, float width, float height)
	{
		if (!panel.IsVisible)
		{
			return;
		}
		Rect dragRect = new Rect(panel.Position.x, panel.Position.y, width - 90f, 40f);
		HandlePanelDragging(panel, dragRect, Event.current);
		panel.Position.x = Mathf.Clamp(panel.Position.x, 0f, (float)Screen.width - width);
		panel.Position.y = Mathf.Clamp(panel.Position.y, 0f, (float)Screen.height - height);
		GUI.backgroundColor = Color.black;
		if (cachedPanelBackground == null)
		{
			cachedPanelBackground = new Texture2D(1, 1);
			cachedPanelBackground.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.2f, 0.95f));
			cachedPanelBackground.Apply();
		}
		ImguiCompat.DrawTexture(new Rect(panel.Position.x, panel.Position.y, width, height), cachedPanelBackground);
		string[] contentLines = panel.Content.Split(new char[1] { '\n' }, StringSplitOptions.None);
		float contentTop = panel.Position.y + 40f;
		float contentWidth = width - 20f;
		panel.ScrollPosition = GUI.BeginScrollView(new Rect(panel.Position.x, contentTop, width, height - 40f), panel.ScrollPosition, new Rect(0f, 0f, contentWidth, (float)(contentLines.Length * 25)));
		float lineY = 0f;
		string[] lines = contentLines;
		foreach (string line in lines)
		{
			if (line.StartsWith("<section>"))
			{
				string sectionTitle = line.Replace("<section>", "").Replace("</section>", "");
				GUI.Label(new Rect(10f, lineY, contentWidth, 25f), sectionTitle, sectionHeaderStyle);
				lineY += 30f;
			}
			else if (line.Contains("<label>"))
			{
				string labelText = "";
				string valueText = "";
				int labelStart = line.IndexOf("<label>") + 7;
				int labelEnd = line.IndexOf("</label>");
				if (labelStart >= 7 && labelEnd > labelStart)
				{
					labelText = line.Substring(labelStart, labelEnd - labelStart);
				}
				int valueStart = (line.Contains("<value-good>") ? (line.IndexOf("<value-good>") + 12) : (line.IndexOf("<value>") + 7));
				int valueEnd = (line.Contains("</value-good>") ? line.IndexOf("</value-good>") : line.IndexOf("</value>"));
				if (valueStart >= 7 && valueEnd > valueStart)
				{
					valueText = line.Substring(valueStart, valueEnd - valueStart);
				}
				if (!string.IsNullOrEmpty(labelText))
				{
					GUI.Label(new Rect(10f, lineY, 140f, 20f), labelText, infoLabelStyle);
					if (!string.IsNullOrEmpty(valueText))
					{
						bool isHighlight = line.Contains("<value-good>");
						GUI.Label(new Rect(160f, lineY, contentWidth - 160f, 20f), valueText, isHighlight ? infoValueHighlightStyle : infoValueStyle);
					}
					lineY += 25f;
				}
			}
			else if (line.StartsWith("<value>"))
			{
				string valueLine = line.Replace("<value>", "").Replace("</value>", "");
				GUI.Label(new Rect(20f, lineY, contentWidth - 20f, 20f), valueLine, infoValueStyle);
				lineY += 25f;
			}
			else if (!string.IsNullOrWhiteSpace(line))
			{
				GUI.Label(new Rect(10f, lineY, contentWidth, 20f), line, infoValueStyle);
				lineY += 25f;
			}
		}
		GUI.EndScrollView();
		GUI.Box(new Rect(panel.Position.x, panel.Position.y, width, 40f), "", salesPanelStyle);
		GUI.Label(new Rect(panel.Position.x, panel.Position.y, width, 40f), panel.Title + " (Drag to Move)", salesHeaderStyle);
		if (GUI.Button(new Rect(panel.Position.x + width - 30f, panel.Position.y + 5f, 25f, 25f), "X"))
		{
			panel.IsVisible = false;
			panel.Position = new Vector2(20f, 20f);
			if (panel == salesPanel)
			{
				SelectedProduct = null;
			}
			else if (panel == contactPanel)
			{
				SelectedCustomer = null;
			}
		}
		if (GUI.Button(new Rect(panel.Position.x + width - 60f, panel.Position.y + 5f, 25f, 25f), "+"))
		{
			if (guiScale < 10)
			{
				guiScale++;
			}
			sectionHeaderStyle.fontSize = 16 + guiScale;
			sectionHeaderStyle.padding = new RectOffset(0, 0, 10 + guiScale * 4, 5 + guiScale * 2);
			infoLabelStyle.fontSize = 14 + guiScale;
			infoLabelStyle.padding = new RectOffset(10, 0, 2 + guiScale * 4, 2 + guiScale * 4);
			infoValueStyle.fontSize = 14 + guiScale;
			infoValueStyle.padding = new RectOffset(10 + guiScale * 8, 0, 2 + guiScale * 2, 8 + guiScale * 4);
			infoValueHighlightStyle.fontSize = 14 + guiScale;
			infoValueHighlightStyle.padding = new RectOffset(10 + guiScale * 8, 0, 2 + guiScale * 2, 8 + guiScale * 4);
		}
		if (GUI.Button(new Rect(panel.Position.x + width - 90f, panel.Position.y + 5f, 25f, 25f), "-"))
		{
			guiScale--;
			if (guiScale < 1)
			{
				guiScale = 1;
			}
			sectionHeaderStyle.fontSize = 16 + guiScale;
			sectionHeaderStyle.padding = new RectOffset(0, 0, 10 + guiScale * 4, 5 + guiScale * 2);
			infoLabelStyle.fontSize = 14 + guiScale;
			infoLabelStyle.padding = new RectOffset(10, 0, 2 + guiScale * 4, 2 + guiScale * 4);
			infoValueStyle.fontSize = 14 + guiScale;
			infoValueStyle.padding = new RectOffset(10 + guiScale * 8, 0, 2 + guiScale * 2, 8 + guiScale * 4);
			infoValueHighlightStyle.fontSize = 14 + guiScale;
			infoValueHighlightStyle.padding = new RectOffset(10 + guiScale * 8, 0, 2 + guiScale * 2, 8 + guiScale * 4);
		}
	}

	public void UpdateSalesInfoPanel(ProductEntry entry, float? overridePrice = null)
	{
		if (currentPriceCalculator != null)
		{
			currentPriceCalculator.Cancel();
			currentPriceCalculator = null;
		}
		SelectedProduct = entry;
		salesPanel.IsVisible = true;
		salesPanel.ScrollPosition = Vector2.zero;
		salesPanel.Content = FormatSalesInfo(entry, overridePrice);
	}

	public void UpdateContactInfoPanel(Customer customer)
	{
		if (customer == null)
		{
			contactPanel.IsVisible = false;
			return;
		}
		if (customer.customerData == null || customer.NPC == null || customer.NPC.RelationData == null)
		{
			Main.Log("Contact info panel skipped because customer data, NPC, or relation data is null.", error: false, warning: true);
			contactPanel.IsVisible = false;
			return;
		}
		TimeManager instance = NetworkSingleton<TimeManager>.Instance;
		if (instance == null)
		{
			Main.Log("Contact info panel skipped because TimeManager is unavailable.", error: false, warning: true);
			contactPanel.IsVisible = false;
			return;
		}
		EDay currentDay = instance.CurrentDay;
		SelectedCustomer = customer;
		contactPanel.IsVisible = true;
		contactPanel.ScrollPosition = Vector2.zero;
		contactPanel.Content = FormatCustomerInfo(customer, customer.customerData, customer.NPC.RelationData, currentDay);
	}

	private void DrawSalesInfoPanel()
	{
		DrawPanel(salesPanel, 420f, 800f);
	}

	private void DrawContactInfoPanel()
	{
		DrawPanel(contactPanel, 440f, 700f);
	}

	public void OnGUI()
	{
		Scene activeScene = SceneManager.GetActiveScene();
		if (activeScene.name != "Main")
		{
			return;
		}
		Phone instance = PlayerSingleton<Phone>.Instance;
		if (instance == null || !instance.IsOpen)
		{
			return;
		}
		if ((!PlayerSingleton<HomeScreen>.Instance.isOpen && (PlayerSingleton<MessagesApp>.Instance != null)) && PlayerSingleton<MessagesApp>.Instance.CounterofferInterface.IsOpen)
		{
			if (Config.CounterOfferDarkModeEnabled.Value)
			{
				GUI.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
				displayTextStyle.normal.textColor = Color.white;
			}
			else
			{
				displayTextStyle.normal.textColor = Color.black;
			}
			GUI.Label(new Rect((float)(Screen.width / 2 - 190), (float)(Screen.height / 2 - 250), 380f, 70f), counterOfferDisplayText, displayTextStyle);
			GUI.backgroundColor = Color.white;
		}
		if (Main.productManagerApp != null && Main.productManagerApp.isOpen && salesPanel.IsVisible)
		{
			DrawSalesInfoPanel();
		}
		if (!(PlayerSingleton<ContactsApp>.Instance == null) && PlayerSingleton<ContactsApp>.Instance.isOpen && contactPanel.IsVisible)
		{
			DrawContactInfoPanel();
		}
	}
}
