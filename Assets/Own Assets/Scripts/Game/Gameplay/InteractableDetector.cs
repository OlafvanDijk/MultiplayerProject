using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    public class InteractableDetector : MonoBehaviour
    {

        [SerializeField] private Vector3 _offset;
        [SerializeField] private float _maxDistance;
        [SerializeField] private LayerMask _ignoreMask;

        [SerializeField] private InputActionReference _interactAction;

        public static UnityEvent<string> E_ShowInteractable = new();
        public static UnityEvent E_HideInteractable = new();


        private Interactable _currentInteractable;

        private void Awake()
        {
            _ignoreMask = ~_ignoreMask;
        }

        private void Update()
        {
            Ray ray = new Ray(transform.position + _offset, transform.forward);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, _maxDistance, _ignoreMask))
            {
                CheckHideInteractable();
                return;
            }

            if (hit.collider == null)
            {
                CheckHideInteractable();
                return;
            }

            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable == null)
            {
                CheckHideInteractable();
                return;
            }

            if (_interactAction.action.triggered)
                interactable.E_OnInteraction.Invoke();

            if (_currentInteractable == interactable)
                return;

            _currentInteractable = interactable;
            E_ShowInteractable.Invoke(_currentInteractable.InteractionText);
        }

        private void CheckHideInteractable()
        {
            if (!_currentInteractable)
                return;
            _currentInteractable = null;
            E_HideInteractable.Invoke();
        }
    }

}
