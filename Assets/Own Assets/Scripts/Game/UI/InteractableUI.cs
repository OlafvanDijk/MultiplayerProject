using Game.Gameplay;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class InteractableUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _interactField;
        [SerializeField] private GameObject _parent;

        private void Awake()
        {
            InteractableDetector.E_ShowInteractable.AddListener(ShowInteractable);
            InteractableDetector.E_HideInteractable.AddListener(HideInteractable);
        }

        private void ShowInteractable(string interactText)
        {
            _interactField.text = interactText;
            _parent.SetActive(true);
        }

        private void HideInteractable()
        {
            _parent.SetActive(false);
        }
    }
}
