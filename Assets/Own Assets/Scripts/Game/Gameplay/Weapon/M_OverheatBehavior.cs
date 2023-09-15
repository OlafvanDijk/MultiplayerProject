using UnityEngine;
using System.Collections.Generic;
using System;
using EmissionModule = UnityEngine.ParticleSystem.EmissionModule;

namespace Game.Gameplay.Weapons
{
    [RequireComponent(typeof(M_WeaponController))]
    public class M_OverheatBehavior : MonoBehaviour
    {
        [Serializable]
        public struct RendererIndexData
        {
            public Renderer Renderer;
            public int MaterialIndex;

            public RendererIndexData(Renderer renderer, int index)
            {
                this.Renderer = renderer;
                this.MaterialIndex = index;
            }
        }

        [Header("General")]
        [Tooltip("Reference to the Weapon Controller")]
        [SerializeField] private M_WeaponController _weapon;

        [Header("Visual")]
        [Tooltip("The VFX to scale the spawn rate based on the ammo ratio")]
        [SerializeField] private ParticleSystem _steamVfx;

        [Tooltip("The emission rate for the effect when fully overheated")]
        [SerializeField] private float _steamVfxEmissionRateMax = 8f;

        //Set gradient field to HDR
        [Tooltip("Overheat color based on ammo ratio")]
        [GradientUsage(true)] [SerializeField] private Gradient _overheatGradient;

        [Tooltip("The material for overheating color animation")]
        [SerializeField] private Material _overheatingMaterial;


        [Header("Sound")]
        [Tooltip("Reference to the AudioSource")]
        [SerializeField] private AudioSource _audioSource;

        [Tooltip("Sound played when a cell are cooling")]
        [SerializeField] private AudioClip _coolingCellsSound;

        [Tooltip("Curve for ammo to volume ratio")]
        [SerializeField] private AnimationCurve _ammoToVolumeRatioCurve;

        
        private float _lastAmmoRatio;

        private EmissionModule _steamVfxEmissionModule;
        private MaterialPropertyBlock _overheatMaterialPropertyBlock;
        private List<RendererIndexData> _overheatingRenderersData = new();

        private void Awake()
        {
            EmissionModule emissionModule = _steamVfx.emission;
            emissionModule.rateOverTimeMultiplier = 0f;

            _overheatingRenderersData = new List<RendererIndexData>();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] == _overheatingMaterial)
                        _overheatingRenderersData.Add(new RendererIndexData(renderer, i));
                }
            }

            _overheatMaterialPropertyBlock = new MaterialPropertyBlock();
            _steamVfxEmissionModule = _steamVfx.emission;

            _weapon = GetComponent<M_WeaponController>();

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = _coolingCellsSound;
            //m_AudioSource.outputAudioMixerGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponOverheat);
        }

        private void Update()
        {
            // visual smoke shooting out of the gun
            float currentAmmoRatio = _weapon.CurrentAmmoRatio;
            if (currentAmmoRatio != _lastAmmoRatio)
            {
                _overheatMaterialPropertyBlock.SetColor("_EmissionColor",
                    _overheatGradient.Evaluate(1f - currentAmmoRatio));

                foreach (RendererIndexData data in _overheatingRenderersData)
                {
                    data.Renderer.SetPropertyBlock(_overheatMaterialPropertyBlock, data.MaterialIndex);
                }

                _steamVfxEmissionModule.rateOverTimeMultiplier = _steamVfxEmissionRateMax * (1f - currentAmmoRatio);
            }

            // cooling sound
            if (_coolingCellsSound)
            {
                if (!_audioSource.isPlaying && currentAmmoRatio != 1 && _weapon.IsWeaponActive && _weapon.IsCooling)
                {
                    _audioSource.Play();
                }
                else if (_audioSource.isPlaying && (currentAmmoRatio == 1 || !_weapon.IsWeaponActive || !_weapon.IsCooling))
                {
                    _audioSource.Stop();
                    return;
                }

                _audioSource.volume = _ammoToVolumeRatioCurve.Evaluate(1 - currentAmmoRatio);
            }

            _lastAmmoRatio = currentAmmoRatio;
        }
    }
}