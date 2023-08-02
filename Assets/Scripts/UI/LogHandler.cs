using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogHandler : MonoBehaviour
{
    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string LogString, string stackTrace, LogType type)
    {
        string message = LogString;
        if (type == LogType.Error)
            message = $"<color=red>Error!!</color> {message}\n{stackTrace}";
        Chat.E_SendMessage.Invoke(message, false);
    }
}
