using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Genes40k;

/// <summary>
/// Optional behaviour attached to a ChapterTraitDef, loaded from XML with a Class attribute. One instance exists per
/// trait def and is shared by every pawn carrying a chapter gene built from it, so a worker must keep no per-pawn
/// state: everything it needs comes from the gene and pawn it is handed, or from a hediff on that pawn.
/// </summary>
public class ChapterTraitWorker
{
    [Unsaved(false)]
    public ChapterTraitDef def;

    /// <summary>
    /// Ticks between Tick calls. 0 means the worker is never ticked.
    /// </summary>
    public int tickInterval;

    public virtual void PostMake(Gene_CustomChapter gene, Pawn pawn)
    {
    }

    public virtual void PostAdd(Gene_CustomChapter gene, Pawn pawn)
    {
    }

    public virtual void PostRemove(Gene_CustomChapter gene, Pawn pawn)
    {
    }

    public virtual void Tick(Gene_CustomChapter gene, Pawn pawn)
    {
    }

    public virtual void Notify_PawnDied(Gene_CustomChapter gene, Pawn pawn, DamageInfo? dinfo, Hediff culprit)
    {
    }

    public virtual void Notify_IngestedThing(Gene_CustomChapter gene, Pawn pawn, Thing thing, int numTaken)
    {
    }

    public virtual void Notify_NewColony(Gene_CustomChapter gene, Pawn pawn)
    {
    }

    public virtual void Reset(Gene_CustomChapter gene, Pawn pawn)
    {
    }

    public virtual IEnumerable<Gizmo> GetGizmos(Gene_CustomChapter gene, Pawn pawn)
    {
        yield break;
    }

    public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats(Gene_CustomChapter gene, Pawn pawn)
    {
        yield break;
    }

    public virtual IEnumerable<string> ConfigErrors()
    {
        yield break;
    }
}
