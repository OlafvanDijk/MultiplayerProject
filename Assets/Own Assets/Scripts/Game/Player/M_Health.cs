using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Utility;

public class M_Health : NetworkBehaviour
{
    [Tooltip("Maximum amount of health")]
    public float MaxHealth = 10f;

    [Tooltip("Health ratio at which the critical health vignette starts appearing")]
    public float CriticalHealthRatio = 0.3f;

    public UnityEvent<float> OnDamaged = new();
    public UnityEvent<float> OnHealed = new();
    public UnityEvent OnDie = new();

    public float CurrentHealth => _currentHealth.Value;
    public bool Invincible { get; set; }
    public bool CanPickup() => CurrentHealth < MaxHealth;

    public float GetRatio() => CurrentHealth / MaxHealth;
    public bool IsCritical() => GetRatio() <= CriticalHealthRatio;

    private bool _isDead;

    private NetworkVariable<float> _currentHealth;

    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
            SetHealthServerRpc(MaxHealth);
    }

    public void Heal(float healAmount)
    {
        HealServerRpc(healAmount, Helper.CreateServerRpcParams(OwnerClientId));
    }

    public void TakeDamage(float damage)
    {
        TakeDamageServerRpc(damage, Helper.CreateServerRpcParams(OwnerClientId));
    }

    public void Kill()
    {
        KillServerRpc(Helper.CreateServerRpcParams(OwnerClientId));
    }

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

    [ServerRpc]
    private void SetHealthServerRpc(float health)
    {
        _currentHealth.Value = health;
    }

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

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage, ServerRpcParams serverRpcParams)
    {
        if (Invincible)
            return;

        float healthBefore = CurrentHealth;
        float currentHealth = CurrentHealth - damage;;
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
        _currentHealth.Value = currentHealth;

        float trueDamageAmount = healthBefore - CurrentHealth;
        if (trueDamageAmount > 0f)
            DamagedClientRpc(trueDamageAmount, Helper.CreateClientRpcParams(serverRpcParams));

        HandleDeath(serverRpcParams);
    }

    [ServerRpc(RequireOwnership = false)]
    private void KillServerRpc(ServerRpcParams serverRpcParams)
    {
        _currentHealth.Value = 0f;
        DamagedClientRpc(MaxHealth, Helper.CreateClientRpcParams(serverRpcParams));

        HandleDeath(serverRpcParams);
    }

    [ClientRpc]
    private void HealClientRpc(float trueHealAmount, ClientRpcParams clientRpcParams)
    {
         OnHealed.Invoke(trueHealAmount);
    }

    [ClientRpc]
    private void DamagedClientRpc(float damageAmount, ClientRpcParams clientRpcParams)
    {
        OnDamaged.Invoke(damageAmount);
    }

    [ClientRpc]
    private void OnDeathClientRpc(ClientRpcParams clientRpcParams)
    {
        _isDead = true;
        OnDie.Invoke();
    }
}
