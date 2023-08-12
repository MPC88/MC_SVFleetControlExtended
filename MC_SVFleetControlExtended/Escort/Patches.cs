using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MC_SVFleetControlExtended.Escort
{
    internal class Patches
    {
        private static PlayerControl pc = null;

        [HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.DefineFollowPosition))]
        [HarmonyPostfix]
        private static void AIMercDefineFollowPos_Post(AIMercenary __instance, int mercNumber, ref GameObject ___followPosition)
        {
            Transform escortee = GetEscorteeTransform(__instance);
            if (escortee == null)
                return;

            if (!escortee.Find("FollowPositionParent"))
            {
                GameObject gameObject = new GameObject("FollowPositionParent");
                gameObject.transform.SetParent(escortee, false);
                gameObject.name = "FollowPositionParent";
                mercNumber = gameObject.transform.childCount;
                ___followPosition.transform.SetParent(gameObject.transform, false);
            }
            else
            {
                if (mercNumber < 0)
                {
                    mercNumber = escortee.Find("FollowPositionParent").childCount;
                }
                ___followPosition.transform.SetParent(escortee.Find("FollowPositionParent"), false);
            }

            ___followPosition.transform.localPosition =
                (Vector3)AccessTools.Method(typeof(AIMercenary), "GetMercXFollowPosition").Invoke(
                    __instance, 
                    new object[] { mercNumber, escortee.transform.localScale.x });
        }

        [HarmonyPatch(typeof(AIMercenary), "SearchForEnemies")]
        [HarmonyPostfix]
        private static void AIMercSearchForEnemies_Post(AIMercenary __instance, DockingControl ___targetDocking, Transform ___tf, SpaceShip ___ss)
        {
            Transform escortee = GetEscorteeTransform(__instance);
            if (__instance.guardTarget == null || ___targetDocking != null || 
                __instance.carrierDocking || __instance.returningToBoss ||
                escortee == null)
                return;

            Transform curTarget = __instance.target;
            Transform curTargetTarget = null;
            if (curTarget != null)
                curTargetTarget = curTarget.CompareTag("NPC") ? curTarget.GetComponent<AIControl>().target : curTarget.GetComponent<AIStationControl>().target;

            Transform newTarget = null;

            if (curTarget == null || curTargetTarget == __instance.guardTarget)
            {
                Collider[] array = Physics.OverlapSphere(___tf.position, 250f, 8704);                
                int i = 0;
                while(i < array.Length && newTarget == null)
                {
                    Transform transform = array[i].transform;
                    if (transform.CompareTag("Collider"))
                    {
                        transform = transform.GetComponent<ColliderControl>().ownerEntity.transform;
                    }
                    SpaceShip component = transform.GetComponent<SpaceShip>();
                    if (component && component != ___ss && ___ss.ffSys.TargetIsEnemy(component.ffSys) && !component.IsCloaked)
                    {
                        if (transform.CompareTag("NPC"))
                        {
                            AIControl component2 = transform.GetComponent<AIControl>();
                            if (component2 != null && ((component2.target == escortee && !component2.repairingTarget) || FactionDB.GetRel(__instance.Char.factionIndex, component2.Char.factionIndex) <= -1000 || (component2.target == ___tf && !component2.repairingTarget)))
                            {
                                newTarget = transform;
                                break;
                            }
                        }
                        if (transform.CompareTag("Station"))
                        {
                            AIStationControl component3 = transform.GetComponent<AIStationControl>();
                            if (component3 != null && component3.target == escortee)
                            {
                                newTarget = transform;
                                break;
                            }
                        }
                    }
                    i++;
                }

                if(newTarget != null)
                    __instance.SetNewTarget(newTarget, true);
            }
        }

        [HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.SearchForRepairTargets))]
        [HarmonyPostfix]
        private static void AIMercSearchForRepairTargets_Post(AIMercenary __instance, DockingControl ___targetDocking, Transform ___tf, SpaceShip ___ss)
        {
            Transform escortee = GetEscorteeTransform(__instance);
            if (__instance.guardTarget == null || ___targetDocking != null ||
                __instance.carrierDocking || __instance.returningToBoss ||
                escortee == null)
                return;
                        
            SpaceShip component = escortee.GetComponent<SpaceShip>();
            if(component && component.currHP < component.baseHP)
                __instance.SetRepairTarget(escortee);
        }

        private static void AIMercSetActions_Post(AIMercenary __instance, Transform ___tf)
        {
            Transform escortee = GetEscorteeTransform(__instance);
            if (escortee == null || __instance.guardTarget == null)
                return;

            int role = __instance.aiMercChar.behavior.role;

            float num = (float)(150 + GameData.data.fleetDistanceFromBoss * 4) + (___tf.localScale.x + escortee.localScale.x) * 3f;
            float num2 = Vector3.Distance(___tf.position, escortee.position);
            if (num2 > num && role != 2)
            {
                __instance.returningToBoss = true;
            }

            AIControl guardTarget = __instance.guardTarget.GetComponent<AIControl>();

            if (__instance.target == null || __instance.target == guardTarget.target)
            {
                AIControl component2 = escortee.GetComponent<AIControl>();
                if (role == 0 && component2 && component2.target != null && !component2.repairingTarget)
                {
                    __instance.SetNewTarget(component2.target, true);
                }
            }
        }

        [HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.SetGuardTarget))]
        [HarmonyPostfix]
        private static void AIMercSetGuardTarget_Post(AIMercenary __instance)
        {
            Transform escortee = GetEscorteeTransform(__instance);
            if (escortee == null || __instance.guardTarget == null)
                return;

            AIControl component = escortee.GetComponent<AIControl>();
            if (component && component.target != null)
            {
                __instance.SetNewTarget(component.target, true);
            }
        }

        private static void AIMercSetNewDestination_Post(AIMercenary __instance, DockingControl ___targetDocking, Transform ___tf, GameObject ___followPosition)
        {
            Transform escortee = GetEscorteeTransform(__instance);
            if (escortee == null || 
                (__instance.carrierDocking && __instance.guardTarget != null) ||
                ___targetDocking ||
                __instance.repairingTarget)
                return;

            if (__instance.guardTarget != null)
            {
                float num = (float)(150 + GameData.data.fleetDistanceFromBoss * 4) + (___tf.localScale.x + escortee.localScale.x) * 3f;
                if (Vector3.Distance(___tf.position, escortee.position) < num)
                {
                    __instance.destination = Vector3.zero;
                    return;
                }
                __instance.destination = ___followPosition.transform.position;
            }
        }

        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Open))]
        [HarmonyPrefix]
        private static void FBCOpen_Pre(FleetBehaviorControl __instance, GameObject ___emergencyWarpGO)
        {
            UI.ValidateEscortGO(__instance, ___emergencyWarpGO);
        }

        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Open))]
        [HarmonyPostfix]
        private static void FBCOpen_Post(AIMercenaryCharacter ___aiMercChar)
        {
            if (Main.data == null)
                Main.data = new PersistentData();

            if (___aiMercChar != null && ___aiMercChar is PlayerFleetMember)
            {
                int crewID = (___aiMercChar as PlayerFleetMember).crewMemberID;
                bool gotValue = Main.data.escorts.TryGetValue(crewID, out int curEscorteeID);

                if (!gotValue)
                    curEscorteeID = Config.PLAYER_ESCORT_ID;

                UI.EnableEscortDropDown(true, ___aiMercChar, curEscorteeID);
            }
            else
            {
                UI.EnableEscortDropDown(false, null, -1);
            }
        }

        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Close))]
        [HarmonyPostfix]
        private static void FBCClose_Post()
        {
            UI.EnableEscortDropDown(false, null, -1);
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
                Main.data.escorts != null)
            {
                int id = (__instance.aiMercChar as PlayerFleetMember).crewMemberID;
                if (Main.data.escorts.ContainsKey(id))
                {
                    Main.data.escorts.Remove(id);
                }

                List<int> alsoRemove = new List<int>();
                foreach (KeyValuePair<int, int> escortDef in Main.data.escorts)
                    if (escortDef.Value == id)
                        alsoRemove.Add(escortDef.Key);

                if (pc == null)
                    pc = GameManager.instance.Player.GetComponent<PlayerControl>();
                alsoRemove.ForEach(x =>
                {
                    Main.data.escorts.Remove(x);
                    foreach (Transform aimTrans in pc.mercenaries)
                    {
                        AIMercenary aim = aimTrans.gameObject.GetComponent<AIMercenary>();
                        if (aim != null && aim.isPlayerFleet &&
                        (aim.aiMercChar as PlayerFleetMember).crewMemberID == x)
                        {
                            aim.DefineFollowPosition(-1, -1);
                        }
                    }
                });
            }
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
                Main.data.escorts != null)
            {
                int id = (__instance.aiMercChar as PlayerFleetMember).crewMemberID;
                if (Main.data.escorts.ContainsKey(id))
                {
                    Main.data.escorts.Remove(id);
                }

                List<int> alsoRemove = new List<int>();
                foreach (KeyValuePair<int, int> escortDef in Main.data.escorts)
                    if (escortDef.Value == id)
                        alsoRemove.Add(escortDef.Key);

                if (pc == null)
                    pc = GameManager.instance.Player.GetComponent<PlayerControl>();
                alsoRemove.ForEach(x =>
                {
                    Main.data.escorts.Remove(x);
                    foreach(Transform aimTrans in pc.mercenaries)
                    {
                        AIMercenary aim = aimTrans.gameObject.GetComponent<AIMercenary>();                        
                        if(aim != null && aim.isPlayerFleet &&
                        (aim.aiMercChar as PlayerFleetMember).crewMemberID == x)
                        {
                            aim.DefineFollowPosition(-1, -1);
                        }
                    }
                });
            }
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
                Main.data.escorts != null)
            {
                int id = (aiChar as PlayerFleetMember).crewMemberID;
                if (Main.data.escorts.ContainsKey(id))
                {
                    Main.data.escorts.Remove(id);
                }

                List<int> alsoRemove = new List<int>();
                foreach (KeyValuePair<int, int> escortDef in Main.data.escorts)
                    if (escortDef.Value == id)
                        alsoRemove.Add(escortDef.Key);

                if (pc == null)
                    pc = GameManager.instance.Player.GetComponent<PlayerControl>();
                alsoRemove.ForEach(x =>
                {
                    Main.data.escorts.Remove(x);
                    foreach (Transform aimTrans in pc.mercenaries)
                    {
                        AIMercenary aim = aimTrans.gameObject.GetComponent<AIMercenary>();
                        if (aim != null && aim.isPlayerFleet &&
                        (aim.aiMercChar as PlayerFleetMember).crewMemberID == x)
                        {
                            aim.DefineFollowPosition(-1, -1);
                        }
                    }
                });
            }
        }

        private static Transform GetEscorteeTransform(AIMercenary aim)
        {
            if (!aim.isPlayerFleet ||
                Main.data.escorts == null || Main.data.escorts.Count == 0)
                return null;

            bool found = Main.data.escorts.TryGetValue((aim.Char as PlayerFleetMember).crewMemberID, out int escorteeID);

            if (!found)
                return null;

            if (pc == null)
                pc = GameManager.instance.Player.GetComponent<PlayerControl>();

            if (pc.mercenaries == null || pc.mercenaries.Count == 0)
                return null;

            foreach(Transform aimTrans in pc.mercenaries)
            {
                AIMercenary aimc = aimTrans.GetComponent<AIMercenary>();
                if(aimc != null && 
                    aimc.isPlayerFleet &&
                    (aimc.Char as PlayerFleetMember).crewMemberID == escorteeID)
                    return aimTrans;                    
            }

            return null;
        }
    }
}
