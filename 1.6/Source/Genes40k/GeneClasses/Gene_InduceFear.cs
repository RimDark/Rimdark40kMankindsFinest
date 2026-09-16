using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Genes40k;

public class Gene_InduceFear : Gene
{
    private DefModExtension_GeneInducedFear cachedDefMod;
    private bool defModResolved;

    private DefModExtension_GeneInducedFear DefMod
    {
        get
        {
            if (defModResolved)
            {
                return cachedDefMod;
            }

            cachedDefMod = def.GetModExtension<DefModExtension_GeneInducedFear>();
            defModResolved = true;
            return cachedDefMod;
        }
    }

    public override void Tick()
    {
        base.Tick();

        var defMod = DefMod;

        if (defMod == null)
        {
            return;
        }

        if (!pawn.IsHashIntervalTick(defMod.tickInterval) || pawn.Faction == null)
        {
            return;
        }

        if (!pawn.Spawned || pawn.Downed || pawn.InMentalState || pawn.Crawling)
        {
            return;
        }

        if (!pawn.Drafted)
        {
            return;
        }

        var pawns = GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, defMod.effectRadius, useCenter: true).OfType<Pawn>().ToList();
        AffectPawns(pawn, pawns, defMod);
    }

    private void AffectPawns(Pawn p, List<Pawn> pawns, DefModExtension_GeneInducedFear defMod)
    {
        if (pawns.NullOrEmpty() || defMod == null)
        {
            return;
        }
        foreach (var otherPawn in pawns)
        {
            if (otherPawn == null || p == otherPawn || !p.RaceProps.Humanlike || otherPawn.Faction == null || otherPawn.Faction == Faction.OfPlayer || !otherPawn.Faction.HostileTo(Faction.OfPlayer))
            {
                continue;
            }

            if (otherPawn.IsPrisoner)
            {
                continue;
            }

            if (otherPawn.genes != null && otherPawn.genes.GenesListForReading.Any(gene => defMod.genesCausesImmunityToFear.Contains(gene.def)))
            {
                continue;
            }
                
            var traits = otherPawn.story?.traits;

            if (traits != null && !traits.allTraits.NullOrEmpty() && defMod.traitCausesImmunityToFear.Any(traitData => traits.HasTrait(traitData.traitDef)))
            {
                continue;
            }

            if (!Rand.Chance(defMod.chanceToFear / 100f))
            {
                continue;
            }
                
            otherPawn.jobs.StopAll(canReturnToPool: false);
            CellFinderLoose.GetFleeExitPosition(otherPawn, 999, out var intVec);
            var job = JobMaker.MakeJob(Genes40kDefOf.BEWH_InducedFearJob,intVec);
            otherPawn.jobs.StartJob(job);
            otherPawn.CurJob.playerForced = true;
                
            otherPawn.mindState.mentalStateHandler.CurState?.RecoverFromState();
            otherPawn.mindState.mentalStateHandler.TryStartMentalState(Genes40kDefOf.BEWH_InducedFear);
        }
    }
}