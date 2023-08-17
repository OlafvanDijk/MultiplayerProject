using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Game
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _codeField;

        void Awake()
        {
            _codeField.text = $"Lobby Code: {GameLobbyManager.Instance.GetLobbyCode()}";
        }
    }
}
