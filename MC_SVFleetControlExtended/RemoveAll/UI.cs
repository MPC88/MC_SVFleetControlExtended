using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

namespace MC_SVFleetControlExtended.RemoveAll
{
    internal class UI
    {
        private static GameObject btnRemoveAll;
        private static PlayerControl pc;

        internal static void EnableDisable()
        {
            if (Patches.dockingUI == null || !Patches.dockingUI.playerDocked || Patches.inventory == null)
            {
                if (btnRemoveAll != null)
                    btnRemoveAll.SetActive(false);

                return;
            }

            int cargoMode = (int)AccessTools.Field(typeof(Inventory), "cargoMode").GetValue(Patches.inventory);

            if (cargoMode != 2)
            {
                if (btnRemoveAll != null)
                    btnRemoveAll.SetActive(false);

                return;
            }

            int selectedSlot = (int)AccessTools.Field(typeof(Inventory), "selectedSlot").GetValue(Patches.inventory);

            if (selectedSlot < 0)
            {
                if (btnRemoveAll != null)
                    btnRemoveAll.SetActive(false);

                return;
            }

            if (btnRemoveAll == null)
                Initialise();

            btnRemoveAll.SetActive(true);
        }

        internal static void Initialise()
        {
            if (btnRemoveAll != null)
                return;

            GameObject src = (GameObject)AccessTools.Field(typeof(Inventory), "btnAddToFleet").GetValue(Patches.inventory);
            btnRemoveAll = GameObject.Instantiate(src);
            btnRemoveAll.name = "btnRemoveAllFromFleet";
            btnRemoveAll.transform.SetParent(src.transform.parent);
            btnRemoveAll.GetComponentInChildren<Text>().text = "Remove all from fleet";
            RectTransform rt = btnRemoveAll.GetComponent<RectTransform>();
            ButtonClickedEvent removeBCE = new Button.ButtonClickedEvent();
            removeBCE.AddListener(btnRemoveAll_Click);
            btnRemoveAll.GetComponentInChildren<Button>().onClick = removeBCE;
            btnRemoveAll.transform.localScale = src.transform.localScale;
            RectTransform btnRT = btnRemoveAll.GetComponent<RectTransform>();
            btnRemoveAll.transform.localPosition = new Vector3(src.transform.localPosition.x, src.transform.localPosition.y - btnRT.rect.height - 1.5f, src.transform.localPosition.z);
        }

        internal static void btnRemoveAll_Click()
        {
            InventorySlot[] slots = Patches.inventory.gameObject.GetComponentsInChildren<InventorySlot>();

            List<PlayerFleetMember> validFleetMembers = new List<PlayerFleetMember>();

            foreach (InventorySlot slot in slots)
            {
                if (slot.itemIndex < 0)
                    continue;

                if (slot.itemIndex < 0)
                    continue;

                PlayerFleetMember pfm = PChar.Char.mercenaries[slot.itemIndex] as PlayerFleetMember;
                if (pfm == null || (pfm.dockedStationID != Patches.dockingUI.station.id && !pfm.hangarDocked))
                    continue;

                validFleetMembers.Add(pfm);
            }

            if (pc == null)
                pc = GameManager.instance.Player.GetComponent<PlayerControl>();

            if (validFleetMembers.Count < 1)
                return;

            foreach (PlayerFleetMember fleetMemeber in validFleetMembers)
                RemoveFleetMember(fleetMemeber);

            pc.CalculateShip(changeWeapon: false);
            Patches.inventory.RefreshIfOpen(null, resetScreen: true, alsoUpdateShipInfo: true);
            AccessTools.Method(typeof(Inventory), "ResetScreen").Invoke(Patches.inventory, null);
            Patches.inventory.LoadItems();
            FleetControl.instance.Refresh(forceOpen: false);
            SoundSys.PlaySound(11, keepPlaying: true);
        }

        internal static void RemoveFleetMember(PlayerFleetMember fleetMember)
        {
            ShipModelData shipModelData = fleetMember.ModelData();
            SpaceShipData shipData = fleetMember.shipData;
            ShipInfo shipInfo = (ShipInfo)AccessTools.Field(typeof(Inventory), "shipInfo").GetValue(Patches.inventory);
            CargoSystem cs = (CargoSystem)AccessTools.Field(typeof(Inventory), "cs").GetValue(Patches.inventory);
            if (shipData.HPPercent < 0.99f)
            {
                InfoPanelControl.inst.ShowWarning(Lang.Get(5, 296), 1, playAudio: true);
                return;
            }
            if (shipInfo.editingFleetShip == fleetMember)
            {
                shipInfo.StopEditingFleetShip();
            }
            int num = GameData.data.NewShipLoadout(null);
            GameData.data.SetShipLoadout(shipData, num);
            cs.StoreItem(4, shipData.shipModelID, shipModelData.rarity, 1, 0f, Patches.inventory.currStation.id, num);
            CrewMember crewMember = CrewDB.GetCrewMember(fleetMember.crewMemberID);
            crewMember.aiChar.behavior = fleetMember.behavior;
            cs.StoreItem(5, crewMember.id, crewMember.rarity, 1, 0f, Patches.inventory.currStation.id, -1);
            if (fleetMember.hangarDocked)
            {
                CarrierControl getCarrierControl = pc.GetCarrierControl;
                if ((bool)getCarrierControl)
                {
                    getCarrierControl.RemoveDockedShip(fleetMember);
                }
            }
            FleetControl.instance.CleanFleetSlot(fleetMember);
            PChar.Char.mercenaries.Remove(fleetMember);
            pc.RemoveMercenaryGO(fleetMember);
        }
    }
}
