using System;
using System.Collections;
using System.Reflection;
using Il2CppScheduleOne.NPCs;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;
namespace HighBaller;

public static class Config
{
	private static MelonPreferences_Category DealOptimizerCategory;

	public static MelonPreferences_Entry<float> MinSuccessProbability;

	public static MelonPreferences_Entry<bool> PriceOptimizationEnabled;

	public static MelonPreferences_Entry<bool> CounterOfferDarkModeEnabled;

	private static MelonPreferences_Category DealOptimizerCategory_Automation;

	public static MelonPreferences_Entry<bool> AutoCounterOfferEnabled;

	public static MelonPreferences_Entry<bool> AutoContractAcceptedEnabled;

	public static MelonPreferences_Entry<int> AutoContractAcceptedWindow;

	public static MelonPreferences_Entry<bool> IncreaseQuantityEnabled;

	public static MelonPreferences_Entry<bool> AutoHandoverEnabled;

	public static MelonPreferences_Entry<bool> AlwaysAcceptCounterOffersEnabled;

	private static MelonPreferences_Category DealOptimizerCategory_Features;

	public static MelonPreferences_Entry<bool> DelayAutoResponseDuringSave;

	public static MelonPreferences_Entry<bool> DebugModeEnabled;

	public static MelonPreferences_Entry<int> CustomNPCHandling;

	private static bool _s1apiResolved;

	private static Type _s1apiNpcType;

	private static FieldInfo _s1npcField;

	private static FieldInfo _isCustomField;

	private static FieldInfo _allField;

	public static void Initialize()
	{
		DealOptimizerCategory = MelonPreferences.CreateCategory("HighBaller_IL2CPP");
		MinSuccessProbability = DealOptimizerCategory.CreateEntry<float>("MinSuccessProbability", 0.98f, "Minimum Success Probability", "The minimum probability of success required for a deal (0.0 to 1.0)", false, false, (ValueValidator)null, (string)null);
		PriceOptimizationEnabled = DealOptimizerCategory.CreateEntry<bool>("PriceOptimizationEnabled", false, "Price Optimization Enabled", "Whether to enable price optimization", false, false, (ValueValidator)null, (string)null);
		CounterOfferDarkModeEnabled = DealOptimizerCategory.CreateEntry<bool>("CounterOfferDarkModeEnabled", true, "Counter Offer Dark Mode Enabled", "Whether to enable counter offer dark mode", false, false, (ValueValidator)null, (string)null);
		DealOptimizerCategory_Automation = MelonPreferences.CreateCategory("HighBaller_IL2CPP_Automation");
		AutoCounterOfferEnabled = DealOptimizerCategory_Automation.CreateEntry<bool>("AutoCounterOfferEnabled", false, "Enable Auto Counter Offer", "Whether to enable auto counter offer", false, false, (ValueValidator)null, (string)null);
		AutoContractAcceptedEnabled = DealOptimizerCategory_Automation.CreateEntry<bool>("AutoContractAcceptedEnabled", false, "Enable Auto Deal Scheduling", "Whether to enable auto deal scheduling", false, false, (ValueValidator)null, (string)null);
		AutoContractAcceptedWindow = DealOptimizerCategory_Automation.CreateEntry<int>("AutoContractAcceptedWindow", 4, "Auto Deal Time Window (1 = Morning, 2 = Afternoon, 3 = Night, 4 = Late Night, 5 = Current Window)", "The window to auto accept deal. 5 = use the current in-game time window; falls back to the next window once the current one is too close to its cutoff.", false, false, (ValueValidator)null, (string)null);
		AlwaysAcceptCounterOffersEnabled = DealOptimizerCategory_Automation.CreateEntry<bool>("AlwaysAcceptCounterOffersEnabled", false, "Always Accept Counter Offers", "Force every customer counter-offer evaluation to succeed (regardless of price/quantity).", false, false, (ValueValidator)null, (string)null);
		IncreaseQuantityEnabled = DealOptimizerCategory_Automation.CreateEntry<bool>("IncreaseQuantityEnabled", false, "Increase Quantity till max spend is reached", "If enabled, the quantity will be increased until the max spend is reached", false, false, (ValueValidator)null, (string)null);
		AutoHandoverEnabled = DealOptimizerCategory_Automation.CreateEntry<bool>("AutoHandoverEnabled", false, "Enable automatic handovers", "Whether to enable automatic handover", false, false, (ValueValidator)null, (string)null);
		DealOptimizerCategory_Features = MelonPreferences.CreateCategory("HighBaller_IL2CPP_Features");
		DelayAutoResponseDuringSave = DealOptimizerCategory_Features.CreateEntry<bool>("DelayAutoResponseDuringSave", true, "Don't send auto responses during save", "Whether to delay auto response during save", false, false, (ValueValidator)null, (string)null);
		DebugModeEnabled = DealOptimizerCategory_Features.CreateEntry<bool>("DebugModeEnabled", false, "Debug Mode Enabled", "Enables a lot of logging in the console if needed", false, false, (ValueValidator)null, (string)null);
		CustomNPCHandling = DealOptimizerCategory_Features.CreateEntry<int>("CustomNPCHandling", 0, "S1API Custom NPC Handling (0=Include, 1=No Automation, 2=Full Skip)", "0: Default mod. 1: Allow orders but skip auto counter-offer/scheduling/handover for S1API. 2: Skip all mod features for S1API.", false, false, (ValueValidator)null, (string)null);
		ClampConfigValues();
	}

