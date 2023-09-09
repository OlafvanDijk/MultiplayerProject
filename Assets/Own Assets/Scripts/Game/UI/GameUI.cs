using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameUI : MonoBehaviour
{
    [SerializeField] private InputActionReference _pauseGameInput;
    [SerializeField] private PauseMenuUI _pauseMenuUI;

    /// <summary>
    /// Checks for the pause input.
    /// </summary>
    void Update()
    {
        if(_pauseGameInput.action.triggered)
            _pauseMenuUI.ToggleMenu();
    }
}
