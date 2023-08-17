using UnityEngine;
using UnityEngine.Events;

public class PlayerInfoManager : Singleton<PlayerInfoManager>
{
    public string ID;
    public string IsReady;
    public bool LockInput;
    public string Name;
    public Transform EyesPosition;

    public UnityEvent<string> E_OnNameChange = new();

    public void SetName(string name)
    {
        Name = name;
        E_OnNameChange.Invoke(Name);
    }
}
