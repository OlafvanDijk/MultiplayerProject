using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.UI.Messaging
{
    [Serializable]
    public class ChatTab
    {
        public string TabName;


        [SerializeField] private Color _selectedColor;
        [SerializeField] private int _maxMessages = 30;

        private Button _button;
        private GameObject _view;
        private Transform _content;
        private Scrollbar _scrollBar;

        public EMessageType MessageTypes;

        private Color _basicButtonColor;
        private GameObject _messagePrefab;
        private List<GameObject> _messages = new();

        public UnityEvent<ChatTab> ShowTab = new();

        public void Init(Button button, Scrollbar scrollbar, Transform content, GameObject view, GameObject messagePrefab)
        {
            _button = button;
            _scrollBar = scrollbar;
            _content = content;
            _view = view;
            _messagePrefab = messagePrefab;

            _view.SetActive(false);

            _basicButtonColor = button.image.color;
            _button.onClick.AddListener(OnClickTab);
        }

        public void AddMessage(string message)
        {
            GameObject messageObject = Object.Instantiate(_messagePrefab, _content);
            TextMeshProUGUI chatMessage = messageObject.GetComponent<TextMeshProUGUI>();
            chatMessage.text = message;
            _messages.Add(messageObject);
            messageObject.SetActive(true);

            if (_messages.Count > _maxMessages)
            {
                GameObject toDestroy = _messages[0];
                _messages.RemoveAt(0);
                Object.Destroy(toDestroy);
            }
        }

        public void SetScrollBarValue(float value)
        {
            _scrollBar.value = value;
        }

        public void OnClickTab()
        {
            _button.image.color = _selectedColor;
            _view.SetActive(true);
            ShowTab.Invoke(this);
        }

        public void HideTab()
        {
            _button.image.color = _basicButtonColor;
            _view.SetActive(false);
        }

        public void Cleanup()
        {
            Object.Destroy(_button.gameObject);
            Object.Destroy(_view.gameObject);
        }
    }
}