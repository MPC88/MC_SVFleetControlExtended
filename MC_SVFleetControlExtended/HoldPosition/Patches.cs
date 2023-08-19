using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MC_SVFleetControlExtended.HoldPosition
{
    internal class Patches
    {
        private static ConfigEntry<KeyCodeSubset> cfgHoldPos;
        private static ConfigEntry<KeyCodeSubset> cfgHoldPosModifier;

        internal static void Config(Main main)
        {
            // Dock, undock, drop cargo
            cfgHoldPos = main.Config.Bind("1. Hold position",
                "Hold position",
                KeyCodeSubset.X,
                "Hold position toggle command key.");
            cfgHoldPosModifier = main.Config.Bind("Keybinds",
                "Modifier key",
                KeyCodeSubset.LeftAlt,
                "Set to \"None\" to disable modifer key.");
        }

        internal static void Update()
        {
            if (GameManager.instance == null || !GameManager.instance.inGame ||
                PChar.Char.mercenaries.Count == 0)
                return;

            if ((cfgHoldPosModifier.Value == KeyCodeSubset.None || Input.GetKey((KeyCode)cfgHoldPosModifier.Value)) &&
                Input.GetKeyDown((KeyCode)cfgHoldPos.Value) && Main.data != null)
            {
                Main.data.holdPosition = !Main.data.holdPosition;
                if(Main.data.holdPosition)
                    SideInfo.AddMsg("Fleet holding position.");
                else
                    SideInfo.AddMsg("Fleet moving out.");
            }
        }
        
        [HarmonyPatch(typeof(AIControl), "TravelToDestination")]
        [HarmonyPrefix]
        private static void AIControlTravelToDestination_Pre(AIControl __instance, Transform ___tf, out Vector3 __state)
        {
            __state = new Vector3(__instance.destination.x,
                __instance.destination.y,
                __instance.destination.z);

            if ((__instance is AIMercenary) &&
                (__instance.isPlayerFleet) &&
                Main.data != null && Main.data.holdPosition)
                __instance.destination = ___tf.position;
        }

        [HarmonyPatch(typeof(AIControl), "TravelToDestination")]
        [HarmonyPostfix]
        private static void AIControlTravelToDestination_Post(AIControl __instance, Vector3 __state)
        {
            __instance.destination = __state;
        }
    }
}
