using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MC_SVFleetControlExtended.Cloak
{
    internal class UI
    {
        private static GameObject cloakToggleGO = null;
        private static Toggle cloakToggle = null;
        private static Text cloakToggleText = null;

        internal static void ValidateUIElements(FleetBehaviorControl fleetBehaviourControl, Toggle collectLootToggle, GameObject emergencyWarpGO)
        {
            if (cloakToggleGO == null)
                CreateUIElements(fleetBehaviourControl, collectLootToggle, emergencyWarpGO);
        }

        internal static void CreateUIElements(FleetBehaviorControl fleetBehaviourControl, Toggle collectLootToggle, GameObject emergencyWarpGO)
        {
            cloakToggleGO = GameObject.Instantiate(collectLootToggle.gameObject);
            cloakToggleGO.SetActive(true);
            cloakToggleText = cloakToggleGO.transform.Find("Label").gameObject.GetComponentInChildren<Text>();
            cloakToggleText.text = "Cloak with player";
            cloakToggle = cloakToggleGO.GetComponent<Toggle>();
            cloakToggleGO.SetActive(false);

            RectTransform goRect = cloakToggleGO.GetComponent<RectTransform>();
            goRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, goRect.rect.height);

            Util.AddFleetBehaviourControlUIElement(
                cloakToggleGO,
                collectLootToggle.gameObject.transform.localPosition.x,
                goRect.rect.height,
                goRect.rect.height,
                fleetBehaviourControl);
        }

        internal static void EnableUIElements(bool state, AIMercenaryCharacter aiChar, bool value)
        {
            if (cloakToggle == null)
                return;

            if (state && aiChar != null)
            {
                Toggle.ToggleEvent te = new Toggle.ToggleEvent();
                UnityAction<bool> ua = null;
                ua += (bool val) => CloakValueChanged((aiChar as PlayerFleetMember).crewMemberID);
                te.AddListener(ua);
                cloakToggle.onValueChanged = te;
                cloakToggleGO.SetActive(true);
                cloakToggle.SetIsOnWithoutNotify(value);
            }
            else
            {
                cloakToggle.onValueChanged = null;
                cloakToggle.SetIsOnWithoutNotify(false);
                cloakToggleGO.SetActive(false);
            }
        }

        private static void CloakValueChanged(int crewID)
        {
            if (Main.data.cloakWithPlayerStates != null &&
                Main.data.cloakWithPlayerStates.ContainsKey(crewID))
                Main.data.cloakWithPlayerStates[crewID] = cloakToggle.isOn;
        }
    }
}
