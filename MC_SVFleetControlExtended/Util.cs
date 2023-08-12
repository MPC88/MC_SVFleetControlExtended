using HarmonyLib;
using System;
using UnityEngine;

namespace MC_SVFleetControlExtended
{
    internal class Util
    {
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

        internal static bool HasActiveEquipment(AIMercenaryCharacter aiMechChar, Type activeEquipmentType)
        {
            return true;
            PlayerControl pc = GameManager.instance.Player.GetComponent<PlayerControl>();
            if (pc == null || pc.mercenaries.Count == 0)
                return false;

            foreach(Transform mercTrans in pc.mercenaries)
            {
                AICharacter aic = mercTrans.GetComponent<AICharacter>();
                if (aic == null || !(aic is AIMercenaryCharacter))
                    return false;

                AIMercenaryCharacter aimc = aic as AIMercenaryCharacter;
                if(aimc == aiMechChar)
                {
                    SpaceShip ss = mercTrans.GetComponent<SpaceShip>();
                    if (ss == null)
                        return false;

                    if (ss.activeEquips.Count == 0)
                        return false;

                    foreach(ActiveEquipment activeEquipment in ss.activeEquips)
                        if (activeEquipment.GetType() == activeEquipmentType)
                            return true;
                }
            }

            return false;
        }
    }
}
