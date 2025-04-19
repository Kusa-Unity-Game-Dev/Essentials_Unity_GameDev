#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace com.kusabb
{
    public class SceneSelectorEditor : EditorWindow
    {
        private List<string> scenePaths = new List<string>();
        private List<string> favoriteScenes = new List<string>();
        private int selectedSceneIndex = 0;

        [MenuItem("Tools/Scene Selector")]
        public static void ShowWindow()
        {
            GetWindow<SceneSelectorEditor>("Scene Selector");
        }

        private void OnEnable()
        {
            LoadAllScenes();
            LoadFavorites();
        }

        private void LoadAllScenes()
        {
            string[] scenes = AssetDatabase.FindAssets("t:Scene");
            scenePaths.Clear();
            foreach (string sceneGuid in scenes)
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneGuid);
                scenePaths.Add(path);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Scene Selector", EditorStyles.boldLabel);

            // Dropdown for selecting a scene from all scenes
            string[] sceneNames = new string[scenePaths.Count];
            for (int i = 0; i < scenePaths.Count; i++)
            {
                sceneNames[i] = System.IO.Path.GetFileNameWithoutExtension(scenePaths[i]);
            }

            selectedSceneIndex = EditorGUILayout.Popup("Select Scene", selectedSceneIndex, sceneNames);

            if (GUILayout.Button("Add to Favorites"))
            {
                AddToFavorites(scenePaths[selectedSceneIndex]);
            }

            GUILayout.Space(10);
            GUILayout.Label("Favorites", EditorStyles.boldLabel);

            if (favoriteScenes.Count == 0)
            {
                GUILayout.Label("No favorite scenes added.");
            }
            else
            {
                foreach (string favScene in favoriteScenes)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(System.IO.Path.GetFileNameWithoutExtension(favScene));
                    if (GUILayout.Button("Play Mode"))
                    {
                        LoadSceneAndPlay(favScene);
                    }

                    if (GUILayout.Button("Open"))
                    {
                        LoadSceneWithoutPlay(favScene);
                    }

                    if (GUILayout.Button("Remove"))
                    {
                        RemoveFromFavorites(favScene);
                        break; // Break to avoid modifying the list during iteration
                    }

                    GUILayout.EndHorizontal();
                }
            }
        }

        private void AddToFavorites(string scenePath)
        {
            if (!favoriteScenes.Contains(scenePath))
            {
                favoriteScenes.Add(scenePath);
                SaveFavorites();
            }
        }

        private void RemoveFromFavorites(string scenePath)
        {
            favoriteScenes.Remove(scenePath);
            SaveFavorites();
        }

        private void LoadSceneAndPlay(string scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
                EditorApplication.isPlaying = true;
            }
        }

        private void LoadSceneWithoutPlay(string scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        private void SaveFavorites()
        {
            // Save as EditorPrefs (can be replaced by a more sophisticated solution like JSON in a file)
            EditorPrefs.SetString("FavoriteScenes", string.Join(";", favoriteScenes.ToArray()));
        }

        private void LoadFavorites()
        {
            // Load from EditorPrefs
            string savedFavorites = EditorPrefs.GetString("FavoriteScenes", "");
            if (!string.IsNullOrEmpty(savedFavorites))
            {
                favoriteScenes = new List<string>(savedFavorites.Split(';'));
            }
        }
    }
}
#endif