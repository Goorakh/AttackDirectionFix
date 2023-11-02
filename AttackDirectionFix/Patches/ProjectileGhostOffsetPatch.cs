using RoR2;
using RoR2.ConVar;
using RoR2.Projectile;
using UnityEngine;

namespace AttackDirectionFix.Patches
{
    static class ProjectileGhostOffsetPatch
    {
        static readonly FloatConVar _cvProjectileInterpolationTime = new FloatConVar("projectile_visual_interp", ConVarFlags.None, "0.7", "");

        public static void Init()
        {
            On.RoR2.Projectile.ProjectileController.Start += ProjectileController_Start;

            On.RoR2.Projectile.ProjectileGhostController.LerpTransform += ProjectileGhostController_LerpTransform;
            On.RoR2.Projectile.ProjectileGhostController.CopyTransform += ProjectileGhostController_CopyTransform;

            On.RoR2.Projectile.ProjectileStickOnImpact.TrySticking += ProjectileStickOnImpact_TrySticking;
        }

        static void ProjectileController_Start(On.RoR2.Projectile.ProjectileController.orig_Start orig, ProjectileController self)
        {
            orig(self);

            ProjectileGhostController ghostController = self.ghost;
            if (!ghostController)
                return;

            if (!self.owner || !self.owner.TryGetComponent(out CharacterBody ownerBody))
                return;

            InputBankTest inputBank = ownerBody.inputBank;
            if (!inputBank)
                return;

            if (!self.TryGetComponent(out FireProjectileInfoTracker fireProjectileInfoTracker))
                return;

            FireProjectileInfo fireInfo = fireProjectileInfoTracker.FireInfo;

            float projectileAimOriginSqrDistance = (inputBank.aimOrigin - fireInfo.position).sqrMagnitude;
            const float MAX_PROJECTILE_DISTANCE_TO_APPLY_OFFSET = 1.15f;

#if DEBUG
            Log.Debug($"{self.name}: projectileAimOriginDistance={Mathf.Sqrt(projectileAimOriginSqrDistance)}, maxDistance={MAX_PROJECTILE_DISTANCE_TO_APPLY_OFFSET}");
#endif

            // If the projectile position is far away from the aim origin,
            // it likely wasn't fired from there, so applying an offset doesn't make sense
            if (projectileAimOriginSqrDistance > MAX_PROJECTILE_DISTANCE_TO_APPLY_OFFSET * MAX_PROJECTILE_DISTANCE_TO_APPLY_OFFSET)
                return;

            Vector3 visualOffset = inputBank.GetUnalteredAimOrigin() - fireInfo.position;

#if DEBUG
            Log.Debug($"{self.name}: visualOffsetDist={visualOffset.magnitude}");
#endif

            // If the calculated visual offset is small enough,
            // don't bother with interpolating the position since it would probably not be very noticeable anyway
            const float MIN_VISUAL_OFFSET_DISTANCE = 0.25f;
            if (visualOffset.sqrMagnitude < MIN_VISUAL_OFFSET_DISTANCE * MIN_VISUAL_OFFSET_DISTANCE)
                return;
            
            ProjectileInitialOffset projectileOffset = ghostController.gameObject.AddComponent<ProjectileInitialOffset>();
            projectileOffset.InitialPositionOffset = visualOffset;
            projectileOffset.InterpolationTime = _cvProjectileInterpolationTime.value;
        }

        static void ProjectileGhostController_LerpTransform(On.RoR2.Projectile.ProjectileGhostController.orig_LerpTransform orig, ProjectileGhostController self, Transform a, Transform b, float t)
        {
            orig(self, a, b, t);
            tryApplyOffset(self);
        }

        static void ProjectileGhostController_CopyTransform(On.RoR2.Projectile.ProjectileGhostController.orig_CopyTransform orig, ProjectileGhostController self, Transform src)
        {
            orig(self, src);
            tryApplyOffset(self);
        }

        static void tryApplyOffset(ProjectileGhostController ghostController)
        {
            if (ghostController.TryGetComponent(out ProjectileInitialOffset projectileInitialOffset))
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                Transform transform = ghostController.transform;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                transform.position += projectileInitialOffset.CurrentPositionOffset;
                transform.rotation *= projectileInitialOffset.CurrentRotationOffset;
            }
        }

        static bool ProjectileStickOnImpact_TrySticking(On.RoR2.Projectile.ProjectileStickOnImpact.orig_TrySticking orig, ProjectileStickOnImpact self, Collider hitCollider, Vector3 impactNormal)
        {
            bool stickingSuccess = orig(self, hitCollider, impactNormal);

            if (stickingSuccess && self.TryGetComponent(out ProjectileController projectileController))
            {
                ProjectileGhostController ghostController = projectileController.ghost;
                if (ghostController && ghostController.TryGetComponent(out ProjectileInitialOffset projectileInitialOffset))
                {
                    GameObject.Destroy(projectileInitialOffset);
                }
            }

            return stickingSuccess;
        }
    }
}
