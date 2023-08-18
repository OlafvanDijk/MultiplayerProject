using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using UnityEngine.UI;
using TMPro;
using System;

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

    private MapItemUI _currentSelected;

    private void Awake()
    {
        gameObject.SetActive(false);
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

    private void MapChosen(MapItemUI mapItem)
    {
        if(_currentSelected)
            _currentSelected.Deselect();
        _currentSelected = mapItem;
        ShowLocalMap();
        //TODO show it to everyone
    }

    private void ShowLocalMap()
    {
        _currentMapImage.sprite = _currentSelected.MapInfo.MapSprite;
        _currentMapNameField.text = $"Map: {_currentSelected.MapInfo.MapName}";
    }
    private void OnDropdownChanged(int index)
    {
        _difficultyField.text = $"Difficulty: {_difficultyDropdown.options[index].text}";
    }

    private void OnSelect()
    {
        gameObject.SetActive(false);
    }

    private void OnChooseMap()
    {
        gameObject.SetActive(true);
    }
}
