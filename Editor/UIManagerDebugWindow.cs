#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BB.Framework
{
    /// <summary>
    /// Industry-standard editor window for debugging and tracking UI elements managed by UIManager.
    /// Provides real-time visualization of UI states, layer maps, stacks, and history.
    /// </summary>
    public class UIManagerDebugWindow : EditorWindow
    {
        // Scroll positions for different sections
        private Vector2 m_mainScrollPosition;
        private Vector2 m_activeUIScrollPosition;
        private Vector2 m_historyScrollPosition;

        // Foldout states for each layer
        private Dictionary<UILayer, bool> m_layerFoldouts = new Dictionary<UILayer, bool>();
        private Dictionary<UILayer, bool> m_stackFoldouts = new Dictionary<UILayer, bool>();

        // General foldout states
        private bool m_activeUIFoldout = true;
        private bool m_layerMapFoldout = true;
        private bool m_stacksFoldout = true;
        private bool m_historyFoldout = true;
        private bool m_settingsFoldout = false;

        // Search/filter
        private string m_searchFilter = "";
        private string m_searchFilterLower = "";  // Cached lowercase for efficient filtering
        private UILayer? m_layerFilter = null;
        private bool m_showOnlyVisible = false;

        // Auto-refresh settings
        private bool m_autoRefresh = true;
        private float m_refreshInterval = 0.5f;
        private double m_lastRefreshTime;

        // Cached reflection data for accessing private dictionaries
        private FieldInfo m_layerUIMapField;
        private FieldInfo m_layerStacksField;

        // Styles
        private GUIStyle m_headerStyle;
        private GUIStyle m_subHeaderStyle;
        private GUIStyle m_boxStyle;
        private GUIStyle m_itemStyle;
        private GUIStyle m_visibleStyle;
        private GUIStyle m_hiddenStyle;
        private bool m_stylesInitialized = false;

        [MenuItem("Tools/UI Manager Debug Window")]
        public static void ShowWindow()
        {
            UIManagerDebugWindow window = GetWindow<UIManagerDebugWindow>("UI Manager Debug");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // Initialize layer foldouts
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                if (!m_layerFoldouts.ContainsKey(layer))
                    m_layerFoldouts[layer] = false;
                if (!m_stackFoldouts.ContainsKey(layer))
                    m_stackFoldouts[layer] = false;
            }

            // Cache reflection fields for accessing private dictionaries
            Type uiManagerType = typeof(UIManager);
            m_layerUIMapField = uiManagerType.GetField("m_layerUIMap", BindingFlags.NonPublic | BindingFlags.Instance);
            m_layerStacksField = uiManagerType.GetField("m_layerStacks", BindingFlags.NonPublic | BindingFlags.Instance);

            // Log warning if reflection fields are not found
            if (m_layerUIMapField == null)
            {
                Debug.LogWarning("[UIManagerDebugWindow] Could not find 'm_layerUIMap' field via reflection. Layer map visualization may not work.");
            }
            if (m_layerStacksField == null)
            {
                Debug.LogWarning("[UIManagerDebugWindow] Could not find 'm_layerStacks' field via reflection. Stack visualization may not work.");
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Repaint();
        }

        private void InitializeStyles()
        {
            if (m_stylesInitialized) return;

            m_headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 10, 5)
            };

            m_subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };

            m_boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(5, 5, 2, 2)
            };

            m_itemStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(5, 5, 2, 2)
            };

            m_visibleStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                padding = new RectOffset(5, 5, 2, 2)
            };

            m_hiddenStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                padding = new RectOffset(5, 5, 2, 2)
            };

            m_stylesInitialized = true;
        }

        private void Update()
        {
            // Auto-refresh during play mode
            if (m_autoRefresh && Application.isPlaying)
            {
                if (EditorApplication.timeSinceStartup - m_lastRefreshTime > m_refreshInterval)
                {
                    m_lastRefreshTime = EditorApplication.timeSinceStartup;
                    Repaint();
                }
            }
        }

        private void OnGUI()
        {
            InitializeStyles();

            m_mainScrollPosition = EditorGUILayout.BeginScrollView(m_mainScrollPosition);

            DrawHeader();
            DrawSearchAndFilter();

            if (!Application.isPlaying)
            {
                DrawPlayModeWarning();
            }
            else if (UIManager.Instance == null)
            {
                DrawNoUIManagerWarning();
            }
            else
            {
                DrawActiveUIs();
                DrawLayerMap();
                DrawLayerStacks();
                DrawUIHistory();
            }

            DrawSettings();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("UI Manager Debug", m_headerStyle);
            
            if (Application.isPlaying && UIManager.Instance != null)
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                {
                    Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Status bar
            if (Application.isPlaying && UIManager.Instance != null)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label($"Active UIs: {UIManager.Instance.uiScreens.Count}", EditorStyles.miniLabel);
                GUILayout.Label($"| History: {UIManager.Instance.uiLastShownScreens.Count}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
        }

        private void DrawSearchAndFilter()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Search field
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            
            EditorGUI.BeginChangeCheck();
            m_searchFilter = EditorGUILayout.TextField(m_searchFilter);
            if (EditorGUI.EndChangeCheck())
            {
                // Cache the lowercase version for efficient filtering
                m_searchFilterLower = m_searchFilter.ToLower();
            }
            
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                m_searchFilter = "";
                m_searchFilterLower = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // Layer filter dropdown
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Layer:", GUILayout.Width(50));
            
            string[] layerOptions = new string[Enum.GetValues(typeof(UILayer)).Length + 1];
            layerOptions[0] = "All Layers";
            int selectedIndex = 0;
            int i = 1;
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                layerOptions[i] = layer.ToString();
                if (m_layerFilter.HasValue && m_layerFilter.Value == layer)
                {
                    selectedIndex = i;
                }
                i++;
            }
            
            int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, layerOptions);
            if (newSelectedIndex == 0)
            {
                m_layerFilter = null;
            }
            else
            {
                m_layerFilter = (UILayer)Enum.GetValues(typeof(UILayer)).GetValue(newSelectedIndex - 1);
            }

            m_showOnlyVisible = EditorGUILayout.ToggleLeft("Visible Only", m_showOnlyVisible, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawPlayModeWarning()
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode to see UI Manager debug information.\n\n" +
                "The UI Manager is only active during runtime.",
                MessageType.Info);
        }

        private void DrawNoUIManagerWarning()
        {
            EditorGUILayout.HelpBox(
                "No UIManager instance found.\n\n" +
                "Make sure a UIManager component exists in the scene.",
                MessageType.Warning);
        }

        private void DrawActiveUIs()
        {
            m_activeUIFoldout = EditorGUILayout.Foldout(m_activeUIFoldout, "Active UI Screens", true, EditorStyles.foldoutHeader);
            
            if (m_activeUIFoldout)
            {
                EditorGUILayout.BeginVertical(m_boxStyle);
                
                List<UIBase> filteredUIs = GetFilteredUIs(UIManager.Instance.uiScreens);
                
                if (filteredUIs.Count == 0)
                {
                    GUILayout.Label("No active UIs" + (HasActiveFilters() ? " (matching filters)" : ""), EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    m_activeUIScrollPosition = EditorGUILayout.BeginScrollView(m_activeUIScrollPosition, GUILayout.MaxHeight(200));
                    
                    foreach (UIBase ui in filteredUIs)
                    {
                        DrawUIItem(ui, true);
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(5);
        }

        private void DrawLayerMap()
        {
            m_layerMapFoldout = EditorGUILayout.Foldout(m_layerMapFoldout, "Layer Map (Dictionary Visualization)", true, EditorStyles.foldoutHeader);
            
            if (m_layerMapFoldout)
            {
                EditorGUILayout.BeginVertical(m_boxStyle);

                var layerMap = GetLayerUIMap();
                if (layerMap != null)
                {
                    foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
                    {
                        // Skip if layer filter is active and doesn't match
                        if (m_layerFilter.HasValue && m_layerFilter.Value != layer)
                            continue;

                        if (!layerMap.TryGetValue(layer, out List<UIBase> uiList))
                            continue;

                        List<UIBase> filteredList = GetFilteredUIs(uiList);
                        int count = filteredList.Count;
                        
                        // Always show layer header, but indicate if empty
                        EditorGUILayout.BeginHorizontal();
                        
                        // Color code based on UI count
                        Color originalColor = GUI.backgroundColor;
                        if (count > 0)
                        {
                            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.3f, 0.3f);
                        }
                        
                        string layerLabel = $"{layer} (Order: {(int)layer}) [{count} UIs]";
                        m_layerFoldouts[layer] = EditorGUILayout.Foldout(m_layerFoldouts[layer], layerLabel, true);
                        
                        GUI.backgroundColor = originalColor;
                        EditorGUILayout.EndHorizontal();

                        if (m_layerFoldouts[layer] && count > 0)
                        {
                            EditorGUI.indentLevel++;
                            foreach (UIBase ui in filteredList)
                            {
                                DrawUIItem(ui, false);
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }
                else
                {
                    GUILayout.Label("Unable to access layer map", EditorStyles.centeredGreyMiniLabel);
                }

                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(5);
        }

        private void DrawLayerStacks()
        {
            m_stacksFoldout = EditorGUILayout.Foldout(m_stacksFoldout, "Layer Stacks (Dictionary Visualization)", true, EditorStyles.foldoutHeader);
            
            if (m_stacksFoldout)
            {
                EditorGUILayout.BeginVertical(m_boxStyle);

                var layerStacks = GetLayerStacks();
                if (layerStacks != null)
                {
                    int totalStacked = 0;
                    
                    foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
                    {
                        // Skip if layer filter is active and doesn't match
                        if (m_layerFilter.HasValue && m_layerFilter.Value != layer)
                            continue;

                        if (!layerStacks.TryGetValue(layer, out Stack<UIBase> stack))
                            continue;

                        int count = stack.Count;
                        totalStacked += count;
                        
                        if (count == 0 && !m_stackFoldouts[layer])
                            continue; // Skip empty stacks unless expanded

                        EditorGUILayout.BeginHorizontal();
                        
                        // Color code based on stack count
                        Color originalColor = GUI.backgroundColor;
                        if (count > 0)
                        {
                            GUI.backgroundColor = new Color(0.6f, 0.4f, 0.2f, 0.3f);
                        }
                        
                        string stackLabel = $"{layer} Stack [{count} stacked]";
                        m_stackFoldouts[layer] = EditorGUILayout.Foldout(m_stackFoldouts[layer], stackLabel, true);
                        
                        GUI.backgroundColor = originalColor;
                        EditorGUILayout.EndHorizontal();

                        if (m_stackFoldouts[layer] && count > 0)
                        {
                            EditorGUI.indentLevel++;
                            
                            // Convert stack to array for display (without modifying the stack)
                            UIBase[] stackArray = stack.ToArray();
                            for (int i = 0; i < stackArray.Length; i++)
                            {
                                UIBase ui = stackArray[i];
                                if (ui != null)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    GUILayout.Label($"[{i}]", GUILayout.Width(30));
                                    DrawUIItem(ui, false);
                                    EditorGUILayout.EndHorizontal();
                                }
                            }
                            
                            EditorGUI.indentLevel--;
                        }
                    }
                    
                    if (totalStacked == 0)
                    {
                        GUILayout.Label("No stacked UIs", EditorStyles.centeredGreyMiniLabel);
                    }
                }
                else
                {
                    GUILayout.Label("Unable to access layer stacks", EditorStyles.centeredGreyMiniLabel);
                }

                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(5);
        }

        private void DrawUIHistory()
        {
            m_historyFoldout = EditorGUILayout.Foldout(m_historyFoldout, "UI History (Last Shown)", true, EditorStyles.foldoutHeader);
            
            if (m_historyFoldout)
            {
                EditorGUILayout.BeginVertical(m_boxStyle);
                
                List<UIBase> history = UIManager.Instance.uiLastShownScreens;
                List<UIBase> filteredHistory = GetFilteredUIs(history);
                
                if (filteredHistory.Count == 0)
                {
                    GUILayout.Label("No UI history" + (HasActiveFilters() ? " (matching filters)" : ""), EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    m_historyScrollPosition = EditorGUILayout.BeginScrollView(m_historyScrollPosition, GUILayout.MaxHeight(150));
                    
                    // Show in reverse order (most recent first)
                    for (int i = filteredHistory.Count - 1; i >= 0; i--)
                    {
                        UIBase ui = filteredHistory[i];
                        if (ui != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label($"[{filteredHistory.Count - i}]", GUILayout.Width(30));
                            DrawUIItem(ui, false);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space(5);
        }

        private void DrawUIItem(UIBase ui, bool showActions)
        {
            if (ui == null)
            {
                GUILayout.Label("(Destroyed)", m_hiddenStyle);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            
            // Status indicator
            GUIStyle statusStyle = ui.IsVisible ? m_visibleStyle : m_hiddenStyle;
            string statusIcon = ui.IsVisible ? "●" : "○";
            GUILayout.Label(statusIcon, statusStyle, GUILayout.Width(15));
            
            // Name with click to select
            string uiName = ui.gameObject.name;
            if (GUILayout.Button(uiName, EditorStyles.linkLabel))
            {
                Selection.activeGameObject = ui.gameObject;
                EditorGUIUtility.PingObject(ui.gameObject);
            }
            
            GUILayout.FlexibleSpace();
            
            // Layer badge
            GUILayout.Label($"[{ui.Layer}]", EditorStyles.miniLabel, GUILayout.Width(70));
            
            // Sorting order
            GUILayout.Label($"SO: {ui.CurrentSortingOrder}", EditorStyles.miniLabel, GUILayout.Width(60));
            
            // Stackable indicator
            if (ui.isAllowedToStack)
            {
                GUILayout.Label("⇅", EditorStyles.miniLabel, GUILayout.Width(15));
            }
            
            // Action buttons
            if (showActions && Application.isPlaying)
            {
                if (ui.IsVisible)
                {
                    if (GUILayout.Button("Hide", EditorStyles.miniButton, GUILayout.Width(40)))
                    {
                        UIManager.Instance.HideUI(ui);
                    }
                }
                else
                {
                    if (GUILayout.Button("Show", EditorStyles.miniButton, GUILayout.Width(40)))
                    {
                        UIManager.Instance.ShowUI(ui);
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettings()
        {
            EditorGUILayout.Space(10);
            m_settingsFoldout = EditorGUILayout.Foldout(m_settingsFoldout, "Settings", true, EditorStyles.foldoutHeader);
            
            if (m_settingsFoldout)
            {
                EditorGUILayout.BeginVertical(m_boxStyle);
                
                m_autoRefresh = EditorGUILayout.Toggle("Auto Refresh", m_autoRefresh);
                
                using (new EditorGUI.DisabledScope(!m_autoRefresh))
                {
                    m_refreshInterval = EditorGUILayout.Slider("Refresh Interval", m_refreshInterval, 0.1f, 2f);
                }

                EditorGUILayout.Space(5);
                
                // Quick actions
                EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                
                using (new EditorGUI.DisabledScope(!Application.isPlaying || UIManager.Instance == null))
                {
                    if (GUILayout.Button("Hide All UIs"))
                    {
                        UIManager.Instance.HideAllUI();
                    }
                    
                    if (GUILayout.Button("Clear All Stacks"))
                    {
                        UIManager.Instance.ClearAllStacks();
                    }
                    
                    if (GUILayout.Button("Clear All Data"))
                    {
                        UIManager.ClearAllList_s();
                    }
                }
                
                EditorGUILayout.EndHorizontal();

                // Expand/Collapse all
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Expand All Layers"))
                {
                    foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
                    {
                        m_layerFoldouts[layer] = true;
                        m_stackFoldouts[layer] = true;
                    }
                    m_activeUIFoldout = true;
                    m_layerMapFoldout = true;
                    m_stacksFoldout = true;
                    m_historyFoldout = true;
                }
                
                if (GUILayout.Button("Collapse All Layers"))
                {
                    foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
                    {
                        m_layerFoldouts[layer] = false;
                        m_stackFoldouts[layer] = false;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
        }

        #region Helper Methods

        private Dictionary<UILayer, List<UIBase>> GetLayerUIMap()
        {
            if (UIManager.Instance == null || m_layerUIMapField == null)
                return null;
            
            return m_layerUIMapField.GetValue(UIManager.Instance) as Dictionary<UILayer, List<UIBase>>;
        }

        private Dictionary<UILayer, Stack<UIBase>> GetLayerStacks()
        {
            if (UIManager.Instance == null || m_layerStacksField == null)
                return null;
            
            return m_layerStacksField.GetValue(UIManager.Instance) as Dictionary<UILayer, Stack<UIBase>>;
        }

        private List<UIBase> GetFilteredUIs(List<UIBase> uis)
        {
            if (uis == null) return new List<UIBase>();
            
            List<UIBase> filtered = new List<UIBase>();
            
            foreach (UIBase ui in uis)
            {
                if (ui == null) continue;
                
                // Apply search filter using cached lowercase for efficiency
                if (!string.IsNullOrEmpty(m_searchFilter))
                {
                    if (!ui.gameObject.name.ToLower().Contains(m_searchFilterLower))
                        continue;
                }
                
                // Apply layer filter
                if (m_layerFilter.HasValue && ui.Layer != m_layerFilter.Value)
                    continue;
                
                // Apply visibility filter
                if (m_showOnlyVisible && !ui.IsVisible)
                    continue;
                
                filtered.Add(ui);
            }
            
            return filtered;
        }

        private bool HasActiveFilters()
        {
            return !string.IsNullOrEmpty(m_searchFilter) || m_layerFilter.HasValue || m_showOnlyVisible;
        }

        #endregion
    }
}
#endif
