using Game.Data;
using Game.Events;
using Game.Managers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GameLobby.UI
{
    public class ChooseMapUI : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] private MapSelectionData _mapSelectionData;

        [Header("Lobby Info")]
        [SerializeField] private Button _chooseMapButton;
        [SerializeField] private Image _currentMapImage;
        [SerializeField] private TextMeshProUGUI _currentMapNameField;
        [SerializeField] private TextMeshProUGUI _difficultyField;

        [Header("Choose Map UI")]
        [SerializeField] private Button _selectButton;
        [SerializeField] private GameObject _mapItemPrefab;
        [SerializeField] private Transform _grid;
        [SerializeField] private TMP_Dropdown _difficultyDropdown;

        private int _mapIndex;
        private string _difficulty;
        private MapItemUI _currentSelected;

        public string ScenePath
        {
            get
            {
                return _mapSelectionData.Maps[_mapIndex].SceneReference.ScenePath;
            }
        }

        /// <summary>
        /// Populates the choose map UI when Host.
        /// If not the host then set a listener for the lobby update and disable the ChooseMap button.
        /// </summary>
        public void Init()
        {
            _difficulty = "Easy";
            //TODO set difficulty from enum of difficulties or saved last used difficulty
            ShowLocalMap();

            if (GameLobbyManager.Instance.IsHost)
            {
                _difficultyDropdown.onValueChanged.AddListener(OnDropdownChanged);
                _chooseMapButton.onClick.AddListener(OnChooseMap);
                _selectButton.onClick.AddListener(OnSelect);

                List<MapInfo> allMapsInfo = _mapSelectionData.Maps;
                for (int i = 0; i < allMapsInfo.Count; i++)
                {
                    MapInfo mapInfo = allMapsInfo[i];
                    GameObject mapItemObject = Instantiate(_mapItemPrefab, _grid);
                    MapItemUI mapItem = mapItemObject.GetComponent<MapItemUI>();
                    mapItem.Init(mapInfo);
                    mapItem.E_MapChosen.AddListener(MapChosen);

                    if (i == 0)
                        mapItem.ChooseMap();
                }
            }
            else
            {
                LobbyEvents.E_LobbyUpdated.AddListener(OnLobbyUpdated);
                _chooseMapButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Gets the lobby map data after the lobby has been updated and shows the currently selected map and difficulty.
        /// </summary>
        private void OnLobbyUpdated()
        {
            Tuple<int, string> mapData = GameLobbyManager.Instance.GetLobbyMapData();
            _mapIndex = mapData.Item1;
            _difficulty = mapData.Item2;
            ShowLocalMap();
        }

        /// <summary>
        /// Deselect the old MapItem and sets the MapIndex.
        /// </summary>
        /// <param name="mapItem">New selected MapItem.</param>
        private void MapChosen(MapItemUI mapItem)
        {
            if (_currentSelected == mapItem)
                return;

            if (_currentSelected)
                _currentSelected.Deselect();
            _currentSelected = mapItem;
            _mapIndex = _mapSelectionData.Maps.IndexOf(_currentSelected.MapInfo);
        }

        /// <summary>
        /// Changes the sprite and text based on the mapIndex.
        /// </summary>
        private void ShowLocalMap()
        {
            MapInfo mapInfo = _mapSelectionData.Maps[_mapIndex];
            _currentMapImage.sprite = mapInfo.MapSprite;
            _currentMapNameField.text = $"Map: {mapInfo.MapName}";
            _difficultyField.text = $"Difficulty: {_difficulty}";
        }

        /// <summary>
        /// Update the difficulty based on the given index.
        /// </summary>
        /// <param name="index">Selected dropdown index.</param>
        private void OnDropdownChanged(int index)
        {
            _difficulty = _difficultyDropdown.options[index].text;
        }

        /// <summary>
        /// 
        /// </summary>
        private async void OnSelect()
        {
            await GameLobbyManager.Instance.SetSelectedMap(_mapIndex, _difficulty);
            ShowLocalMap();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows map selection screen.
        /// </summary>
        private void OnChooseMap()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Removes the listener
        /// </summary>
        private void OnDestroy()
        {
            if (!GameLobbyManager.Instance.IsHost)
                LobbyEvents.E_LobbyUpdated.RemoveListener(OnLobbyUpdated);
        }
    }
}
