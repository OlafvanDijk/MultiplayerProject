using Unity.Netcode;
using UnityEngine;
using Utility;

namespace Game.Gameplay.Triggers
{
    public class Triggerable_Interactable : Triggerable
    {
        [SerializeField] private Interactable interactable;
        [SerializeField] private float _deactivationTime = 2.5f;

        public override void OnNetworkSpawn()
        {
            interactable.E_OnInteraction.AddListener(OnInteracted);
        }

        private void OnInteracted()
        {
            if (IsServer && IsLocalPlayer)
            {
                ActivateClientRpc(true);
                Timer.Instance.StartNewTimer(_deactivationTime, () => ActivateClientRpc(false));
            }
            else
            {
                ActivateServerRpc(true);
                Timer.Instance.StartNewTimer(_deactivationTime, () => ActivateServerRpc(false));
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ActivateServerRpc(bool activate)
        {
            ActivateClientRpc(activate);
        }

        [ClientRpc]
        private void ActivateClientRpc(bool activate)
        {
            if (activate)
                Activate();
            else
                Deactivate();
        }
    }
}

