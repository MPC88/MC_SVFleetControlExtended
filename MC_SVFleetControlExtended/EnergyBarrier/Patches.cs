using HarmonyLib;
using UnityEngine;

namespace MC_SVFleetControlExtended.EnergyBarrier
{
    internal class Patches
    {
        [HarmonyPatch(typeof(AE_EnergyBarrier), nameof(AE_EnergyBarrier.ShouldBeActivated))]
        [HarmonyPostfix]
        private static void AE_EBShouldBeActive_Post(AE_EnergyBarrier __instance, AIControl aiControl, ref bool __result)
        {
            if (aiControl.Char is PlayerFleetMember && Main.data != null &&
                Main.data.energyBarrierThresholds.TryGetValue((aiControl.Char as PlayerFleetMember).crewMemberID, out int threshold))
                __result = __instance.ss.currHP < __instance.ss.baseHP * ((float)threshold / 10) && __instance.ss.fluxChargeSys.charges > 0 && __instance.cooldownRemaining <= 0f;
        }

        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Open))]
        [HarmonyPrefix]
        private static void FBCOpen_Pre(FleetBehaviorControl __instance, GameObject ___emergencyWarpGO)
        {
            UI.ValidateUIElements(__instance, ___emergencyWarpGO);
        }

        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Open))]
        [HarmonyPostfix]
        private static void FBCOpen_Post(FleetBehaviorControl __instance, AIMercenaryCharacter ___aiMercChar)
        {
            if (Main.data == null)
                Main.data = new PersistentData();

            if (Main.data.energyBarrierThresholds.Count != Util.CountPlayerFleetMemebers())
            {
                for (int i = 0; i < PChar.Char.mercenaries.Count; i++)
                    if (PChar.Char.mercenaries[i] is PlayerFleetMember &&
                        !Main.data.energyBarrierThresholds.ContainsKey((PChar.Char.mercenaries[i] as PlayerFleetMember).crewMemberID))
                        Main.data.energyBarrierThresholds.Add((PChar.Char.mercenaries[i] as PlayerFleetMember).crewMemberID, Config.DEFAULT_ENERGY_BARRIER_THRESHOLD);
            }

            if (___aiMercChar != null && ___aiMercChar is PlayerFleetMember && Util.HasActiveEquipment(___aiMercChar, typeof(AE_EnergyBarrier)))
            {
                int crewID = (___aiMercChar as PlayerFleetMember).crewMemberID;
                bool gotValue = Main.data.energyBarrierThresholds.TryGetValue(crewID, out int curThreshold);

                if (!gotValue)
                {
                    Main.data.energyBarrierThresholds.Add(crewID, Config.DEFAULT_ENERGY_BARRIER_THRESHOLD);
                    curThreshold = Config.DEFAULT_ENERGY_BARRIER_THRESHOLD;
                }
                                

                UI.EnableEnergyBarrierDropDown(true, ___aiMercChar, curThreshold);
            }
            else
            {
                UI.EnableEnergyBarrierDropDown(false, null, -1);
            }
        }

        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Close))]
        [HarmonyPostfix]
        private static void FBCClose_Post()
        {
            UI.EnableEnergyBarrierDropDown(false, null, -1);
        }

        [HarmonyPatch(typeof(AIMercenary), "SetActions")]
        [HarmonyPrefix]
        private static void AIMercSetActions_Pre(out int __state)
        {
            __state = PChar.Char.mercenaries.Count;
        }

        [HarmonyPatch(typeof(AIMercenary), "SetActions")]
        [HarmonyPostfix]
        private static void AIMercSetActions_Post(AIMercenary __instance, int __state)
        {
            if (PChar.Char.mercenaries.Count < __state &&
                __instance.aiMercChar is PlayerFleetMember &&
                Main.data.energyBarrierThresholds != null &&
                Main.data.energyBarrierThresholds.ContainsKey((__instance.aiMercChar as PlayerFleetMember).crewMemberID))
                Main.data.energyBarrierThresholds.Remove((__instance.aiMercChar as PlayerFleetMember).crewMemberID);
        }

        [HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.Die))]
        [HarmonyPrefix]
        private static void AIMercDie_Pre(out int __state)
        {
            __state = PChar.Char.mercenaries.Count;
        }

        [HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.Die))]
        [HarmonyPostfix]
        private static void AIMercDie_Post(AIMercenary __instance, int __state)
        {
            if (PChar.Char.mercenaries.Count < __state &&
                __instance.aiMercChar is PlayerFleetMember &&
                Main.data.energyBarrierThresholds != null &&
                Main.data.energyBarrierThresholds.ContainsKey((__instance.aiMercChar as PlayerFleetMember).crewMemberID))
                Main.data.energyBarrierThresholds.Remove((__instance.aiMercChar as PlayerFleetMember).crewMemberID);
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.GenerateGameEvent))]
        [HarmonyPostfix]
        private static void GMGenerateGameEvent_Post(GameEvent ge)
        {
            if (ge.type == GameEventType.SpawnAlly && ge.par1 >= 0)
            {
                AICharacter aiChar = PChar.Char.mercenaries[PChar.Char.mercenaries.Count - 1];
                if (aiChar is PlayerFleetMember &&
                    Main.data != null &&
                    !Main.data.energyBarrierThresholds.ContainsKey((aiChar as PlayerFleetMember).crewMemberID))
                    Main.data.energyBarrierThresholds.Add((aiChar as PlayerFleetMember).crewMemberID, Config.DEFAULT_ENERGY_BARRIER_THRESHOLD);
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.CreatePlayerFleetMember))]
        [HarmonyPostfix]
        private static void GMCreatePlayerFleetMember_Post(CrewMember crewMember)
        {
            if (crewMember.aiChar is PlayerFleetMember &&
                Main.data != null &&
                !Main.data.energyBarrierThresholds.ContainsKey(crewMember.id))
                Main.data.energyBarrierThresholds.Add(crewMember.id, Config.DEFAULT_ENERGY_BARRIER_THRESHOLD);
        }

        [HarmonyPatch(typeof(Inventory), "RemoveFromFleet")]
        [HarmonyPrefix]
        private static void InvRemoveFromFleet_Pre(Inventory __instance, int ___selectedItem)
        {
            if (__instance == null || __instance.currStation == null ||
                !(PChar.Char.mercenaries[___selectedItem] is PlayerFleetMember))
                return;

            AICharacter aiChar = PChar.Char.mercenaries[___selectedItem];
            if (aiChar is PlayerFleetMember &&
               Main.data.energyBarrierThresholds != null &&
               Main.data.energyBarrierThresholds.ContainsKey((aiChar as PlayerFleetMember).crewMemberID))
                Main.data.energyBarrierThresholds.Remove((aiChar as PlayerFleetMember).crewMemberID);
        }
    }
}
