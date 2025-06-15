using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
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
            IL.RoR2.Projectile.ProjectileController.Start += ProjectileController_Start;

            On.RoR2.Projectile.ProjectileGhostController.LerpTransform += ProjectileGhostController_LerpTransform;
            On.RoR2.Projectile.ProjectileGhostController.CopyTransform += ProjectileGhostController_CopyTransform;

            On.RoR2.Projectile.ProjectileStickOnImpact.TrySticking += ProjectileStickOnImpact_TrySticking;
        }

        static void ProjectileController_Start(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            VariableDefinition hasVisualOffsetVar = new VariableDefinition(il.Import(typeof(bool)));
            il.Method.Body.Variables.Add(hasVisualOffsetVar);

            VariableDefinition positionOffsetVar = new VariableDefinition(il.Import(typeof(Vector3)));
            il.Method.Body.Variables.Add(positionOffsetVar);

            VariableDefinition rotationOffsetVar = new VariableDefinition(il.Import(typeof(Quaternion)));
            il.Method.Body.Variables.Add(rotationOffsetVar);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, hasVisualOffsetVar);
            c.Emit(OpCodes.Ldloca, positionOffsetVar);
            c.Emit(OpCodes.Ldloca, rotationOffsetVar);
            c.EmitDelegate(getOffsets);
            static void getOffsets(ProjectileController projectileController, out bool hasVisualOffset, out Vector3 positionOffset, out Quaternion rotationOffset)
            {
                hasVisualOffset = false;
                positionOffset = Vector3.zero;
                rotationOffset = Quaternion.identity;

                if (projectileController && projectileController.TryGetComponent(out ProjectileDisplacementInfoProvider projectileDisplacementProvider))
                {
                    FireProjectileInfo unmodifiedFireInfo = projectileDisplacementProvider.UnmodifiedFireProjectileInfo;
                    FireProjectileInfo modifiedFireInfo = projectileDisplacementProvider.ModifiedFireProjectileInfo;

                    Vector3 visualPositionOffset = unmodifiedFireInfo.position - modifiedFireInfo.position;

                    Log.Debug($"{projectileController.name} visual offset dst: {visualPositionOffset.magnitude}");

                    hasVisualOffset = true;
                    positionOffset = visualPositionOffset;
                }
            }

            VariableDefinition rotationTempVar = new VariableDefinition(il.Import(typeof(Quaternion)));
            il.Method.Body.Variables.Add(rotationTempVar);

            void emitPositionOffset(ILCursor c)
            {
                c.Emit(OpCodes.Ldloc, positionOffsetVar);
                c.EmitDelegate(getVisualPosition);
                static Vector3 getVisualPosition(Vector3 position, Vector3 offset)
                {
                    return position + offset;
                }
            }

            void emitRotationOffset(ILCursor c)
            {
                c.Emit(OpCodes.Ldloc, rotationOffsetVar);
                c.EmitDelegate(getVisualRotation);
                static Quaternion getVisualRotation(Quaternion rotation, Quaternion offset)
                {
                    return rotation * offset;
                }
            }

            void emitPositionRotationOffset(ILCursor c)
            {
                c.Emit(OpCodes.Stloc, rotationTempVar);

                emitPositionOffset(c);

                c.Emit(OpCodes.Ldloc, rotationTempVar);

                emitRotationOffset(c);
            }

            if (c.TryGotoNext(MoveType.Before,
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => UnityEngine.Object.Instantiate(default(GameObject), default(Vector3), default)))))
            {
                emitPositionRotationOffset(c);
            }
            else
            {
                Log.Error("Failed to find non-pooled ghost instantiate patch location");
            }

            if (c.TryGotoNext(MoveType.Before,
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => EffectManager.GetAndActivatePooledEffect(default, default(Vector3), default)))))
            {
                emitPositionRotationOffset(c);
            }
            else
            {
                Log.Error("Failed to find pooled ghost instantiate patch location");
            }

            if (c.TryGotoNext(MoveType.AfterLabel, x => x.MatchRet()))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, hasVisualOffsetVar);
                c.Emit(OpCodes.Ldloc, positionOffsetVar);
                c.Emit(OpCodes.Ldloc, rotationOffsetVar);
                c.EmitDelegate(initializeGhostOffset);
                static void initializeGhostOffset(ProjectileController projectileController, bool hasVisualOffset, Vector3 positionOffset, Quaternion rotationOffset)
                {
                    if (!projectileController)
                        return;

                    ProjectileGhostController ghostController = projectileController.ghost;
                    if (!ghostController)
                        return;

                    ProjectileInitialOffset projectileOffset = ghostController.GetComponent<ProjectileInitialOffset>();

                    if (hasVisualOffset)
                    {
                        if (!projectileOffset)
                            projectileOffset = ghostController.gameObject.AddComponent<ProjectileInitialOffset>();

                        projectileOffset.StartTime = Time.time;
                        projectileOffset.InitialPositionOffset = positionOffset;
                        projectileOffset.InitialRotationOffset = rotationOffset;
                        projectileOffset.InterpolationTime = _cvProjectileInterpolationTime.value;
                    }
                    else
                    {
                        if (projectileOffset)
                        {
                            GameObject.Destroy(projectileOffset);
                        }
                    }
                }
            }
            else
            {
                Log.Error("Failed to find ret match");
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
            if (ghostController && ghostController.TryGetComponent(out ProjectileInitialOffset projectileInitialOffset))
            {
                Transform transform = ghostController.transform;

                transform.position += projectileInitialOffset.CurrentPositionOffset;
                transform.rotation *= projectileInitialOffset.CurrentRotationOffset;
            }
        }

        static bool ProjectileStickOnImpact_TrySticking(On.RoR2.Projectile.ProjectileStickOnImpact.orig_TrySticking orig, ProjectileStickOnImpact self, Collider hitCollider, Vector3 impactNormal)
        {
            bool stickingSuccess = orig(self, hitCollider, impactNormal);

            if (stickingSuccess && self && self.TryGetComponent(out ProjectileController projectileController))
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
