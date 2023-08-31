using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "GameObjects/Create Prefab Collection")]
    public class PrefabCollection : ScriptableObject
    {
        public List<GameObject> Prefabs = new();
    }
}

