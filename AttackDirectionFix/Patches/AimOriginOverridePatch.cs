using AttackDirectionFix.Utils;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using RoR2;
using System.Reflection;
using UnityEngine;

namespace AttackDirectionFix.Patches
{
    static class AimOriginOverridePatch
    {
        static bool _tempDisablePatch = false;

        public static void Init()
        {
            MethodInfo get_aimOrigin = AccessTools.DeclaredPropertyGetter(typeof(InputBankTest), nameof(InputBankTest.aimOrigin));
            if (get_aimOrigin is not null)
            {
                new Hook(get_aimOrigin, InputBankTest_get_aimOrigin);
            }
            else
            {
                Log.Error_NoCallerPrefix($"Unable to find InputBankTest.aimOrigin getter");
            }

            On.RoR2.InteractionDriver.FindBestInteractableObject += (orig, self) =>
            {
                _tempDisablePatch = true;
                GameObject result = orig(self);
                _tempDisablePatch = false;
                return result;
            };
        }

        delegate Vector3 orig_get_aimOrigin(InputBankTest self);
        static Vector3 InputBankTest_get_aimOrigin(orig_get_aimOrigin orig, InputBankTest self)
        {
            Vector3 defaultAimOrigin = orig(self);

            if (!_tempDisablePatch)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                CharacterBody body = self.characterBody;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                CameraRigController cameraRigController = CameraRigUtils.FindCameraRigControllerForBody(body);
                if (cameraRigController)
                {
                    Transform cameraRigTransform = cameraRigController.transform;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    CameraTargetParams cameraTargetParams = cameraRigController.targetParams;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                    if (cameraTargetParams && cameraTargetParams.cameraPivotTransform)
                    {
                        Vector3 cameraPivot = cameraTargetParams.cameraPivotTransform.position;

                        return VectorUtils.ClosestPointAlongRay(new Ray(cameraRigTransform.position, cameraRigTransform.forward), cameraPivot);
                    }
                }
            }

            return defaultAimOrigin;
        }
    }
}
