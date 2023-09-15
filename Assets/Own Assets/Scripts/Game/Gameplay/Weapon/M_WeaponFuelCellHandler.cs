using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Weapons
{
    [RequireComponent(typeof(M_WeaponController))]
    public class M_WeaponFuelCellHandler : MonoBehaviour
    {
        [Tooltip("Retract All Fuel Cells Simultaneously")]
        [SerializeField] private bool _simultaneousFuelCellsUsage = false;

        [Tooltip("List of GameObjects representing the fuel cells on the weapon")]
        [SerializeField] private List<GameObject> _fuelCells;

        [Tooltip("Cell local position when used")]
        [SerializeField] private Vector3 _fuelCellUsedPosition;

        [Tooltip("Cell local position before use")]
        [SerializeField] private Vector3 _fuelCellUnusedPosition = new Vector3(0f, -0.1f, 0f);

        private M_WeaponController _weapon;
        private List<bool> _fuelCellsCooled = new();

        private void Start()
        {
            _weapon = GetComponent<M_WeaponController>();

            foreach (GameObject fuelCell in _fuelCells)
            {
                _fuelCellsCooled.Add(true);
            }
        }

        private void Update()
        {
            if (_simultaneousFuelCellsUsage)
            {
                foreach (GameObject fuelCell in _fuelCells)
                {
                    fuelCell.transform.localPosition = Vector3.Lerp(_fuelCellUsedPosition, _fuelCellUnusedPosition, _weapon.CurrentAmmoRatio);
                }
            }
            else
            {
                for (int i = 0; i < _fuelCells.Count; i++)
                {
                    float length = _fuelCells.Count;
                    float lim1 = i / length;
                    float lim2 = (i + 1) / length;

                    float value = Mathf.InverseLerp(lim1, lim2, _weapon.CurrentAmmoRatio);
                    value = Mathf.Clamp01(value);

                    _fuelCells[i].transform.localPosition = Vector3.Lerp(_fuelCellUsedPosition, _fuelCellUnusedPosition, value);
                }
            }
        }
    }
}