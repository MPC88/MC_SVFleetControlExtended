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
        public const string pluginVersion = "1.0.0";

        internal static PersistentData data = null;

        private const string modSaveFolder = "/MCSVSaveData/";  // /SaveData/ sub folder
        private const string modSaveFilePrefix = "FleetCntrlEx_"; // modSaveFilePrefixNN.dat
        
        internal static ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Main));
            Harmony.CreateAndPatchAll(typeof(EnergyBarrier.Patches));
            Harmony.CreateAndPatchAll(typeof(Cloak.Patches));
            Harmony.CreateAndPatchAll(typeof(Escort.Patches));
            Harmony.CreateAndPatchAll(typeof(DesiredDistance.Patches));
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.SaveGame))]
        [HarmonyPrefix]
        private static void GameDataSaveGame_Pre()
        {
            SaveGame();
        }

        private void Update()
        {
            int option = -1;
            if (Input.GetKeyDown(KeyCode.F2))
                option = 1; //0

            if (Input.GetKeyDown(KeyCode.F3))
                option = 5; //200

            if (Input.GetKeyDown(KeyCode.F4))
                option = 9; //400
            
            if(option > 0)
            {
                int[] keys = new int[Main.data.desiredDistances.Count];
                Main.data.desiredDistances.Keys.CopyTo(keys, 0);

                for (int i = 0; i < Main.data.desiredDistances.Count; i++)
                {
                    Main.data.desiredDistances[keys[i]] = option;
                }                
            }
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
                SideInfo.AddMsg("<color=red>Fleet enegy barrier control mod load failed.</color>");
            }
        }
    }
}
