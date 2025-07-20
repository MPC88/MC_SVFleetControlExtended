using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MC_SVFleetControlExtended.Escort
{
    internal class Patches
    {
        private static PlayerControl pc = null;
        private static Dictionary<Transform, int> followPositionTracking = new Dictionary<Transform, int>();

        internal static void PostLoadInitialise()
        {
            if (pc == null)
                GameManager.instance.Player.GetComponent<PlayerControl>();

            if (pc == null)
                return;

            followPositionTracking.Clear();
            foreach (Transform merc in pc.mercenaries)
            {
                AIMercenary aim = merc.GetComponent<AIMercenary>();
                if(aim != null)
                    aim.DefineFollowPosition(GameData.data.fleetDistanceFromBoss, -1);
            }
        }

        [HarmonyPatch(typeof(AIMercenary), nameof(AIMercenary.DefineFollowPosition))]
        [HarmonyPostfix]
        private static void AIMercDefineFollowPos_Post(AIMercenary __instance, GameObject ___followPosition)
        {
            Transform escortee = GetEscorteeTransform(__instance);
            if (escortee == null)
            {
                if (__instance.isPlayerFleet)
                    escortee = GameManager.instance.Player.transform;
                else
                    return;
            }

            Transform followPosParent = escortee.Find("FollowPositionParent");
            if (followPosParent == null)
            {                
                GameObject gameObject = new GameObject("FollowPositionParent");
                gameObject.transform.SetParent(escortee, false);
                gameObject.name = "FollowPositionParent";
                followPosParent = gameObject.transform;
            }

            if (followPositionTracking.TryGetValue(escortee, out int mercNumber))
            {
                mercNumber++;
                followPositionTracking[escortee] = mercNumber;
            }
            else
            {
                followPositionTracking.Add(escortee, followPosParent.childCount);
            }

            ___followPosition.transform.SetParent(followPosParent.transform, false);

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
                !(__instance.Char is PlayerFleetMember))
                return;

            Main.data.dedicatedDefenderStates.TryGetValue((__instance.Char as PlayerFleetMember).crewMemberID, out bool isDD);

            // Small objects for dedicated defenders
            if (pc == null)
                pc = GameManager.instance.Player.GetComponent<PlayerControl>();

            if (isDD && pc != null)
            {       
                List<ScanObject> objs = pc.objectsScan.smallObjects;
                int objectIndex = -1;
                float distanceToCurObject = 1000f;
                for (int i = 0; i < objs.Count; i++)
                {
                    if (objs[i] != null && objs[i].trans != null && objs[i].trans.gameObject.activeSelf)
                    {
                        float distanceToNextObject = Vector3.Distance(___tf.position, objs[i].trans.position);
                        float additionalDesiredDistance = objs[i].trans.localScale.x * 2f;
                        if (distanceToNextObject > __instance.desiredDistance + additionalDesiredDistance && distanceToNextObject < distanceToCurObject)
                        {
                            objectIndex = i;
                            distanceToCurObject = distanceToNextObject;
                        }
                    }
                }
                if (objectIndex > 0)
                {
                    __instance.target = objs[objectIndex].trans;
                    __instance.targetEntity = objs[objectIndex].trans.GetComponent<Entity>();
                    return;
                }
            }

            // Either not dedicated defender or no small objects
            if (escortee == null)
                escortee = __instance.guardTarget;

            Transform curTarget = __instance.target;
            Transform curTargetTarget = null;
            if (curTarget != null)
                curTargetTarget = curTarget.CompareTag("NPC") ? curTarget.GetComponent<AIControl>().target : curTarget.GetComponent<AIStationControl>().target;

            Transform newTarget = null;

            if (curTarget == null || (curTargetTarget == __instance.guardTarget && escortee != __instance.guardTarget))
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

                if (newTarget != null)
                {
                    __instance.SetNewTarget(newTarget, true);
                }
                else if (isDD && 
                    ((curTargetTarget != __instance.guardTarget && escortee == __instance.guardTarget) || 
                    escortee != __instance.guardTarget))
                {
                    // No one attacking escortee and no small objects
                    __instance.target = null;
                    __instance.targetEntity = null;
                }
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
                __instance.SetRepairTarget(escortee.GetComponent<Entity>());
        }

        [HarmonyPatch(typeof(AIMercenary), "SetActions")]
        [HarmonyPostfix]
        private static void AIMercSetActions_Post(AIMercenary __instance, Transform ___tf)
        {            
            if (!(__instance.Char is PlayerFleetMember))
                return;

            // Set actions is a 3 second update method.  Use that to ensure dedicated defenders are prioritising
            // missiles and drones by invoking search for enemies if their current target is a space ship.
            if (Main.data.dedicatedDefenderStates.TryGetValue((__instance.Char as PlayerFleetMember).crewMemberID, out bool dedicatedDefender) &&
                dedicatedDefender && __instance.target != null && __instance.target.GetComponent<SpaceShip>() != null)
            {
                // Check for inactive targets
                if(!__instance.target.gameObject.activeSelf)
                    __instance.ForgetTarget(false);
                
                // Search for new targets
                typeof(AIMercenary).GetMethod("SearchForEnemies", AccessTools.all).Invoke(__instance, null);
            }

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

            if (__instance.target == null || __instance.target == __instance.guardTarget.GetComponent<PlayerControl>())
            {            
                AIControl component2 = escortee.GetComponent<AIControl>();             
                if (role == 0 && component2 != null && component2.target != null && !component2.repairingTarget)
                    __instance.SetNewTarget(component2.target, true);
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

        [HarmonyPatch(typeof(AIMercenary), "SetNewDestination")]
        [HarmonyPrefix]
        private static bool AIMercSetNewDestination_Pre(AIMercenary __instance, DockingControl ___targetDocking)
        {
            Transform escortee = GetEscorteeTransform(__instance);

            if ((__instance.carrierDocking && __instance.guardTarget != null) ||
                ___targetDocking)
                return true;

            if (__instance.Char != null && (__instance.Char is PlayerFleetMember) &&
                Main.data.escorts.ContainsKey((__instance.Char as PlayerFleetMember).crewMemberID) &&
                escortee == null)
                return false;

            return true;
        }

        [HarmonyPatch(typeof(AIMercenary), "SetNewDestination")]
        [HarmonyPostfix]
        private static void AIMercSetNewDestination_Post(AIMercenary __instance, DockingControl ___targetDocking, Transform ___tf, GameObject ___followPosition)
        {
            Transform escortee = GetEscorteeTransform(__instance);
            if (!__instance.isPlayerFleet ||
                (__instance.carrierDocking && __instance.guardTarget != null) ||
                ___targetDocking ||
                __instance.repairingTarget)
                return;

            if (__instance.guardTarget != null)
            {
                if (escortee == null)
                    escortee = GameManager.instance.Player.transform;

                float num = (float)(GameData.data.fleetDistanceFromBoss + 20) + ((___tf.localScale.x + escortee.localScale.x) * 3f);
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
        private static void FBCOpen_Pre(FleetBehaviorControl __instance, GameObject ___emergencyWarpGO, Toggle ___collectLootToggle)
        {
            if (___emergencyWarpGO == null || ___collectLootToggle == null)
            {
                AccessTools.Method(typeof(FleetBehaviorControl), "Validate").Invoke(__instance, null);
                ___collectLootToggle = AccessTools.Field(typeof(FleetBehaviorControl), "collectLootToggle").GetValue(__instance) as Toggle;
                ___emergencyWarpGO = AccessTools.Field(typeof(FleetBehaviorControl), "emergencyWarpGO").GetValue(__instance) as GameObject;
            }
            UI.ValidateUIElements(__instance, ___emergencyWarpGO, ___collectLootToggle);
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

                gotValue = Main.data.dedicatedDefenderStates.TryGetValue(crewID, out bool curDDState);
                if (!gotValue)
                {
                    Main.data.dedicatedDefenderStates.Add(crewID, Config.DEFAULT_DEDICATED_DEFENDER_STATE);
                    curDDState = Config.DEFAULT_DEDICATED_DEFENDER_STATE;
                }

                UI.EnableUIElements(true, ___aiMercChar, curEscorteeID, curDDState);
            }
            else
            {
                UI.EnableUIElements(false, null, -1, false);
            }
        }

        [HarmonyPatch(typeof(FleetBehaviorControl), nameof(FleetBehaviorControl.Close))]
        [HarmonyPostfix]
        private static void FBCClose_Post()
        {
            UI.EnableUIElements(false, null, -1, false);
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
            if (aiChar is PlayerFleetMember && Main.data.escorts != null)
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
