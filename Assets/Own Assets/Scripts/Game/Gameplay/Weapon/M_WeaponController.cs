using Game.Gameplay.Weapons.Projectiles;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay.Weapons
{
    public enum WeaponShootType
    {
        Manual,
        Automatic,
        Charge,
    }

    [Serializable]
    public struct CrosshairData
    {
        [Tooltip("The image that will be used for this weapon's crosshair")]
        public Sprite CrosshairSprite;

        [Tooltip("The size of the crosshair image")]
        public int CrosshairSize;

        [Tooltip("The color of the crosshair image")]
        public Color CrosshairColor;
    }

    [RequireComponent(typeof(AudioSource))]
    public class M_WeaponController : MonoBehaviour
    {
        [Header("Information")]
        [Tooltip("The name that will be displayed in the UI for this weapon")]
        [SerializeField] private string _weaponName;

        [Tooltip("The image that will be displayed in the UI for this weapon")]
        [SerializeField] private Sprite _weaponIcon;

        [Tooltip("Default data for the crosshair")]
        [SerializeField] private CrosshairData _crosshairDataDefault;

        [Tooltip("Data for the crosshair when targeting an enemy")]
        [SerializeField] private CrosshairData _crosshairDataTargetInSight;


        [Header("Internal References")]
        [Tooltip("The root object for the weapon, this is what will be deactivated when the weapon isn't active")]
        [SerializeField] private GameObject _weaponRoot;

        [Tooltip("Tip of the weapon, where the projectiles are shot")]
        [SerializeField] private Transform _weaponMuzzle;


        [Header("Shoot Parameters")]
        [Tooltip("The type of weapon wil affect how it shoots")]
        [SerializeField] private WeaponShootType _shootType;

        [Tooltip("The projectile prefab")] [SerializeField] private M_ProjectileBase _projectilePrefab;

        [Tooltip("Minimum duration between two shots")]
        [SerializeField] private float _delayBetweenShots = 0.5f;

        [Tooltip("Angle for the cone in which the bullets will be shot randomly (0 means no spread at all)")]
        [SerializeField] private float _bulletSpreadAngle = 0f;

        [Tooltip("Amount of bullets per shot")]
        [SerializeField] private int _bulletsPerShot = 1;

        [Tooltip("Force that will push back the weapon after each shot")]
        [Range(0f, 2f)]
        [SerializeField] private float _recoilForce = 1;

        [Tooltip("Ratio of the default FOV that this weapon applies while aiming")]
        [Range(0f, 1f)]
        [SerializeField] private float _aimZoomRatio = 1f;

        [Tooltip("Translation to apply to weapon arm when aiming with this weapon")]
        [SerializeField] private Vector3 _aimOffset;

        [Header("Ammo Parameters")]
        [Tooltip("Should the player manually reload")]
        [SerializeField] private bool _automaticReload = true;

        [Tooltip("Has physical clip on the weapon and ammo shells are ejected when firing")]
        [SerializeField] private bool _hasPhysicalBullets = false;

        [Tooltip("Number of bullets in a clip")]
        [SerializeField] private int _clipSize = 30;

        [Tooltip("Bullet Shell Casing")]
        [SerializeField] private GameObject _shellCasing;

        [Tooltip("Weapon Ejection Port for physical ammo")]
        [SerializeField] private Transform _ejectionPort;

        [Tooltip("Force applied on the shell")]
        [Range(0.0f, 5.0f)] [SerializeField] private float _shellCasingEjectionForce = 2.0f;

        [Tooltip("Maximum number of shell that can be spawned before reuse")]
        [Range(1, 30)] [SerializeField] private int _shellPoolSize = 1;

        [Tooltip("Amount of ammo reloaded per second")]
        [SerializeField] private float _ammoReloadRate = 1f;

        [Tooltip("Delay after the last shot before starting to reload")]
        [SerializeField] private float _ammoReloadDelay = 2f;

        [Tooltip("Maximum amount of ammo in the gun")]
        [SerializeField] private int _maxAmmo = 8;


        [Header("Charging parameters (charging weapons only)")]
        [Tooltip("Trigger a shot when maximum charge is reached")]
        [SerializeField] private bool _automaticReleaseOnCharged;

        [Tooltip("Duration to reach maximum charge")]
        [SerializeField] private float _maxChargeDuration = 2f;

        [Tooltip("Initial ammo used when starting to charge")]
        [SerializeField] private float _ammoUsedOnStartCharge = 1f;

        [Tooltip("Additional ammo used when charge reaches its maximum")]
        [SerializeField] private float _ammoUsageRateWhileCharging = 1f;


        [Header("Audio & Visual")]
        [Tooltip("Optional weapon animator for OnShoot animations")]
        [SerializeField] private Animator _weaponAnimator;

        [Tooltip("Prefab of the muzzle flash")]
        [SerializeField] private GameObject _muzzleFlashPrefab;

        [Tooltip("Unparent the muzzle flash instance on spawn")]
        [SerializeField] private bool _unparentMuzzleFlash;

        [Tooltip("sound played when shooting")]
        [SerializeField] private AudioClip _shootSfx;

        [Tooltip("Sound played when changing to this weapon")]
        [SerializeField] private AudioClip _changeWeaponSfx;

        [Tooltip("Continuous Shooting Sound")]
        [SerializeField] private bool _useContinuousShootSound = false;

        [Tooltip("Continuous Shooting Sound Start")]
        [SerializeField] private AudioClip ContinuousShootStartSfx;

        [Tooltip("Continuous Shooting Sound Loop")]
        [SerializeField] private AudioClip ContinuousShootLoopSfx;

        [Tooltip("Continuous Shooting Sound End")]
        [SerializeField] private AudioClip ContinuousShootEndSfx;

        public GameObject Owner { get; set; }
        public GameObject SourcePrefab { get; set; }

        public float CurrentAmmoRatio { get; private set; }
        public float CurrentCharge { get; private set; }
        public float LastChargeTriggerTimestamp { get; private set; }
       
        public bool IsCharging { get; private set; }
        public bool IsWeaponActive { get; private set; }
        public bool IsCooling { get; private set; }
        public bool IsReloading { get; private set; }
        public bool AutomaticReload => _automaticReload;

        public float AimZoomRatio => _aimZoomRatio;
        public float RecoilForce => _recoilForce;

        public Vector3 AimOffset => _aimOffset;
        public Vector3 MuzzleWorldVelocity { get; private set; }

        public int GetCarriedPhysicalBullets() => _carriedPhysicalBullets;
        public int GetCurrentAmmo() => Mathf.FloorToInt(_currentAmmo);
        public float GetAmmoNeededToShoot() =>
            (_shootType != WeaponShootType.Charge ? 1f : Mathf.Max(1f, _ammoUsedOnStartCharge)) /
            (_maxAmmo * _bulletsPerShot);


        public UnityEvent E_OnShoot = new();
        public UnityEvent OnShootProcessed = new();

        
        private bool _wantsToShoot = false;

        private int _carriedPhysicalBullets;
        private float _currentAmmo;
        private float _lastTimeShot = Mathf.NegativeInfinity;

        private Vector3 _lastMuzzlePosition;

        private Queue<Rigidbody> _physicalAmmoPool;

        private AudioSource _shootAudioSource;
        private AudioSource _continuousShootAudioSource = null;

        private const string AnimAttackParameter = "Attack";


        private void Awake()
        {
            _currentAmmo = _maxAmmo;
            _carriedPhysicalBullets = _hasPhysicalBullets ? _clipSize : 0;
            _lastMuzzlePosition = _weaponMuzzle.position;

            _shootAudioSource = GetComponent<AudioSource>();

            SetupContinuesSFX();
            SetupAmmoPool();
        }

        private void Update()
        {
            UpdateAmmo();
            UpdateCharge();
            UpdateContinuousShootSound();

            if (Time.deltaTime > 0)
            {
                MuzzleWorldVelocity = (_weaponMuzzle.position - _lastMuzzlePosition) / Time.deltaTime;
                _lastMuzzlePosition = _weaponMuzzle.position;
            }
        }

        public void AddCarriablePhysicalBullets(int count)
        {
            _carriedPhysicalBullets = Mathf.Max(_carriedPhysicalBullets + count, _maxAmmo);
        }

        public void StartReloadAnimation()
        {
            if (_currentAmmo < _carriedPhysicalBullets)
            {
                GetComponent<Animator>().SetTrigger("Reload");
                IsReloading = true;
            }
        }

        public void ShowWeapon(bool show)
        {
            _weaponRoot.SetActive(show);

            if (show && _changeWeaponSfx)
            {
                _shootAudioSource.PlayOneShot(_changeWeaponSfx);
            }

            IsWeaponActive = show;
        }

        public void UseAmmo(float amount)
        {
            _currentAmmo = Mathf.Clamp(_currentAmmo - amount, 0f, _maxAmmo);
            _carriedPhysicalBullets -= Mathf.RoundToInt(amount);
            _carriedPhysicalBullets = Mathf.Clamp(_carriedPhysicalBullets, 0, _maxAmmo);
            _lastTimeShot = Time.time;
        }

        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            _wantsToShoot = inputDown || inputHeld;
            switch (_shootType)
            {
                case WeaponShootType.Manual:
                    if (inputDown)
                    {
                        return TryShoot();
                    }

                    return false;

                case WeaponShootType.Automatic:
                    if (inputHeld)
                    {
                        return TryShoot();
                    }

                    return false;

                case WeaponShootType.Charge:
                    if (inputHeld)
                    {
                        TryBeginCharge();
                    }

                    // Check if we released charge or if the weapon shoot autmatically when it's fully charged
                    if (inputUp || (_automaticReleaseOnCharged && CurrentCharge >= 1f))
                    {
                        return TryReleaseCharge();
                    }

                    return false;

                default:
                    return false;
            }
        }

        public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            float spreadAngleRatio = _bulletSpreadAngle / 180f;
            Vector3 spreadWorldDirection = Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere,
                spreadAngleRatio);

            return spreadWorldDirection;
        }

        private bool TryShoot()
        {
            if (_currentAmmo >= 1f
                && _lastTimeShot + _delayBetweenShots < Time.time)
            {
                HandleShoot();
                _currentAmmo -= 1f;

                return true;
            }

            return false;
        }

        private bool TryBeginCharge()
        {
            if (!IsCharging
                && _currentAmmo >= _ammoUsedOnStartCharge
                && Mathf.FloorToInt((_currentAmmo - _ammoUsedOnStartCharge) * _bulletsPerShot) > 0
                && _lastTimeShot + _delayBetweenShots < Time.time)
            {
                UseAmmo(_ammoUsedOnStartCharge);

                LastChargeTriggerTimestamp = Time.time;
                IsCharging = true;

                return true;
            }

            return false;
        }

        private bool TryReleaseCharge()
        {
            if (IsCharging)
            {
                HandleShoot();

                CurrentCharge = 0f;
                IsCharging = false;

                return true;
            }

            return false;
        }

        private void HandleShoot()
        {
            //Todo call same method on server

            int bulletsPerShotFinal = _shootType == WeaponShootType.Charge
                ? Mathf.CeilToInt(CurrentCharge * _bulletsPerShot)
                : _bulletsPerShot;

            // spawn all bullets with random direction
            for (int i = 0; i < bulletsPerShotFinal; i++)
            {
                Vector3 shotDirection = GetShotDirectionWithinSpread(_weaponMuzzle);
                M_ProjectileBase newProjectile = Instantiate(_projectilePrefab, _weaponMuzzle.position, Quaternion.LookRotation(shotDirection));
                newProjectile.Shoot(this);
            }

            // muzzle flash
            if (_muzzleFlashPrefab != null)
            {
                GameObject muzzleFlashInstance = Instantiate(_muzzleFlashPrefab, _weaponMuzzle.position,
                    _weaponMuzzle.rotation, _weaponMuzzle.transform);
                // Unparent the muzzleFlashInstance
                if (_unparentMuzzleFlash)
                {
                    muzzleFlashInstance.transform.SetParent(null);
                }

                Destroy(muzzleFlashInstance, 2f);
            }

            if (_hasPhysicalBullets)
            {
                ShootShell();
                _carriedPhysicalBullets--;
            }

            _lastTimeShot = Time.time;

            // play shoot SFX
            if (_shootSfx && !_useContinuousShootSound)
            {
                _shootAudioSource.PlayOneShot(_shootSfx);
            }

            // Trigger attack animation if there is any
            if (_weaponAnimator)
            {
                _weaponAnimator.SetTrigger(AnimAttackParameter);
            }

            E_OnShoot?.Invoke();
            OnShootProcessed?.Invoke();
        }

        private void SetupContinuesSFX()
        {
            if (!_useContinuousShootSound)
                return;
            _continuousShootAudioSource = gameObject.AddComponent<AudioSource>();
            _continuousShootAudioSource.playOnAwake = false;
            _continuousShootAudioSource.clip = ContinuousShootLoopSfx;
            //_continuousShootAudioSource.outputAudioMixerGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponShoot);
            _continuousShootAudioSource.loop = true;
        }

        private void SetupAmmoPool()
        {
            if (!_hasPhysicalBullets)
                return;

            _physicalAmmoPool = new Queue<Rigidbody>(_shellPoolSize);

            for (int i = 0; i < _shellPoolSize; i++)
            {
                GameObject shell = Instantiate(_shellCasing, transform);
                shell.SetActive(false);
                _physicalAmmoPool.Enqueue(shell.GetComponent<Rigidbody>());
            }

            //TODO also do this on the server but only let the server ones deal damage.
        }

        private void ShootShell()
        {
            Rigidbody nextShell = _physicalAmmoPool.Dequeue();

            nextShell.transform.position = _ejectionPort.transform.position;
            nextShell.transform.rotation = _ejectionPort.transform.rotation;
            nextShell.gameObject.SetActive(true);
            nextShell.transform.SetParent(null);
            nextShell.collisionDetectionMode = CollisionDetectionMode.Continuous;
            nextShell.AddForce(nextShell.transform.up * _shellCasingEjectionForce, ForceMode.Impulse);

            _physicalAmmoPool.Enqueue(nextShell);

            //TODO do same on server
        }

        private void PlaySFX(AudioClip sfx)
        {
            //AudioUtility.CreateSFX(sfx, transform.position, AudioUtility.AudioGroups.WeaponShoot, 0.0f);
        }

        private void Reload()
        {
            if (_carriedPhysicalBullets > 0)
            {
                _currentAmmo = Mathf.Min(_carriedPhysicalBullets, _clipSize);
            }

            IsReloading = false;
        }

        private void UpdateAmmo()
        {
            if (_automaticReload && _lastTimeShot + _ammoReloadDelay < Time.time && _currentAmmo < _maxAmmo && !IsCharging)
            {
                // reloads weapon over time
                _currentAmmo += _ammoReloadRate * Time.deltaTime;

                // limits ammo to max value
                _currentAmmo = Mathf.Clamp(_currentAmmo, 0, _maxAmmo);

                IsCooling = true;
            }
            else
            {
                IsCooling = false;
            }

            if (_maxAmmo == Mathf.Infinity)
            {
                CurrentAmmoRatio = 1f;
            }
            else
            {
                CurrentAmmoRatio = _currentAmmo / _maxAmmo;
            }
        }

        private void UpdateCharge()
        {
            if (IsCharging)
            {
                if (CurrentCharge < 1f)
                {
                    float chargeLeft = 1f - CurrentCharge;

                    // Calculate how much charge ratio to add this frame
                    float chargeAdded = 0f;
                    if (_maxChargeDuration <= 0f)
                    {
                        chargeAdded = chargeLeft;
                    }
                    else
                    {
                        chargeAdded = (1f / _maxChargeDuration) * Time.deltaTime;
                    }

                    chargeAdded = Mathf.Clamp(chargeAdded, 0f, chargeLeft);

                    // See if we can actually add this charge
                    float ammoThisChargeWouldRequire = chargeAdded * _ammoUsageRateWhileCharging;
                    if (ammoThisChargeWouldRequire <= _currentAmmo)
                    {
                        // Use ammo based on charge added
                        UseAmmo(ammoThisChargeWouldRequire);

                        // set current charge ratio
                        CurrentCharge = Mathf.Clamp01(CurrentCharge + chargeAdded);
                    }
                }
            }
        }

        private void UpdateContinuousShootSound()
        {
            if (_useContinuousShootSound)
            {
                if (_wantsToShoot && _currentAmmo >= 1f)
                {
                    if (!_continuousShootAudioSource.isPlaying)
                    {
                        _shootAudioSource.PlayOneShot(_shootSfx);
                        _shootAudioSource.PlayOneShot(ContinuousShootStartSfx);
                        _continuousShootAudioSource.Play();
                    }
                }
                else if (_continuousShootAudioSource.isPlaying)
                {
                    _shootAudioSource.PlayOneShot(ContinuousShootEndSfx);
                    _continuousShootAudioSource.Stop();
                }
            }
        }
    }
}