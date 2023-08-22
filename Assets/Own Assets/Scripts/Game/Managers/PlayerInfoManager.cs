using UnityEngine;
using UnityEngine.Events;
using Utility;

namespace Game.Managers
{
    /// <summary>
    /// Class that will hold the players information regardless of the services used.
    /// </summary>
    public class PlayerInfoManager : Singleton<PlayerInfoManager>
    {
        public bool IsHost;
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
}
