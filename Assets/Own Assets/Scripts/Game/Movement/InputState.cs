using UnityEngine;

public class InputState
{
    public int Tick;
    public Vector3 MoveInput;
    public Vector2 LookInput;
    public bool Crouch;
    public bool Sprint;
    public bool Jump;

    public InputState(int tick, Vector3 moveInput, Vector2 lookInput, bool crouch, bool sprint, bool jump)
    {
        Tick = tick;
        MoveInput = moveInput;
        LookInput = lookInput;
        Crouch = crouch;
        Sprint = sprint;
        Jump = jump;
    }
}
