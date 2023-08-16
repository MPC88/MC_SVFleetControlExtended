using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MC_SVFleetControlExtended.Escort
{
    internal class UI
    {
        internal static AICharacter menuOpenForChar = null;

        private static GameObject escortGO = null;
        private static Text escortText = null;
        private static Dropdown escortDropdown = null;
        private static Dictionary<int, int> escortDropdownValueCrewID = new Dictionary<int, int>();
        private static PlayerControl pc = null;

        internal static void ValidateUIElements(FleetBehaviorControl fleetBehaviourControl, GameObject emergencyWarpGO, Toggle collectLootToggle)
        {
            if (escortGO == null)
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
        }

        internal static void EnableEscortDropDown(bool state, AIMercenaryCharacter aiChar, int escorteeID)
        {
            if (escortGO == null)
                return;

            if (state && aiChar != null)
            {
                // Set available options
                escortDropdown.ClearOptions();
                escortDropdownValueCrewID.Clear();
                menuOpenForChar = aiChar;
                int selectedValue = RefreshEscortOptions(escorteeID);
                
                // Enable
                Dropdown.DropdownEvent dde = new Dropdown.DropdownEvent();
                UnityAction<int> ua = null;
                ua += (int index) => EscortChanged(aiChar as PlayerFleetMember);
                dde.AddListener(ua);
                escortDropdown.onValueChanged = dde;
                escortGO.SetActive(true);
                escortDropdown.value = selectedValue;
            }
            else
            {
                menuOpenForChar = null;
                escortDropdown.onValueChanged = null;
                escortGO.SetActive(false);
            }
        }

        internal static int RefreshEscortOptions(int escorteeID)
        {
            int val = 1;
            int escorteeIDListEntry = 0;
            List<string> optionDataList = new List<string>();
            optionDataList.Add("Player");
            foreach (AIMercenaryCharacter aimc in PChar.Char.mercenaries)
            {
                if (aimc is PlayerFleetMember &&
                    (!(menuOpenForChar is PlayerFleetMember) ||
                    (aimc as PlayerFleetMember).crewMemberID != (menuOpenForChar as PlayerFleetMember).crewMemberID))
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
    }
}
