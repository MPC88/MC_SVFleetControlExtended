using HarmonyLib;
using UnityEngine;

namespace MC_SVFleetControlExtended.RemoveAll
{
    public class Patches
    {        
        internal static DockingUI dockingUI;
        internal static Inventory inventory;        

        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.OpenPanel))]
        [HarmonyPostfix]
        private static void DockingUI_OpenPanelPost(DockingUI __instance)
        {
            dockingUI = __instance;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.Open))]
        [HarmonyPostfix]
        private static void Inventory_OpenPost(Inventory __instance)
        {
            inventory = __instance;
            UI.EnableDisable();
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.SelectItem))]
        [HarmonyPostfix]
        private static void Inventory_SelectItemPost(Inventory __instance)
        {
            inventory = __instance;
            UI.EnableDisable();
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.LoadItems))]
        [HarmonyPostfix]
        private static void Inventory_LoadItemsPost(Inventory __instance)
        {
            inventory = __instance;
            UI.EnableDisable();
        }
    }
}