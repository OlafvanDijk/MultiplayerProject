using Game.Managers;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using System;
using Utility;

public class Movement_Client : NetworkBehaviour
{
    [Header("Movement")]
    [Tooltip("Reference to the input handler.")]
    [SerializeField] private M_PlayerInputHandler _inputHandler;

    [SerializeField] private Movement _movement;
    [SerializeField] private Movement_Server _movementServer;

    private int _tick = 0;
    private float _tickrate = 1f / 60f;
    private float _tickDeltaTime = 0f;

    private const int BUFFER_SIZE = 1024;
    private TransformState _previousTransformState;
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

    /// <summary>
    /// Correct the client if the server's transform position is different.
    /// After correction will then correct all ticks that have happened after.
    /// </summary>
    /// <param name="previousTransformState"></param>
    /// <param name="serverState"></param>
    private void OnTransformStateChanged(TransformState previousTransformState, TransformState serverState)
    {
        if (!IsLocalPlayer)
            return;

        if (previousTransformState == null)
            _previousTransformState = serverState;

        try
        {
            TransformState calculatedState = _transformStates.First(localState => localState != null && localState.Tick == serverState.Tick);
            if (calculatedState.Position != serverState.Position)    //Out of sync
            {
                Debug.Log("Correcting client position");
                TeleportPlayer(serverState);
                ReplayInput(serverState);
            }
        }
        catch (Exception)
        {
            //No matching elements
        }
    }

    /// <summary>
    /// Teleport the player to the given state's position.
    /// </summary>
    /// <param name="state">State to teleport the player to.</param>
    private void TeleportPlayer(TransformState state)
    {
        _movement.TeleportPlayer(state);
        ReplaceTransformState(state);
    }

    /// <summary>
    /// Replays input after the tick of the given state.
    /// </summary>
    /// <param name="serverState">State of the transform on the server.</param>
    private void ReplayInput(TransformState serverState)
    {
        try
        {
            IEnumerable<InputState> inputs = _inputStates.Where(input => input != null && input.Tick > serverState.Tick);
            inputs = from input in inputs orderby input.Tick select input;
            foreach (InputState inputState in inputs)
            {
                _movement.Move(inputState.MoveInput, inputState.LookInput, inputState.Crouch, inputState.Sprint, inputState.Jump);
                TransformState newTransformState = new TransformState()
                {
                    Tick = inputState.Tick,
                    Position = transform.position,
                    Rotation = transform.rotation,
                    HasStartedMoving = true
                };

                ReplaceTransformState(newTransformState);
            }
        }
        catch (Exception)
        {
            //No matching elements
        }
    }

    /// <summary>
    /// Replaces a transformstate in the array of states.
    /// </summary>
    /// <param name="newState"></param>
    private void ReplaceTransformState(TransformState newState)
    {
        for (int i = 0; i < _transformStates.Length; i++)
        {
            if (_transformStates[i].Tick == newState.Tick)
            {
                _transformStates[i] = newState;
                break;
            }
        }
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
