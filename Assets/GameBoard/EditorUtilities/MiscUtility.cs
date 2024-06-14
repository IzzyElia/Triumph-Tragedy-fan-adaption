using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameBoard.EditorUtilities
{
    public class MiscUtility : EditorWindow
    {
        private void OnEnable()
        {
            //_map = GameObject.Find("Map")?.GetComponent<Map>();
            
        }

        [MenuItem("Window/Misc Dev Utilities")]
        public static void ShowWindow()
        {
            GetWindow<MiscUtility>("Misc Utilities");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Fix Flag Names"))
            {
                RenameFlags();
            }
            GUILayout.Space(15);
        }

        void RenameFlags()
        {
            string resourcesPath = "Assets/Resources/Icons/Flags";
            string[] directories = Directory.GetDirectories(resourcesPath, "*", SearchOption.AllDirectories);

            foreach (string dir in directories)
            {
                string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string fileName = file.Split('/')[^1];
                    if (!file.EndsWith(".meta"))
                    {
                        try
                        {
                            string withoutExtension = fileName.Split('.')[0];
                            string extension = fileName.Split('.')[1];
                            string ideology = withoutExtension.Split('_')[1];
                            string idologyName;
                            switch (ideology.ToLower())
                            {
                                case "communism":
                                    idologyName = "Communists";
                                    break;
                                case "democratic":
                                    idologyName = "Capitalists";
                                    break;
                                case "fascism":
                                    idologyName = "Fascists";
                                    break;
                                case "neutrality":
                                    idologyName = "Default";
                                    break;
                                default:
                                    Debug.Log($"Ignoring file {file} ({ideology}");
                                    continue;
                            }
                        
                            string assetPath = file.Replace("\\", "/");
                            string newAssetName = $"Flag_{idologyName}.{extension}"; // Implement this method based on your naming logic
                            string newAssetPath = Path.Combine(Path.GetDirectoryName(assetPath), newAssetName + Path.GetExtension(assetPath));
                            AssetDatabase.RenameAsset(assetPath, newAssetName);
                            Debug.Log($"Renamed {assetPath} to {newAssetPath}");
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }

                    }
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}