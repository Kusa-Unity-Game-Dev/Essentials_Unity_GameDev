using UnityEditor;
using UnityEngine;
using BB.Framework;

namespace BB.Framework.Editor
{
    /// <summary>
    /// Custom editor for UIGroup to provide better controls for managing UI groups.
    /// </summary>
    [CustomEditor(typeof(UIGroup))]
    public class UIGroupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            UIGroup uiGroup = (UIGroup)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Group Controls", EditorStyles.boldLabel);
            
            // Runtime controls
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Show All"))
                {
                    uiGroup.ShowAll();
                }
                if (GUILayout.Button("Show At Once"))
                {
                    uiGroup.ShowAllAtOnce();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Show Sequentially"))
                {
                    uiGroup.ShowSequentially();
                }
                if (GUILayout.Button("Hide All"))
                {
                    uiGroup.HideAll();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Layer Management", EditorStyles.boldLabel);
                
                UILayer selectedLayer = UILayer.Main;
                selectedLayer = (UILayer)EditorGUILayout.EnumPopup("Set Group Layer", selectedLayer);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply Layer"))
                {
                    uiGroup.SetGroupLayer(selectedLayer);
                }
                if (GUILayout.Button("Set Incremental Priorities"))
                {
                    uiGroup.SetIncrementalPriorities(0);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Group Status", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("All Visible", uiGroup.AreAllVisible().ToString());
                EditorGUILayout.LabelField("Any Visible", uiGroup.IsAnyVisible().ToString());
                EditorGUILayout.LabelField("UI Count", uiGroup.UIElements.Count.ToString());
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use group controls.", MessageType.Info);
            }
        }
    }
}
