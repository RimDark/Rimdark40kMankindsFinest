using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Genes40k;

public class WorkerClass_PhaseDevelopment : Recipe_Surgery
{
    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    { 
        if (!base.AvailableOnNow(thing, part))
        {
            return false;
        }
        if (thing is not Pawn pawn)
        {
            return false;
        }
        if (!pawn.health.hediffSet.HasHediff(recipe.removesHediff))
        {
            return false;
        }
        var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(recipe.removesHediff);
            
        return Mathf.Approximately(hediff.Severity, hediff.def.maxSeverity);
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

        if (recipe.removesHediff == null)
        {
            return;
        }

        var hediffToRemove = pawn.health.hediffSet.GetFirstHediffOfDef(recipe.removesHediff);

        if (hediffToRemove == null)
        {
            return;
        }

        if (hediffToRemove.TryGetComp<HediffComp_PhaseDevelopment>() != null)
        {
            pawn.health.RemoveHediff(hediffToRemove);
            return;
        }

        var defMod = recipe.GetModExtension<DefModExtension_PhaseDevelopment>();

        if (defMod != null)
        {
            if (!defMod.addsGenes.NullOrEmpty() && pawn.genes != null)
            {
                foreach (var gene in defMod.addsGenes)
                {
                    pawn.genes.AddGene(gene, true);
                }
            }

            if (defMod.addHediff != null)
            {
                pawn.health.AddHediff(defMod.addHediff);
            }
        }

        pawn.health.RemoveHediff(hediffToRemove);
    }
}
