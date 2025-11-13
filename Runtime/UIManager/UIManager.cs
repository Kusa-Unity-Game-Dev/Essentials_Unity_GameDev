using System.Collections.Generic;
using UnityEngine;

namespace BB.Framework {

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public List<UIBase> uiScreens = new List<UIBase>();
    public List<UIBase> uiLastShownScreens = new List<UIBase>();
    public List<UIBase> ui_stackedScreens = new List<UIBase>();

    private const short LAST_UI_REMEBER_LIST = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        _onAwake();
    }

    private void Start()
    {
        _onStart();
        //FSM.AddListener_s(GameConstants.E__CLEARUI, privateClearAllData);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        _onDestroy();
        //Instance = null;
        //FSM.RemoveListener_s(GameConstants.E__CLEARUI, privateClearAllData);
    }

    public void RegisterUI(UIBase uiName)
    {
        if (!uiScreens.Contains(uiName))
        {
            uiScreens.Add(uiName);
            // Sort the list by sort order for easier management
            SortUIScreens();
        }

    }
    
    /// <summary>
    /// Sorts the UI screens list by their sort order (lowest to highest).
    /// </summary>
    private void SortUIScreens()
    {
        uiScreens.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
    }

    public void UnregisterUI(UIBase uiName, bool a_bringStackUI = false)
    {
        if (uiScreens.Contains(uiName))
        {
            uiScreens.Remove(uiName);
        }

        if (uiName.isAllowedToStack && !a_bringStackUI)
        {
            if (ui_stackedScreens.Count > 0)
            {
                ShowUI(ui_stackedScreens[ui_stackedScreens.Count - 1]);
                ui_stackedScreens.RemoveAt(ui_stackedScreens.Count - 1);
            }
        }

        // if stacking ready stack them
        if (a_bringStackUI)
        {
            ui_stackedScreens.Add(uiName);
        }

        //ass to last shown ui
        uiLastShownScreens.Add(uiName);

        if (uiLastShownScreens.Count >= LAST_UI_REMEBER_LIST)
            uiLastShownScreens.RemoveAt(0);

    }

    public void ShowUI(UIBase uiName, float a_delay = 0)
    {
        uiName._UIBaseShowUI(a_delay);
        RegisterUI(uiName);
        
    }

    public void HideUI(UIBase uiName)
    {
        UnregisterUI(uiName);
        uiName._UIBaseHideUI();
        
    }

    public void HideAllUI()
    {
        foreach (var ui in uiScreens)
        {
            HideUI(ui);
        }
    }
    
    /// <summary>
    /// Hides all UI elements in a specific layer.
    /// </summary>
    public void HideAllUIInLayer(UILayer layer)
    {
        for (int i = uiScreens.Count - 1; i >= 0; i--)
        {
            if (uiScreens[i].Layer == layer)
            {
                HideUI(uiScreens[i]);
            }
        }
    }
    
    /// <summary>
    /// Gets all visible UI elements in a specific layer.
    /// </summary>
    public List<UIBase> GetUIInLayer(UILayer layer)
    {
        List<UIBase> layerUIs = new List<UIBase>();
        foreach (var ui in uiScreens)
        {
            if (ui.Layer == layer)
            {
                layerUIs.Add(ui);
            }
        }
        return layerUIs;
    }
    
    /// <summary>
    /// Gets the topmost (highest priority) visible UI in a specific layer.
    /// </summary>
    public UIBase GetTopUIInLayer(UILayer layer)
    {
        UIBase topUI = null;
        int maxPriority = -1;
        
        foreach (var ui in uiScreens)
        {
            if (ui.Layer == layer && ui.LayerPriority > maxPriority)
            {
                topUI = ui;
                maxPriority = ui.LayerPriority;
            }
        }
        
        return topUI;
    }


    public static void ClearAllList_s()
    {
        Instance.uiScreens.Clear();
        Instance.uiLastShownScreens.Clear();
        Instance.ui_stackedScreens.Clear();
    }

    public void privateClearAllData(string game)
    {
        Instance.uiScreens.Clear();
        Instance.uiLastShownScreens.Clear();
        Instance.ui_stackedScreens.Clear();
    }
    
    //virtual Methods
    public virtual void _onAwake()
    {
        // Override this method in derived classes to add custom behavior
    }
    
    public virtual void _onStart()
    {
        // Override this method in derived classes to add custom behavior
    }
    
    public virtual void _onDestroy()
    {
        // Override this method in derived classes to add custom behavior
    }
    
}
}