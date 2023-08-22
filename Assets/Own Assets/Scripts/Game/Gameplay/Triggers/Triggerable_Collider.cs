using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay.Triggers
{
    public class Triggerable_Collider : Triggerable
    {
        public UnityEvent<bool> E_OnSwitchChanged = new();

        private NetworkVariable<bool> _isActive = new();

        /// <summary>
        /// Listen to networkvariable changes.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            _isActive.OnValueChanged += OnValueChanged;
        }

        /// <summary>
        /// Remove listener.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            _isActive.OnValueChanged -= OnValueChanged;
        }

        /// <summary>
        /// Activate or deactivates based on given bool isActive.
        /// This is executed on both server and client.
        /// A switchbox will destroy itself if it is not on the server.
        /// </summary>
        /// <param name="wasActive">Previous state.</param>
        /// <param name="isActive">Current active state.</param>
        private void OnValueChanged(bool wasActive, bool isActive)
        {
            if (isActive)
            {
                Activate();
            }
            else
            {
                Deactivate();
            }
            E_OnSwitchChanged.Invoke(isActive);
        }

        /// <summary>
        /// On Trigger enter change active value to true.
        /// </summary>
        /// <param name="other">Collider entering the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (IsServer && IsLocalPlayer)
            {
                _isActive.Value = true;
                return;
            }

            OnSwitchChangedServerRpc(true);
        }

        /// <summary>
        /// On Trigger exit change active value to false.
        /// </summary>
        /// <param name="other">Collider exiting the trigger.</param>
        private void OnTriggerExit(Collider other)
        {
            if (IsServer && IsLocalPlayer)
            {
                _isActive.Value = false;
                return;
            }

            OnSwitchChangedServerRpc(false);
        }

        /// <summary>
        /// Server is the only one allowed to set the value. But everyone is able to call this method.
        /// </summary>
        /// <param name="isActive">New isActive value.</param>
        [ServerRpc(RequireOwnership = false)]
        public void OnSwitchChangedServerRpc(bool isActive)
        {
            _isActive.Value = isActive;
        }
    }
}