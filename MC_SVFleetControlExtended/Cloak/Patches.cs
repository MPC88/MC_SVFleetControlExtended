using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MC_SVFleetControlExtended.Cloak
{
    internal class Patches
    {
        [HarmonyPatch(typeof(ActiveEquipment), nameof(ActiveEquipment.ShouldBeActivated))]
        [HarmonyPostfix]
        private static void AEShouldBeAct_Post(ActiveEquipment __instance, AIControl aiControl, ref bool __result)
        {
            if (aiControl.isPlayerFleet && __instance is AE_CloakingDevice &&
                Main.data.cloakWithPlayerStates.TryGetValue((aiControl.Char as PlayerFleetMember).crewMemberID, out bool enabled) &&
                enabled)
                __result = GameManager.instance.Player.GetComponent<SpaceShip>().IsCloaked;
        }

        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Open))]
        [HarmonyPrefix]
        private static void FBCOpen_Pre(FleetBehaviorControl __instance, Toggle ___collectLootToggle, GameObject ___emergencyWarpGO)
        {
            UI.ValidateCloakToggle(__instance, ___collectLootToggle, ___emergencyWarpGO);
        }

        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Open))]
        [HarmonyPostfix]
        private static void FBCOpen_Post(FleetBehaviorControl __instance, AIMercenaryCharacter ___aiMercChar)
        {
            if (Main.data == null)
                Main.data = new PersistentData();

            if (Main.data.cloakWithPlayerStates.Count != Util.CountPlayerFleetMemebers())
            {
                for (int i = 0; i < PChar.Char.mercenaries.Count; i++)
                    if (PChar.Char.mercenaries[i] is PlayerFleetMember &&
                        !Main.data.cloakWithPlayerStates.ContainsKey((PChar.Char.mercenaries[i] as PlayerFleetMember).crewMemberID))
                        Main.data.cloakWithPlayerStates.Add((PChar.Char.mercenaries[i] as PlayerFleetMember).crewMemberID, Config.DEFAULT_CLOAK_WITH_PLAYER_STATE);
            }

            if (___aiMercChar != null && ___aiMercChar is PlayerFleetMember && Util.HasActiveEquipment(___aiMercChar, typeof(AE_CloakingDevice)))
            {
                int crewID = (___aiMercChar as PlayerFleetMember).crewMemberID;
                bool gotValue = Main.data.cloakWithPlayerStates.TryGetValue(crewID, out bool curState);

                if (!gotValue)
                {
                    Main.data.cloakWithPlayerStates.Add(crewID, Config.DEFAULT_CLOAK_WITH_PLAYER_STATE);
                    curState = Config.DEFAULT_CLOAK_WITH_PLAYER_STATE;
                }

                UI.EnableCloakWithPlayerToggle(true, ___aiMercChar, curState);
            }
            else
            {
                UI.EnableCloakWithPlayerToggle(false, null, false);
            }
        }

        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Close))]
        [HarmonyPostfix]
        private static void FBCClose_Post()
        {
            UI.EnableCloakWithPlayerToggle(false, null, false);
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
                Main.data.cloakWithPlayerStates != null &&
                Main.data.cloakWithPlayerStates.ContainsKey((__instance.aiMercChar as PlayerFleetMember).crewMemberID))
                Main.data.cloakWithPlayerStates.Remove((__instance.aiMercChar as PlayerFleetMember).crewMemberID);
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
                Main.data.cloakWithPlayerStates != null &&
                Main.data.cloakWithPlayerStates.ContainsKey((__instance.aiMercChar as PlayerFleetMember).crewMemberID))
                Main.data.cloakWithPlayerStates.Remove((__instance.aiMercChar as PlayerFleetMember).crewMemberID);
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
                    !Main.data.cloakWithPlayerStates.ContainsKey((aiChar as PlayerFleetMember).crewMemberID))
                    Main.data.cloakWithPlayerStates.Add((aiChar as PlayerFleetMember).crewMemberID, Config.DEFAULT_CLOAK_WITH_PLAYER_STATE);
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.CreatePlayerFleetMember))]
        [HarmonyPostfix]
        private static void GMCreatePlayerFleetMember_Post(CrewMember crewMember)
        {
            if (crewMember.aiChar is PlayerFleetMember &&
                Main.data != null &&
                !Main.data.cloakWithPlayerStates.ContainsKey(crewMember.id))
                Main.data.cloakWithPlayerStates.Add(crewMember.id, Config.DEFAULT_CLOAK_WITH_PLAYER_STATE);
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
               Main.data.cloakWithPlayerStates != null &&
               Main.data.cloakWithPlayerStates.ContainsKey((aiChar as PlayerFleetMember).crewMemberID))
                Main.data.cloakWithPlayerStates.Remove((aiChar as PlayerFleetMember).crewMemberID);
        }
    }
}
