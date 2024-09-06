using UnityEngine;

namespace AttackDirectionFix
{
    public class ProjectileInitialOffset : MonoBehaviour
    {
        public float StartTime;

        public Vector3 InitialPositionOffset;
        public Quaternion InitialRotationOffset = Quaternion.identity;

        public float InterpolationTime;

        float age => Time.time - StartTime;

        float interpolationFraction => Mathf.Pow(Mathf.Clamp01(age / InterpolationTime), 1f / 3f);

        public Vector3 CurrentPositionOffset => Vector3.Lerp(InitialPositionOffset, Vector3.zero, interpolationFraction);
        public Quaternion CurrentRotationOffset => Quaternion.Lerp(InitialRotationOffset, Quaternion.identity, interpolationFraction);

        void FixedUpdate()
        {
            if (age >= InterpolationTime)
            {
                Destroy(this);
            }
        }
    }
}
