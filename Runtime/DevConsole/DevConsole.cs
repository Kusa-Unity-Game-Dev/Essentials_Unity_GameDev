using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class DevConsole : MonoBehaviour
{
    public static DevConsole Instance { get; private set; } // Singleton instance

    public GameObject consolePanel;
    public TMP_Text logText;
    public TMP_InputField inputField;
    public Button m_cancelBtn;
    public Button m_execute;

    private Dictionary<string, Func<string[], string>> commands = new Dictionary<string, Func<string[], string>>();

    #region Essential
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    private void Start()
    {
        consolePanel.SetActive(false); // Hide console initially
        RegisterDefaultCommands();

        m_cancelBtn.onClick.AddListener(ToggleConsole);
        m_execute.onClick.AddListener(executeCommandWithInput);
    }
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote)) // Toggle console with `
        {
            ToggleConsole();
        }
    }
#endif
    
    #endregion

    private void ToggleConsole()
    {
        consolePanel.SetActive(!consolePanel.activeSelf);
        if (consolePanel.activeSelf) inputField.Select();
    }

    private void executeCommandWithInput()
    {
        ExecuteCommand(inputField.text);
        inputField.text = "";
    }

    public void ExecuteCommand(string input)
    {
        string[] args = input.Split(' ');
        string command = args[0].ToLower();

        if (commands.ContainsKey(command))
        {
            string result = commands[command].Invoke(args);
            Log($"Result : {result}");
        }
        else
        {
            Log($"Unknown command: {command}");
        }

        inputField.text = "";
        inputField.Select();
    }

    /// <summary>
    /// Registers a new command dynamically.
    /// </summary>
    public void RegisterCommand(string command, Func<string[], string> action)
    {
        if (!commands.ContainsKey(command.ToLower()))
        {
            commands.Add(command.ToLower(), action);
            Log($"Command '{command}' registered.");
        }
        else
        {
            Log($"Command '{command}' already exists.");
        }
    }

    /// <summary>
    /// Removes a registered command.
    /// </summary>
    public void UnregisterCommand(string command)
    {
        if (commands.ContainsKey(command.ToLower()))
        {
            commands.Remove(command.ToLower());
            Log($"Command '{command}' removed.");
        }
        else
        {
            Log($"Command '{command}' not found.");
        }
    }

    private void RegisterDefaultCommands()
    {

    }

    private void Log(string message)
    {
        logText.text += message + "\n";
    }
}
