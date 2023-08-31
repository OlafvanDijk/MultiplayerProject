using Game.Data;
using Game.Managers;
using TMPro;
using UnityEngine;
using Utility;

namespace Game
{
    public class LobbyPlayer : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] private TextMeshProUGUI _nameField;

        [Header("Player Ready")]
        [SerializeField] private GameObject _readyObject;
        [SerializeField] private SwitchObjectActive switchObjectActive;

        [Header("Character Select")]
        [SerializeField] private GameObject _characterSelectObject;
        [SerializeField] private TextMeshProUGUI _characterNameField;
        [SerializeField] private PrefabCollection _characters;
        [SerializeField] private int _startingCharacterIndex;

        private float _lastUpdated;
        private int _currentCharacterIndex;
        private GameObject _characterObject;
        private LobbyPlayerData _playerData;
        private PlayerInfoManager _playerInfoManager;

        public string ID
        {
            get
            {
                if (_playerData != null)
                    return _playerData.ID;
                return string.Empty;
            }
        }

        public string Name
        {
            get
            {
                if (_playerData != null)
                    return _playerData.Name;
                return string.Empty;
            }
        }

        public void Awake()
        {
            _currentCharacterIndex = _startingCharacterIndex;
            _characterNameField.text = _characters.Prefabs[_currentCharacterIndex].name;
            _playerInfoManager = PlayerInfoManager.Instance;
        }

        /// <summary>
        /// Set the data for the joined player and spawns in a character.
        /// </summary>
        /// <param name="playerData"></param>
        public void SetDataExternal(LobbyPlayerData playerData)
        {
            bool spawnCharacter = false;
            if (playerData.CharacterIndex != -1 && playerData.CharacterIndex != _currentCharacterIndex && _lastUpdated + 1f < Time.time)
            {
                _currentCharacterIndex = playerData.CharacterIndex;
                spawnCharacter = true;
            }
            if (!_characterObject)
                spawnCharacter = true;

            SetDataInternal(playerData, playerData.Name, playerData.IsReady, true, true);
            if (spawnCharacter)
                SpawnCharacter();
        }

        /// <summary>
        /// Enables the character select.
        /// </summary>
        public void EnableCharacterSelect()
        {
            if(!_characterSelectObject.activeSelf)
                _characterSelectObject.SetActive(true);
        }

        /// <summary>
        /// Updates the _currentCharacterIndex based on the given value and spawns the related character.
        /// </summary>
        /// <param name="nextPrevious">True for next, false for previous index.</param>
        public void NextCharacter(bool nextPrevious)
        {
            _currentCharacterIndex = Helper.SetIndex(_currentCharacterIndex, _characters.Prefabs.Count, nextPrevious);
            _lastUpdated = Time.time;
            SpawnCharacter();
        }

        /// <summary>
        /// Reset the data for the player that left.
        /// </summary>
        public void PlayerLeft()
        {
            _currentCharacterIndex = _startingCharacterIndex;
            if (_characterObject)
                Destroy(_characterObject);
            SetDataInternal(null, string.Empty, false, false, false);
        }

        /// <summary>
        /// Sets all character related fields.
        /// </summary>
        /// <param name="playerData"></param>
        /// <param name="name"></param>
        /// <param name="isReady"></param>
        /// <param name="showReady"></param>
        /// <param name="showPlayer"></param>
        private void SetDataInternal(LobbyPlayerData playerData, string name, bool isReady, bool showReady, bool showPlayer)
        {
            _playerData = playerData;
            _nameField.text = name;

            switchObjectActive.Activate(isReady);
            _readyObject.SetActive(showReady);
            _nameField.gameObject.SetActive(showPlayer);
        }

        /// <summary>
        /// Destroys current character and spawns in a new character based on the _currentCharacterIndex.
        /// Updates the lobby player data so other players can see the newly chosen character.
        /// </summary>
        private async void SpawnCharacter()
        {
            if (_characterObject)
                Destroy(_characterObject);
            _characterObject = Instantiate(_characters.Prefabs[_currentCharacterIndex], transform);
            _characterNameField.text = _characterObject.name.Replace("(Clone)", "");
            if (_playerData.ID == _playerInfoManager.AuthPlayerID)
            {
                await GameLobbyManager.Instance.SetPlayerCharacter(_currentCharacterIndex);
                _playerInfoManager.CharacterIndex = _currentCharacterIndex;
            }
        }
    }
}