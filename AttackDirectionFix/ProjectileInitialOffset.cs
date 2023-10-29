using RoR2.Projectile;
using UnityEngine;

namespace AttackDirectionFix
{
    [RequireComponent(typeof(ProjectileGhostController))]
    public class ProjectileInitialOffset : MonoBehaviour
    {
        public Vector3 InitialPositionOffset;
        public Quaternion InitialRotationOffset = Quaternion.identity;

        public float InterpolationTime;

        float _age;

        float interpolationFraction => Mathf.Pow(Mathf.Clamp01(_age / InterpolationTime), 1f / 3f);

        public Vector3 CurrentPositionOffset => Vector3.Lerp(InitialPositionOffset, Vector3.zero, interpolationFraction);
        public Quaternion CurrentRotationOffset => Quaternion.Lerp(InitialRotationOffset, Quaternion.identity, interpolationFraction);

        void FixedUpdate()
        {
            if (_age < InterpolationTime)
            {
                _age += Time.fixedDeltaTime;
            }
            else
            {
                Destroy(this);
            }
        }
    }
}
