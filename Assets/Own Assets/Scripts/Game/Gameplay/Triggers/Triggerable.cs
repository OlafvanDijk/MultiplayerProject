using Unity.Netcode;
using UnityEngine.Events;

namespace Game.Gameplay.Triggers
{
    public class Triggerable : NetworkBehaviour
    {
        public UnityEvent<bool> E_Activate = new();

        public void Activate()
        {
            E_Activate.Invoke(true);
        }

        public void Deactivate()
        {
            E_Activate.Invoke(false);
        }
    }
}