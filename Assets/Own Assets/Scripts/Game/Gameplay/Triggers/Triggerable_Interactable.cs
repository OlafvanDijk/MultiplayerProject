using Unity.Netcode;
using UnityEngine;

namespace Game.Gameplay.Triggers
{
    public class Triggerable_Interactable : Triggerable
    {
        [SerializeField] private Interactable interactable;

        public override void OnNetworkSpawn()
        {
            interactable.E_OnInteraction.AddListener(OnInteracted);
        }

        private void OnInteracted()
        {
            if (IsServer && IsLocalPlayer)
                ActivateClientRpc();
            else
                ActivateServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ActivateServerRpc()
        {
            ActivateClientRpc();
        }

        [ClientRpc]
        private void ActivateClientRpc()
        {
            Activate();
        }
    }
}

