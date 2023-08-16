using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MC_SVFleetControlExtended.Escort
{
    internal class UI
    {
        private static GameObject escortGO = null;
        private static Text escortText = null;
        private static Dropdown escortDropdown = null;
        private static Dictionary<int, int> escortDropdownValueCrewID = new Dictionary<int, int>();
        private static GameObject dedicatedDefenderToggleGO = null;
        private static Toggle dedicatedDefenderToggle = null;
        private static PlayerControl pc = null;

        internal static void ValidateUIElements(FleetBehaviorControl fleetBehaviourControl, GameObject emergencyWarpGO, Toggle collectLootToggle)
        {
            if (escortGO == null || dedicatedDefenderToggle == null)
                CreateUIElements(fleetBehaviourControl, emergencyWarpGO, collectLootToggle);
        }

        internal static void CreateUIElements(FleetBehaviorControl fleetBehaviourControl, GameObject emergencyWarpGO, Toggle collectLootToggle)
        {
            escortGO = GameObject.Instantiate(emergencyWarpGO);
            escortGO.SetActive(true);
            escortText = escortGO.transform.Find("Text").gameObject.GetComponent<Text>();
            escortText.text = "Assign escort target";
            escortDropdown = escortGO.transform.Find("Dropdown").GetComponent<Dropdown>();
            escortDropdown.ClearOptions();
            GameObject.Destroy(escortGO.transform.Find("Note").gameObject);
            escortGO.SetActive(false);

            RectTransform goRect = escortGO.GetComponent<RectTransform>();
            goRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 
                escortDropdown.GetComponent<RectTransform>().rect.height);

            Util.AddFleetBehaviourControlUIElement(
                escortGO,
                emergencyWarpGO.transform.localPosition.x,
                goRect.rect.height,
                0,
                fleetBehaviourControl);

            dedicatedDefenderToggleGO = GameObject.Instantiate(collectLootToggle.gameObject);
            dedicatedDefenderToggleGO.SetActive(true);
            dedicatedDefenderToggleGO.transform.Find("Label").gameObject.GetComponentInChildren<Text>().text = "Dedicated defender";
            dedicatedDefenderToggle = dedicatedDefenderToggleGO.GetComponent<Toggle>();
            dedicatedDefenderToggleGO.SetActive(false);

            goRect = dedicatedDefenderToggleGO.GetComponent<RectTransform>();
            goRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, goRect.rect.height);

            Util.AddFleetBehaviourControlUIElement(
                dedicatedDefenderToggleGO,
                collectLootToggle.gameObject.transform.localPosition.x,
                goRect.rect.height,
                goRect.rect.height,
                fleetBehaviourControl);
        }

        internal static void EnableUIElements(bool state, AIMercenaryCharacter aiChar, int escorteeID, bool dedicatedDefender)
        {
            if (escortGO == null)
                return;

            if (state && aiChar != null)
            {
                // Enable dropdown
                int selectedValue = RefreshEscortOptions(escorteeID, aiChar);
                Dropdown.DropdownEvent dde = new Dropdown.DropdownEvent();
                UnityAction<int> ua = null;
                ua += (int index) => EscortChanged(aiChar as PlayerFleetMember);
                dde.AddListener(ua);
                escortDropdown.onValueChanged = dde;
                escortGO.SetActive(true);
                escortDropdown.value = selectedValue;

                // Enable dedicated defender toggle
                Toggle.ToggleEvent te = new Toggle.ToggleEvent();
                UnityAction<bool> ua2 = null;
                ua2 += (bool val) => DedicatedDefenderValueChanged((aiChar as PlayerFleetMember).crewMemberID);
                te.AddListener(ua2);
                dedicatedDefenderToggle.onValueChanged = te;
                dedicatedDefenderToggleGO.SetActive(true);
                dedicatedDefenderToggle.SetIsOnWithoutNotify(dedicatedDefender);
            }
            else
            {
                escortDropdown.onValueChanged = null;
                escortGO.SetActive(false);
            }
        }

        internal static int RefreshEscortOptions(int escorteeID, AICharacter aiChar)
        {
            escortDropdown.ClearOptions();
            escortDropdownValueCrewID.Clear();
            int val = 1;
            int escorteeIDListEntry = 0;
            List<string> optionDataList = new List<string>();
            optionDataList.Add("Player");
            foreach (AIMercenaryCharacter aimc in PChar.Char.mercenaries)
            {
                if (aimc is PlayerFleetMember &&
                    (!(aiChar is PlayerFleetMember) ||
                    (aimc as PlayerFleetMember).crewMemberID != (aiChar as PlayerFleetMember).crewMemberID))
                {
                    optionDataList.Add(aimc.Name());
                    int crewID = (aimc as PlayerFleetMember).crewMemberID;
                    escortDropdownValueCrewID.Add(val, crewID);
                    if (escorteeIDListEntry == 0 && crewID == escorteeID)
                        escorteeIDListEntry = val;
                    val++;
                }
            }
            escortDropdown.AddOptions(optionDataList);
            return escorteeIDListEntry;
        }

        private static void EscortChanged(PlayerFleetMember aiChar)
        {
            if (Main.data.escorts == null)
                return;

            if (escortDropdown.value == 0 && Main.data.escorts.ContainsKey(aiChar.crewMemberID))
            {
                Main.data.escorts.Remove(aiChar.crewMemberID);
            }
            else
            {
                if (Main.data.escorts.ContainsKey(aiChar.crewMemberID))
                    Main.data.escorts[aiChar.crewMemberID] = escortDropdownValueCrewID[escortDropdown.value];
                else
                    Main.data.escorts.Add(aiChar.crewMemberID, escortDropdownValueCrewID[escortDropdown.value]);
            }

            if (pc == null)
                pc = GameManager.instance.Player.GetComponent<PlayerControl>();

            foreach (Transform aimTrans in pc.mercenaries)
            {
                AIMercenary aim = aimTrans.gameObject.GetComponent<AIMercenary>();
                if (aim != null && aim.isPlayerFleet &&
                (aim.aiMercChar as PlayerFleetMember).crewMemberID == aiChar.crewMemberID)
                {
                    aim.DefineFollowPosition(-1, -1);
                    break;
                }
            }
        }

        private static void DedicatedDefenderValueChanged(int crewID)
        {
            if (Main.data.dedicatedDefenderStates != null &&
                Main.data.dedicatedDefenderStates.ContainsKey(crewID))
                Main.data.dedicatedDefenderStates[crewID] = dedicatedDefenderToggle.isOn;
        }
    }
}
