using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
public class M_PlayerCharacterController : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the main camera used for the player")]
    [SerializeField] private Camera PlayerCamera;

    [Tooltip("Reference to the player's actor.")]
    [SerializeField] private Actor _actor;

    [Tooltip("Reference to the player's health.")]
    [SerializeField] private Health _health;

    [Tooltip("Reference to the audiosource playing all the player's sounds.")]
    [SerializeField] private AudioSource AudioSource;

    [Tooltip("Reference to the character controller used for moving the player around.")]
    [SerializeField] private CharacterController _controller;

    [Tooltip("Reference to the input handler.")]
    [SerializeField] private M_PlayerInputHandler _inputHandler;

    //[Tooltip("Reference to the weapons manager.")]
    //[SerializeField] private PlayerWeaponsManager _weaponsManager;


    [Header("General")]
    [Tooltip("Force applied downward when in the air")]
    [SerializeField] private float GravityDownForce = 20f;

    [Tooltip("Physic layers checked to consider the player grounded")]
    [SerializeField] private LayerMask GroundCheckLayers = -1;

    [Tooltip("distance from the bottom of the character controller capsule to test for grounded")]
    [SerializeField] private float GroundCheckDistance = 0.05f;


    [Header("Movement")]
    [Tooltip("Max movement speed when grounded (when not sprinting)")]
    [SerializeField] private float MaxSpeedOnGround = 10f;

    [Tooltip("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
    [SerializeField] private float MovementSharpnessOnGround = 15;

    [Tooltip("Max movement speed when crouching")]
    [SerializeField] [Range(0, 1)] private float MaxSpeedCrouchedRatio = 0.5f;

    [Tooltip("Max movement speed when not grounded")]
    [SerializeField] private float MaxSpeedInAir = 10f;

    [Tooltip("Acceleration speed when in the air")]
    [SerializeField] private float AccelerationSpeedInAir = 25f;

    [Tooltip("Multiplicator for the sprint speed (based on grounded speed)")]
    [SerializeField] private float SprintSpeedModifier = 2f;

    [Tooltip("Height at which the player dies instantly when falling off the map")]
    [SerializeField] private float KillHeight = -50f;

    
    [Header("Rotation")]
    [Tooltip("Rotation speed for moving the camera")]
    [SerializeField] private float RotationSpeed = 200f;

    [Tooltip("Rotation speed multiplier when aiming")]
    [SerializeField] [Range(0.1f, 1f)] private float AimingRotationMultiplier = 0.4f;

    [Tooltip("Vertical rotation angles speed multiplier when aiming")]
    [SerializeField] private Vector2 _verticalCameraMaxAgles = new Vector2(89f, -89f);


    [Header("Jump")]
    [Tooltip("Force applied upward when jumping")]
    [SerializeField] private float JumpForce = 9f;


    [Header("Stance")]
    [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
    [SerializeField] private float CameraHeightRatio = 0.9f;

    [Tooltip("Height of character when standing")]
    [SerializeField] private float CapsuleHeightStanding = 1.8f;

    [Tooltip("Height of character when crouching")]
    [SerializeField] private float CapsuleHeightCrouching = 0.9f;

    [Tooltip("Speed of crouching transitions")]
    [SerializeField] private float CrouchingSharpness = 10f;


    [Header("Audio")]
    [Tooltip("Amount of footstep sounds played when moving one meter")]
    [SerializeField] private float FootstepSfxFrequency = 1f;

    [Tooltip("Amount of footstep sounds played when moving one meter while sprinting")]
    [SerializeField] private float FootstepSfxFrequencyWhileSprinting = 1f;

    [Tooltip("Sound played for footsteps")]
    [SerializeField] private AudioClip FootstepSfx;

    [Tooltip("Sound played when jumping")]
    [SerializeField] private AudioClip JumpSfx;

    [Tooltip("Sound played when landing")]
    [SerializeField] private AudioClip LandSfx;

    [Tooltip("Sound played when taking damage froma fall")]
    [SerializeField] private AudioClip FallDamageSfx;


    [Header("Fall Damage")]
    [Tooltip("Whether the player will recieve damage when hitting the ground at high speed")]
    [SerializeField] private bool RecievesFallDamage;

    [Tooltip("Minimun fall speed for recieving fall damage")]
    [SerializeField] private float MinSpeedForFallDamage = 10f;

    [Tooltip("Fall speed for recieving th emaximum amount of fall damage")]
    [SerializeField] private float MaxSpeedForFallDamage = 30f;

    [Tooltip("Damage recieved when falling at the mimimum speed")]
    [SerializeField] private float FallDamageAtMinSpeed = 10f;

    [Tooltip("Damage recieved when falling at the maximum speed")]
    [SerializeField] private float FallDamageAtMaxSpeed = 50f;


    public UnityEvent<bool> OnStanceChanged = new();

    public Vector3 CharacterVelocity { get; set; }
    public bool IsGrounded { get; private set; }
    public bool HasJumpedThisFrame { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsCrouching { get; private set; }

    public float RotationMultiplier
    {
        get
        {
            //if (_weaponsManager.IsAiming)
            //{
            //    return AimingRotationMultiplier;
            //}

            return 1f;
        }
    }

    private Vector3 _groundNormal;
    private Vector3 _characterVelocity;
    private Vector3 _latestImpactSpeed;

    private float _lastTimeJumped = 0f;
    private float _cameraVerticalAngle = 0f;
    private float _footstepDistanceCounter;
    private float _targetCharacterHeight;

    private const float JumpGroundingPreventionTime = 0.2f;
    private const float GroundCheckDistanceInAir = 0.07f;

    /// <summary>
    /// Sets the player in the ActorsManager.
    /// Set the death listener and force the crouch state to false.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //TODO see if we can replace the find object of type
        ActorsManager actorsManager = FindObjectOfType<ActorsManager>();
        if (actorsManager != null)
            actorsManager.SetPlayer(gameObject);

        _controller.enableOverlapRecovery = true;
        _health.OnDie += OnDie;

        SetCrouchingState(false, true);
        UpdateCharacterHeight(true);
    }

    /// <summary>
    /// Checks if the player is below the killheight.
    /// Checks if grounded and if the player is landing first.
    /// Afterwards it sets the crouching state before moving.
    /// </summary>
    private void Update()
    {
        if (!IsLocalPlayer)
            return;

        if (!IsDead && transform.position.y < KillHeight)
        {
            _health.Kill();
            return;
        }

        HasJumpedThisFrame = false;

        bool wasGrounded = IsGrounded;
        ServerRpcParams serverRpcParams = Helper.CreateServerParam(OwnerClientId);

        GroundCheckServerRPC();
        CheckLandingServerRPC(wasGrounded, serverRpcParams);

        if (_inputHandler.GetCrouchInputDown())
        {
            SetCrouchingState(!IsCrouching, false);
        }

        UpdateCharacterHeight(false);

        Vector2 lookInput = _inputHandler.GetLookInput();
        HandleCharacterMovementServerRPC(lookInput.x, lookInput.y,
            _inputHandler.GetSprintInputHeld(), _inputHandler.GetMoveInput(), _inputHandler.GetJumpInputDown(), serverRpcParams);
    }

    /// <summary>
    /// Lower weapon and invoke the player death event.
    /// </summary>
    private void OnDie()
    {
        IsDead = true;
        //_weaponsManager.SwitchToWeaponIndex(-1, true);
        EventManager.Broadcast(Events.PlayerDeathEvent);
    }

    /// <summary>
    /// Check if the player has a normal landing or recieves damages.
    /// </summary>
    /// <param name="wasGrounded">True if the player was grounded before the ground check.</param>
    [ServerRpc]
    private void CheckLandingServerRPC(bool wasGrounded, ServerRpcParams serverRpcParams)
    {
        if (!Helper.CheckPlayer(serverRpcParams, OwnerClientId))
            return;

        if (IsGrounded && !wasGrounded)
        {
            float fallSpeed = -Mathf.Min(CharacterVelocity.y, _latestImpactSpeed.y);
            float fallSpeedRatio = (fallSpeed - MinSpeedForFallDamage) / (MaxSpeedForFallDamage - MinSpeedForFallDamage);
            if (RecievesFallDamage && fallSpeedRatio > 0f)
            {
                float dmgFromFall = Mathf.Lerp(FallDamageAtMinSpeed, FallDamageAtMaxSpeed, fallSpeedRatio);
                _health.TakeDamage(dmgFromFall, null); //TODO Do something so that the client also sees that they are getting damaged
                PlayAudioClientRPC(FallDamageSfx.name, Helper.ServerToClientParam(serverRpcParams));
            }
            else
            {
                PlayAudioClientRPC(LandSfx.name, Helper.ServerToClientParam(serverRpcParams));
            }
        }
    }

    /// <summary>
    /// Checks if grounded. Only tries to ground when time after jumping has exceeded the prevention time.
    /// Looks for ground with a capsule cast representing the player's capsule but a bit below the player's original position.
    /// Checks ground normal and slope when grounding.
    /// </summary>
    [ServerRpc]
    private void GroundCheckServerRPC()
    {
        float chosenGroundCheckDistance = IsGrounded ? (_controller.skinWidth + GroundCheckDistance) : GroundCheckDistanceInAir;
        IsGrounded = false;
        _groundNormal = Vector3.up;

        if (Time.time >= _lastTimeJumped + JumpGroundingPreventionTime)
        {
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(_controller.height),
                _controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundCheckLayers,
                QueryTriggerInteraction.Ignore))
            {
                _groundNormal = hit.normal;
                if (Vector3.Dot(hit.normal, transform.up) > 0f && IsNormalUnderSlopeLimit(_groundNormal))
                {
                    IsGrounded = true;
                    if (hit.distance > _controller.skinWidth)
                    {
                        _controller.Move(Vector3.down * hit.distance);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handles camera and character movement.
    /// Saves lastestImpactSpeed in case of fall damage.
    /// </summary>
    [ServerRpc]
    private void HandleCharacterMovementServerRPC(float horizontalLook, float verticalLook, bool sprintHeld, Vector3 moveInput, bool jumpInputDown, ServerRpcParams serverRpcParams)
    {
        if (!Helper.CheckPlayer(serverRpcParams, OwnerClientId))
            return;

        transform.Rotate(new Vector3(0f, (horizontalLook * RotationSpeed * RotationMultiplier), 0f), Space.Self);
        RotateCameraVerticallyClientRPC(verticalLook, Helper.ServerToClientParam(serverRpcParams));

        bool isSprinting = sprintHeld;
        if (isSprinting)
            isSprinting = SetCrouchingState(false, false);

        float speedModifier = isSprinting ? SprintSpeedModifier : 1f;
        Vector3 worldspaceMoveInput = transform.TransformVector(moveInput);

        if (IsGrounded)
            GroundMovement(serverRpcParams, isSprinting, worldspaceMoveInput, speedModifier, jumpInputDown);
        else
            AirMovement(worldspaceMoveInput, speedModifier);

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(_controller.height);
        _controller.Move(CharacterVelocity * Time.deltaTime);

        // detect obstructions to adjust velocity accordingly
        _latestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, _controller.radius,
            CharacterVelocity.normalized, out RaycastHit hit, CharacterVelocity.magnitude * Time.deltaTime, -1,
            QueryTriggerInteraction.Ignore))
        {
            _latestImpactSpeed = CharacterVelocity;
            CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
        }
    }

    /// <summary>
    /// Handles the movement on the ground and checks for jumps and footsteps.
    /// </summary>
    /// <param name="isSprinting">True if the player is sprinting.</param>
    /// <param name="worldspaceMoveInput">Directional movement input.</param>
    /// <param name="speedModifier">Speed modifier.</param>
    private void GroundMovement(ServerRpcParams serverRpcParams, bool isSprinting, Vector3 worldspaceMoveInput, float speedModifier, bool jumpInputDown)
    {
        Vector3 targetVelocity = worldspaceMoveInput * MaxSpeedOnGround * speedModifier;

        if (IsCrouching)
            targetVelocity *= MaxSpeedCrouchedRatio;
        targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, _groundNormal) * targetVelocity.magnitude;
        CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity, MovementSharpnessOnGround * Time.deltaTime);

        Jump(serverRpcParams, jumpInputDown);
        CheckFootsteps(serverRpcParams, isSprinting);
    }

    /// <summary>
    /// Forces the ground state to false. Stop the Y velocity and add our own jump force.
    /// </summary>
    private void Jump(ServerRpcParams serverRpcParams, bool jumpInputDown)
    {
        if (IsGrounded == false || !_inputHandler.GetJumpInputDown())
            return;

        if (SetCrouchingState(false, false))
        {
            CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);
            CharacterVelocity += Vector3.up * JumpForce;

            PlayAudioClientRPC(JumpSfx.name, Helper.ServerToClientParam(serverRpcParams));

            _lastTimeJumped = Time.time;
            HasJumpedThisFrame = true;
            IsGrounded = false;
            _groundNormal = Vector3.up;
        }
    }

    /// <summary>
    /// Plays the correct footstep sound if the footstep distance has been reached.
    /// </summary>
    /// <param name="isSprinting">True if the player is sprinting</param>
    private void CheckFootsteps(ServerRpcParams serverRpcParams, bool isSprinting)
    {
        float chosenFootstepSfxFrequency = (isSprinting ? FootstepSfxFrequencyWhileSprinting : FootstepSfxFrequency);
        if (_footstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
        {
            _footstepDistanceCounter = 0f;
            PlayAudioClientRPC(FootstepSfx.name, Helper.ServerToClientParam(serverRpcParams));
        }
        _footstepDistanceCounter += CharacterVelocity.magnitude * Time.deltaTime;
    }

    /// <summary>
    /// Limits the horizontal air speed and adds gravity to the velocity.
    /// </summary>
    /// <param name="worldspaceMoveInput">Directional movement input.</param>
    /// <param name="speedModifier">Speed modifier.</param>
    private void AirMovement(Vector3 worldspaceMoveInput, float speedModifier) 
    {
        CharacterVelocity += worldspaceMoveInput * AccelerationSpeedInAir * Time.deltaTime;
        float verticalVelocity = CharacterVelocity.y;

        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeedInAir * speedModifier);
        CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

        CharacterVelocity += Vector3.down * GravityDownForce * Time.deltaTime;
    }

    /// <summary>
    /// Checks if the normal is under the slope angle limit.
    /// </summary>
    /// <param name="normal">Ground normal.</param>
    /// <returns>Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller.</returns>
    private bool IsNormalUnderSlopeLimit(Vector3 normal)
    {
        return Vector3.Angle(transform.up, normal) <= _controller.slopeLimit;
    }

    /// <summary>
    /// Gets the center point of the bottom hemisphere of the character controller capsule.
    /// </summary>
    /// <returns>Center point of the bottom hemisphere of the capsule.</returns>
    private Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * _controller.radius);
    }
    
    /// <summary>
    /// Gets the center point of the top hemisphere of the character controller capsule.
    /// </summary>
    /// <returns>Center point of the top hemisphere of the capsule.</returns>
    private Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position + (transform.up * (atHeight - _controller.radius));
    }    

    /// <summary>
    /// Updates character height either instantly or smoothly based on the given force boolean.
    /// </summary>
    /// <param name="force">True if it should update the height instantly.</param>
    private void UpdateCharacterHeight(bool force)
    {
        if (force)
        {
            _controller.height = _targetCharacterHeight;
            _controller.center = Vector3.up * _controller.height * 0.5f;
            PlayerCamera.transform.localPosition = Vector3.up * _targetCharacterHeight * CameraHeightRatio;
            _actor.AimPoint.transform.localPosition = _controller.center;
        }
        else if (_controller.height != _targetCharacterHeight)
        {
            _controller.height = Mathf.Lerp(_controller.height, _targetCharacterHeight, CrouchingSharpness * Time.deltaTime);
            _controller.center = Vector3.up * _controller.height * 0.5f;
            PlayerCamera.transform.localPosition = Vector3.Lerp(PlayerCamera.transform.localPosition,
                Vector3.up * _targetCharacterHeight * CameraHeightRatio, CrouchingSharpness * Time.deltaTime);
            _actor.AimPoint.transform.localPosition = _controller.center;
        }
    }

    /// <summary>
    /// Sets the crouching state. Returns false if there was an obstruction.
    /// </summary>
    /// <param name="crouched">True if the player is already crouched.</param>
    /// <param name="ignoreObstructions">True if obstructions should not be checked.</param>
    /// <returns></returns>
    private bool SetCrouchingState(bool crouched, bool ignoreObstructions)
    {
        if (crouched)
        {
            _targetCharacterHeight = CapsuleHeightCrouching;
        }
        else
        {
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(CapsuleHeightStanding),
                    _controller.radius,
                    -1,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps)
                {
                    if (c != _controller)
                    {
                        return false;
                    }
                }
            }

            _targetCharacterHeight = CapsuleHeightStanding;
        }

        OnStanceChanged.Invoke(crouched);
        IsCrouching = crouched;
        return true;
    }

    /// <summary>
    /// Gets a reoriented direction that is tangent to a given slope.
    /// </summary>
    /// <param name="direction">Movement Direction.</param>
    /// <param name="slopeNormal">Ground/Slope normal.</param>
    /// <returns></returns>
    public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
    {
        Vector3 directionRight = Vector3.Cross(direction, transform.up);
        return Vector3.Cross(slopeNormal, directionRight).normalized;
    }

    [ClientRpc]
    private void PlayAudioClientRPC(string sfxName, ClientRpcParams clientRpcParams)
    {
        AudioClip clip = null;
        if (sfxName.Equals(FallDamageSfx.name))
            clip = FallDamageSfx;
        else if (sfxName.Equals(LandSfx.name))
            clip = LandSfx;
        else if (sfxName.Equals(JumpSfx.name))
            clip = JumpSfx;
        else if (sfxName.Equals(FootstepSfx.name))
                clip = FootstepSfx;

        AudioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Rotates the camera vertically.
    /// We use the players rotation for the horizontal movement of the camera.
    /// </summary>
    [ClientRpc]
    private void RotateCameraVerticallyClientRPC(float verticalLook, ClientRpcParams clientRpcParams)
    {
        _cameraVerticalAngle -= verticalLook * RotationSpeed * RotationMultiplier;
        _cameraVerticalAngle = Mathf.Clamp(_cameraVerticalAngle, _verticalCameraMaxAgles.x, _verticalCameraMaxAgles.y);
        PlayerCamera.transform.localEulerAngles = new Vector3(_cameraVerticalAngle, 0, 0);
    }


}