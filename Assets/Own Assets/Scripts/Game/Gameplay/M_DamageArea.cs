using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    public class M_DamageArea : MonoBehaviour
    {
        [Tooltip("Area of damage when the projectile hits something")]
        [SerializeField] private float _areaOfEffectDistance = 5f;

        [Tooltip("Damage multiplier over distance for area of effect")]
        [SerializeField] private AnimationCurve _damageRatioOverDistance;

        [Header("Debug")] [Tooltip("Color of the area of effect radius")]
        [SerializeField] private Color _areaOfEffectColor = Color.red * 0.5f;

        public void InflictDamageInArea(float damage, Vector3 center, LayerMask layers, QueryTriggerInteraction interaction, GameObject owner)
        {
            Dictionary<M_Health, M_Damageable> uniqueDamagedHealths = new ();
            Collider[] affectedColliders = Physics.OverlapSphere(center, _areaOfEffectDistance, layers, interaction);
            foreach (Collider collider in affectedColliders)
            {
                M_Damageable damageable = collider.GetComponent<M_Damageable>();
                if (damageable)
                {
                    M_Health health = damageable.GetComponentInParent<M_Health>();
                    if (health && !uniqueDamagedHealths.ContainsKey(health))
                    {
                        uniqueDamagedHealths.Add(health, damageable);
                    }
                }
            }

            // Apply damages with distance falloff
            foreach (M_Damageable uniqueDamageable in uniqueDamagedHealths.Values)
            {
                float distance = Vector3.Distance(uniqueDamageable.transform.position, transform.position);
                uniqueDamageable.InflictDamage(damage * _damageRatioOverDistance.Evaluate(distance / _areaOfEffectDistance), true, owner);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _areaOfEffectColor;
            Gizmos.DrawSphere(transform.position, _areaOfEffectDistance);
        }
    }
}