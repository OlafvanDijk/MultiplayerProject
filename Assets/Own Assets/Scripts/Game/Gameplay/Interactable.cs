using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay
{
    public class Interactable : MonoBehaviour
    {
        public string InteractionText;

        public UnityEvent E_OnInteraction = new();

    }
}