using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MC_SVFleetControlExtended
{
    internal class Util
    {
        internal enum AIBehaviourRole { dps, healer, miner }
        internal enum ActiveEquipmentType { energybarrier, cloak }
        private static Dictionary<ActiveEquipmentType, int> effectID = new Dictionary<ActiveEquipmentType, int>()
        {
            {ActiveEquipmentType.energybarrier, 54}, // Barrier duration
            {ActiveEquipmentType.cloak, 30} // Cloak cooldown
        };

        private static GameObject emergencyWarpGO;
        private static GameObject lastAddedFleetBehaviourUI = null;

        internal static void ResetInstanceBasedCummulatives()
        {
            lastAddedFleetBehaviourUI = null;
        }

        internal static int CountPlayerFleetMemebers()
        {
            int result = 0;
            foreach (AIMercenaryCharacter aimc in PChar.Char.mercenaries)
                if (aimc is PlayerFleetMember)
                    result++;
            return result;
        }

        internal static void AddFleetBehaviourControlUIElement(GameObject newUIElement, float refX, float height, float additionalYPadding, FleetBehaviorControl fleetBehaviourControl)
        {
            // Add element
            if(emergencyWarpGO == null)
                emergencyWarpGO = (GameObject)typeof(FleetBehaviorControl).GetField("emergencyWarpGO", AccessTools.all).GetValue(fleetBehaviourControl);

            Transform refTransform = null;
            if (lastAddedFleetBehaviourUI == null)
                refTransform = emergencyWarpGO.transform;
            else
                refTransform = lastAddedFleetBehaviourUI.transform;

            Vector3 refPosition = new Vector3(
                refX,
                refTransform.localPosition.y - (refTransform.gameObject.GetComponent<RectTransform>().rect.height / 2),
                refTransform.localPosition.z);

            float offset = (height / 2) + additionalYPadding;
            newUIElement.transform.SetParent(emergencyWarpGO.transform.parent, false);
            newUIElement.transform.localPosition = new Vector3(
                refPosition.x,
                refPosition.y - offset,
                refPosition.z);
            newUIElement.transform.localScale = emergencyWarpGO.transform.localScale;
            newUIElement.layer = emergencyWarpGO.layer;

            // Adjust panel size and component positions
            Transform bg = fleetBehaviourControl.transform.Find("BG");
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bgRect.rect.height + height + additionalYPadding);

            for (int i = 1; i < fleetBehaviourControl.transform.childCount; i++)
            {
                Transform child = fleetBehaviourControl.transform.GetChild(i);
                if(!child.transform.name.Equals("BtnClose"))
                    child.localPosition = new Vector3(
                        child.localPosition.x,
                        child.localPosition.y + ((height + additionalYPadding) / 2),
                        child.localPosition.z);
            }

            Transform btnClose = fleetBehaviourControl.transform.Find("BtnClose");
            btnClose.localPosition = new Vector3(
                btnClose.localPosition.x,
                btnClose.localPosition.y - ((height + additionalYPadding) / 2),
                btnClose.localPosition.z);

            lastAddedFleetBehaviourUI = newUIElement;
        }

        internal static bool HasActiveEquipment(AIMercenaryCharacter aiMechChar, ActiveEquipmentType activeEquipmentType)
        {
            if (aiMechChar == null)
            {
                Main.log.LogInfo("AIMC null");
                return false;
            }

            if (aiMechChar.shipData == null)
            {
                Main.log.LogInfo("AIMC.shipData null");
                return false;
            }

            foreach (InstalledEquipment installEquip in aiMechChar.shipData.equipments)
            {
                Equipment equipment = EquipmentDB.GetEquipment(installEquip.equipmentID);
                for (int effectIndex = 0; effectIndex < equipment.effects.Count; effectIndex++)
                    if (equipment.effects[effectIndex].type == effectID[activeEquipmentType])
                        return true;
            }

            foreach (BuiltInEquipmentData builtInEquip in aiMechChar.shipData.builtInData)
            {
                Equipment equipment = EquipmentDB.GetEquipment(builtInEquip.equipmentID);
                for (int effectIndex = 0; effectIndex < equipment.effects.Count; effectIndex++)
                    if (equipment.effects[effectIndex].type == effectID[activeEquipmentType])
                        return true;
            }

            return false;
        }
    }
}
