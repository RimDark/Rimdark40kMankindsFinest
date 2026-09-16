using System.Linq;
using RimWorld;
using Verse;

namespace Genes40k;

public class HediffComp_PhaseDevelopment : HediffComp
{
    private bool applied = false;

    private HediffCompProperties_PhaseDevelopment Props => (HediffCompProperties_PhaseDevelopment)props;

    public override bool CompShouldRemove => base.CompShouldRemove || parent.Severity >= parent.def.maxSeverity;

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        ApplyPhase();
    }

    /// <summary>
    /// Grants the phase's genes and starts the following phase. Runs on any removal of the hediff, so the pawn
    /// keeps what the phase owes them even if something other than the phase finishing takes the hediff away.
    /// </summary>
    private void ApplyPhase()
    {
        if (applied)
        {
            return;
        }

        applied = true;

        var pawn = Pawn;

        if (pawn?.health == null || pawn.Dead)
        {
            return;
        }

        if (pawn.genes != null && !Props.addsGenes.NullOrEmpty())
        {
            foreach (var gene in Props.addsGenes.Where(gene => !pawn.genes.HasActiveGene(gene)))
            {
                pawn.genes.AddGene(gene, true);
            }
        }

        if (Props.nextPhase != null && !pawn.health.hediffSet.HasHediff(Props.nextPhase))
        {
            pawn.health.AddHediff(Props.nextPhase);
        }

        SendLetter(pawn);
    }

    private void SendLetter(Pawn pawn)
    {
        if (Props.letterLabel.NullOrEmpty() || Props.letterText.NullOrEmpty() || !PawnUtility.ShouldSendNotificationAbout(pawn))
        {
            return;
        }

        Find.LetterStack.ReceiveLetter(Props.letterLabel, Props.letterText, Props.letterDef ?? LetterDefOf.PositiveEvent, pawn);
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref applied, "applied", false);
    }
}
