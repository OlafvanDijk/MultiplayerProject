using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

public class Movement : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the main camera used for the player")]
    [SerializeField] private Camera _playerCamera;

    [Tooltip("Reference to the player's actor.")]
    [SerializeField] private Actor _actor;

    [Tooltip("Reference to the player's health.")]
    [SerializeField] private Health _health;

    [Tooltip("Reference to the audiosource playing all the player's sounds.")]
    [SerializeField] private AudioSource _audioSource;

    [Tooltip("Reference to the character controller used for moving the player around.")]
    [SerializeField] private CharacterController _controller;

    //[Tooltip("Reference to the weapons manager.")]
    //[SerializeField] private PlayerWeaponsManager _weaponsManager;


    [Header("General")]
    [Tooltip("Force applied downward when in the air")]
    [SerializeField] private float _gravityDownForce = 20f;

    [Tooltip("Physic layers checked to consider the player grounded")]
    [SerializeField] private LayerMask _groundCheckLayers = -1;

    [Tooltip("distance from the bottom of the character controller capsule to test for grounded")]
    [SerializeField] private float _groundCheckDistance = 1f;


    [Header("Movement")]
    [Tooltip("Max movement speed when grounded (when not sprinting)")]
    [SerializeField] private float _maxSpeedOnGround = 13f;

    [Tooltip("Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
    [SerializeField] private float _movementSharpnessOnGround = 15;

    [Tooltip("Max movement speed when crouching")]
    [SerializeField] [Range(0, 1)] private float _maxSpeedCrouchedRatio = 0.5f;

    [Tooltip("Max movement speed when not grounded")]
    [SerializeField] private float _maxSpeedInAir = 10f;

    [Tooltip("Acceleration speed when in the air")]
    [SerializeField] private float _accelerationSpeedInAir = 25f;

    [Tooltip("Multiplicator for the sprint speed (based on grounded speed)")]
    [SerializeField] private float _sprintSpeedModifier = 1.5f;

    [Tooltip("Height at which the player dies instantly when falling off the map")]
    [SerializeField] private float _killHeight = -50f;


    [Header("Rotation")]
    [Tooltip("Rotation speed for moving the camera")]
    [SerializeField] private float _rotationSpeed = 200f;

    [Tooltip("Rotation speed multiplier when aiming")]
    [SerializeField] [Range(0.1f, 1f)] private float _aimingRotationMultiplier = 0.4f;

    [Tooltip("Vertical rotation angles speed multiplier when aiming")]
    [SerializeField] private Vector2 _verticalCameraMaxAgles = new Vector2(-89f, 89f);


    [Header("Jump")]
    [Tooltip("Force applied upward when jumping")]
    [SerializeField] private float _jumpForce = 9f;


    [Header("Stance")]
    [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
    [SerializeField] private float _cameraHeightRatio = 0.8f;

    [Tooltip("Height of character when standing")]
    [SerializeField] private float _capsuleHeightStanding = 1.8f;

    [Tooltip("Height of character when crouching")]
    [SerializeField] private float _capsuleHeightCrouching = 0.9f;

    [Tooltip("Speed of crouching transitions")]
    [SerializeField] private float _crouchingSharpness = 15f;


    [Header("Audio")]
    [Tooltip("Amount of footstep sounds played when moving one meter")]
    [SerializeField] private float _footstepSfxFrequency = 0.3f;

    [Tooltip("Amount of footstep sounds played when moving one meter while sprinting")]
    [SerializeField] private float _footstepSfxFrequencyWhileSprinting = 0.2f;

    [Tooltip("Sound played for footsteps")]
    [SerializeField] private AudioClip _footstepSfx;

    [Tooltip("Sound played when jumping")]
    [SerializeField] private AudioClip _jumpSfx;

    [Tooltip("Sound played when landing")]
    [SerializeField] private AudioClip _landSfx;

    [Tooltip("Sound played when taking damage froma fall")]
    [SerializeField] private AudioClip _fallDamageSfx;


    [Header("Fall Damage")]
    [Tooltip("Whether the player will recieve damage when hitting the ground at high speed")]
    [SerializeField] private bool _recievesFallDamage;

    [Tooltip("Minimun fall speed for recieving fall damage")]
    [SerializeField] private float _minSpeedForFallDamage = 20f;

    [Tooltip("Fall speed for recieving th emaximum amount of fall damage")]
    [SerializeField] private float _maxSpeedForFallDamage = 40f;

    [Tooltip("Damage recieved when falling at the mimimum speed")]
    [SerializeField] private float _fallDamageAtMinSpeed = 10f;

    [Tooltip("Damage recieved when falling at the maximum speed")]
    [SerializeField] private float _fallDamageAtMaxSpeed = 30f;

    public UnityEvent<bool> E_OnStanceChanged = new();

    public Vector3 CharacterVelocity { get; set; }
    public bool IsLocalPlayer { get; set; }
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

    private float _tickrate;
    private float _lastTimeJumped = 0f;
    private float _cameraVerticalAngle = 0f;
    private float _footstepDistanceCounter;
    private float _targetCharacterHeight;

    private const float JumpGroundingPreventionTime = 0.2f;
    private const float GroundCheckDistanceInAir = 0.07f;

    public void Awake()
    {
        _controller.enableOverlapRecovery = true;
        _health.OnDie += OnDie;
        SetCrouchingState(false, true);
        UpdateCharacterHeight(true);
    }

    public void SetTickrate(float newTickrate)
    {
        _tickrate = newTickrate;
    }

    public void Move(Vector3 moveInput, Vector2 lookInput, bool crouch, bool sprint, bool jump) 
    {
        if (IsDead || CheckKillHeight())
        {
            //Player died
            return;
        }
        bool wasGrounded = GroundCheck();
        CheckLanding(wasGrounded);
        CheckCrouch(crouch);
        UpdateCharacterHeight(false);
        HandleCharacterMovement(moveInput, lookInput, sprint, jump);
    }

    /// <summary>
    /// Check if player is below height that should automatically kill it.
    /// </summary>
    /// <returns>True if below height and not dead yet.</returns>
    public bool CheckKillHeight()
    {
        if (!IsDead && transform.position.y < _killHeight)
        {
            _health.Kill();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if grounded. Only tries to ground when time after jumping has exceeded the prevention time.
    /// Looks for ground with a capsule cast representing the player's capsule but a bit below the player's original position.
    /// Checks ground normal and slope when grounding.
    /// </summary>
    /// <returns>If the player was grounded before the ground check.</returns>
    public bool GroundCheck()
    {
        HasJumpedThisFrame = false;
        bool wasGrounded = IsGrounded;

        float chosenGroundCheckDistance = IsGrounded ? (_controller.skinWidth + _groundCheckDistance) : GroundCheckDistanceInAir;
        IsGrounded = false;
        _groundNormal = Vector3.up;

        if (Time.time >= _lastTimeJumped + JumpGroundingPreventionTime)
        {
            if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(_controller.height),
                _controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, _groundCheckLayers,
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
        return wasGrounded;
    }

    /// <summary>
    /// Check if the player has a normal landing or recieves damages.
    /// </summary>
    /// <param name="wasGrounded">True if the player was grounded before the ground check.</param>
    public void CheckLanding(bool wasGrounded)
    {
        if (IsGrounded && !wasGrounded)
        {
            float fallSpeed = -Mathf.Min(CharacterVelocity.y, _latestImpactSpeed.y);
            float fallSpeedRatio = (fallSpeed - _minSpeedForFallDamage) / (_maxSpeedForFallDamage - _minSpeedForFallDamage);
            if (_recievesFallDamage && fallSpeedRatio > 0f)
            {
                float dmgFromFall = Mathf.Lerp(_fallDamageAtMinSpeed, _fallDamageAtMaxSpeed, fallSpeedRatio);
                _health.TakeDamage(dmgFromFall, null); //TODO Do something so that the client also sees that they are getting damaged
                if(IsLocalPlayer)
                    _audioSource.PlayOneShot(_fallDamageSfx);
            }
            else
            {
                if (IsLocalPlayer)
                    _audioSource.PlayOneShot(_landSfx);
            }
        }
    }

    /// <summary>
    /// Check if crouching state should be changed
    /// </summary>
    /// <param name="crouch">True if crouching state should be changed.</param>
    public void CheckCrouch(bool crouch)
    {
        if (!crouch)
            return;
        SetCrouchingState(!IsCrouching, false);
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
            _targetCharacterHeight = _capsuleHeightCrouching;
        }
        else
        {
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(_capsuleHeightStanding),
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

            _targetCharacterHeight = _capsuleHeightStanding;
        }

        E_OnStanceChanged.Invoke(crouched);
        IsCrouching = crouched;
        return true;
    }

    /// <summary>
    /// Updates character height either instantly or smoothly based on the given force boolean.
    /// </summary>
    /// <param name="force">True if it should update the height instantly.</param>
    public void UpdateCharacterHeight(bool force)
    {
        if (force)
        {
            _controller.height = _targetCharacterHeight;
            _controller.center = Vector3.up * _controller.height * 0.5f;
            _actor.AimPoint.transform.localPosition = _controller.center;
            if (IsLocalPlayer)
                _playerCamera.transform.localPosition = Vector3.up * _targetCharacterHeight * _cameraHeightRatio;
        }
        else if (_controller.height != _targetCharacterHeight)
        {
            _controller.height = Mathf.Lerp(_controller.height, _targetCharacterHeight, _crouchingSharpness * _tickrate);
            _controller.center = Vector3.up * _controller.height * 0.5f;
            _actor.AimPoint.transform.localPosition = _controller.center;
            if (IsLocalPlayer)
            {
                _playerCamera.transform.localPosition = Vector3.Lerp(_playerCamera.transform.localPosition,
                    Vector3.up * _targetCharacterHeight * _cameraHeightRatio, _crouchingSharpness * _tickrate);
            }
        }
    }

    /// <summary>
    /// Handles camera and character movement.
    /// Saves lastestImpactSpeed in case of fall damage.
    /// </summary>
    public void HandleCharacterMovement(Vector3 moveInput, Vector2 lookInput, bool sprint, bool jump)
    {
        transform.Rotate(new Vector3(0f, (lookInput.x * _rotationSpeed * RotationMultiplier), 0f), Space.Self);
        if(IsLocalPlayer)
            RotateCameraVertically(lookInput.y);

        bool isSprinting = sprint;
        if (isSprinting)
            isSprinting = SetCrouchingState(false, false);

        float speedModifier = isSprinting ? _sprintSpeedModifier : 1f;
        Vector3 worldspaceMoveInput = transform.TransformVector(moveInput);

        if (IsGrounded)
            GroundMovement(isSprinting, worldspaceMoveInput, speedModifier, jump);
        else
            AirMovement(worldspaceMoveInput, speedModifier);

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(_controller.height);
        _controller.Move(CharacterVelocity * _tickrate);

        // detect obstructions to adjust velocity accordingly
        _latestImpactSpeed = Vector3.zero;
        if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, _controller.radius,
            CharacterVelocity.normalized, out RaycastHit hit, CharacterVelocity.magnitude * _tickrate, -1,
            QueryTriggerInteraction.Ignore))
        {
            _latestImpactSpeed = CharacterVelocity;
            CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
        }
    }

    public void TeleportPlayer(TransformState state)
    {
        _controller.enabled = false;
        transform.position = state.Position;
        transform.rotation = state.Rotation;
        _controller.enabled = true;
    }

    /// <summary>
    /// Handles the movement on the ground and checks for jumps and footsteps.
    /// </summary>
    /// <param name="isSprinting">True if the player is sprinting.</param>
    /// <param name="worldspaceMoveInput">Directional movement input.</param>
    /// <param name="speedModifier">Speed modifier.</param>
    private void GroundMovement(bool isSprinting, Vector3 worldspaceMoveInput, float speedModifier, bool jumpInputDown)
    {
        Vector3 targetVelocity = worldspaceMoveInput * _maxSpeedOnGround * speedModifier;

        if (IsCrouching)
            targetVelocity *= _maxSpeedCrouchedRatio;
        targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, _groundNormal) * targetVelocity.magnitude;
        CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity, _movementSharpnessOnGround * _tickrate);

        Jump(jumpInputDown);
        if (IsLocalPlayer)
            CheckFootsteps(isSprinting);
    }

    /// Forces the ground state to false. Stop the Y velocity and add our own jump force.
    /// </summary>
    private void Jump(bool jumpInputDown)
    {
        if (IsGrounded == false || !jumpInputDown)
            return;

        if (SetCrouchingState(false, false))
        {
            CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);
            CharacterVelocity += Vector3.up * _jumpForce;

            if(IsLocalPlayer)
                _audioSource.PlayOneShot(_jumpSfx);

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
    private void CheckFootsteps(bool isSprinting)
    {
        float chosenFootstepSfxFrequency = (isSprinting ? _footstepSfxFrequencyWhileSprinting : _footstepSfxFrequency);
        if (_footstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
        {
            _footstepDistanceCounter = 0f;
            _audioSource.PlayOneShot(_footstepSfx);
        }
        _footstepDistanceCounter += CharacterVelocity.magnitude * _tickrate;
    }

    /// <summary>
    /// Limits the horizontal air speed and adds gravity to the velocity.
    /// </summary>
    /// <param name="worldspaceMoveInput">Directional movement input.</param>
    /// <param name="speedModifier">Speed modifier.</param>
    private void AirMovement(Vector3 worldspaceMoveInput, float speedModifier)
    {
        CharacterVelocity += worldspaceMoveInput * _accelerationSpeedInAir * _tickrate;
        float verticalVelocity = CharacterVelocity.y;

        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, _maxSpeedInAir * speedModifier);
        CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

        CharacterVelocity += Vector3.down * _gravityDownForce * _tickrate;
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
    /// Lower weapon and invoke the player death event.
    /// </summary>
    private void OnDie()
    {
        IsDead = true;
        //_weaponsManager.SwitchToWeaponIndex(-1, true);
        EventManager.Broadcast(Events.PlayerDeathEvent);
    }

    /// <summary>
    /// Rotates the camera vertically.
    /// We use the players rotation for the horizontal movement of the camera.
    /// </summary>
    private void RotateCameraVertically(float verticalLook)
    {
        _cameraVerticalAngle -= verticalLook * _rotationSpeed * RotationMultiplier;
        _cameraVerticalAngle = Mathf.Clamp(_cameraVerticalAngle, _verticalCameraMaxAgles.x, _verticalCameraMaxAgles.y);
        _playerCamera.transform.localEulerAngles = new Vector3(_cameraVerticalAngle, 0, 0);
    }
}
