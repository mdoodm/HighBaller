using System;
using System.Collections;
using System.Reflection;
using MelonLoader;
using MelonLoader.Preferences;
#if MONO
using ScheduleOne.NPCs;
#else
using Il2CppScheduleOne.NPCs;
#endif

namespace HighBaller;

public static class Config
{
    private static MelonPreferences_Category _dealOptimizerCategory;

    internal static MelonPreferences_Entry<float> MinSuccessProbability;

    internal static MelonPreferences_Entry<bool> PriceOptimizationEnabled;

    internal static MelonPreferences_Entry<bool> CounterOfferDarkModeEnabled;


    private static MelonPreferences_Category _dealOptimizerCategoryAutomation;

    internal static MelonPreferences_Entry<bool> AutoCounterOfferEnabled;

    internal static MelonPreferences_Entry<bool> AutoContractAcceptedEnabled;

    internal static MelonPreferences_Entry<DealWindow> AutoContractAcceptedWindow;

    internal static MelonPreferences_Entry<bool> IncreaseQuantityEnabled;

    internal static MelonPreferences_Entry<bool> AutoHandoverEnabled;

    internal static MelonPreferences_Entry<bool> AlwaysAcceptCounterOffersEnabled;


    private static MelonPreferences_Category _dealOptimizerCategoryFeatures;

    internal static MelonPreferences_Entry<bool> DelayAutoResponseDuringSave;

    internal static MelonPreferences_Entry<bool> DebugModeEnabled;

    private static MelonPreferences_Entry<S1APINPCHandling> _customNpcHandling;


    private static bool _s1APIResolved;

    private static Type _s1APINpcType;

    private static FieldInfo _s1NpcField;

    private static FieldInfo _isCustomField;

    private static FieldInfo _allField;

    public static void Initialize()
    {
        _dealOptimizerCategory = MelonPreferences.CreateCategory("HighBaller_IL2CPP", "High Baller");
        MinSuccessProbability = _dealOptimizerCategory.CreateEntry("MinSuccessProbability", 0.98f,
            "Minimum Success Probability", "The minimum probability of success required for a deal [0.00, 0.99]",
            validator: new ValueRange<float>(0f, 0.99f));
        PriceOptimizationEnabled = _dealOptimizerCategory.CreateEntry("PriceOptimizationEnabled", false,
            "Price Optimization Enabled", "Whether to enable price optimization");
        CounterOfferDarkModeEnabled = _dealOptimizerCategory.CreateEntry("CounterOfferDarkModeEnabled", true,
            "Counter Offer Dark Mode", "Whether to enable counter offer dark mode");

        _dealOptimizerCategoryAutomation =
            MelonPreferences.CreateCategory("HighBaller_IL2CPP_Automation", "High Baller Automation");
        AutoCounterOfferEnabled = _dealOptimizerCategoryAutomation.CreateEntry("AutoCounterOfferEnabled", false,
            "Enable Auto Counter Offer", "Whether to enable auto counter offer");
        AutoContractAcceptedEnabled = _dealOptimizerCategoryAutomation.CreateEntry("AutoContractAcceptedEnabled", false,
            "Enable Auto Deal Scheduling", "Whether to enable auto deal scheduling");
        AutoContractAcceptedWindow = _dealOptimizerCategoryAutomation.CreateEntry("AutoDealWindow",
            DealWindow.LateNight,
            "Auto Deal Time Window",
            "The window to auto accept deal. Current falls back to the next window once the current one is too close to its cutoff.",
            oldIdentifier: "AutoContractAcceptedWindow");
        AlwaysAcceptCounterOffersEnabled = _dealOptimizerCategoryAutomation.CreateEntry(
            "AlwaysAcceptCounterOffersEnabled", false, "Always Accept Counter Offers",
            "Force every customer counter-offer evaluation to succeed (regardless of price/quantity).");
        IncreaseQuantityEnabled = _dealOptimizerCategoryAutomation.CreateEntry("IncreaseQuantityEnabled", false,
            "Increase Quantity till max spend",
            "If enabled, the quantity will be increased until the max spend is reached");
        AutoHandoverEnabled = _dealOptimizerCategoryAutomation.CreateEntry("AutoHandoverEnabled", false,
            "Enable automatic handovers", "Whether to enable automatic handover");

        _dealOptimizerCategoryFeatures = MelonPreferences.CreateCategory("HighBaller_IL2CPP_Features");
        DelayAutoResponseDuringSave = _dealOptimizerCategoryFeatures.CreateEntry("DelayAutoResponseDuringSave", true,
            "Don't send auto responses during save", "Whether to delay auto response during save");
        DebugModeEnabled = _dealOptimizerCategoryFeatures.CreateEntry("DebugModeEnabled", false, "Debug Mode Enabled",
            "Enables a lot of logging in the console if needed");
        _customNpcHandling = _dealOptimizerCategoryFeatures.CreateEntry("S1APINPCHandling", S1APINPCHandling.Include,
            "S1API Custom NPC Handling",
            "NoAutomation allows orders but skips auto counter-offer/scheduling/handover. Exclude skips all mod features for custom NPCs.",
            oldIdentifier: "CustomNPCHandling");
    }

    public static S1APINPCHandling GetCustomNpcMode(NPC npc)
    {
        if (npc == null || _customNpcHandling.Value == S1APINPCHandling.Include) return S1APINPCHandling.Include;

        try
        {
            if (!TryResolveS1ApiMembers() || _allField.GetValue(null) is not IEnumerable allNpcs)
                return S1APINPCHandling.Include;

            foreach (var entry in allNpcs)
            {
                if (entry == null) continue;

                var s1NpcValue = _s1NpcField.GetValue(entry);
                if (s1NpcValue is not NPC candidateNpc || candidateNpc != npc) continue;

                var isCustomValue = _isCustomField.GetValue(entry);
                return isCustomValue is true ? _customNpcHandling.Value : S1APINPCHandling.Include;
            }
        }
        catch (Exception ex)
        {
            Main.Log("GetCustomNpcMode error: " + ex.Message, false, true);
        }

        return S1APINPCHandling.Include;
    }

    private static bool TryResolveS1ApiMembers()
    {
        if (_s1APIResolved)
            return _s1APINpcType != null && _s1NpcField != null && _isCustomField != null && _allField != null;

        _s1APIResolved = true;

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            _s1APINpcType = assembly.GetType("S1API.Entities.NPC");
            if (_s1APINpcType != null) break;
        }

        if (_s1APINpcType == null) return false;

        _s1NpcField = _s1APINpcType.GetField("S1NPC",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _isCustomField = _s1APINpcType.GetField("IsCustomNPC",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _allField = _s1APINpcType.GetField("All", BindingFlags.Static | BindingFlags.Public);

        Main.Log(
            $"S1API detected: NPC type={_s1APINpcType.FullName}, S1NPC={_s1NpcField != null}, IsCustomNPC={_isCustomField != null}, All={_allField != null}",
            false, false, true);

        return _s1NpcField != null && _isCustomField != null && _allField != null;
    }
}

public enum DealWindow
{
    Morning = 1,
    Afternoon,
    Night,
    LateNight,
    Current,
}

// ReSharper disable once InconsistentNaming
public enum S1APINPCHandling
{
    Include = 0,
    NoAutomation,
    Exclude,
}