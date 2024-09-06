using RoR2;
using RoR2.ConVar;
using RoR2.Projectile;
using UnityEngine;

namespace AttackDirectionFix.Patches
{
    static class ProjectileGhostOffsetPatch
    {
        static readonly FloatConVar _cvProjectileInterpolationTime = new FloatConVar("projectile_visual_interp", ConVarFlags.None, "0.7", "[Attack Direction Fix] How long projectile visuals should take to interpolate to their actual path");

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

            // Projectile ghosts can be pooled now, so make sure we remove the offset if it's not needed

            ProjectileInitialOffset projectileOffset = ghostController.GetComponent<ProjectileInitialOffset>();

            bool shouldHaveOffset = false;

            if (self.TryGetComponent(out ProjectileDisplacementInfoProvider projectileDisplacementProvider))
            {
                FireProjectileInfo unmodifiedFireInfo = projectileDisplacementProvider.UnmodifiedFireProjectileInfo;
                FireProjectileInfo modifiedFireInfo = projectileDisplacementProvider.ModifiedFireProjectileInfo;

                Vector3 visualPositionOffset = unmodifiedFireInfo.position - modifiedFireInfo.position;

                // If the offset is very small, don't bother with the interpolation
                const float MIN_VISUAL_OFFSET_DISTANCE = 0.15f;
                if (visualPositionOffset.sqrMagnitude >= MIN_VISUAL_OFFSET_DISTANCE * MIN_VISUAL_OFFSET_DISTANCE)
                {
                    if (!projectileOffset)
                        projectileOffset = ghostController.gameObject.AddComponent<ProjectileInitialOffset>();

#if DEBUG
                    Log.Debug($"{self.name} visual offset dst: {visualPositionOffset.magnitude}");
#endif

                    projectileOffset.StartTime = Time.time;
                    projectileOffset.InitialPositionOffset = visualPositionOffset;
                    projectileOffset.InterpolationTime = _cvProjectileInterpolationTime.value;

                    shouldHaveOffset = true;
                }
            }

            if (!shouldHaveOffset && projectileOffset)
            {
                GameObject.Destroy(projectileOffset);
            }
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
                Transform transform = ghostController.transform;

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
