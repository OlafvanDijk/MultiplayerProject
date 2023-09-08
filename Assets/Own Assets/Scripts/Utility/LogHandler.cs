using Game.UI.Messaging;
using UnityEngine;

namespace Utility
{
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

        /// <summary>
        /// Tracks logs and sends them to the chat with the correct message type.
        /// </summary>
        /// <param name="LogString"></param>
        /// <param name="stackTrace"></param>
        /// <param name="type"></param>
        private void HandleLog(string LogString, string stackTrace, LogType type)
        {
            string message = LogString;
            bool error = type == LogType.Error || type == LogType.Exception;
            if (error)
                message = $"<color=red>Error!!</color> {message}\n{stackTrace}";
            EMessageType messageType = error ? EMessageType.Error : EMessageType.Log;
            MessageHandler.E_SendMessage.Invoke(message, messageType);
        }
    }
}