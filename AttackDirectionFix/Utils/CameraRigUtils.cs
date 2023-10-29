using RoR2;
using System.Linq;

namespace AttackDirectionFix.Utils
{
    public static class CameraRigUtils
    {
        public static CameraRigController FindCameraRigControllerForBody(CharacterBody body)
        {
            return CameraRigController.readOnlyInstancesList.FirstOrDefault(c => c.targetBody == body);
        }
    }
}
