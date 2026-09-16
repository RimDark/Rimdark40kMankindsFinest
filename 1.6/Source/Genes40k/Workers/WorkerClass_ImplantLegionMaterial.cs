using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Genes40k;

public class WorkerClass_ImplantLegionMaterial : Recipe_Surgery
{
    private GameComponent_UnlockedMaterials GameComp => GameComponent_UnlockedMaterials.Instance;
    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        if (!base.AvailableOnNow(thing, part) || thing is not Pawn pawn)
        {
            return false;
        }

        if (pawn.genes == null)
        {
            return false;
        }

        if (!pawn.IsFirstborn())
        {
            return false;
        }
        
        if (pawn.genes.GenesListForReading.Any(gene => gene.def.HasModExtension<DefModExtension_ChapterGene>()))
        {
            return false;
        }

        var defMod = recipe.GetModExtension<DefModExtension_LegionMaterialCreation>();

        if (defMod == null)
        {
            return false;
        }

        if (defMod.selectsChapter)
        {
            return ChapterChoiceUtility.AnyChoice();
        }

        if (ChapterChoiceUtility.GenericChapterRecipeExists)
        {
            return false;
        }

        return GameComp.HasMaterial(defMod.requiredLegionMaterial);
    }

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        if (billDoer != null)
        {
            if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }
            TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
        }
        OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
    }

    protected override void OnSurgerySuccess(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        var addedGene = (bill as Bill_ApplyChapter)?.chapterGene;

        if (addedGene == null)
        {
            var material = recipe.GetModExtension<DefModExtension_LegionMaterialCreation>()?.requiredLegionMaterial;
            addedGene = material?.GetModExtension<DefModExtension_GeneFromMaterial>()?.addedGene;
        }

        CustomChapterGeneUtility.AddChapterGene(pawn, addedGene, false);
    }
}
