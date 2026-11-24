using System;
using System.Globalization;
using MelonLoader;
using UnityEngine.SceneManagement;
using HighBaller.Core;
using HighBaller.UI;
using Object = UnityEngine.Object;
#if MONO
using ScheduleOne.Product;
using ScheduleOne.UI.Phone.Messages;
#else
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.UI.Phone.Messages;
#endif

[assembly: MelonInfo(typeof(HighBaller.Main), "HighBaller", "2.7.12", "xyrilyn, zocke18r, SFox & Virtunerd")]
[assembly: MelonAdditionalCredits("Original Deal Optimizer by xyrilyn; 0.4.7f6 compat by k0Mods; 0.4.7f12 update")]
[assembly: MelonGame("TVGS", "Schedule I")]

#if MONO
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#endif

namespace HighBaller;

public class Main : MelonMod
{
    internal static bool SuppressNextPriceEval;

    private string _lastCounterofferPriceText;

    private bool _listening;

    internal UIHandler UIHandler;

    public static void Log(string message, bool error = false, bool warning = false, bool bypassConfig = false)
    {
        if (!Config.DebugModeEnabled.Value && !bypassConfig) return;
        if (error)
            Melon<Main>.Logger.Error(message);
        else if (warning)
            Melon<Main>.Logger.Warning(message);
        else
            Melon<Main>.Logger.Msg(message);
    }

    public override void OnInitializeMelon()
    {
        UIHandler = new UIHandler();
        Config.Initialize();
        Log("Initialized HighBaller Fixed v" + Info.Version, false, false, true);
        Log(
            "DebugMode is " + (Config.DebugModeEnabled.Value ? "enabled" : "disabled") +
            ", logs will show if it is enabled.", false, false, true);
    }

    public override void OnGUI()
    {
        UIHandler.OnGUI();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main") return;
        try
        {
            var foundManagers = Object.FindObjectsOfType<ProductManager>();
            if (foundManagers is { Length: > 0 })
            {
                var productManager = foundManagers[0];
                Log($"ProductManager: {productManager}");
            }
            else
            {
                Log("No ProductManager found in scene.", false, true);
            }
        }
        catch (Exception ex)
        {
            Log("Error finding ProductManager: " + ex.Message, false, false, true);
        }
    }

    public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
    {
        if (_listening) _listening = false;
        _lastCounterofferPriceText = null;
    }

    public override void OnUpdate()
    {
        var activeScene = SceneManager.GetActiveScene();
        var isMainScene = activeScene.name == "Main";
        var instance = MessagesApp.Instance;
        if (isMainScene && !_listening && instance != null) Subscribe();
        if (isMainScene && instance != null && instance.CounterofferInterface != null &&
            instance.CounterofferInterface.PriceSelector != null && instance.CounterofferInterface.IsOpen)
        {
            var currentPriceText =
                instance.CounterofferInterface.PriceSelector.SelectedAmount.ToString(CultureInfo.InvariantCulture);
            if (string.Equals(_lastCounterofferPriceText, currentPriceText, StringComparison.Ordinal)) return;
            _lastCounterofferPriceText = currentPriceText;
            OfferEvaluator.OfferPriceChangeCheckWithLog();
        }
        else
        {
            _lastCounterofferPriceText = null;
        }
    }

    private void Subscribe()
    {
        var instance = MessagesApp.Instance;
        if (instance == null || instance.CounterofferInterface == null ||
            instance.CounterofferInterface.PriceSelector == null)
        {
            Log("Listener attachment skipped because the counter-offer UI is not ready.", false, true);
            return;
        }

        Log("Counter-offer polling initialized");
        _listening = true;
    }
}
