using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Utility;

namespace Game.Gameplay.Triggers
{
    public class Switchbox : NetworkBehaviour
    {
        [SerializeField] private bool _triggerOnce;

        [SerializeField] private bool _timed;
        [SerializeField] private float _maxTimeInSeconds = 2.5f;

        [SerializeField] private List<Triggerable> _triggerables;

        public UnityEvent E_TriggerActions = new();

        private bool _cleanedUp;
        private bool _withinTime;
        private Guid _timerGuid;

        private Dictionary<Triggerable, bool> _activatedTriggers = new();

        /// <summary>
        /// If this is the server then add listeners to all the triggerables.
        /// </summary>
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

        /// <summary>
        /// On Triggerable Value change check if switchbox should be triggered.
        /// Switchbox only triggers if all triggers have been triggered.
        /// </summary>
        /// <param name="triggerable"></param>
        /// <param name="active"></param>
        private void OnTriggerValueChange(Triggerable triggerable, bool active)
        {
            List<bool> activated = _activatedTriggers.Values.ToList().FindAll(v => v == true);
            int activatedCountbefore = activated.Count;

            _activatedTriggers[triggerable] = active;

            if (_timed)
            {
                if(activatedCountbefore == 0 && active)
                {
                    _timerGuid = Timer.Instance.StartNewTimer(_maxTimeInSeconds, () => { _withinTime = false; Debug.LogError("Time Up"); });
                } else if(activatedCountbefore == 1 && !active)
                {
                    if(_timerGuid != null)
                        Timer.Instance.AbortTimer(_timerGuid);
                    _withinTime = true;
                }
            }

            if (!_activatedTriggers.Any(t => t.Value == false) && (_timed == false || _withinTime))
                Trigger();
        }

        /// <summary>
        /// Trigger switchbox.
        /// Calls cleanup if switchbox can only trigger once.
        /// </summary>
        private void Trigger()
        {
            if (_timerGuid != null)
                Timer.Instance.AbortTimer(_timerGuid);

            E_TriggerActions.Invoke();
            if (_triggerOnce == true)
            {
                CleanUp();
            }
        }

        public override void OnDestroy()
        {
            if (!_cleanedUp)
                CleanUp();
        }

        /// <summary>
        /// Removes listeners, clears lists and destroys this component
        /// </summary>
        private void CleanUp()
        {
            foreach (Triggerable triggerable in _triggerables)
            {
                triggerable.E_Activate.RemoveListener((active) => OnTriggerValueChange(triggerable, active));
            }
            _triggerables.Clear();
            _activatedTriggers.Clear();
            _cleanedUp = true;
            Destroy(this);
        }
    }
}