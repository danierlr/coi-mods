using System.Reflection;
using Mafi;
using Mafi.Core.Entities;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using Mafi.Core.Trains;
using Mafi.TrainsDlc;

namespace CustomMod
{
    public class ElectricTrainModData : IModData
    {
        private static readonly BindingFlags FLAGS =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public void RegisterData(ProtoRegistrator registrator)
        {
            ProtosDb db = registrator.PrototypesDb;

            ModifyElectricLoco(db, IdsTrainsDlc.LocomotiveT1Electric,
                enginePowerKw: 950,
                tractiveEffortKn: 230,
                powerRequiredKw: 1000,
                maxSpeedKmh: 105,
                brakingForceKn: 320,
                frontalAreaM2: 7,
                lengthDragExtra: 1.5,
                dragStandalone: 3,
                dragInline: 2,
                costMultiplier: 1.5);

            ModifyElectricLoco(db, IdsTrainsDlc.LocomotiveT2Electric,
                enginePowerKw: 3350,
                tractiveEffortKn: 1070,
                powerRequiredKw: 3400,
                maxSpeedKmh: 160,
                brakingForceKn: 1600,
                massEmpty: 85,
                massFull: 85,
                rollingResistCoeff: 2,
                frontalAreaM2: 5,
                lengthDragExtra: 1.5,
                dragStandalone: 3,
                dragInline: 2,
                costMultiplier: 2.5);
        }

        private static void ModifyElectricLoco(ProtosDb db, Proto.ID id,
            int enginePowerKw, int tractiveEffortKn, int powerRequiredKw,
            int? maxSpeedKmh = null, int? brakingForceKn = null,
            int? massEmpty = null, int? massFull = null,
            int? rollingResistCoeff = null, int? frontalAreaM2 = null,
            double? lengthDragExtra = null,
            int? dragStandalone = null, int? dragInline = null,
            double? costMultiplier = null)
        {
            var loco = db.GetOrThrow<ElectricLocomotiveProto>(id);

            SetField<LocomotiveProto>(loco, "EnginePowerKw", enginePowerKw.KwMech());
            SetField<LocomotiveProto>(loco, "StartingTractiveEffort", (Fix32)tractiveEffortKn);

            if (!SetProperty<ElectricLocomotiveProto>(loco, "PowerRequired", powerRequiredKw.Kw()))
                SetField<ElectricLocomotiveProto>(loco, "<PowerRequired>k__BackingField", powerRequiredKw.Kw());

            if (maxSpeedKmh.HasValue)
                SetField<TrainCarBaseProto>(loco, "MaxSpeed", maxSpeedKmh.Value.Kmh());

            if (brakingForceKn.HasValue)
                SetField<TrainCarBaseProto>(loco, "BrakingForceKn", (Fix32)brakingForceKn.Value);

            if (massEmpty.HasValue)
                SetField<TrainCarBaseProto>(loco, "MassTonsWhenEmpty", (Fix32)massEmpty.Value);

            if (massFull.HasValue)
                SetField<TrainCarBaseProto>(loco, "MassTonsWhenFull", (Fix32)massFull.Value);

            if (rollingResistCoeff.HasValue)
                SetField<TrainCarBaseProto>(loco, "RollingResistanceCoefficientTimesThousand", (Fix32)rollingResistCoeff.Value);

            if (frontalAreaM2.HasValue)
                SetField<TrainCarBaseProto>(loco, "FrontalAreaM2", (Fix32)frontalAreaM2.Value);

            if (lengthDragExtra.HasValue)
                SetField<TrainCarBaseProto>(loco, "LengthDragAsExtraFrontalArea", lengthDragExtra.Value.ToFix32());

            if (dragStandalone.HasValue)
                SetField<TrainCarBaseProto>(loco, "DragCoefficientStandalone", (Fix32)dragStandalone.Value);

            if (dragInline.HasValue)
                SetField<TrainCarBaseProto>(loco, "DragCoefficientInline", (Fix32)dragInline.Value);

            if (costMultiplier.HasValue)
            {
                var costs = loco.Costs;
                var percent = Percent.FromRatio((Fix32)costMultiplier.Value, Fix32.One);
                var scaledConstruction = costs.BaseConstructionCost.ScaledBy(percent);
                var newCosts = new EntityCosts(scaledConstruction, costs.DefaultPriority, costs.Workers, costs.Maintenance);
                SetField<EntityProto>(loco, "<Costs>k__BackingField", newCosts);
                Mafi.Log.Info($"CustomMod: {id} construction cost multiplied by {costMultiplier.Value}x");
            }

            Mafi.Log.Info($"CustomMod: {id} modified — Power:{enginePowerKw}kW TE:{tractiveEffortKn}kN Elec:{powerRequiredKw}kW");
        }

        private static void SetField<T>(object obj, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName, FLAGS);
            if (field != null)
                field.SetValue(obj, value);
            else
                Mafi.Log.Warning($"CustomMod: Field {fieldName} not found on {typeof(T).Name}");
        }

        private static bool SetProperty<T>(object obj, string propName, object value)
        {
            var prop = typeof(T).GetProperty(propName, FLAGS);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
                return true;
            }
            return false;
        }
    }
}
