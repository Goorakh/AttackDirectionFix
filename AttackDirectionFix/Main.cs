using AttackDirectionFix.Patches;
using BepInEx;
using System.Diagnostics;

namespace AttackDirectionFix
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "AttackDirectionFix";
        public const string PluginVersion = "1.2.3";

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            AimOriginOverridePatch.Init();

            ProjectileGhostOffsetPatch.Init();

            PlayerAimVisualizer.Init();

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }
    }
}
