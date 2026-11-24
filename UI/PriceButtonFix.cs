using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if MONO
using ScheduleOne.UI.Phone.ProductManagerApp;
#else
using Il2CppScheduleOne.UI.Phone.ProductManagerApp;
#endif

namespace HighBaller.UI;

// The Products app's asking-price +/- buttons (AskingPrice/Container/Add|Remove) have no click
// listeners in 0.4.7f12: the price is wired through InputValueRamp (gamepad/hold input) only, so
// mouse clicks land on the buttons and do nothing. Hook them up to the game's own AdjustPrice.
internal static class PriceButtonFix
{
    private static readonly HashSet<int> Wired = [];

    internal static void Apply(ProductManagerApp app)
    {
        if (app == null) return;
        var panel = app.GetComponentInChildren<ProductAppDetailPanel>(true);
        if (panel == null) return;

        foreach (var button in panel.GetComponentsInChildren<Button>(true))
        {
            var parent = button.transform.parent;
            if (parent == null || parent.parent == null || parent.parent.name != "AskingPrice") continue;

            float change;
            if (button.name == "Add") change = 1f;
            else if (button.name == "Remove") change = -1f;
            else continue;

            if (!Wired.Add(button.GetInstanceID())) continue;
            var target = panel;
            var delta = change;
#if MONO
            button.onClick.AddListener(() => Click(target, delta));
#else
            button.onClick.AddListener((UnityEngine.Events.UnityAction)(System.Action)(() => Click(target, delta)));
#endif
            Main.Log($"Wired Products asking-price '{button.name}' button ({delta:+0;-0})");
        }
    }

    private static void Click(ProductAppDetailPanel panel, float change)
    {
        if (panel == null || panel.ActiveProduct == null) return;
        panel.AdjustPrice(change);
    }
}
