using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay.Triggers
{
    public class Triggerable_Collider : Triggerable
    {
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
        /// This is executed on both server and client so that scripts relying on events can work their magic.
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
        }

        /// <summary>
        /// On Trigger enter change active value to true.
        /// </summary>
        /// <param name="other">Collider entering the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            OnSwitchChanged(true);
        }

        /// <summary>
        /// On Trigger exit change active value to false.
        /// </summary>
        /// <param name="other">Collider exiting the trigger.</param>
        private void OnTriggerExit(Collider other)
        {
            OnSwitchChanged(false);
        }

        /// <summary>
        /// Change the active value based on the given bool.
        /// </summary>
        /// <param name="active">Current active state.</param>
        private void OnSwitchChanged(bool active)
        {
            if (IsServer && IsLocalPlayer)
            {
                _isActive.Value = active;
                return;
            }

            OnSwitchChangedServerRpc(active);
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