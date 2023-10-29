using UnityEngine;

namespace AttackDirectionFix.Utils
{
    public static class VectorUtils
    {
        public static Vector3 ClosestPointAlongRay(Ray ray, Vector3 target)
        {
            // This math is magic don't worry about it, it just works :)
            return ray.origin + (ray.direction * Vector3.Dot(ray.direction, target - ray.origin));
        }
    }
}
