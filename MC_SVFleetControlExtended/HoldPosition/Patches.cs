using HarmonyLib;

namespace MC_SVFleetControlExtended.HoldPosition
{
    internal class Patches
    {
        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Open))]
        [HarmonyPostfix]
        private static void FBCOpen_Post(FleetBehaviorControl __instance, AIMercenaryCharacter ___aiMercChar)
        {
            
        }
    }
}
