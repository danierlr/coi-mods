using System.Reflection;
using Mafi;
using Mafi.Base;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Maintenance;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using Mafi.Core.Vehicles.Excavators;

namespace CustomMod
{
    public class ExcavatorModData : IModData
    {
        private static readonly BindingFlags FLAGS =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public void RegisterData(ProtoRegistrator registrator)
        {
            ProtosDb db = registrator.PrototypesDb;

            ScaleExcavator(db, Ids.Vehicles.ExcavatorT3, multiplier: 2);
            ScaleExcavator(db, Ids.Vehicles.ExcavatorT3H, multiplier: 3);
        }

        private static void SetField(System.Type type, object obj, string fieldName, object value)
        {
            var field = type.GetField(fieldName, FLAGS);
            if (field != null)
                field.SetValue(obj, value);
            else
                Mafi.Log.Warning($"CustomMod: Field {fieldName} not found on {type.Name}");
        }

        private static void ScaleExcavator(ProtosDb db, Proto.ID id, int multiplier)
        {
            var excavator = db.GetOrThrow<ExcavatorProto>(id);

            var oldBucket = excavator.Capacity;
            var newBucket = new Quantity(oldBucket.Value * multiplier);
            SetField(typeof(ExcavatorProto), excavator, "Capacity", newBucket);
            Mafi.Log.Info($"CustomMod: {id} bucket capacity {oldBucket.Value} -> {newBucket.Value}");

            var costs = excavator.Costs;
            var costPercent = Percent.FromRatio((Fix32)multiplier, Fix32.One);
            var scaledConstruction = costs.BaseConstructionCost.ScaledBy(costPercent);
            var maintenancePercent = 150.Percent();
            var oldMaint = costs.Maintenance;
            var scaledMaint = new MaintenanceCosts(oldMaint.Product,
                oldMaint.MaintenancePerMonth.ScaledBy(maintenancePercent),
                oldMaint.MaxMaintenancePerMonth.ScaledBy(maintenancePercent),
                oldMaint.ExtraBufferDuration, oldMaint.InitialMaintenanceBoost);
            var newCosts = new EntityCosts(scaledConstruction, costs.DefaultPriority, costs.Workers, scaledMaint);
            SetField(typeof(EntityProto), excavator, "<Costs>k__BackingField", newCosts);
            Mafi.Log.Info($"CustomMod: {id} construction cost {multiplier}x, maintenance 1.5x");

            var newThickness = ImmutableArray.Create(2.5, 2.5, 2.0, 1.5, 1.0)
                .Map((double x) => x.TilesThick());
            SetField(typeof(ExcavatorProto), excavator, "MinedThicknessByDistance", newThickness);
            Mafi.Log.Info($"CustomMod: {id} mined thickness increased by +0.5 per ring");

            if (excavator.FuelTankProto.HasValue)
            {
                var fuelTank = excavator.FuelTankProto.Value;
                var tankType = typeof(FuelTankProto);

                SetField(tankType, fuelTank, "IdleFuelConsumption", Percent.Zero);

                var oldCapacity = fuelTank.Capacity;
                var newFuelCapacity = new Quantity(oldCapacity.Value * 3);
                SetField(tankType, fuelTank, "Capacity", newFuelCapacity);

                var oldDuration = fuelTank.Duration;
                var newDuration = new Duration(oldDuration.Ticks * 2);
                SetField(tankType, fuelTank, "Duration", newDuration);

                var oldReserve = fuelTank.ReserveDuration;
                SetField(tankType, fuelTank, "ReserveDuration", new Duration(oldReserve.Ticks * 2));

                SetField(tankType, fuelTank, "OneQuantityDuration", fuelTank.QuantityToDuration(Quantity.One));

                Mafi.Log.Info($"CustomMod: {id} fuel tank — capacity {oldCapacity}->{newFuelCapacity}, " +
                    $"duration {oldDuration}->{newDuration}, idle consumption set to 0%");
            }
        }
    }
}
