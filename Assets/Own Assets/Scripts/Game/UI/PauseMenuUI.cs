using DG.Tweening;
using Game.Managers;
using UnityEngine;
using UnityEngine.UI;
using Utility;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private Button _leaveButton;
    [SerializeField] private Button _returnButton;
    [SerializeField] private Transform _menuTransform;
    [SerializeField] private float _scaleDuration = 0.5f;
    [SerializeField] private SceneReference _mainMenu;

    private bool _cursorBefore;
    private CursorLockMode _cursorLockModeBefore;

    private bool _paused;
    private Sequence _sequence;

    private void Awake()
    {
        _leaveButton.onClick.AddListener(OnLeaveGame);
    }

    private async void OnLeaveGame()
    {
        await GameLobbyManager.Instance.LeaveGame();
        LoadScene.E_LoadScene.Invoke(_mainMenu);
    }

    public void ToggleMenu()
    {
        if (PlayerInfoManager.Instance.LockInput && !_paused)
            return;

        _paused = !_paused;
        PlayerInfoManager.Instance.LockInput = _paused;
        if(_paused)
            ShowMenu();
        else
            HideMenu();
    }

    private void ShowMenu()
    {
        _cursorBefore = Cursor.visible;
        _cursorLockModeBefore = Cursor.lockState;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        _returnButton.onClick.AddListener(ToggleMenu);
        _sequence?.Kill(true);
        _sequence = DOTween.Sequence();
        _sequence.InsertCallback(0, () => _menuTransform.localScale = Vector3.zero);
        _sequence.InsertCallback(0, () => gameObject.SetActive(true));
        _sequence.Insert(0, _menuTransform.DOScale(1, _scaleDuration));
    }

    private void HideMenu()
    {
        Cursor.visible = _cursorBefore;
        Cursor.lockState = _cursorLockModeBefore;

        _returnButton.onClick.RemoveListener(ToggleMenu);
        _sequence?.Kill(true);
        _sequence = DOTween.Sequence();
        _sequence.Insert(0, _menuTransform.DOScale(0, _scaleDuration));
        _sequence.InsertCallback(_scaleDuration, () => gameObject.SetActive(false));
    }
}
