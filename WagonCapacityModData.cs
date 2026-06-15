using System;
using System.Reflection;
using Mafi;
using Mafi.Base;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using Mafi.Core.Trains;
using Mafi.TrainsDlc;

namespace CustomMod
{
    public class WagonCapacityModData : IModData
    {
        private static readonly BindingFlags FLAGS =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private const int MULTIPLIER = 4;

        public void RegisterData(ProtoRegistrator registrator)
        {
            ProtosDb db = registrator.PrototypesDb;

            QuadrupleWagonCapacity(db, Ids.Trains.WagonT1Unit);
            QuadrupleWagonCapacity(db, Ids.Trains.WagonT1Loose);
            QuadrupleWagonCapacity(db, Ids.Trains.WagonT1Fluid);
            QuadrupleWagonCapacity(db, Ids.Trains.WagonT2Unit);
            QuadrupleWagonCapacity(db, Ids.Trains.WagonT2Loose);
            QuadrupleWagonCapacity(db, Ids.Trains.WagonT2Fluid);

            try
            {
                QuadrupleWagonCapacity(db, IdsTrainsDlc.WagonT2Molten);
            }
            catch (Exception)
            {
                Mafi.Log.Info("CustomMod: Molten wagon not found (Trains DLC not active), skipping");
            }
        }

        private static void QuadrupleWagonCapacity(ProtosDb db, Proto.ID id)
        {
            var wagon = db.GetOrThrow<CargoWagonProto>(id);

            var baseCapField = typeof(CargoWagonProto).GetField("m_baseCapacity", FLAGS);
            var capacityProp = typeof(CargoWagonProto).GetProperty("Capacity", FLAGS);
            var subCarCapProp = typeof(CargoWagonProto).GetProperty("SubCarCapacity", FLAGS);

            if (baseCapField == null || capacityProp == null || subCarCapProp == null)
            {
                Mafi.Log.Error($"CustomMod: Could not find capacity fields on CargoWagonProto for {id}");
                return;
            }

            var oldCapacity = (Quantity)baseCapField.GetValue(wagon);
            var newCapacity = new Quantity(oldCapacity.Value * MULTIPLIER);

            baseCapField.SetValue(wagon, newCapacity);
            capacityProp.SetValue(wagon, newCapacity);
            subCarCapProp.SetValue(wagon, new Quantity(newCapacity.Value / wagon.SubCarCount));

            Mafi.Log.Info($"CustomMod: {id} capacity {oldCapacity.Value} -> {newCapacity.Value}");
        }
    }
}
