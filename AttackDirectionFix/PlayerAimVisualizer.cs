using RoR2;
using RoR2.ConVar;
using UnityEngine;

namespace AttackDirectionFix
{
    static class PlayerAimVisualizer
    {
        static GameObject _aimOriginVisualizer;
        static GameObject _cameraPivotVisualizer;

        static readonly BoolConVar _cvEnableAimVisualization = new BoolConVar("debug_aim_visualization", ConVarFlags.None, "0", "Enables/Disables player aim visualization. Green=Aim Origin, Red=Camera Pivot");

        public static void Init()
        {
            {
                _aimOriginVisualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                GameObject.DontDestroyOnLoad(_aimOriginVisualizer);

                _aimOriginVisualizer.GetComponent<Collider>().enabled = false;
                _aimOriginVisualizer.GetComponent<Renderer>().material.color = Color.green;
                _aimOriginVisualizer.transform.localScale = Vector3.one * 0.1f;
            }

            {
                _cameraPivotVisualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                GameObject.DontDestroyOnLoad(_cameraPivotVisualizer);

                _cameraPivotVisualizer.GetComponent<Collider>().enabled = false;
                _cameraPivotVisualizer.GetComponent<Renderer>().material.color = Color.red;
                _cameraPivotVisualizer.transform.localScale = Vector3.one * 0.1f;
            }

            SceneCamera.onSceneCameraPreCull += SceneCameraPreCull;
        }

        static void SceneCameraPreCull(SceneCamera sceneCamera)
        {
            if (_aimOriginVisualizer)
                _aimOriginVisualizer.SetActive(false);

            if (_cameraPivotVisualizer)
                _cameraPivotVisualizer.SetActive(false);

            if (!_cvEnableAimVisualization.value)
                return;

            CameraRigController cameraRigController = sceneCamera.cameraRigController;
            if (!cameraRigController)
                return;

            LocalUser localUserViewer = cameraRigController.localUserViewer;
            if (localUserViewer is null)
                return;
            
            CharacterBody body = cameraRigController.targetBody;
            if (body)
            {
                if (_aimOriginVisualizer)
                {
                    _aimOriginVisualizer.SetActive(true);
                    _aimOriginVisualizer.transform.position = body.inputBank.aimOrigin;
                }
            }

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            CameraTargetParams cameraTargetParams = cameraRigController.targetParams;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            if (cameraTargetParams && cameraTargetParams.cameraPivotTransform)
            {
                if (_cameraPivotVisualizer)
                {
                    _cameraPivotVisualizer.SetActive(true);
                    _cameraPivotVisualizer.transform.position = cameraTargetParams.cameraPivotTransform.position;
                }
            }
        }
    }
}
