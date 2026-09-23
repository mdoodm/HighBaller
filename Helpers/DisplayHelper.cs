namespace HighBaller.Helpers;

internal static class DisplayHelper
{
    public static void UpdateCounterOfferDisplayText(string text, string reasonText)
    {
        UI.UIHandler.UpdateCounterOfferDisplayText(text, reasonText);
    }
}