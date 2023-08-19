using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MC_SVFleetControlExtended.DesiredDistance
{
    internal class UI
    {
        private static GameObject desiredDistanceGO = null;
        private static Text desiredDistanceText = null;
        private static Dropdown desiredDistanceDropdown = null;

        internal static void ValidateUIElements(FleetBehaviorControl fleetBehaviourControl, GameObject emergencyWarpGO)
        {
            if (desiredDistanceGO == null)
                CreateUIElements(fleetBehaviourControl, emergencyWarpGO);
        }

        internal static void CreateUIElements(FleetBehaviorControl fleetBehaviourControl, GameObject emergencyWarpGO)
        {
            desiredDistanceGO = GameObject.Instantiate(emergencyWarpGO);
            desiredDistanceGO.SetActive(true);
            desiredDistanceText = desiredDistanceGO.transform.Find("Text").gameObject.GetComponent<Text>();
            desiredDistanceText.text = "Force engagement range (DPS only)";
            desiredDistanceDropdown = desiredDistanceGO.transform.Find("Dropdown").GetComponent<Dropdown>();
            desiredDistanceDropdown.ClearOptions();
            List<string> optionsList = new List<string>();
            foreach(int opt in Config.DESIRED_DISTANCE_OPTIONS)
            {
                if (opt == -1)
                    optionsList.Add("Off");
                else
                    optionsList.Add(opt.ToString());
            }
            desiredDistanceDropdown.AddOptions(optionsList);
            GameObject.Destroy(desiredDistanceGO.transform.Find("Note").gameObject);
            desiredDistanceGO.SetActive(false);

            RectTransform goRect = desiredDistanceGO.GetComponent<RectTransform>();
            goRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                desiredDistanceDropdown.GetComponent<RectTransform>().rect.height);

            Util.AddFleetBehaviourControlUIElement(
                desiredDistanceGO,
                emergencyWarpGO.transform.localPosition.x,
                goRect.rect.height,
                0,
                fleetBehaviourControl);
        }

        internal static void EnableUIElements(bool state, AIMercenaryCharacter aiChar, int value)
        {
            if (desiredDistanceGO == null)
                return;

            if (state && aiChar != null && value >= 0 && value < Config.DESIRED_DISTANCE_OPTIONS.Length)
            {
                Dropdown.DropdownEvent dde = new Dropdown.DropdownEvent();
                UnityAction<int> ua = null;
                ua += (int index) => DesiredDistanceChanged(aiChar as PlayerFleetMember);
                dde.AddListener(ua);
                desiredDistanceDropdown.onValueChanged = dde;
                desiredDistanceGO.SetActive(true);
                desiredDistanceDropdown.value = value;
            }
            else
            {
                desiredDistanceDropdown.onValueChanged = null;
                desiredDistanceGO.SetActive(false);
            }
        }

        private static void DesiredDistanceChanged(PlayerFleetMember aiChar)
        {
            if (Main.data.desiredDistances != null &&
                Main.data.desiredDistances.ContainsKey(aiChar.crewMemberID))
                Main.data.desiredDistances[aiChar.crewMemberID] = desiredDistanceDropdown.value;
        }
    }
}
