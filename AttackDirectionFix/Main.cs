using BepInEx;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using RoR2;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AttackDirectionFix
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "AttackDirectionFix";
        public const string PluginVersion = "1.0.0";

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

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
                _disablePatch = true;
                GameObject result = orig(self);
                _disablePatch = false;
                return result;
            };

#if DEBUG
            GameObject aimOriginVisualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DontDestroyOnLoad(aimOriginVisualizer);

            aimOriginVisualizer.GetComponent<Collider>().enabled = false;
            aimOriginVisualizer.GetComponent<Renderer>().material.color = Color.green;
            aimOriginVisualizer.transform.localScale = Vector3.one * 0.1f;

            GameObject cameraPivotVisualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DontDestroyOnLoad(cameraPivotVisualizer);

            cameraPivotVisualizer.GetComponent<Collider>().enabled = false;
            cameraPivotVisualizer.GetComponent<Renderer>().material.color = Color.red;
            cameraPivotVisualizer.transform.localScale = Vector3.one * 0.1f;

            On.RoR2.CharacterBody.Update += (orig, self) =>
            {
                orig(self);

                if (self.isPlayerControlled && self.inputBank)
                {
                    aimOriginVisualizer.transform.position = self.inputBank.aimOrigin;

                    CameraRigController cameraRigController = findCameraRigControllerForBody(self);
                    if (cameraRigController)
                    {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        CameraTargetParams cameraTargetParams = cameraRigController.targetParams;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                        if (cameraTargetParams && cameraTargetParams.cameraPivotTransform)
                        {
                            cameraPivotVisualizer.transform.position = cameraTargetParams.cameraPivotTransform.position;
                        }
                    }
                }
            };
#endif

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }

        static Vector3 closestPointAlongRay(Ray ray, Vector3 target)
        {
            return ray.origin + (ray.direction * Vector3.Dot(ray.direction, target - ray.origin));
        }

        static CameraRigController findCameraRigControllerForBody(CharacterBody body)
        {
            return CameraRigController.readOnlyInstancesList.FirstOrDefault(c => c.targetBody == body);
        }

        static bool _disablePatch = false;

        delegate Vector3 orig_get_aimOrigin(InputBankTest self);
        static Vector3 InputBankTest_get_aimOrigin(orig_get_aimOrigin orig, InputBankTest self)
        {
            if (!_disablePatch)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                CharacterBody body = self.characterBody;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                CameraRigController cameraRigController = findCameraRigControllerForBody(body);
                if (cameraRigController)
                {
                    Transform cameraRigTransform = cameraRigController.transform;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    CameraTargetParams cameraTargetParams = cameraRigController.targetParams;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                    if (cameraTargetParams && cameraTargetParams.cameraPivotTransform)
                    {
                        Vector3 cameraPivot = cameraTargetParams.cameraPivotTransform.position;

                        return closestPointAlongRay(new Ray(cameraRigTransform.position, cameraRigTransform.forward), cameraPivot);
                    }
                }
            }

            return orig(self);
        }
    }
}
