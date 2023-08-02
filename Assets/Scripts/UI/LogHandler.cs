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
        bool error = type == LogType.Error;
        if (error)
            message = $"<color=red>Error!!</color> {message}\n{stackTrace}";
        Chat.E_SendMessage.Invoke(message, false, error);
    }
}
