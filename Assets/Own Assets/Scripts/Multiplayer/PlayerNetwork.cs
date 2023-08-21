using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Linq;
using System;
using Game;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private Transform _eyesParent;

    [SerializeField] private NetworkObject _networkObject;

    [SerializeField] private ToDestroy _toDestroyWhenPlayer;
    [SerializeField] private ToDestroy _toDestroyWhenNotPlayer;

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
        if (IsClient && IsLocalPlayer)
        {
            PlayerInfoManager.Instance.EyesPosition = _eyesParent;
            SetEyesLayer(_eyesParent);
            DestoryPlayerItems(_toDestroyWhenPlayer);
        } else
        {
            DestoryPlayerItems(_toDestroyWhenNotPlayer);
        }
    }
    
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

    private void SetEyesLayer(Transform parent)
    {
        Helper.SetLayerMask(parent, _layerMask);
    }
}
