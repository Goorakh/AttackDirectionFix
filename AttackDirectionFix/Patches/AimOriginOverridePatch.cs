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
                try
                {
                    return orig(self);
                }
                finally
                {
                    _tempDisablePatch = false;
                }
            };
        }

        public static Vector3 GetUnalteredAimOrigin(this InputBankTest inputBankTest)
        {
            _tempDisablePatch = true;
            try
            {
                return inputBankTest.aimOrigin;
            }
            finally
            {
                _tempDisablePatch = false;
            }
        }

        delegate Vector3 orig_get_aimOrigin(InputBankTest self);
        static Vector3 InputBankTest_get_aimOrigin(orig_get_aimOrigin orig, InputBankTest self)
        {
            Vector3 defaultAimOrigin = orig(self);

            if (!_tempDisablePatch)
            {
                CameraRigController cameraRigController = CameraRigUtils.FindCameraRigControllerForBody(self.characterBody);
                if (cameraRigController)
                {
                    CameraTargetParams cameraTargetParams = cameraRigController.targetParams;
                    if (cameraTargetParams && cameraTargetParams.cameraPivotTransform)
                    {
                        Vector3 cameraPivot = cameraTargetParams.cameraPivotTransform.position;

                        CameraState cameraState = cameraRigController.currentCameraState;

                        return VectorUtils.ClosestPointAlongRay(new Ray(cameraState.position, cameraState.rotation * Vector3.forward), cameraPivot);
                    }
                }
            }

            return defaultAimOrigin;
        }
    }
}
