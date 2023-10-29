using AttackDirectionFix.Patches;
using AttackDirectionFix.Utils;
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

            AimOriginOverridePatch.Init();

            PlayerAimVisualizer.Init();

            stopwatch.Stop();
            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }
    }
}
