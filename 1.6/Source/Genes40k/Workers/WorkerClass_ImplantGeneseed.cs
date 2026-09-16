using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Genes40k;

public class WorkerClass_ImplantGeneseed : Recipe_Surgery
{
    private GeneseedVial geneseedVialForText = null;

    private static bool? genericImplantRecipeExists;
    private static bool GenericImplantRecipeExists => genericImplantRecipeExists ??= DefDatabase<RecipeDef>.AllDefsListForReading.Any(recipeDef => recipeDef.GetModExtension<DefModExtension_GeneseedVialRecipe>()?.selectsVialKind == true);
    
    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        if (!base.AvailableOnNow(thing, part))
        {
            return false;
        }
        if (thing is not Pawn pawn || !pawn.Spawned)
        {
            return false;
        }
        if (pawn.IsSuperHuman())
        {
            return false;
        }
        if (pawn.UndergoingPhaseDevelopment())
        {
            return false;
        }
        if (pawn.story != null && pawn.story.traits.HasTrait(Genes40kDefOf.BEWH_Serf))
        {
            return false;
        }

        var defMod = recipe.GetModExtension<DefModExtension_GeneseedVialRecipe>();

        if (defMod == null)
        {
            return false;
        }

        if (defMod.selectsVialKind)
        {
            geneseedVialForText = null;
            return true;
        }

        if (GenericImplantRecipeExists && GeneseedVialKindUtility.UsesPicker(defMod.geneseedVial))
        {
            return false;
        }

        var list = pawn.Map.listerThings.ThingsOfDef(defMod.geneseedVial);

        foreach (var item in list)
        {
            if (item is not GeneseedVial geneseedVial)
            {
                continue;
            }

            if (!defMod.MatchesVial(geneseedVial))
            {
                continue;
            }
            
            geneseedVialForText = geneseedVial;
            return true;
        }

        return false;
    }

    public override TaggedString GetConfirmation(Pawn pawn)
    {
        if (recipe.GetModExtension<DefModExtension_GeneseedVialRecipe>()?.selectsVialKind == true)
        {
            return null;
        }

        return Genes40kUtils.GetGeneseedImplantationSuccessChanceDesc(pawn, geneseedVialForText);
    }

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
        {
            return;
        }
            
        var geneseedVial = (GeneseedVial)ingredients.First(x => x is GeneseedVial);
            
        ImplantGeneseed(pawn, geneseedVial);

        if (IsViolationOnPawn(pawn, part, Faction.OfPlayer))
        {
            ReportViolation(pawn, billDoer, pawn.HomeFaction, -70);
        }
    }

    private static void ImplantGeneseed(Pawn pawn, GeneseedVial geneseedVial)
    {
        var defMod = geneseedVial.def.GetModExtension<DefModExtension_GeneseedVial>();

        var failChance = Genes40kUtils.GetGeneseedImplantationSuccessChance(pawn, geneseedVial);
            
        if (Rand.Chance(failChance / 100f))
        {
            pawn.Kill(null);
            return;
        }
            
        pawn.genes.SetXenotypeDirect(defMod.xenotype);

        if (defMod.overrideXenotypeGenesGiven)
        {
            foreach (var gene in defMod.overridenAddedGenes.Where(gene => !pawn.genes.HasActiveGene(gene)))
            {
                pawn.genes.AddGene(gene, true);
            }
        }
        else
        {
            foreach (var gene in defMod.xenotype.genes.Where(gene => !pawn.genes.HasActiveGene(gene)))
            {
                pawn.genes.AddGene(gene, true);
            }
        }

        if (defMod.appliesHediff != null)
        {
            pawn.health.AddHediff(defMod.appliesHediff);
        }

        pawn.genes.iconDef = geneseedVial.iconDef;

        CustomChapterGeneUtility.AddChapterGeneFromVial(pawn, geneseedVial);
    }
}