using HarmonyLib;
using System.Collections.Generic;

[HarmonyPatch]
public static class ClearPatch
{
    [HarmonyPatch(typeof(BuiltinFunctions), "Clear")]
    [HarmonyPostfix]
    private static void AfterClear(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId, double __result)
    {
        HighlightClass.highlights.Clear();
    }

}