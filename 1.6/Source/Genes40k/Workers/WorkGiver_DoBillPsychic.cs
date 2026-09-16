using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Genes40k;

public class WorkGiver_DoBillPsychic : WorkGiver_DoBill
{
    private GameComponent_UnlockedMaterials GameComp => GameComponent_UnlockedMaterials.Instance;
    private static readonly List<Bill> tmpOriginalBills = new();
    private static readonly List<Bill> tmpBlockedBills = new();

    public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
    {
        if (thing is not Building_GeneTable building_GeneTable)
        {
            return null;
        }

        var bills = building_GeneTable.billStack.Bills;
        var gameComp = GameComp;
        float? psychicSensitivity = null;

        tmpBlockedBills.Clear();

        foreach (var bill in bills)
        {
            var matrixDefMod = bill.recipe.GetModExtension<DefModExtension_GeneMatrixRecipe>();

            if (matrixDefMod != null && matrixDefMod.drainsUserWhenMaking)
            {
                psychicSensitivity ??= pawn.GetStatValue(StatDefOf.PsychicSensitivity);

                if (psychicSensitivity <= 0)
                {
                    tmpBlockedBills.Add(bill);
                    JobFailReason.Is("BEWH.MankindsFinest.GeneManupulationTable.PsychicSensitivityRequired".Translate(ProductLabel(bill)), bill.Label);
                    continue;
                }
            }

            var materialDefMod = bill.recipe.GetModExtension<DefModExtension_LegionMaterialCreation>();

            if (materialDefMod != null && (gameComp == null || !gameComp.HasMaterial(materialDefMod.requiredLegionMaterial)))
            {
                tmpBlockedBills.Add(bill);
                JobFailReason.Is("BEWH.MankindsFinest.GeneManupulationTable.MissingLegionMaterial".Translate(ProductLabel(bill), materialDefMod.requiredLegionMaterial.label), bill.Label);
            }
        }

        if (tmpBlockedBills.Count == 0)
        {
            return base.JobOnThing(pawn, thing, forced);
        }

        tmpOriginalBills.Clear();
        tmpOriginalBills.AddRange(bills);

        foreach (var bill in tmpBlockedBills)
        {
            bills.Remove(bill);
        }

        try
        {
            return base.JobOnThing(pawn, thing, forced);
        }
        finally
        {
            tmpOriginalBills.RemoveAll(bill => !bill.CompletableEver);
            bills.Clear();
            bills.AddRange(tmpOriginalBills);
            tmpOriginalBills.Clear();
            tmpBlockedBills.Clear();
        }
    }

    private static string ProductLabel(Bill bill)
    {
        var products = bill.recipe.products;
        return products.NullOrEmpty() ? bill.recipe.label : products[0].Label;
    }
}