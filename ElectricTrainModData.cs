using Mafi;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using Mafi.Core.Trains;
using Mafi.TrainsDlc;

namespace CustomMod
{
    public class ElectricTrainModData : IModData
    {
        private static readonly System.Reflection.BindingFlags FLAGS =
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance;

        public void RegisterData(ProtoRegistrator registrator)
        {
            ProtosDb db = registrator.PrototypesDb;

            ModifyElectricLoco(db, IdsTrainsDlc.LocomotiveT1Electric,
                enginePowerKw: 870, tractiveEffortKn: 209, powerRequiredKw: 1000);

            ModifyElectricLoco(db, IdsTrainsDlc.LocomotiveT2Electric,
                enginePowerKw: 2850, tractiveEffortKn: 900, powerRequiredKw: 2900,
                maxSpeedKmh: 150);
        }

        private static void ModifyElectricLoco(ProtosDb db, Proto.ID id,
            int enginePowerKw, int tractiveEffortKn, int powerRequiredKw,
            int? maxSpeedKmh = null)
        {
            var loco = db.GetOrThrow<ElectricLocomotiveProto>(id);

            var engineField = typeof(LocomotiveProto).GetField("EnginePowerKw", FLAGS);
            if (engineField != null)
            {
                engineField.SetValue(loco, enginePowerKw.KwMech());
                Mafi.Log.Info($"CustomMod: {id} EnginePowerKw set to {enginePowerKw}");
            }

            var tractiveField = typeof(LocomotiveProto).GetField("StartingTractiveEffort", FLAGS);
            if (tractiveField != null)
            {
                tractiveField.SetValue(loco, (Fix32)tractiveEffortKn);
                Mafi.Log.Info($"CustomMod: {id} StartingTractiveEffort set to {tractiveEffortKn}");
            }

            var powerProp = typeof(ElectricLocomotiveProto).GetProperty("PowerRequired", FLAGS);
            if (powerProp != null && powerProp.CanWrite)
            {
                powerProp.SetValue(loco, powerRequiredKw.Kw());
                Mafi.Log.Info($"CustomMod: {id} PowerRequired set to {powerRequiredKw}");
            }
            else
            {
                var powerField = typeof(ElectricLocomotiveProto).GetField("<PowerRequired>k__BackingField", FLAGS);
                if (powerField != null)
                {
                    powerField.SetValue(loco, powerRequiredKw.Kw());
                    Mafi.Log.Info($"CustomMod: {id} PowerRequired set to {powerRequiredKw} (via backing field)");
                }
            }

            if (maxSpeedKmh.HasValue)
            {
                var maxSpeedField = typeof(TrainCarBaseProto).GetField("MaxSpeed", FLAGS);
                if (maxSpeedField != null)
                {
                    maxSpeedField.SetValue(loco, maxSpeedKmh.Value.Kmh());
                    Mafi.Log.Info($"CustomMod: {id} MaxSpeed set to {maxSpeedKmh.Value} km/h");
                }
            }
        }
    }
}
