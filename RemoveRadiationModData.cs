using System.Reflection;
using Mafi;
using Mafi.Base;
using Mafi.Core.Buildings.Storages;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;

namespace CustomMod
{
    public class RemoveRadiationModData : IModData
    {
        public void RegisterData(ProtoRegistrator registrator)
        {
            var proto = registrator.PrototypesDb.GetOrThrow<NuclearWasteStorageProto>(
                Ids.Buildings.NuclearWasteStorage);

            SetField(proto, "Capacity", int.MaxValue.Quantity());

            Mafi.Log.Info($"CustomMod: NuclearWasteStorage capacity -> {int.MaxValue}");
        }

        private static void SetField<T>(object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Mafi.Log.Warning(
                    $"CustomMod: Field {fieldName} not found on {obj.GetType().Name}");
            }
        }
    }
}
