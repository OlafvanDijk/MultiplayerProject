using Unity.Netcode;
using UnityEngine.Events;

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
