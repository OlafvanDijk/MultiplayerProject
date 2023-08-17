using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using Sirenix.OdinInspector;

public class PlayerName : NetworkBehaviour
{
    [SerializeField] private Transform _canvas;
    [SerializeField] private TextMeshProUGUI _playerNameField;

    [SerializeField] private bool _ignoreCamera;
    [SerializeField] private LayerMask _ignoreCameraMask;

    [SerializeField] private LookAt _lookAt;
    [SerializeField] private bool _destroyLookAtIfPlayer = true;

    private NetworkVariable<FixedString64Bytes> _playerName = new();

    /// <summary>
    /// Sets up the player name above a player's character.
    /// Sents updates to the server to change the playername when the player has changed their name.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            if(_ignoreCamera)
                Helper.SetLayerMask(_canvas, _ignoreCameraMask);

            string name = PlayerInfoManager.Instance.Name;
            _playerNameField.text = name;
            SetNameServerRpc(name);
            PlayerInfoManager.Instance.E_OnNameChange.AddListener((newName) => SetNameServerRpc(newName));

            if(_lookAt && _destroyLookAtIfPlayer)
                Destroy(_lookAt);
        }
        else 
        {
            if (_playerName.Value.ToString() != string.Empty)
            {
                _playerNameField.text = _playerName.Value.ToString();
            }
            _playerName.OnValueChanged += NameChanged;
        }
    }

    /// <summary>
    /// Remove listeners.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (IsLocalPlayer)
        {
            PlayerInfoManager.Instance.E_OnNameChange.RemoveListener((newName) => SetNameServerRpc(newName));
        } else 
        {
            _playerName.OnValueChanged -= NameChanged;
        }
    }

    /// <summary>
    /// Sets the namefield with the given name.
    /// </summary>
    /// <param name="previousName">Previous player name.</param>
    /// <param name="newName">New player name.</param>
    private void NameChanged(FixedString64Bytes previousName, FixedString64Bytes newName)
    {
        _playerNameField.text = newName.ToString();
    }

    /// <summary>
    /// Change the player's name networkvariable so all the clients will get the updated name.
    /// </summary>
    /// <param name="name">New player name.</param>
    [ServerRpc(RequireOwnership = false)]
    private void SetNameServerRpc(string name)
    {
        _playerName.Value = name;
    }
}
