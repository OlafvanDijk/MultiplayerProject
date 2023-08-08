﻿using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class M_PlayerInputHandler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Sensitivity multiplier for moving the camera around")]
    [SerializeField] private float _lookSensitivity = 1f;

    [Tooltip("Additional sensitivity multiplier for WebGL")]
    [SerializeField] private float _webglLookSensitivityMultiplier = 0.25f;

    [Tooltip("Limit to consider an input when using a trigger on a controller")]
    [SerializeField] private float _triggerAxisThreshold = 0.4f;

    [Tooltip("Used to flip the vertical input axis")]
    [SerializeField] private bool _invertYAxis = false;

    [Tooltip("Used to flip the horizontal input axis")]
    [SerializeField] private bool _invertXAxis = false;

   
    [Header("Controls References")]
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _movementReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _lookReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _jumpReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _fireReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _aimReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _sprintReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _crouchReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _reloadReference;

    [Header("Item References")]
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _SwitchItemMouseReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _switchItemGamepadReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _inventoryOneReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _inventoryTwoReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _inventoryThreeReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _inventoryFourReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _inventoryFiveReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _inventorySixReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _inventorySevenReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _inventoryEightReference;
    [FoldoutGroup("Input Action References")] [SerializeField] private InputActionReference _inventoryNineReference;

    private float _tickRate;

    private void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    public void SetTrickRate(float tickRate)
    {
        _tickRate = tickRate;
    }

    public bool CanProcessInput()
    {
        return true; // Cursor.lockState == CursorLockMode.Locked; //TODO Add to this if input may not be processed like the game being paused
    }

    public Vector3 GetMoveInput()
    {
        if (!CanProcessInput())
            return Vector3.zero;

        Vector2 axis = _movementReference.action.ReadValue<Vector2>();
        Vector3 move = new Vector3(axis.x, 0f, axis.y);

        // constrain move input to a maximum magnitude of 1, otherwise diagonal movement might exceed the max move speed defined
        move = Vector3.ClampMagnitude(move, 1);
        return move;
    }

    public Vector2 GetLookInput()
    {
        Vector2 axis = _lookReference.action.ReadValue<Vector2>();
        return GetMouseOrStickLookAxis(axis);
    }

    public bool GetJumpInputDown()
    {
        if (CanProcessInput())
        {
            return _jumpReference.action.triggered;
        }
        
        return false;
    }

    public bool GetJumpInputHeld()
    {
        if (CanProcessInput())
        {
            return _jumpReference.action.IsPressed();
        }
        return false;
    }

    public bool GetFireInputDown()
    {
        return _fireReference.action.triggered;
    }

    public bool GetFireInputReleased()
    {
        return _fireReference.action.WasReleasedThisFrame();
    }

    public bool GetFireInputHeld()
    {
        return _fireReference.action.IsPressed();
    }

    public bool GetAimInputHeld()
    {
        if (CanProcessInput())
            return _aimReference.action.IsPressed();
        return false;
    }

    public bool GetSprintInputHeld()
    {
        if (CanProcessInput())
            return _sprintReference.action.IsPressed();
        return false;
    }

    public bool GetCrouchInputDown()
    {
        if (CanProcessInput())
            return _crouchReference.action.triggered;
        return false;
    }

    public bool GetCrouchInputReleased()
    {
        if (CanProcessInput())
            return _crouchReference.action.WasReleasedThisFrame();
        return false;
    }

    public bool GetReloadButtonDown()
    {
        if (CanProcessInput())
            return _reloadReference.action.triggered;
        return false;
    }

    public int GetSwitchWeaponInput()
    {
        if (!CanProcessInput())
        {
            float scrollValue = _SwitchItemMouseReference.action.ReadValue<Vector2>().y;

            if (scrollValue == 0 || !_SwitchItemMouseReference.action.WasPressedThisFrame())
                return 0;

            if (scrollValue > 0f)
                return -1;
            else if (scrollValue < 0f)
                return 1;
        }
        return 0;
    }

    public int GetSelectWeaponInput()
    {
        if (!CanProcessInput())
            return 0;

        if (_inventoryOneReference.action.triggered)
            return 1;
        else if (_inventoryTwoReference.action.triggered)
            return 2;
        else if (_inventoryThreeReference.action.triggered)
            return 3;
        else if (_inventoryFourReference.action.triggered)
            return 4;
        else if (_inventoryFiveReference.action.triggered)
            return 5;
        else if (_inventorySixReference.action.triggered)
            return 6;
        else if (_inventorySevenReference.action.triggered)
            return 7;
        else if (_inventoryEightReference.action.triggered)
            return 8;
        else if (_inventoryNineReference.action.triggered)
            return 9;
        else
            return 0;
    }

    private Vector2 GetMouseOrStickLookAxis(Vector2 axis)
    {
        if (!CanProcessInput())
            return Vector2.zero;

        bool isGamepad = PlayerNetwork.Player != null && PlayerNetwork.Player.currentControlScheme == "Gamepad";

        if (_invertYAxis)
            axis.y *= -1f;
        if (_invertXAxis)
            axis.x *= -1f;

        axis *= _lookSensitivity;
        if (isGamepad)
        {
            // since mouse input is already deltaTime-dependant, only scale input with frame time if it's coming from sticks
            axis *= _tickRate;
        }
        else
        {
            // reduce mouse input amount to be equivalent to stick movement
            axis *= 0.01f;
#if UNITY_WEBGL
            // Mouse tends to be even more sensitive in WebGL due to mouse acceleration, so reduce it even more
            i *= _webglLookSensitivityMultiplier;
#endif
        }

        return axis;
    }
}