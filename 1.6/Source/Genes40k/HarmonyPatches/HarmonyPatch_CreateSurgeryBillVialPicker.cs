using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Genes40k;

/// <summary>
/// The generic gene-seed implantation and chapter application recipes open their picker instead of creating a plain
/// bill; the picker creates the pinned bill itself once a choice is made.
/// </summary>
[HarmonyPatch(typeof(HealthCardUtility), nameof(HealthCardUtility.CreateSurgeryBill))]
public static class CreateSurgeryBillVialPicker
{
    public static bool Prefix(Pawn medPawn, RecipeDef recipe, BodyPartRecord part, List<Thing> uniqueIngredients, ref Bill_Medical __result)
    {
        if (recipe == null || medPawn == null)
        {
            return true;
        }

        if (recipe.GetModExtension<DefModExtension_GeneseedVialRecipe>() is { selectsVialKind: true })
        {
            Find.WindowStack.Add(new Dialog_SelectGeneseedVial(medPawn, recipe, part));
            __result = null;
            return false;
        }

        if (recipe.GetModExtension<DefModExtension_LegionMaterialCreation>() is not { selectsChapter: true })
        {
            return true;
        }

        Find.WindowStack.Add(new Dialog_SelectChapter(medPawn, recipe, part));
        __result = null;
        return false;
    }
}
