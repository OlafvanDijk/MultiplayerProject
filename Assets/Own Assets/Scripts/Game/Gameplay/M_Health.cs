using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Utility;

namespace Game.Gameplay
{
    public class M_Health : NetworkBehaviour
    {
        [Tooltip("Maximum amount of health")]
        public float MaxHealth = 10f;

        [Tooltip("Health ratio at which the critical health vignette starts appearing")]
        public float CriticalHealthRatio = 0.3f;

        public UnityEvent<float> E_OnHealthChanged = new();
        public UnityEvent<float> E_OnDamaged = new();
        public UnityEvent<float> E_OnHealed = new();
        public UnityEvent E_OnDie = new();

        public float CurrentHealth => _currentHealth.Value;
        public bool Invincible { get; set; }
        public bool CanPickup() => CurrentHealth < MaxHealth;
        public float GetRatio() => CurrentHealth / MaxHealth;
        public bool IsCritical() => GetRatio() <= CriticalHealthRatio;

        private bool _isDead;

        private NetworkVariable<float> _currentHealth = new();

        #region Network Spawn/Despawn
        /// <summary>
        /// Set current health to max and adds a listener to the health change.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            _currentHealth.OnValueChanged += HealthChanged;
            if (IsLocalPlayer)
                SetHealthServerRpc(MaxHealth);
        }

        /// <summary>
        /// Removes health listener.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            _currentHealth.OnValueChanged -= HealthChanged;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Let the server handle the healing process.
        /// </summary>
        /// <param name="healAmount"></param>
        public void Heal(float healAmount)
        {
            HealServerRpc(healAmount, Helper.CreateServerRpcParams(OwnerClientId));
        }

        /// <summary>
        /// Let the server handle the damage.
        /// </summary>
        /// <param name="damage"></param>
        public void TakeDamage(float damage)
        {
            TakeDamageServerRpc(damage, Helper.CreateServerRpcParams(OwnerClientId));
        }

        /// <summary>
        /// Let the server know that it should kill this object.
        /// </summary>
        public void Kill()
        {
            KillServerRpc(Helper.CreateServerRpcParams(OwnerClientId));
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Checks if this object is dead. If so it will update the client.
        /// This method should only be called on the server.
        /// </summary>
        /// <param name="serverRpcParams"></param>
        private void HandleDeath(ServerRpcParams serverRpcParams)
        {
            if (_isDead)
                return;

            if (CurrentHealth <= 0f)
            {
                _isDead = true;
                OnDeathClientRpc(Helper.CreateClientRpcParams(serverRpcParams));
            }
        }

        /// <summary>
        /// Invokes the E_OnHealthChanged event with the new health value.
        /// </summary>
        /// <param name="previousValue"></param>
        /// <param name="newValue"></param>
        private void HealthChanged(float previousValue, float newValue)
        {
            E_OnHealthChanged.Invoke(newValue);
        }
        #endregion

        #region Rpc's

        #region Server
        /// <summary>
        /// Set's the current health and updates the clients.
        /// </summary>
        /// <param name="health"></param>
        [ServerRpc]
        private void SetHealthServerRpc(float health)
        {
            _currentHealth.Value = health;
        }

        /// <summary>
        /// Add heal amount to health and clamp it to the max health.
        /// Updates clients afterwards with the true heal amount.
        /// </summary>
        /// <param name="healAmount"></param>
        /// <param name="serverRpcParams"></param>
        [ServerRpc(RequireOwnership = false)]
        private void HealServerRpc(float healAmount, ServerRpcParams serverRpcParams)
        {
            float healthBefore = CurrentHealth;
            float currentHealth = CurrentHealth + healAmount;
            currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
            _currentHealth.Value = currentHealth;

            float trueHealAmount = CurrentHealth - healthBefore;
            if (trueHealAmount > 0f)
                HealClientRpc(trueHealAmount, Helper.CreateClientRpcParams(serverRpcParams));
        }

        /// <summary>
        /// Substracts the damage from the current health and updates the client on the true damage dealt.
        /// Also checks if the object has died
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="serverRpcParams"></param>
        [ServerRpc(RequireOwnership = false)]
        private void TakeDamageServerRpc(float damage, ServerRpcParams serverRpcParams)
        {
            if (Invincible)
                return;

            float healthBefore = CurrentHealth;
            float currentHealth = CurrentHealth - damage; ;
            currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
            _currentHealth.Value = currentHealth;

            float trueDamageAmount = healthBefore - CurrentHealth;
            if (trueDamageAmount > 0f)
                DamagedClientRpc(trueDamageAmount, Helper.CreateClientRpcParams(serverRpcParams));

            HandleDeath(serverRpcParams);
        }

        /// <summary>
        /// Kills the object by dealing the max health as damage.
        /// Then calls the HandleDeath method.
        /// </summary>
        /// <param name="serverRpcParams"></param>
        [ServerRpc(RequireOwnership = false)]
        private void KillServerRpc(ServerRpcParams serverRpcParams)
        {
            _currentHealth.Value = 0f;
            DamagedClientRpc(MaxHealth, Helper.CreateClientRpcParams(serverRpcParams));

            HandleDeath(serverRpcParams);
        }
        #endregion

        #region Client
        /// <summary>
        /// Calls the E_OnHealed event so that listeners can be updated.
        /// </summary>
        /// <param name="trueHealAmount"></param>
        /// <param name="clientRpcParams"></param>
        [ClientRpc]
        private void HealClientRpc(float trueHealAmount, ClientRpcParams clientRpcParams)
        {
            E_OnHealed.Invoke(trueHealAmount);
        }

        /// <summary>
        /// Calls the E_OnDamaged event so that listeners can be updated.
        /// </summary>
        /// <param name="damageAmount"></param>
        /// <param name="clientRpcParams"></param>
        [ClientRpc]
        private void DamagedClientRpc(float damageAmount, ClientRpcParams clientRpcParams)
        {
            E_OnDamaged.Invoke(damageAmount);
        }

        /// <summary>
        /// Set isDead and calls the E_OnDie event.
        /// </summary>
        /// <param name="clientRpcParams"></param>
        [ClientRpc]
        private void OnDeathClientRpc(ClientRpcParams clientRpcParams)
        {
            _isDead = true;
            E_OnDie.Invoke();
        }
        #endregion
        #endregion
    }

}