using Game.Gameplay;
using Game.Gameplay.Weapons;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Managers
{
    //TODO Show current weapon on every client.

    public class M_PlayerWeaponsManager : MonoBehaviour
    {
        public enum WeaponSwitchState
        {
            Up,
            Down,
            PutDownPrevious,
            PutUpNew,
        }

        [Tooltip("List of weapon the player will start with.")]
        [SerializeField] private List<M_WeaponController> _startingWeapons = new List<M_WeaponController>();

        [Header("References")]
        [Tooltip("Reference to the script that handles the movement.")]
        [SerializeField] private Movement _movement;

        [Tooltip("Reference to the input handler.")]
        [SerializeField] private M_PlayerInputHandler _inputHandler;

        [Tooltip("Secondary camera used to avoid seeing weapon go throw geometries.")]
        [SerializeField] private Camera _weaponCamera;

        [Tooltip("Parent transform where all weapon will be added in the hierarchy.")]
        [SerializeField] private Transform _weaponParentSocket;

        [Tooltip("Position for weapons when active but not actively aiming.")]
        [SerializeField] private Transform _defaultWeaponPosition;

        [Tooltip("Position for weapons when aiming.")]
        [SerializeField] private Transform _aimingWeaponPosition;

        [Tooltip("Position for innactive weapons.")]
        [SerializeField] private Transform _downWeaponPosition;


        [Header("Weapon Bob")]
        [Tooltip("Frequency at which the weapon will move around in the screen when the player is in movement.")]
        [SerializeField] private float _bobFrequency = 10f;

        [Tooltip("How fast the weapon bob is applied, the bigger value the fastest.")]
        [SerializeField] private float _bobSharpness = 10f;

        [Tooltip("Distance the weapon bobs when not aiming.")]
        [SerializeField] private float _defaultBobAmount = 0.05f;

        [Tooltip("Distance the weapon bobs when aiming.")]
        [SerializeField] private float _aimingBobAmount = 0.02f;

        [Header("Weapon Recoil")]
        [Tooltip("This will affect how fast the recoil moves the weapon, the bigger the value, the fastest.")]
        [SerializeField] private float _recoilSharpness = 50f;

        [Tooltip("Maximum distance the recoil can affect the weapon.")]
        [SerializeField] private float _maxRecoilDistance = 0.2f;

        [Tooltip("How fast the weapon goes back to it's original position after the recoil is finished.")]
        [SerializeField] private float _recoilRestitutionSharpness = 10f;


        [Header("Misc")]
        [Tooltip("Speed at which the aiming animatoin is played.")]
        [SerializeField] private float _aimingAnimationSpeed = 10f;

        [Tooltip("Field of view when not aiming.")]
        [SerializeField] private float _defaultFov = 60f;

        [Tooltip("Portion of the regular FOV to apply to the weapon camera.")]
        [SerializeField] private float _weaponFovMultiplier = 1f;

        [Tooltip("Delay before switching weapon a second time, to avoid recieving multiple inputs from mouse wheel.")]
        [SerializeField] private float _weaponSwitchDelay = 0.2f;

        [Tooltip("Layer to set FPS weapon gameObjects to.")]
        [SerializeField] private LayerMask _fpsWeaponLayer;


        public bool IsAiming { get; private set; }
        public bool IsPointingAtEnemy { get; private set; }
        public int ActiveWeaponIndex { get; private set; }
        public Camera WeaponCamera => _weaponCamera;


        public UnityEvent<M_WeaponController> E_OnSwitchedToWeapon = new();
        public UnityEvent<M_WeaponController, int> E_OnAddedWeapon = new();
        public UnityEvent<M_WeaponController, int> E_OnRemovedWeapon = new();


        private M_WeaponController[] _weaponSlots = new M_WeaponController[9]; // 9 available weapon slots
        
        private float _weaponBobFactor;

        private Vector3 _lastCharacterPosition;
        private Vector3 _weaponMainLocalPosition;
        private Vector3 _weaponBobLocalPosition;

        private Vector3 _weaponRecoilLocalPosition;
        private Vector3 _accumulatedRecoil;

        private float _timeStartedWeaponSwitch;
        private WeaponSwitchState _weaponSwitchState;
        private int _weaponSwitchNewWeaponIndex;

        private PlayerInfoManager _playerInfoManager;

        private void Awake()
        {
            _playerInfoManager = PlayerInfoManager.Instance;
            ActiveWeaponIndex = -1;
            _weaponSwitchState = WeaponSwitchState.Down;
            SetFov(_defaultFov);
            E_OnSwitchedToWeapon.AddListener(OnWeaponSwitched);

            foreach (M_WeaponController weapon in _startingWeapons)
            {
                AddWeapon(weapon);
            }

            SwitchWeapon(true);
        }

        /// <summary>
        /// Handles Input and checks if player is pointing at an enemy.
        /// </summary>
        private void Update()
        {
            if (_playerInfoManager.LockInput)
                return;
            M_WeaponController activeWeapon = GetActiveWeapon();

            if (activeWeapon != null && activeWeapon.IsReloading)
                return;

            if (!HandleShooting(activeWeapon))
                return;

            HandleWeaponSwitching(activeWeapon);
            HandlePointAtEnemy(activeWeapon);
        }

        /// <summary>
        /// Update various animated features in LateUpdate because it needs to override the animated arm position.
        /// Set final weapon socket position based on all the combined animation influences.
        /// </summary>
        private void LateUpdate()
        {
            UpdateWeaponAiming();
            UpdateWeaponBob();
            UpdateWeaponRecoil();
            UpdateWeaponSwitching();

            _weaponParentSocket.localPosition = _weaponMainLocalPosition + _weaponBobLocalPosition + _weaponRecoilLocalPosition;
        }

        /// <summary>
        /// Handles reloading, aiming down the sights, shooting and recoil.
        /// </summary>
        /// <param name="activeWeapon"></param>
        /// <returns>False when a reload has started. This will block the other actions.</returns>
        private bool HandleShooting(M_WeaponController activeWeapon)
        {
            if (activeWeapon != null && _weaponSwitchState == WeaponSwitchState.Up)
            {
                if (!activeWeapon.AutomaticReload && _inputHandler.GetReloadButtonDown() && activeWeapon.CurrentAmmoRatio < 1.0f)
                {
                    IsAiming = false;
                    activeWeapon.StartReloadAnimation();
                    return false;
                }
                IsAiming = _inputHandler.GetAimInputHeld();

                bool hasFired = activeWeapon.HandleShootInputs(
                    _inputHandler.GetFireInputDown(),
                    _inputHandler.GetFireInputHeld(),
                    _inputHandler.GetFireInputReleased());

                if (hasFired)
                {
                    _accumulatedRecoil += Vector3.back * activeWeapon.RecoilForce;
                    _accumulatedRecoil = Vector3.ClampMagnitude(_accumulatedRecoil, _maxRecoilDistance);
                }
            }
            return true;
        }

        /// <summary>
        /// Switches weapon based on what kind of switch input was used.
        /// </summary>
        /// <param name="activeWeapon"></param>
        private void HandleWeaponSwitching(M_WeaponController activeWeapon)
        {
            if (!IsAiming &&
                (activeWeapon == null || !activeWeapon.IsCharging) &&
                (_weaponSwitchState == WeaponSwitchState.Up || _weaponSwitchState == WeaponSwitchState.Down))
            {
                int switchWeaponInput = _inputHandler.GetSwitchWeaponInput();
                if (switchWeaponInput != 0)
                {
                    bool switchUp = switchWeaponInput > 0;
                    SwitchWeapon(switchUp);
                }
                else
                {
                    switchWeaponInput = _inputHandler.GetSelectWeaponInput();
                    if (switchWeaponInput != 0)
                    {
                        if (GetWeaponAtSlotIndex(switchWeaponInput - 1) != null)
                            SwitchToWeaponIndex(switchWeaponInput - 1);
                    }
                }
            }
        }

        /// <summary>
        /// Sets IsPointingAtEnemy at true when pointing at an object that has the Health Component.
        /// </summary>
        /// <param name="activeWeapon"></param>
        private void HandlePointAtEnemy(M_WeaponController activeWeapon)
        {
            IsPointingAtEnemy = false;
            if (activeWeapon)
            {
                if (Physics.Raycast(_weaponCamera.transform.position, _weaponCamera.transform.forward, out RaycastHit hit,
                    1000, -1, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponentInParent<M_Health>() != null)
                    {
                        IsPointingAtEnemy = true;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the FOV of the main camera and the weapon camera simultaneously.
        /// </summary>
        /// <param name="fov"></param>
        public void SetFov(float fov)
        {
            _movement.PlayerCamera.fieldOfView = fov;
            _weaponCamera.fieldOfView = fov * _weaponFovMultiplier;
        }

        /// <summary>
        /// Iterate on all weapon slots to find the next valid weapon to switch to.
        /// </summary>
        /// <param name="ascendingOrder"></param>
        public void SwitchWeapon(bool ascendingOrder)
        {
            int newWeaponIndex = -1;
            int closestSlotDistance = _weaponSlots.Length;
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                if (i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
                {
                    int distanceToActiveIndex = GetDistanceBetweenWeaponSlots(ActiveWeaponIndex, i, ascendingOrder);

                    if (distanceToActiveIndex < closestSlotDistance)
                    {
                        closestSlotDistance = distanceToActiveIndex;
                        newWeaponIndex = i;
                    }
                }
            }

            SwitchToWeaponIndex(newWeaponIndex);
        }

        /// <summary>
        /// Switches to the given weapon index in weapon slots if the new index is a valid weapon that is different from our current one.
        /// </summary>
        /// <param name="newWeaponIndex"></param>
        /// <param name="force"></param>
        public void SwitchToWeaponIndex(int newWeaponIndex, bool force = false)
        {
            if (force || (newWeaponIndex != ActiveWeaponIndex && newWeaponIndex >= 0))
            {
                _weaponSwitchNewWeaponIndex = newWeaponIndex;
                _timeStartedWeaponSwitch = Time.time;

                if (GetActiveWeapon() == null)
                {
                    _weaponMainLocalPosition = _downWeaponPosition.localPosition;
                    _weaponSwitchState = WeaponSwitchState.PutUpNew;
                    ActiveWeaponIndex = _weaponSwitchNewWeaponIndex;

                    M_WeaponController newWeapon = GetWeaponAtSlotIndex(_weaponSwitchNewWeaponIndex);
                    if (E_OnSwitchedToWeapon != null)
                    {
                        E_OnSwitchedToWeapon.Invoke(newWeapon);
                    }
                }
                else
                {
                    _weaponSwitchState = WeaponSwitchState.PutDownPrevious;
                }
            }
        }

        /// <summary>
        /// Checks if we already have the given weapon prefab in our invertory.
        /// </summary>
        /// <param name="weaponPrefab"></param>
        /// <returns>Weapon that is in our inventory. Returns null if we don't have it.</returns>
        public M_WeaponController HasWeapon(M_WeaponController weaponPrefab)
        {
            // Checks if we already have a weapon coming from the specified prefab
            for (int index = 0; index < _weaponSlots.Length; index++)
            {
                M_WeaponController weapon = _weaponSlots[index];
                if (weapon != null && weapon.SourcePrefab == weaponPrefab.gameObject)
                {
                    return weapon;
                }
            }

            return null;
        }

        /// <summary>
        /// Updates weapon position and camera FoV for the aiming transition.
        /// </summary>
        private void UpdateWeaponAiming()
        {
            if (_weaponSwitchState == WeaponSwitchState.Up)
            {
                M_WeaponController activeWeapon = GetActiveWeapon();
                if (IsAiming && activeWeapon)
                {
                    _weaponMainLocalPosition = Vector3.Lerp(_weaponMainLocalPosition,
                        _aimingWeaponPosition.localPosition + activeWeapon.AimOffset,
                        _aimingAnimationSpeed * Time.deltaTime);
                    SetFov(Mathf.Lerp(_movement.PlayerCamera.fieldOfView,
                        activeWeapon.AimZoomRatio * _defaultFov, _aimingAnimationSpeed * Time.deltaTime));
                }
                else
                {
                    _weaponMainLocalPosition = Vector3.Lerp(_weaponMainLocalPosition,
                        _defaultWeaponPosition.localPosition, _aimingAnimationSpeed * Time.deltaTime);
                    SetFov(Mathf.Lerp(_movement.PlayerCamera.fieldOfView, _defaultFov,
                        _aimingAnimationSpeed * Time.deltaTime));
                }
            }
        }

        /// <summary>
        /// Updates the weapon bob animation based on character speed.
        /// Calculates a smoothed weapon bob amount based on how close to our max grounded movement velocity we are.
        /// Calculate vertical and horizontal weapon bob values based on a sine function.
        /// </summary>
        private void UpdateWeaponBob()
        {
            if (Time.deltaTime > 0f)
            {
                Vector3 playerCharacterVelocity =
                    (_movement.transform.position - _lastCharacterPosition) / Time.deltaTime;

                float characterMovementFactor = 0f;
                if (_movement.IsGrounded)
                {
                    characterMovementFactor =
                        Mathf.Clamp01(playerCharacterVelocity.magnitude /
                                      (_movement.MaxSpeedOnGround *
                                       _movement.SprintSpeedModifier));
                }

                _weaponBobFactor =
                    Mathf.Lerp(_weaponBobFactor, characterMovementFactor, _bobSharpness * Time.deltaTime);

                float bobAmount = IsAiming ? _aimingBobAmount : _defaultBobAmount;
                float frequency = _bobFrequency;
                float hBobValue = Mathf.Sin(Time.time * frequency) * bobAmount * _weaponBobFactor;
                float vBobValue = ((Mathf.Sin(Time.time * frequency * 2f) * 0.5f) + 0.5f) * bobAmount *
                                  _weaponBobFactor;

                _weaponBobLocalPosition.x = hBobValue;
                _weaponBobLocalPosition.y = Mathf.Abs(vBobValue);

                _lastCharacterPosition = _movement.transform.position;
            }
        }

        /// <summary>
        /// Updates the weapon recoil animation.
        /// If the accumulated recoil is further away from the current position, make the current position move towards the recoil target
        /// Otherwise, move recoil position to make it recover towards its resting pose.
        /// </summary>
        private void UpdateWeaponRecoil()
        {
            if (_weaponRecoilLocalPosition.z >= _accumulatedRecoil.z * 0.99f)
            {
                _weaponRecoilLocalPosition = Vector3.Lerp(_weaponRecoilLocalPosition, _accumulatedRecoil,
                    _recoilSharpness * Time.deltaTime);
            }
            else
            {
                _weaponRecoilLocalPosition = Vector3.Lerp(_weaponRecoilLocalPosition, Vector3.zero,
                    _recoilRestitutionSharpness * Time.deltaTime);
                _accumulatedRecoil = _weaponRecoilLocalPosition;
            }
        }

        /// <summary>
        /// Updates the animated transition of switching weapons
        /// </summary>
        private void UpdateWeaponSwitching()
        {
            float switchingTimeFactor = 0f;
            if (_weaponSwitchDelay == 0f)
            {
                switchingTimeFactor = 1f;
            }
            else
            {
                switchingTimeFactor = Mathf.Clamp01((Time.time - _timeStartedWeaponSwitch) / _weaponSwitchDelay);
            }

            if (switchingTimeFactor >= 1f)
            {
                if (_weaponSwitchState == WeaponSwitchState.PutDownPrevious)
                {
                    M_WeaponController oldWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    if (oldWeapon != null)
                    {
                        oldWeapon.ShowWeapon(false);
                    }

                    ActiveWeaponIndex = _weaponSwitchNewWeaponIndex;
                    switchingTimeFactor = 0f;

                    M_WeaponController newWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    if (E_OnSwitchedToWeapon != null)
                    {
                        E_OnSwitchedToWeapon.Invoke(newWeapon);
                    }

                    if (newWeapon)
                    {
                        _timeStartedWeaponSwitch = Time.time;
                        _weaponSwitchState = WeaponSwitchState.PutUpNew;
                    }
                    else
                    {
                        _weaponSwitchState = WeaponSwitchState.Down;
                    }
                }
                else if (_weaponSwitchState == WeaponSwitchState.PutUpNew)
                {
                    _weaponSwitchState = WeaponSwitchState.Up;
                }
            }

            if (_weaponSwitchState == WeaponSwitchState.PutDownPrevious)
            {
                _weaponMainLocalPosition = Vector3.Lerp(_defaultWeaponPosition.localPosition,
                    _downWeaponPosition.localPosition, switchingTimeFactor);
            }
            else if (_weaponSwitchState == WeaponSwitchState.PutUpNew)
            {
                _weaponMainLocalPosition = Vector3.Lerp(_downWeaponPosition.localPosition,
                    _defaultWeaponPosition.localPosition, switchingTimeFactor);
            }
        }

        /// <summary>
        /// Adds a weapon to our inventory and spawns it in.
        /// If there's no active weapon then this weapon will automatically be selected.
        /// </summary>
        /// <param name="weaponPrefab"></param>
        /// <returns></returns>
        public bool AddWeapon(M_WeaponController weaponPrefab)
        {
            if (HasWeapon(weaponPrefab) != null)
                return false;

            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                if (_weaponSlots[i] != null)
                    continue;

                M_WeaponController weaponInstance = Instantiate(weaponPrefab, _weaponParentSocket);
                weaponInstance.transform.localPosition = Vector3.zero;
                weaponInstance.transform.localRotation = Quaternion.identity;

                weaponInstance.Owner = gameObject;
                weaponInstance.SourcePrefab = weaponPrefab.gameObject;
                weaponInstance.ShowWeapon(false);

                int layerIndex = Mathf.RoundToInt(Mathf.Log(_fpsWeaponLayer.value, 2));
                foreach (Transform t in weaponInstance.gameObject.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = layerIndex;
                }

                _weaponSlots[i] = weaponInstance;
                E_OnAddedWeapon.Invoke(weaponInstance, i);
                return true;
            }

            if (GetActiveWeapon() == null)
            {
                SwitchWeapon(true);
            }

            return false;
        }

        /// <summary>
        /// Removes weapon and switches to the next weapon of given weapon was the active weapon.
        /// </summary>
        /// <param name="weaponInstance"></param>
        /// <returns></returns>
        public bool RemoveWeapon(M_WeaponController weaponInstance)
        {
            // Look through our slots for that weapon
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                // when weapon found, remove it
                if (_weaponSlots[i] == weaponInstance)
                {
                    _weaponSlots[i] = null;

                    E_OnRemovedWeapon.Invoke(weaponInstance, i);

                    Destroy(weaponInstance.gameObject);

                    // Handle case of removing active weapon (switch to next weapon)
                    if (i == ActiveWeaponIndex)
                    {
                        SwitchWeapon(true);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the active weapons controller.
        /// </summary>
        /// <returns></returns>
        public M_WeaponController GetActiveWeapon()
        {
            return GetWeaponAtSlotIndex(ActiveWeaponIndex);
        }

        /// <summary>
        /// Returns the weapon at the given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public M_WeaponController GetWeaponAtSlotIndex(int index)
        {
            if (index >= 0 && index < _weaponSlots.Length)
                return _weaponSlots[index];

            return null;
        }

        /// <summary>
        /// Calculates the "distance" between two weapon slot indexes.
        /// For example: if we have 5 weapon slots, the distance between slots #2 and #4 would be 2 in ascending order, and 3 in descending order.
        /// </summary>
        /// <param name="fromSlotIndex"></param>
        /// <param name="toSlotIndex"></param>
        /// <param name="ascendingOrder"></param>
        /// <returns></returns>
        private int GetDistanceBetweenWeaponSlots(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
        {
            int distanceBetweenSlots = 0;

            if (ascendingOrder)
            {
                distanceBetweenSlots = toSlotIndex - fromSlotIndex;
            }
            else
            {
                distanceBetweenSlots = -1 * (toSlotIndex - fromSlotIndex);
            }

            if (distanceBetweenSlots < 0)
            {
                distanceBetweenSlots = _weaponSlots.Length + distanceBetweenSlots;
            }

            return distanceBetweenSlots;
        }

        /// <summary>
        /// Show the given weapon on switch.
        /// </summary>
        /// <param name="newWeapon"></param>
        private void OnWeaponSwitched(M_WeaponController newWeapon)
        {
            if (newWeapon != null)
            {
                newWeapon.ShowWeapon(true);
            }
        }
    }
}