using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Player/Create Player Data", fileName = "PlayerData")]
public class PlayerData : ScriptableObject
{
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
