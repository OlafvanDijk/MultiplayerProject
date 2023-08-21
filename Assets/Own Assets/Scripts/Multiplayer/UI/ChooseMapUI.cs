using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using UnityEngine.UI;
using TMPro;
using System;
using Game;
using Game.Events;
using Unity.Services.Lobbies.Models;

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

    public void Init()
    {
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

    private void OnLobbyUpdated()
    {
        Tuple<int, string> mapData = GameLobbyManager.Instance.GetLobbyMapData();
        _mapIndex = mapData.Item1;
        _difficulty = mapData.Item2;
        ShowLocalMap();
    }

    private void MapChosen(MapItemUI mapItem)
    {
        if (_currentSelected == mapItem)
            return;

        if(_currentSelected)
            _currentSelected.Deselect();
        _currentSelected = mapItem;
        _mapIndex = _mapSelectionData.Maps.IndexOf(_currentSelected.MapInfo);
    }

    private void ShowLocalMap()
    {
        MapInfo mapInfo = _mapSelectionData.Maps[_mapIndex];
        _currentMapImage.sprite = mapInfo.MapSprite;
        _currentMapNameField.text = $"Map: {mapInfo.MapName}";
        _difficultyField.text = $"Difficulty: {_difficulty}";
    }
    private void OnDropdownChanged(int index)
    {
        _difficulty = _difficultyDropdown.options[index].text;
    }

    private async void OnSelect()
    {
        await GameLobbyManager.Instance.SetSelectedMap(_mapIndex, _difficulty);
        ShowLocalMap();
        gameObject.SetActive(false);
    }

    private void OnChooseMap()
    {
        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (!GameLobbyManager.Instance.IsHost)
            LobbyEvents.E_LobbyUpdated.RemoveListener(OnLobbyUpdated);
    }
}
