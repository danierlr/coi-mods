using System;
using Mafi;
using Mafi.Core.Buildings.Settlements;
using Mafi.Core.PropertiesDb;
using Mafi.Serialization;

namespace FbrSteamMod
{
    [GenerateSerializer(false, null, 0, null)]
    public class UnityPopulationScaler
    {
        private readonly SettlementsManager m_settlementsManager;
        private readonly IProperty<Percent> m_unityMultProp;

        private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction;
        private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction;

        private void UpdateUnityMultiplier() { }

        public static void Serialize(UnityPopulationScaler value, BlobWriter writer)
        {
            if (writer.TryStartClassSerialization(value))
            {
                writer.EnqueueDataSerialization(value, s_serializeDataDelayedAction);
            }
        }

        private void SerializeData(BlobWriter writer)
        {
            writer.WriteGeneric(m_settlementsManager);
            writer.WriteGeneric(m_unityMultProp);
        }

        public static UnityPopulationScaler Deserialize(BlobReader reader)
        {
            if (reader.TryStartClassDeserialization(out UnityPopulationScaler obj,
                (Func<BlobReader, Type, UnityPopulationScaler>)null,
                (Func<BlobReader, string, UnityPopulationScaler>)null,
                nullObjIsOk: false))
            {
                reader.EnqueueDataDeserialization(obj, s_deserializeDataDelayedAction);
            }
            return obj;
        }

        private void DeserializeData(BlobReader reader)
        {
            reader.SetField(this, "m_settlementsManager", reader.ReadGenericAs<SettlementsManager>());
            reader.SetField(this, "m_unityMultProp", reader.ReadGenericAs<IProperty<Percent>>());
        }

        static UnityPopulationScaler()
        {
            s_serializeDataDelayedAction = delegate(object obj, BlobWriter writer)
            {
                ((UnityPopulationScaler)obj).SerializeData(writer);
            };
            s_deserializeDataDelayedAction = delegate(object obj, BlobReader reader)
            {
                ((UnityPopulationScaler)obj).DeserializeData(reader);
            };
        }
    }
}
