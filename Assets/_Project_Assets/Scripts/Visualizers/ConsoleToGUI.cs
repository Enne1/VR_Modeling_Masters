using UnityEngine;
using TMPro;

public class ConsoleToGUI : MonoBehaviour
{
    // Script ONLY used for testing of program
    
    static string myLog = "";
    private string output;
    private string stack;

    public TextMeshProUGUI debugText;

    void OnEnable()
    {
        Application.logMessageReceived += Log;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
    }

    /// <summary>
    /// Takes the console seen in the Unity Editor, and saves the outputs, to show in the text field at runtime 
    /// </summary>
    public void Log(string logString, string stackTrace, LogType type)
    {
        output = logString;
        stack = stackTrace;

        // Add log type prefix
        string prefix = $"[{type}]";
        string logEntry = $"{prefix} {output}";

        // Only include stack trace for errors and exceptions
        if (type == LogType.Error || type == LogType.Exception)
        {
            logEntry += $"\n{stack}";
        }

        // Add to the top of the log
        myLog = logEntry + "\n\n" + myLog;

        // Truncate if too long
        if (myLog.Length > 10000)
        {
            myLog = myLog.Substring(0, 8000);
        }

        if (debugText != null)
        {
            debugText.text = myLog;
        }
    }

    /// <summary>
    /// Removes all content of the log
    /// </summary>
    public void ClearLog()
    {
        myLog = "";
        if (debugText != null)
        {
            debugText.text = "";
        }
    }
}