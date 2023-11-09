using RoR2.Projectile;
using UnityEngine;

namespace AttackDirectionFix
{
    public class FireProjectileInfoTracker : MonoBehaviour
    {
        public static void InitPatch()
        {
            On.RoR2.Projectile.ProjectileManager.InitializeProjectile += (orig, projectileController, fireProjectileInfo) =>
            {
                orig(projectileController, fireProjectileInfo);

                FireProjectileInfoTracker tracker = projectileController.gameObject.AddComponent<FireProjectileInfoTracker>();
                tracker.FireInfo = fireProjectileInfo;
            };
        }

        public FireProjectileInfo FireInfo { get; private set; }
    }
}
