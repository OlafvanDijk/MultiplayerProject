using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Game.Data;

namespace Game.GameLobby.UI
{
    public class MapItemUI : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _selectedBorder;
        [SerializeField] private TextMeshProUGUI _mapNameField;
        [SerializeField] private Image _mapImage;

        [HideInInspector] public MapInfo MapInfo;

        public UnityEvent<MapItemUI> E_MapChosen = new();

        public void Init(MapInfo mapInfo)
        {
            MapInfo = mapInfo;
            _mapNameField.text = mapInfo.MapName;
            _mapImage.sprite = mapInfo.MapSprite;
            _button.onClick.AddListener(ChooseMap);
        }

        public void ChooseMap()
        {
            _selectedBorder.SetActive(true);
            E_MapChosen.Invoke(this);
        }

        public void Deselect()
        {
            _selectedBorder.SetActive(false);
        }
    }
}