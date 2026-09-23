using System;
#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.Messaging;
using ScheduleOne.UI.Phone.Messages;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.UI.Phone.Messages;
#endif
using HighBaller.Automation;

namespace HighBaller.Core;

public static class DealAutoScheduler
{
    private static DealWindow ResolveCurrentWindowOption()
    {
        try
        {
            var instance = TimeManager.Instance;
            if (instance == null)
            {
                Main.Log("Window 5: TimeManager unavailable, defaulting to Late Night.", false, true);
                return DealWindow.LateNight;
            }

            var currentTime = instance.CurrentTime;
            var minSumFrom24HourTime = TimeManager.GetMinSumFrom24HourTime(currentTime);
            var cutoffMins = DealWindowSelector.WINDOW_CUTOFF_MINS;
            if (cutoffMins <= 0) cutoffMins = 60;
            for (var i = 0; i < 4; i++)
            {
                var window = (EDealWindow)i;
                var windowInfo = DealWindowInfo.GetWindowInfo(window);
                var minSumFrom24HourTime2 = TimeManager.GetMinSumFrom24HourTime(windowInfo.StartTime);
                var windowEnd = TimeManager.GetMinSumFrom24HourTime(windowInfo.EndTime);
                if (windowEnd <= minSumFrom24HourTime2) windowEnd += 1440;
                var currentMin = minSumFrom24HourTime;
                if (currentMin < minSumFrom24HourTime2) currentMin += 1440;
                if (currentMin < minSumFrom24HourTime2 || currentMin >= windowEnd) continue;
                if (windowEnd - currentMin > cutoffMins)
                {
                    Main.Log($"Window 5: current time {currentTime} -> window {window} (option {i + 1}).");
                    return (DealWindow)(i + 1);
                }

                var fallbackIndex = (i + 1) % 4;
                Main.Log(
                    $"Window 5: current window {window} too close to cutoff (remaining {windowEnd - currentMin} mins), falling back to {(EDealWindow)fallbackIndex} (option {fallbackIndex + 1}).");
                return (DealWindow)(fallbackIndex + 1);
            }

            Main.Log($"Window 5: current time {currentTime} did not match any window, defaulting to Morning.", false,
                true);
            return DealWindow.Morning;
        }
        catch (Exception ex)
        {
            Main.Log("Window 5 resolution error: " + ex.Message + " - defaulting to Late Night.", false, true);
            return DealWindow.LateNight;
        }
    }

    public static void TryAutoScheduleDeal(Customer customer)
    {
        if (!Config.AutoContractAcceptedEnabled.Value) return;
        if (customer != null && Config.GetCustomNpcMode(customer.NPC) >= S1APINPCHandling.NoAutomation)
        {
            object npcName;
            if (!(customer.customerData != null))
            {
                var resolvedNpc = customer.NPC;
                npcName = (resolvedNpc != null ? resolvedNpc.FullName : null) ?? "unknown";
            }
            else
            {
                npcName = customer.customerData.name;
            }

            Main.Log("Auto scheduling skipped for custom NPC: " + (string)npcName);
            return;
        }

        if (customer == null || customer.OfferedContractInfo == null)
        {
            Main.Log("Auto scheduling skipped because the customer or offered contract is null.", false, true);
            return;
        }

        Main.Log("Auto scheduling for " + (customer.customerData != null ? customer.customerData.name : "unknown"));
        var windowOption = Config.AutoContractAcceptedWindow.Value;
        if (customer.NPC == null || customer.NPC.MSGConversation == null)
        {
            Main.Log("Auto scheduling skipped because NPC or conversation is null.", false, true);
            return;
        }

        if (windowOption == DealWindow.Current) windowOption = ResolveCurrentWindowOption();
        EDealWindow window;
        string windowName;
        switch (windowOption)
        {
            case DealWindow.Morning:
                window = EDealWindow.Morning;
                windowName = "Morning";
                break;
            case DealWindow.Afternoon:
                window = EDealWindow.Afternoon;
                windowName = "Afternoon";
                break;
            case DealWindow.Night:
                window = EDealWindow.Night;
                windowName = "Night";
                break;
            case DealWindow.LateNight:
                window = EDealWindow.LateNight;
                windowName = "Late Night";
                break;
            default:
                window = EDealWindow.LateNight;
                windowName = "Late Night";
                Main.Log(
                    "AutoContractAcceptedWindow is not correctly set!\n" +
                    "Defaulting to Late Night \n" +
                    "Correct Values: DealWindow.Morning, DealWindow.Afternoon, " +
                    "DealWindow.Night, DealWindow.LateNight, DealWindow.Current",
                    false, true);
                break;
        }

        customer.NPC.MSGConversation.SendMessage(new Message(windowName, Message.ESenderType.Player, true));
        customer.NPC.MSGConversation.ClearResponses(true);
        new DelayedScheduler(customer, window).Start();
    }
}