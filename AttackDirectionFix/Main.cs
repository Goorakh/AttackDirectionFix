using AttackDirectionFix.Patches;
using BepInEx;
using System.Diagnostics;

namespace AttackDirectionFix
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "AttackDirectionFix";
        public const string PluginVersion = "1.1.1";

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            AimOriginOverridePatch.Init();

            FireProjectileInfoTracker.InitPatch();

            ProjectileGhostOffsetPatch.Init();

            PlayerAimVisualizer.Init();

#if DEBUG
            // On.RoR2.Networking.NetworkManagerSystem.ClientSendAuth += (orig, self, conn) => { };
#endif

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }
    }
}
