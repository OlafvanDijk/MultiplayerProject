using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using Unity.Netcode;

public class Switchbox : NetworkBehaviour
{
    [SerializeField] private bool _triggerOnce;
    [SerializeField] private List<Triggerable> _triggerables;

    public UnityEvent E_TriggerActions = new();

    private Dictionary<Triggerable, bool> _activatedTriggers = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            Destroy(this);
            return;
        }

        if (_triggerables.Count == 0)
        {
            Debug.LogError($"No triggers on {gameObject.name}", gameObject);
            Destroy(this);
            return;
        }

        foreach (Triggerable triggerable in _triggerables)
        {
            _activatedTriggers.Add(triggerable, false);
            triggerable.E_Activate.AddListener((active) => OnTriggerValueChange(triggerable, active));
        }
    }

    private void OnTriggerValueChange(Triggerable triggerable, bool active)
    {
        _activatedTriggers[triggerable] = active;
        if (!_activatedTriggers.Any(t => t.Value == false))
            Trigger();
    }

    private void Trigger()
    {
        E_TriggerActions.Invoke();
        if (_triggerOnce == true)
        {
            CleanUp();
        }
    }

    public override void OnDestroy()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        foreach (Triggerable triggerable in _triggerables)
        {
            triggerable.E_Activate.RemoveListener((active) => OnTriggerValueChange(triggerable, active));
        }
        _triggerables.Clear();
        _activatedTriggers.Clear();
        Destroy(this);
    }
}
