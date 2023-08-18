using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Data/Map Data", fileName = "MapData_")]
    public class MapSelectionData : ScriptableObject
    {
        public List<MapInfo> Maps = new();
    }

    [Serializable]
    public struct MapInfo
    {
        public Sprite MapSprite;
        public string MapName;
        public SceneReference SceneName;
    }
}