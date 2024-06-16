using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace MC_SVFleetControlExtended
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "mc.starvalor.fleetcontrolextended";
        public const string pluginName = "SV Fleet Control Extended";
        public const string pluginVersion = "0.0.3";

        internal static PersistentData data = null;

        private const string modSaveFolder = "/MCSVSaveData/";  // /SaveData/ sub folder
        private const string modSaveFilePrefix = "FleetCntrlEx_"; // modSaveFilePrefixNN.dat
        
        internal static ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Main));

            // Constant or no new UI elements
            Harmony.CreateAndPatchAll(typeof(Escort.Patches));
            Harmony.CreateAndPatchAll(typeof(DesiredDistance.Patches));
            Harmony.CreateAndPatchAll(typeof(DockUndockUnload.Patches));
            Harmony.CreateAndPatchAll(typeof(HoldPosition.Patches));
            Harmony.CreateAndPatchAll(typeof(RemoveAll.Patches));

            // Conditional new UI elements
            Harmony.CreateAndPatchAll(typeof(EnergyBarrier.Patches));
            Harmony.CreateAndPatchAll(typeof(Cloak.Patches));

            DockUndockUnload.Patches.Config(this);
            HoldPosition.Patches.Config(this);
        }

        private void Update()
        {
            DockUndockUnload.Patches.Update();
            HoldPosition.Patches.Update();
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.SetupGame))]
        [HarmonyPostfix]
        private static void GameManagerSetupGame_Post()
        {
            Escort.Patches.PostLoadInitialise();
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.SaveGame))]
        [HarmonyPrefix]
        private static void GameDataSaveGame_Pre()
        {
            SaveGame();
        }

        private static void SaveGame()
        {
            if (data == null || data.energyBarrierThresholds.Count == 0)
                return;

            string tempPath = Application.dataPath + GameData.saveFolderName + modSaveFolder + "LOTemp.dat";

            if (!Directory.Exists(Path.GetDirectoryName(tempPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(tempPath));

            if (File.Exists(tempPath))
                File.Delete(tempPath);

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream fileStream = File.Create(tempPath);
            binaryFormatter.Serialize(fileStream, data);
            fileStream.Close();

            File.Copy(tempPath, Application.dataPath + GameData.saveFolderName + modSaveFolder + modSaveFilePrefix + GameData.gameFileIndex.ToString("00") + ".dat", true);
            File.Delete(tempPath);
        }

        [HarmonyPatch(typeof(MenuControl), nameof(MenuControl.LoadGame))]
        [HarmonyPostfix]
        private static void MenuControlLoadGame_Post()
        {
            LoadData(GameData.gameFileIndex.ToString("00"));
            Util.ResetInstanceBasedCummulatives();
        }

        private static void LoadData(string saveIndex)
        {
            string modData = Application.dataPath + GameData.saveFolderName + modSaveFolder + modSaveFilePrefix + saveIndex + ".dat";
            try
            {
                if (!saveIndex.IsNullOrWhiteSpace() && File.Exists(modData))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    FileStream fileStream = File.Open(modData, FileMode.Open);
                    PersistentData loadData = (PersistentData)binaryFormatter.Deserialize(fileStream);
                    fileStream.Close();

                    if (loadData == null)
                        data = new PersistentData();
                    else
                        data = loadData;
                }
                else
                    data = new PersistentData();
            }
            catch
            {
                SideInfo.AddMsg("<color=red>Extended fleet control mod load failed.</color>");
            }
        }
    }
}
