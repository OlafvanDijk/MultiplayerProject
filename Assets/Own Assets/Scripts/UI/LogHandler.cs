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
        bool error = type == LogType.Error || type == LogType.Exception;
        if (error)
            message = $"<color=red>Error!!</color> {message}\n{stackTrace}";
        EMessageType messageType = error ? EMessageType.Error : EMessageType.Log;
        Chat.E_SendMessage.Invoke(message, messageType);
    }
}
