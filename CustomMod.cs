using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Mafi;
using Mafi.Collections;
using Mafi.Core.Game;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;

namespace CustomMod
{
    internal static class HarmonyInit
    {
        private static bool s_patched;

        internal static void EnsurePatched()
        {
            if (s_patched) return;
            s_patched = true;
            try
            {
                var harmony = new Harmony("com.custom.coimods");
                harmony.PatchAll(typeof(HarmonyInit).Assembly);
                Mafi.Log.Info("CustomMod: Harmony patches applied successfully");
            }
            catch (Exception ex)
            {
                Mafi.Log.Error($"CustomMod: Harmony patch failed: {ex}");
            }
        }
    }

    public sealed class CustomMod : IMod, IDisposable
    {
        public ModManifest Manifest { get; }
        public bool IsUiOnly => false;
        public Option<IConfig> ModConfig => Option<IConfig>.None;
        public ModJsonConfig JsonConfig { get; }

        static CustomMod()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.Contains("ExampleMod"))
                    return typeof(CustomMod).Assembly;

                var asmName = new AssemblyName(args.Name);
                var modDir = Path.GetDirectoryName(typeof(CustomMod).Assembly.Location);
                var candidate = Path.Combine(modDir, asmName.Name + ".dll");
                if (File.Exists(candidate))
                    return Assembly.LoadFrom(candidate);
                return null;
            };

            HarmonyInit.EnsurePatched();
        }

        public CustomMod(ModManifest manifest)
        {
            Manifest = manifest;
            JsonConfig = new ModJsonConfig(this);
        }

        public void RegisterPrototypes(ProtoRegistrator registrator)
        {
            registrator.RegisterData<FbrSteamModData>();
            registrator.RegisterData<ElectricTrainModData>();
            registrator.RegisterData<WagonCapacityModData>();
            registrator.RegisterData<RemoveRadiationModData>();
            registrator.RegisterData<ExcavatorModData>();
            registrator.RegisterData<TruckModData>();
        }

        public void RegisterDependencies(DependencyResolverBuilder depBuilder, ProtosDb protosDb, bool gameWasLoaded)
        {
            depBuilder.RegisterDependency<UnityPopulationScaler>();
        }

        public void EarlyInit(DependencyResolver resolver)
        {
        }

        public void Initialize(DependencyResolver resolver, bool gameWasLoaded)
        {
        }

        public void MigrateJsonConfig(VersionSlim savedVersion, Dict<string, object> savedValues)
        {
        }

        public void Dispose()
        {
        }
    }
}
