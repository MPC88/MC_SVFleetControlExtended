using HarmonyLib;
using UnityEngine;

namespace MC_SVFleetControlExtended.DesiredDistance
{
    internal class Patches
    {
        [HarmonyPatch(typeof(AIControl), nameof(AIControl.InstallWeapons))]
        [HarmonyPostfix]
        private static void AIContInstallWeapons_Post(AIControl __instance, SpaceShip ___ss)
        {
            AIContSetDesiredDistance_Post(__instance, ___ss);
        }

        [HarmonyPatch(typeof(AIControl), "SetDesiredDistance")]
        [HarmonyPostfix]
        private static void AIContSetDesiredDistance_Post(AIControl __instance, SpaceShip ___ss)
        {
            if (__instance.Char.behavior.role != 0 || !(__instance.Char is PlayerFleetMember))
                return;

            if (Main.data.desiredDistances.TryGetValue((__instance.Char as PlayerFleetMember).crewMemberID, out int optionIndex))
            {
                float distance = Config.DESIRED_DISTANCE_OPTIONS[optionIndex];
                if (distance < 0)
                    return;

                if (__instance.target != null)
                {
                    Collider thisCol = ___ss.gameObject.GetComponent<Collider>();
                    Collider targetCol = __instance.target.gameObject.GetComponent<Collider>();
                    if (thisCol != null && targetCol != null)
                    {
                        Vector3 thisSize = thisCol.bounds.size;
                        Vector3 targetSize = targetCol.bounds.size;

                        distance += Mathf.Min((thisSize.x / 2) * 0.95f, (thisSize.z / 2) * 0.95f);
                        distance += Mathf.Min((targetSize.x / 2) * 0.95f, (targetSize.z / 2) * 0.95f);
                    }
                }

                __instance.desiredDistance = distance;
            }
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

            if (Main.data.desiredDistances.Count != Util.CountPlayerFleetMemebers())
            {
                for (int i = 0; i < PChar.Char.mercenaries.Count; i++)
                    if (PChar.Char.mercenaries[i] is PlayerFleetMember &&
                        !Main.data.desiredDistances.ContainsKey((PChar.Char.mercenaries[i] as PlayerFleetMember).crewMemberID))
                        Main.data.desiredDistances.Add((PChar.Char.mercenaries[i] as PlayerFleetMember).crewMemberID, Config.DEFAULT_DESIRED_DISTANCE_OPT);
            }

            if (___aiMercChar != null && ___aiMercChar is PlayerFleetMember)
            {
                int crewID = (___aiMercChar as PlayerFleetMember).crewMemberID;
                bool gotValue = Main.data.desiredDistances.TryGetValue(crewID, out int curDistanceOptIndex);

                if (!gotValue)
                {
                    Main.data.desiredDistances.Add(crewID, Config.DEFAULT_DESIRED_DISTANCE_OPT);
                    curDistanceOptIndex = Config.DEFAULT_DESIRED_DISTANCE_OPT;
                }


                UI.EnableDesiredDistanceDropdown(true, ___aiMercChar, curDistanceOptIndex);
            }
            else
            {
                UI.EnableDesiredDistanceDropdown(false, null, -1);
            }
        }

        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Close))]
        [HarmonyPostfix]
        private static void FBCClose_Post()
        {
            UI.EnableDesiredDistanceDropdown(false, null, -1);
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
                Main.data.desiredDistances != null &&
                Main.data.desiredDistances.ContainsKey((__instance.aiMercChar as PlayerFleetMember).crewMemberID))
                Main.data.desiredDistances.Remove((__instance.aiMercChar as PlayerFleetMember).crewMemberID);
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
                Main.data.desiredDistances != null &&
                Main.data.desiredDistances.ContainsKey((__instance.aiMercChar as PlayerFleetMember).crewMemberID))
                Main.data.desiredDistances.Remove((__instance.aiMercChar as PlayerFleetMember).crewMemberID);
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
                    !Main.data.desiredDistances.ContainsKey((aiChar as PlayerFleetMember).crewMemberID))
                    Main.data.desiredDistances.Add((aiChar as PlayerFleetMember).crewMemberID, Config.DEFAULT_DESIRED_DISTANCE_OPT);
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.CreatePlayerFleetMember))]
        [HarmonyPostfix]
        private static void GMCreatePlayerFleetMember_Post(CrewMember crewMember)
        {
            if (crewMember.aiChar is PlayerFleetMember &&
                Main.data != null &&
                !Main.data.desiredDistances.ContainsKey(crewMember.id))
                Main.data.desiredDistances.Add(crewMember.id, Config.DEFAULT_DESIRED_DISTANCE_OPT);
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
               Main.data.desiredDistances != null &&
               Main.data.desiredDistances.ContainsKey((aiChar as PlayerFleetMember).crewMemberID))
                Main.data.desiredDistances.Remove((aiChar as PlayerFleetMember).crewMemberID);
        }
    }
}