	private static void ClampConfigValues()
	{
		if (MinSuccessProbability.Value < 0f)
		{
			MinSuccessProbability.Value = 0f;
		}
		else if (MinSuccessProbability.Value > 0.99f)
		{
			MinSuccessProbability.Value = 0.99f;
		}
		if (AutoContractAcceptedWindow.Value < 1)
		{
			AutoContractAcceptedWindow.Value = 1;
		}
		else if (AutoContractAcceptedWindow.Value > 5)
		{
			AutoContractAcceptedWindow.Value = 5;
		}
		if (CustomNPCHandling.Value < 0)
		{
			CustomNPCHandling.Value = 0;
		}
		else if (CustomNPCHandling.Value > 2)
		{
			CustomNPCHandling.Value = 2;
		}
	}

	public static int GetCustomNPCMode(NPC npc)
	{
		if (npc == null || CustomNPCHandling.Value == 0)
		{
			return 0;
		}
		int i;
		try
		{
			if (!_s1apiResolved)
			{
				_s1apiResolved = true;
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				for (i = 0; i < assemblies.Length; i++)
				{
					_s1apiNpcType = assemblies[i].GetType("S1API.Entities.NPC");
					if (_s1apiNpcType != null)
					{
						break;
					}
				}
				if (_s1apiNpcType != null)
				{
					_s1npcField = _s1apiNpcType.GetField("S1NPC", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					_isCustomField = _s1apiNpcType.GetField("IsCustomNPC", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					_allField = _s1apiNpcType.GetField("All", BindingFlags.Static | BindingFlags.Public);
					Main.Log($"S1API detected: NPC type={_s1apiNpcType.FullName}, S1NPC={_s1npcField != null}, IsCustomNPC={_isCustomField != null}, All={_allField != null}", error: false, warning: false, bypassConfig: true);
				}
			}
			if (_s1apiNpcType == null || _s1npcField == null || _isCustomField == null || _allField == null)
			{
				i = 0;
				goto IL_023d;
			}
			if (_allField.GetValue(null) is IEnumerable enumerable)
			{
				foreach (object entry in enumerable)
				{
					if (entry == null)
					{
						continue;
					}
					object s1npcValue = _s1npcField.GetValue(entry);
					NPC candidateNpc = (NPC)((s1npcValue is NPC) ? s1npcValue : null);
					if (candidateNpc == null || !(candidateNpc == npc))
					{
						continue;
					}
					object isCustomValue = _isCustomField.GetValue(entry);
					i = ((isCustomValue is bool && (bool)isCustomValue) ? CustomNPCHandling.Value : 0);
					goto IL_023d;
				}
			}
		}
		catch (Exception ex)
		{
			Main.Log("GetCustomNPCMode error: " + ex.Message, error: false, warning: true);
		}
		return 0;
		IL_023d:
		return i;
	}
}
