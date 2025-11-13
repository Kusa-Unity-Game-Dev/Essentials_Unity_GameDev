using UnityEditor;
using UnityEngine;
using BB.Framework;

namespace BB.Framework.Editor
{
    /// <summary>
    /// Custom editor for UIBase to provide better visualization and controls for UI layering.
    /// </summary>
    [CustomEditor(typeof(UIBase), true)]
    public class UIBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty m_canvasProperty;
        private SerializedProperty m_graphicRaycasterProperty;
        private SerializedProperty m_initAnimationTimeProperty;
        private SerializedProperty m_outroAnimationTimeProperty;
        private SerializedProperty m_isAllowedToStackProperty;
        
        private void OnEnable()
        {
            m_canvasProperty = serializedObject.FindProperty("m_canvas");
            m_graphicRaycasterProperty = serializedObject.FindProperty("m_graphicRaycaster");
            m_initAnimationTimeProperty = serializedObject.FindProperty("m_initAnimationTime");
            m_outroAnimationTimeProperty = serializedObject.FindProperty("m_outroAnimationTime");
            m_isAllowedToStackProperty = serializedObject.FindProperty("isAllowedToStack");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            UIBase uiBase = (UIBase)target;
            
            // Draw default properties
            EditorGUILayout.PropertyField(m_canvasProperty);
            EditorGUILayout.PropertyField(m_graphicRaycasterProperty);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_initAnimationTimeProperty);
            EditorGUILayout.PropertyField(m_outroAnimationTimeProperty);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_isAllowedToStackProperty);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI Layering", EditorStyles.boldLabel);
            
            // Display layer info
            EditorGUI.BeginChangeCheck();
            UILayer newLayer = (UILayer)EditorGUILayout.EnumPopup("UI Layer", uiBase.Layer);
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                uiBase.Layer = newLayer;
            }
            else if (EditorGUI.EndChangeCheck())
            {
                // Update the serialized property for edit mode
                SerializedProperty layerProp = serializedObject.FindProperty("m_uiLayer");
                if (layerProp != null)
                {
                    layerProp.enumValueIndex = (int)newLayer;
                }
            }
            
            EditorGUI.BeginChangeCheck();
            int newPriority = EditorGUILayout.IntSlider("Layer Priority", uiBase.LayerPriority, 0, 9);
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                uiBase.LayerPriority = newPriority;
            }
            else if (EditorGUI.EndChangeCheck())
            {
                // Update the serialized property for edit mode
                SerializedProperty priorityProp = serializedObject.FindProperty("m_layerPriority");
                if (priorityProp != null)
                {
                    priorityProp.intValue = newPriority;
                }
            }
            
            // Display calculated sort order
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Calculated Sort Order", uiBase.SortOrder);
            EditorGUI.EndDisabledGroup();
            
            // Helper info box
            EditorGUILayout.HelpBox(
                $"Layer: {uiBase.Layer} (Base: {(int)uiBase.Layer})\n" +
                $"Priority: {uiBase.LayerPriority}\n" +
                $"Final Sort Order: {uiBase.SortOrder}",
                MessageType.Info
            );
            
            // Runtime controls
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Bring to Front"))
                {
                    uiBase.BringToFront();
                }
                if (GUILayout.Button("Send to Back"))
                {
                    uiBase.SendToBack();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = !uiBase.IsVisible;
                if (GUILayout.Button("Show UI"))
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowUI(uiBase);
                    }
                }
                GUI.enabled = uiBase.IsVisible;
                if (GUILayout.Button("Hide UI"))
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.HideUI(uiBase);
                    }
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField("Is Visible", uiBase.IsVisible.ToString());
            }
            
            serializedObject.ApplyModifiedProperties();
            
            // Draw remaining properties from derived classes
            DrawPropertiesExcluding(serializedObject, 
                "m_Script",
                "m_canvas", 
                "m_graphicRaycaster",
                "m_initAnimationTime",
                "m_outroAnimationTime",
                "isAllowedToStack",
                "m_uiLayer",
                "m_layerPriority"
            );
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
