using UnityEngine;
using TMPro; // Required for using TextMeshProUGUI

public class ConsoleToGUI : MonoBehaviour
{
    static string myLog = "";
    private string output;
    private string stack;

    public TextMeshProUGUI debugText; // Reference to the TextMeshProUGUI component

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
        myLog = output + "\n" + myLog; // Separate logs by newlines for readability
        if (myLog.Length > 5000)
        {
            myLog = myLog.Substring(0, 4000);
        }

        // Update the TextMeshProUGUI text to show the current log
        if (debugText != null)
        {
            debugText.text = myLog;
        }
    }

    // Optional: A method to clear the log if desired (you could call this from a button)
    public void ClearLog()
    {
        myLog = "";
        if (debugText != null)
        {
            debugText.text = "";
        }
    }
}