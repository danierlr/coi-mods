using Mafi;
using Mafi.Base;
using Mafi.Core;
using Mafi.Core.Factory.NuclearReactors;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Mods;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;

namespace CustomMod
{
    public class FbrSteamModData : IModData
    {
        private static readonly System.Reflection.BindingFlags FLAGS =
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance;

        public void RegisterData(ProtoRegistrator registrator)
        {
            ProtosDb db = registrator.PrototypesDb;

            // --- FBR: 4x water/steam, reduce computing to 4 TFlops ---
            NuclearReactorProto fbr = db.GetOrThrow<NuclearReactorProto>(Ids.Buildings.FastBreederReactor);

            // Quadruple water input per power level (16 -> 64)
            var waterField = typeof(NuclearReactorProto).GetField("WaterInPerPowerLevel", FLAGS);
            if (waterField != null)
            {
                var current = (ProductQuantity)waterField.GetValue(fbr);
                waterField.SetValue(fbr, new ProductQuantity(current.Product, new Quantity(current.Quantity.Value * 4)));
                Mafi.Log.Info($"FbrSteamMod: Water input changed from {current.Quantity.Value} to {current.Quantity.Value * 4}");
            }

            // Quadruple SP steam output per power level (16 -> 64)
            var steamField = typeof(NuclearReactorProto).GetField("SteamOutPerPowerLevel", FLAGS);
            if (steamField != null)
            {
                var current = (ProductQuantity)steamField.GetValue(fbr);
                steamField.SetValue(fbr, new ProductQuantity(current.Product, new Quantity(current.Quantity.Value * 4)));
                Mafi.Log.Info($"FbrSteamMod: Steam output changed from {current.Quantity.Value} to {current.Quantity.Value * 4}");
            }

            // Reduce computing requirement (18 TFlops -> 4 TFlops)
            var computingProp = typeof(NuclearReactorProto).GetProperty("ComputingConsumed", FLAGS);
            if (computingProp != null && computingProp.CanWrite)
            {
                computingProp.SetValue(fbr, Computing.FromTFlops(4));
                Mafi.Log.Info("FbrSteamMod: Computing reduced to 4 TFlops");
            }
            else
            {
                // Try backing field
                var computingField = typeof(NuclearReactorProto).GetField("<ComputingConsumed>k__BackingField", FLAGS);
                if (computingField != null)
                {
                    computingField.SetValue(fbr, Computing.FromTFlops(4));
                    Mafi.Log.Info("FbrSteamMod: Computing reduced to 4 TFlops (via backing field)");
                }
            }

            // --- Maintenance III: double output ---
            RecipeProto recipeT3 = db.GetOrThrow<RecipeProto>(Ids.Recipes.MaintenanceT3);
            DoubleRecipeOutputs(recipeT3);

            RecipeProto recipeT3R = db.GetOrThrow<RecipeProto>(Ids.Recipes.MaintenanceT3Recycling);
            DoubleRecipeOutputs(recipeT3R);
        }

        private static void DoubleRecipeOutputs(RecipeProto recipe)
        {
            // RecipeOutput is a class extending RecipeProduct.
            // RecipeProduct has: public readonly Quantity Quantity;
            // We modify each output's Quantity via reflection.
            var quantityField = typeof(RecipeProduct).GetField("Quantity", FLAGS);
            if (quantityField == null)
            {
                Mafi.Log.Error("FbrSteamMod: Could not find Quantity field on RecipeProduct");
                return;
            }

            foreach (var output in recipe.AllOutputs)
            {
                var oldQty = (Quantity)quantityField.GetValue(output);
                quantityField.SetValue(output, new Quantity(oldQty.Value * 2));
                Mafi.Log.Info($"FbrSteamMod: Doubled output {output.Product.Id} from {oldQty.Value} to {oldQty.Value * 2}");
            }
        }
    }
}
