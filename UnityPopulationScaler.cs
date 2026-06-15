using System;
using Mafi;
using Mafi.Core;
using Mafi.Core.Buildings.Settlements;
using Mafi.Core.PropertiesDb;
using Mafi.Core.Simulation;
using Mafi.Serialization;

namespace CustomMod
{
    [GenerateSerializer(false, null, 0, null)]
    public class UnityPopulationScaler
    {
        private readonly SettlementsManager m_settlementsManager;
        private readonly IProperty<Percent> m_unityMultProp;

        private static readonly Action<object, BlobWriter> s_serializeDataDelayedAction;
        private static readonly Action<object, BlobReader> s_deserializeDataDelayedAction;

        public UnityPopulationScaler(
            SettlementsManager settlementsManager,
            IPropertiesDb propsDb,
            ICalendar calendar)
        {
            m_settlementsManager = settlementsManager;
            m_unityMultProp = propsDb.GetProperty(IdsCore.PropertyIds.UnityProductionMultiplier);
            calendar.NewMonthStart.Add(this, UpdateUnityMultiplier);
            UpdateUnityMultiplier();
        }

        private void UpdateUnityMultiplier()
        {
            int totalPop = m_settlementsManager.GetTotalPopulation();
            int deltaPercent = totalPop > 750 ? (totalPop - 750) * 100 / 750 : 0;
            Percent delta = deltaPercent.Percent();
            m_unityMultProp.AddOrSetModifier("PopulationUnityScaling", delta, "PopulationScaling");

            Mafi.Log.Info($"POP UNITY SCALER totalPop:{totalPop} deltaPercent:{deltaPercent}");
        }

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
