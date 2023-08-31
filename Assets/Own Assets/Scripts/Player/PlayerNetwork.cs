using Game.Data;
using Game.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Utility;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private LayerMask _layerMask;

    [SerializeField] private PrefabCollection _characters;

    [SerializeField] private NetworkObject _networkObject;

    [SerializeField] private ToDestroy _toDestroyWhenPlayer;
    [SerializeField] private ToDestroy _toDestroyWhenNotPlayer;

    private GameObject _character;
    private Dictionary<ulong, int> _characterIndexesServer = new();

    [Serializable]
    public class ToDestroy
    {
        public List<Component> ComponentsToDestroy = new();
        public List<GameObject> GameObjectsToDestroy = new();
    }

    public static PlayerInput Player;

    /// <summary>
    /// Set eye position in the PlayerInfo.
    /// Set layer of eyes as to not be seen by the camera.
    /// Destroys all objects and components that are not needed on the player or fake players side.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            Player = FindAnyObjectByType<PlayerInput>();
            PlayerInfoManager playerInfoManager = PlayerInfoManager.Instance;
            playerInfoManager.PlayerID = NetworkManager.Singleton.LocalClientId;

            int index = playerInfoManager.CharacterIndex;
            _character = Instantiate(_characters.Prefabs[index], transform);
            HidePlayer(_character);
            SpawnPlayerServerRpc(playerInfoManager.PlayerID, index);
            DestoryPlayerItems(_toDestroyWhenPlayer);
        } else
        {
            if (_character == null)
                SpawnPlayerServerRpc(OwnerClientId);

            DestoryPlayerItems(_toDestroyWhenNotPlayer);
        }
    }

    /// <summary>
    /// Set the layer of the player objects so they can't be seen by the camera.
    /// </summary>
    /// <param name="character"></param>
    private void HidePlayer(GameObject character)
    {
        Transform eyesParent = null;
        foreach (Transform child in character.transform)
        {
            if (child.name == "Eyes")
            {
                eyesParent = child;
                break;
            }
        }
        if (eyesParent)
        {
            PlayerInfoManager.Instance.EyesPosition = eyesParent;
            Helper.SetLayerMask(eyesParent, _layerMask);
        }
    }

    /// <summary>
    /// Destroys everything in the ComponentsToDestroy and GameObjectsToDestroy lists of the given ToDestroy object.
    /// </summary>
    /// <param name="toDestroy">Data object holding the lists of components and gameobjects to be destroyed.</param>
    private void DestoryPlayerItems(ToDestroy toDestroy)
    {
        List<Component> components = toDestroy.ComponentsToDestroy;
        List<GameObject> gameObjects = toDestroy.GameObjectsToDestroy;

        if (components.Count > 0)
        {
            foreach (Component component in components.ToList())
            {
                DestroyImmediate(component);
            }
        }
        if (gameObjects.Count > 0)
        {
            foreach (GameObject gameObject in gameObjects.ToList())
            {
                DestroyImmediate(gameObject);
            }
        }
    }

    /// <summary>
    /// Waits until the clientID can be found in the dictionary before telling the replica's on the clients what to spawn.
    /// </summary>
    /// <param name="clientID">ID of the player to spawn the character for on the replica's.</param>
    /// <returns></returns>
    private IEnumerator WaitUntilInDict(ulong clientID)
    {
        yield return new WaitUntil(() => _characterIndexesServer.ContainsKey(clientID));
        SpawnPlayerClientRpc(clientID, _characterIndexesServer[clientID]);
    }

    #region Rpc's
    /// <summary>
    /// This method is only being called by the actual players.
    /// Spawns the chosen character on the replica's of the player.
    /// </summary>
    /// <param name="clientID">ID of the player to spawn the character for on the replica's.</param>
    /// <param name="characterIndex">Index of the character to spawn.</param>
    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ulong clientID, int characterIndex)
    {
        _characterIndexesServer.Add(clientID, characterIndex);
        SpawnPlayerClientRpc(clientID, characterIndex);
    }

    /// <summary>
    /// This method is only being called by the replica's.
    /// Spawns the chosen character on the replica's of the player as soon as the client has appeared in the dictionary.
    /// The actual player needs to call the SpawnPlayerServerRpc that adds it's choice to the dictionary first.
    /// </summary>
    /// <param name="clientID">ID of the player to spawn the character for.</param>
    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ulong clientID)
    {
        StartCoroutine(WaitUntilInDict(clientID));
    }

    /// <summary>
    /// Spawns the chosen character if the clientID matches the OwnerID and this is not the local player.
    /// Using ClientParams to filter out the right client to send this to did not work.
    /// </summary>
    /// <param name="clientID">ID of the player to spawn the character for.</param>
    /// <param name="characterIndex">Index of the character to spawn.</param>
    [ClientRpc]
    private void SpawnPlayerClientRpc(ulong clientID, int characterIndex)
    {
        if (IsLocalPlayer || OwnerClientId != clientID || _character != null)
            return;
        _character = Instantiate(_characters.Prefabs[characterIndex], transform);
    }
    #endregion
}
