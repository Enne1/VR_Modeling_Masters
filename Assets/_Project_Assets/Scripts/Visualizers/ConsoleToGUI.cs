using UnityEngine;
using TMPro;

public class ConsoleToGUI : MonoBehaviour
{
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

    public void ClearLog()
    {
        myLog = "";
        if (debugText != null)
        {
            debugText.text = "";
        }
    }
}