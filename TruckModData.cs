using System.Reflection;
using Mafi;
using Mafi.Base;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Maintenance;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using Mafi.Core.Vehicles.Trucks;

namespace CustomMod
{
    public class TruckModData : IModData
    {
        private static readonly BindingFlags FLAGS =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public void RegisterData(ProtoRegistrator registrator)
        {
            ProtosDb db = registrator.PrototypesDb;

            ScaleTruck(db, Ids.Vehicles.TruckT3Loose, multiplier: 2);
            ScaleTruck(db, Ids.Vehicles.TruckT3Fluid, multiplier: 2);
            ScaleTruck(db, Ids.Vehicles.TruckT3LooseH, multiplier: 3);
            ScaleTruck(db, Ids.Vehicles.TruckT3FluidH, multiplier: 3);
        }

        private static void SetField(System.Type type, object obj, string fieldName, object value)
        {
            var field = type.GetField(fieldName, FLAGS);
            if (field != null)
                field.SetValue(obj, value);
            else
                Mafi.Log.Warning($"CustomMod: Field {fieldName} not found on {type.Name}");
        }

        private static void ScaleTruck(ProtosDb db, Proto.ID id, int multiplier)
        {
            var truck = db.GetOrThrow<TruckProto>(id);

            var oldCapacity = truck.CapacityBase;
            var newCapacity = new Quantity(oldCapacity.Value * multiplier);
            SetField(typeof(TruckProto), truck, "CapacityBase", newCapacity);
            Mafi.Log.Info($"CustomMod: {id} capacity {oldCapacity.Value} -> {newCapacity.Value}");

            var costs = truck.Costs;
            var costPercent = Percent.FromRatio((Fix32)multiplier, Fix32.One);
            var scaledConstruction = costs.BaseConstructionCost.ScaledBy(costPercent);
            var maintenancePercent = 150.Percent();
            var oldMaint = costs.Maintenance;
            var scaledMaint = new MaintenanceCosts(oldMaint.Product,
                oldMaint.MaintenancePerMonth.ScaledBy(maintenancePercent),
                oldMaint.MaxMaintenancePerMonth.ScaledBy(maintenancePercent),
                oldMaint.ExtraBufferDuration, oldMaint.InitialMaintenanceBoost);
            var newCosts = new EntityCosts(scaledConstruction, costs.DefaultPriority, costs.Workers, scaledMaint);
            SetField(typeof(EntityProto), truck, "<Costs>k__BackingField", newCosts);
            Mafi.Log.Info($"CustomMod: {id} construction cost {multiplier}x, maintenance 1.5x");

            if (truck.FuelTankProto.HasValue)
            {
                var fuelTank = truck.FuelTankProto.Value;
                var tankType = typeof(FuelTankProto);

                SetField(tankType, fuelTank, "IdleFuelConsumption", Percent.Zero);

                var oldFuelCap = fuelTank.Capacity;
                var newFuelCap = new Quantity(oldFuelCap.Value * 3);
                SetField(tankType, fuelTank, "Capacity", newFuelCap);

                var oldDuration = fuelTank.Duration;
                var newDuration = new Duration(oldDuration.Ticks * 2);
                SetField(tankType, fuelTank, "Duration", newDuration);

                var oldReserve = fuelTank.ReserveDuration;
                SetField(tankType, fuelTank, "ReserveDuration", new Duration(oldReserve.Ticks * 2));

                SetField(tankType, fuelTank, "OneQuantityDuration", fuelTank.QuantityToDuration(Quantity.One));

                Mafi.Log.Info($"CustomMod: {id} fuel tank — capacity {oldFuelCap}->{newFuelCap}, " +
                    $"duration {oldDuration}->{newDuration}, idle consumption set to 0%");
            }
        }
    }
}
