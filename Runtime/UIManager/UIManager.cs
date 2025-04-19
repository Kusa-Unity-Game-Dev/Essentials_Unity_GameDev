using System.Collections.Generic;
using UnityEngine;

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
    }

    private void Start()
    {
        //FSM.AddListener_s(GameConstants.E__CLEARUI, privateClearAllData);
    }

    private void OnDestroy()
    {
        //Instance = null;
        //FSM.RemoveListener_s(GameConstants.E__CLEARUI, privateClearAllData);
    }

    public void RegisterUI(UIBase uiName)
    {
        if (!uiScreens.Contains(uiName))
        {
            uiScreens.Add(uiName);
        }

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
}
