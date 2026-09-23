using HarmonyLib;
#if MONO
using ScheduleOne.Levelling;
#else
using Il2CppScheduleOne.Levelling;
#endif

namespace HighBaller.Patches.Leveling;

[HarmonyPatch(typeof(LevelManager), "AddXP")]
public static class LevelManagerLoggingPatch
{
    public static void Postfix(int xp)
    {
        Main.Log($"LevelManager: AddXP: {xp}");
    }
}
