#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.Product;
using ScheduleOne.UI.Phone;
using ScheduleOne.UI.Phone.ContactsApp;
using ScheduleOne.UI.Phone.Messages;
using ScheduleOne.UI.Phone.ProductManagerApp;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.UI.Phone.ContactsApp;
using Il2CppScheduleOne.UI.Phone.Messages;
using Il2CppScheduleOne.UI.Phone.ProductManagerApp;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HighBaller.UI;

public class UIHandler
{
    internal static ProductManagerApp ProductManagerApp;
    
    private static string _counterOfferDisplayText = "";

    private readonly PanelState _contactPanel;

    private GUIStyle _displayTextStyle;

    private readonly PanelRenderer _panelRenderer;

    private readonly SalesInfoFormatter _salesInfoFormatter;

    private readonly PanelState _salesPanel;

    public UIHandler()
    {
        InitializeStyles();
        _salesPanel = new PanelState("Sales Information");
        _contactPanel = new PanelState("Contact Information");
        _salesInfoFormatter = new SalesInfoFormatter(_salesPanel);
        _panelRenderer = new PanelRenderer(_salesPanel, _contactPanel);
    }

    public static ProductEntry SelectedProduct { get; set; }

    public static Customer SelectedCustomer { get; set; }

    private void InitializeStyles()
    {
        _displayTextStyle = new GUIStyle("label")
        {
            fontSize = 18,
            normal =
            {
                background = Texture2D.whiteTexture
            },
            alignment = TextAnchor.MiddleCenter
        };
    }

    public static void UpdateCounterOfferDisplayText(string text, string reasonText)
    {
        _counterOfferDisplayText = text + "\n" + reasonText;
    }

    public void UpdateSalesInfoPanel(ProductEntry entry, float? overridePrice = null)
    {
        _salesInfoFormatter.CancelPendingCalculation();
        SelectedProduct = entry;
        _salesPanel.IsVisible = true;
        _salesPanel.ScrollPosition = Vector2.zero;
        _salesPanel.Content = _salesInfoFormatter.FormatSalesInfo(entry, overridePrice);
    }

    public void UpdateContactInfoPanel(Customer customer)
    {
        if (customer == null)
        {
            _contactPanel.IsVisible = false;
            return;
        }

        if (customer.customerData == null || customer.NPC == null || customer.NPC.RelationData == null)
        {
            Main.Log("Contact info panel skipped because customer data, NPC, or relation data is null.", false, true);
            _contactPanel.IsVisible = false;
            return;
        }

        var instance = TimeManager.Instance;
        if (instance == null)
        {
            Main.Log("Contact info panel skipped because TimeManager is unavailable.", false, true);
            _contactPanel.IsVisible = false;
            return;
        }

        var currentDay = instance.CurrentDay;
        SelectedCustomer = customer;
        _contactPanel.IsVisible = true;
        _contactPanel.ScrollPosition = Vector2.zero;
        _contactPanel.Content =
            CustomerInfoFormatter.FormatCustomerInfo(customer, customer.customerData, customer.NPC.RelationData,
                currentDay);
    }

    private void DrawSalesInfoPanel()
    {
        _panelRenderer.DrawPanel(_salesPanel, 420f, 800f);
    }

    private void DrawContactInfoPanel()
    {
        _panelRenderer.DrawPanel(_contactPanel, 440f, 700f);
    }

    public void OnGUI()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "Main") return;
        var instance = Phone.Instance;
        if (instance == null || !instance.IsOpen) return;
        if (!HomeScreen.Instance.isOpen && MessagesApp.Instance != null &&
            MessagesApp.Instance.CounterofferInterface.IsOpen)
        {
            if (Config.CounterOfferDarkModeEnabled.Value)
            {
                GUI.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
                _displayTextStyle.normal.textColor = Color.white;
            }
            else
            {
                _displayTextStyle.normal.textColor = Color.black;
            }

            GUI.Label(new Rect(Screen.width / 2 - 190, Screen.height / 2 - 250, 380f, 70f), _counterOfferDisplayText,
                _displayTextStyle);
            GUI.backgroundColor = Color.white;
        }

        if (ProductManagerApp != null && ProductManagerApp.isOpen && _salesPanel.IsVisible)
            DrawSalesInfoPanel();
        if (!(ContactsApp.Instance == null) && ContactsApp.Instance.isOpen &&
            _contactPanel.IsVisible) DrawContactInfoPanel();
    }
}