using System;
using Mafi;
using Mafi.Collections;
using Mafi.Core.Game;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;

namespace CustomMod
{
    public sealed class CustomMod : IMod, IDisposable
    {
        public ModManifest Manifest { get; }
        public bool IsUiOnly => false;
        public Option<IConfig> ModConfig => Option<IConfig>.None;
        public ModJsonConfig JsonConfig { get; }

        public CustomMod(ModManifest manifest)
        {
            Manifest = manifest;
            JsonConfig = new ModJsonConfig(this);
        }

        public void RegisterPrototypes(ProtoRegistrator registrator)
        {
            registrator.RegisterData<FbrSteamModData>();
            registrator.RegisterData<ElectricTrainModData>();
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
