using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MC_SVFleetControlExtended.EnergyBarrier
{
    internal class UI
    {
        private static GameObject energyBarrierGO = null;
        private static Text energyBarrierText = null;
        private static Dropdown energyBarrierDropdown = null;

        internal static void ValidateUIElements(FleetBehaviorControl fleetBehaviourControl, GameObject emergencyWarpGO)
        {
            if (energyBarrierGO == null)
                CreateUIElements(fleetBehaviourControl, emergencyWarpGO);
        }

        internal static void CreateUIElements(FleetBehaviorControl fleetBehaviourControl, GameObject emergencyWarpGO)
        {
            energyBarrierGO = GameObject.Instantiate(emergencyWarpGO);
            energyBarrierGO.SetActive(true);
            energyBarrierText = energyBarrierGO.transform.Find("Text").gameObject.GetComponent<Text>();
            energyBarrierText.text = "Activate Energy Barrier when HP below";
            energyBarrierDropdown = energyBarrierGO.transform.Find("Dropdown").GetComponent<Dropdown>();            
            GameObject.Destroy(energyBarrierGO.transform.Find("Note").gameObject);
            energyBarrierGO.SetActive(false);

            RectTransform goRect = energyBarrierGO.GetComponent<RectTransform>();
            goRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                energyBarrierDropdown.GetComponent<RectTransform>().rect.height);

            Util.AddFleetBehaviourControlUIElement(
                energyBarrierGO,
                emergencyWarpGO.transform.localPosition.x,
                goRect.rect.height,
                10,
                fleetBehaviourControl);
        }

        internal static void EnableEnergyBarrierDropDown(bool state, AIMercenaryCharacter aiChar, int value)
        {
            if (energyBarrierGO == null)
                return;

            if (state && aiChar != null && value >= 0 && value <= 100)
            {
                Dropdown.DropdownEvent dde = new Dropdown.DropdownEvent();
                UnityAction<int> ua = null;
                ua += (int index) => ThresholdChanged(aiChar as PlayerFleetMember);
                dde.AddListener(ua);
                energyBarrierDropdown.onValueChanged = dde;
                energyBarrierGO.SetActive(true);
                energyBarrierDropdown.value = value;
            }
            else
            {
                energyBarrierDropdown.onValueChanged = null;
                energyBarrierGO.SetActive(false);
            }
        }

        private static void ThresholdChanged(PlayerFleetMember aiChar)
        {
            if (Main.data.energyBarrierThresholds != null &&
                Main.data.energyBarrierThresholds.ContainsKey(aiChar.crewMemberID))
                Main.data.energyBarrierThresholds[aiChar.crewMemberID] = energyBarrierDropdown.value;
        }
    }
}
