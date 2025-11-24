using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RoR2;
using RoR2.Projectile;
using System.Linq;
using UnityEngine;

namespace AttackDirectionFix.Patches
{
    public sealed class ProjectileDisplacementInfoProvider : MonoBehaviour
    {
        sealed record class DisplacementData(FireProjectileInfo OriginalFireProjectileInfo);

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.Projectile.ProjectileManager.FireProjectileServer += modifyFirePositionPatch;
            IL.RoR2.Projectile.ProjectileManager.FireProjectileClient += modifyFirePositionPatch;

            static void modifyFirePositionPatch(ILContext il)
            {
                ILCursor c = new ILCursor(il);

                ParameterDefinition fireProjectileInfoParameter = il.Method.Parameters.FirstOrDefault(p => p.ParameterType.Is(typeof(FireProjectileInfo)));
                if (fireProjectileInfoParameter == null)
                {
                    Log.Error("Failed to find FireProjectileInfo parameter");
                    return;
                }

                VariableDefinition displacementDataVar = new VariableDefinition(il.Import(typeof(DisplacementData)));
                il.Method.Body.Variables.Add(displacementDataVar);

                c.Emit(OpCodes.Ldarga, fireProjectileInfoParameter);
                c.Emit(OpCodes.Ldloca, displacementDataVar);
                c.EmitDelegate(tryModifyFireInfo);
                static void tryModifyFireInfo(ref FireProjectileInfo fireProjectileInfo, ref DisplacementData displacementData)
                {
                    if (!fireProjectileInfo.owner || !fireProjectileInfo.owner.TryGetComponent(out CharacterBody ownerBody))
                        return;

                    if (!ownerBody.isPlayerControlled)
                        return;

                    int projectileIndex = ProjectileCatalog.GetProjectileIndex(fireProjectileInfo.projectilePrefab);
                    if (!ProjectileAttributeTracker.ShouldModifyFireDirection(projectileIndex))
                    {
                        Log.Debug($"Not modifying fire info for: {fireProjectileInfo.projectilePrefab.name}");
                        return;
                    }

                    InputBankTest ownerInputBank = ownerBody.inputBank;
                    if (!ownerInputBank)
                        return;

                    Vector3 visualFirePosition = fireProjectileInfo.position;
                    if ((visualFirePosition - ownerInputBank.aimOrigin).sqrMagnitude <= Mathf.Epsilon)
                    {
                        visualFirePosition = ownerInputBank.GetUnalteredAimOrigin();
                    }

                    float maxProjectileDistance = ownerBody.bestFitRadius * 1.5f;
                    float maxProjectileSqrDistance = maxProjectileDistance * maxProjectileDistance;
                    Vector3 projectileOwnerOffset = ownerBody.corePosition - visualFirePosition;

                    Log.Debug($"{fireProjectileInfo.projectilePrefab.name}: dst={projectileOwnerOffset.magnitude}, maxDst={maxProjectileDistance}");

                    if (projectileOwnerOffset.sqrMagnitude > maxProjectileSqrDistance)
                        return;

                    FireProjectileInfo originalFireInfo = fireProjectileInfo;
                    originalFireInfo.position = visualFirePosition;

                    displacementData = new DisplacementData(originalFireInfo);

                    fireProjectileInfo.position = ownerInputBank.aimOrigin;
                }

                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => Instantiate<GameObject>(default, default(Vector3), default)))))
                {
                    c.Emit(OpCodes.Dup); // Instantiated object
                    c.Emit(OpCodes.Ldarg, fireProjectileInfoParameter);
                    c.Emit(OpCodes.Ldloc, displacementDataVar);
                    c.EmitDelegate(handleProjectileInstantiate);
                    static void handleProjectileInstantiate(GameObject projectile, FireProjectileInfo fireProjectileInfo, DisplacementData displacementData)
                    {
                        if (!projectile || displacementData == null)
                            return;

                        ProjectileDisplacementInfoProvider displacementInfoProvider = projectile.AddComponent<ProjectileDisplacementInfoProvider>();
                        displacementInfoProvider.UnmodifiedFireProjectileInfo = displacementData.OriginalFireProjectileInfo;
                        displacementInfoProvider.ModifiedFireProjectileInfo = fireProjectileInfo;
                    }
                }
                else
                {
                    Log.Error("Failed to find instantiate call");
                }
            }
        }

        public FireProjectileInfo UnmodifiedFireProjectileInfo { get; private set; }

        public FireProjectileInfo ModifiedFireProjectileInfo { get; private set; }
    }
}
