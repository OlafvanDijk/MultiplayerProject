using Game.Managers;
using Game.UI.Messaging;
using System;
using System.Diagnostics;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;
using Utility;

namespace Game.UI.Messaging
{
    public class MessageHandler : NetworkBehaviour
    {
        public static UnityEvent<string, EMessageType> E_SendMessage = new();
        public static UnityEvent<string, EMessageType> E_MessageRecieved = new();

        private NetworkVariable<ChatMessage> _message = new();

        private PlayerInfoManager _playerInfoManager;

        public override void OnNetworkSpawn()
        {
            _message.OnValueChanged += OnMessageChanged;
            E_SendMessage.AddListener(SendMessage);
            _playerInfoManager = PlayerInfoManager.Instance;
        }

        public override void OnNetworkDespawn()
        {
            _message.OnValueChanged -= OnMessageChanged;
            E_SendMessage.RemoveListener(SendMessage);
        }

        private void SendMessage(string message, EMessageType messageType)
        {
            ChatMessage chatMessage = new ChatMessage();
            try
            {
                chatMessage.Message = $"{Helper.GetTimeFormatted()} {_playerInfoManager.Name}: {message}";
                chatMessage.MessageType = messageType;
            }
            catch (Exception)
            {
                //Message was too big so we don't log it
                //Otherwise we'll create an infinite loop as the log will also come through here.
                UnityEngine.Debug.Log("Message is too big");
                return;
            }

            SendMessageServerRpc(chatMessage);
        }

        private void OnMessageChanged(ChatMessage previousValue, ChatMessage newValue)
        {
            E_MessageRecieved.Invoke(newValue.Message.ToString(), newValue.MessageType);
        }

        /// <summary>
        /// Sets the message networkvariable value, updating all clients with the new message.
        /// </summary>
        /// <param name="chatMessage"></param>
        [ServerRpc(RequireOwnership = false)]
        private void SendMessageServerRpc(ChatMessage chatMessage)
        {
            _message.Value = chatMessage;
        }
    }
}