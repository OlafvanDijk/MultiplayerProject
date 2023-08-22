using Game.Managers;
using Unity.Netcode;
using UnityEngine;
using Utility;

public class Movement_Client : NetworkBehaviour
{
    [Tooltip("Reference to the input handler.")]
    [SerializeField] private M_PlayerInputHandler _inputHandler;

    [SerializeField] private Movement _movement;
    [SerializeField] private Movement_Server _movementServer;

    private int _tick = 0;
    private float _tickrate = 1f / 60f;
    private float _tickDeltaTime = 0f;

    private const int BUFFER_SIZE = 1024;
    private InputState[] _inputStates = new InputState[BUFFER_SIZE];
    private TransformState[] _transformStates = new TransformState[BUFFER_SIZE];

    private bool _triedJumpingThisTick;
    private bool _triedSprintingThisTick;
    private bool _triedCrouchingThisTick;

    /// <summary>
    /// Add transform network variable listener.
    /// </summary>
    private void OnEnable()
    {
        _movementServer.ServerTransformState.OnValueChanged += OnTransformStateChanged;
    }

    /// <summary>
    /// Remove transform network variable listener.
    /// </summary>
    private void OnDisable()
    {
        _movementServer.ServerTransformState.OnValueChanged -= OnTransformStateChanged;
    }

    /// <summary>
    /// Process Movement. Gets the input if this is the local player.
    /// Otherwise replicates the movement with the networkvariable in Movement_Server.
    /// </summary>
    private void Update()
    {
        if (IsClient && IsLocalPlayer)
        {
            Vector3 moveInput = default;
            Vector2 lookInput = default;
            bool crouch = false;
            bool sprint = false;
            bool jump = false;

            if (!PlayerInfoManager.Instance.LockInput)
            {
                moveInput = _inputHandler.GetMoveInput();
                lookInput = _inputHandler.GetLookInput();
                crouch = _inputHandler.GetCrouchInputDown();
                sprint = _inputHandler.GetSprintInputHeld();
                jump = _inputHandler.GetJumpInputDown();
            }
            ProcessLocalPlayerMovement(moveInput, lookInput, crouch, sprint, jump);
        }
        else
        {
            ProcessSimulatedPlayerMovement();
        }
    }

    /// <summary>
    /// Set tickrates and local player variable.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        _movement.IsLocalPlayer = IsLocalPlayer;
        _movement.SetTickrate(_tickrate);
        if (IsLocalPlayer && IsOwner)
        {
            _inputHandler.SetTrickRate(_tickrate);
            _movementServer.SetTickRateServerRpc(_tickrate);
        }
    }

    private void OnTransformStateChanged(TransformState previousTransformState, TransformState newTransformState)
    {
        //TODO implement new value?
    }

    /// <summary>
    /// Set transform based on the network variable.
    /// </summary>
    private void ProcessSimulatedPlayerMovement()
    {
        if (!UpdateTick())
            return;

        if (_movementServer.ServerTransformState.Value != null)
        {
            TransformState currentState = _movementServer.ServerTransformState.Value;
            if (currentState.HasStartedMoving)
            {
                transform.position = currentState.Position;
                transform.rotation = currentState.Rotation;
            }
        }

        _tickDeltaTime -= _tickrate;
        _tick++;
    }

    /// <summary>
    /// Uses given input to move the local player.
    /// Also saves the transform and input states per tick.
    /// </summary>
    /// <param name="moveInput">Movement Input.</param>
    /// <param name="lookInput">Look Input.</param>
    /// <param name="crouch">True when toggling the crouch.</param>
    /// <param name="sprint">True when sprinting.</param>
    /// <param name="jump">True when trying to jump.</param>
    private void ProcessLocalPlayerMovement(Vector3 moveInput, Vector2 lookInput, bool crouch, bool sprint, bool jump)
    {
        if (crouch == true && !_triedCrouchingThisTick)
            _triedCrouchingThisTick = true;
        if (sprint == true && !_triedSprintingThisTick)
            _triedSprintingThisTick = true;
        if (jump == true && !_triedJumpingThisTick)
            _triedJumpingThisTick = true;

        if (!UpdateTick())
            return;

        int bufferIndex = _tick % BUFFER_SIZE;

        TransformState transformState = Helper.TransformState(_tick, transform.position, transform.rotation, true);
        Move(transformState, moveInput, lookInput, _triedCrouchingThisTick, _triedSprintingThisTick, _triedJumpingThisTick);

        InputState inputState = new InputState(_tick, moveInput, lookInput, _triedCrouchingThisTick, _triedSprintingThisTick, _triedJumpingThisTick);
        _inputStates[bufferIndex] = inputState;
        _transformStates[bufferIndex] = transformState;

        _tickDeltaTime -= _tickrate;
        _tick++;

        _triedCrouchingThisTick = false;
        _triedSprintingThisTick = false;
        _triedJumpingThisTick = false;
    }

    /// <summary>
    /// If this is not the server sent a ServerRpc and also move ourselves.
    /// If this is the server just move and update the transformstate ourselves.
    /// </summary>
    /// <param name="transformState">TransformState to set.</param>
    /// <param name="moveInput">Movement Input.</param>
    /// <param name="lookInput">Look Input.</param>
    /// <param name="crouch">True when toggling the crouch.</param>
    /// <param name="sprint">True when sprinting.</param>
    /// <param name="jump">True when trying to jump.</param>
    private void Move(TransformState transformState, Vector3 moveInput, Vector2 lookInput, bool crouch, bool sprint, bool jump)
    {
        if (!IsServer)
        {
            _movementServer.MovePlayerServerRpc(_tick, moveInput, lookInput, crouch, sprint, jump);
            _movement.Move(moveInput, lookInput, crouch, sprint, jump);
        }
        else
        {
            _movement.Move(moveInput, lookInput, crouch, sprint, jump);
            _movementServer.SetTransformState(transformState);
        }
    }

    /// <summary>
    /// Updates current tickDeltaTime and checks if this frame is a tick.
    /// </summary>
    /// <returns>True if this frame is a tick.</returns>
    private bool UpdateTick()
    {
        _tickDeltaTime += Time.deltaTime;
        if (_tickDeltaTime <= _tickrate)
            return false;
        return true;
    }


}