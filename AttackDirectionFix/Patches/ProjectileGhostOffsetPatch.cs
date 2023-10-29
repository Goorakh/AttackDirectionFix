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
            if (ghostController)
            {
                if (self.owner && self.owner.TryGetComponent(out CharacterBody ownerBody))
                {
                    InputBankTest inputBank = ownerBody.inputBank;
                    if (inputBank)
                    {
                        Vector3 firedFrom = inputBank.aimOrigin;
                        Vector3 visualFiredFrom = inputBank.GetUnalteredAimOrigin();

                        Vector3 visualOffset = visualFiredFrom - firedFrom;

#if DEBUG
                        Log.Debug($"{self.name}: visualOffset={visualOffset}, length={visualOffset.magnitude}");
#endif

                        const float MIN_VISUAL_OFFSET_DISTANCE = 0.25f;
                        if (visualOffset.sqrMagnitude >= MIN_VISUAL_OFFSET_DISTANCE * MIN_VISUAL_OFFSET_DISTANCE)
                        {
                            ProjectileInitialOffset projectileOffset = ghostController.gameObject.AddComponent<ProjectileInitialOffset>();
                            projectileOffset.InitialPositionOffset = visualOffset;
                            projectileOffset.InterpolationTime = _cvProjectileInterpolationTime.value;
                        }
                    }
                }
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
