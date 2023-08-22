using Unity.Netcode;
using UnityEngine;
using Utility;

public class Movement_Server : NetworkBehaviour
{
    [SerializeField] private Movement _movement;
    [SerializeField] private Movement_Client _movementClient;

    public NetworkVariable<TransformState> ServerTransformState = new();

    private TransformState _previousTransformState; //Own copy of the previous recieved state

    /// <summary>
    /// Moves the player on the server side with the given input.
    /// Updates the ServerTransformState when having moved.
    /// </summary>
    /// <param name="tick">Tick that the client is referring to.</param>
    /// <param name="moveInput">Movement Input.</param>
    /// <param name="lookInput">Look Input.</param>
    /// <param name="crouch">True when toggling the crouch.</param>
    /// <param name="sprint">True when sprinting.</param>
    /// <param name="jump">True when trying to jump.</param>
    [ServerRpc]
    public void MovePlayerServerRpc(int tick, Vector3 moveInput, Vector2 lookInput, bool crouch, bool sprint, bool jump) 
    {
        _movement.Move(moveInput, lookInput, crouch, sprint, jump);
        SetTransformState(Helper.TransformState(tick, transform.position, transform.rotation, true));
    }

    /// <summary>
    /// Set the tickrate for the clients' counter part on the server to get the deltaTime right.
    /// </summary>
    /// <param name="tickrate">Tickrate of client.</param>
    [ServerRpc]
    public void SetTickRateServerRpc(float tickrate)
    {
        _movement.SetTickrate(tickrate);
    }

    /// <summary>
    /// Updates the current and previous TransformState.
    /// </summary>
    /// <param name="transformState"></param>
    public void SetTransformState(TransformState transformState)
    {
        _previousTransformState = ServerTransformState.Value;
        ServerTransformState.Value = transformState;
    }
}
