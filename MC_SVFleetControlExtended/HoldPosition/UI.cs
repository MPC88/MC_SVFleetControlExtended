using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MC_SVFleetControlExtended.HoldPosition
{
    internal class UI
    {
        private static GameObject holdPositionGO = null;
        private static Text keyText = null;

        internal static void ValidateHoldPositionGO(FleetBehaviorControl fleetBehaviourControl, GameObject launchKeyGO)
        {
            if (holdPositionGO == null)
                CreateHoldPositionGO(fleetBehaviourControl, launchKeyGO);
        }

        internal static void CreateHoldPositionGO(FleetBehaviorControl fleetBehaviourControl, GameObject launchKeyGO)
        {
            holdPositionGO = GameObject.Instantiate(launchKeyGO);
            holdPositionGO.SetActive(true);
            holdPositionGO.transform.Find("Text").gameObject.GetComponent<Text>().text = "Hold position toggle";
            keyText = holdPositionGO.GetComponent<Text>();
            holdPositionGO.SetActive(false);

            RectTransform goRect = holdPositionGO.GetComponent<RectTransform>();

            Util.AddFleetBehaviourControlUIElement(
                holdPositionGO,
                launchKeyGO.transform.localPosition.x,
                goRect.rect.height,
                0,
                fleetBehaviourControl);
        }
    }
}
