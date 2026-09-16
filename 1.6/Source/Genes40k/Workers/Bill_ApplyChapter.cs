using RimWorld;
using Verse;

namespace Genes40k;

/// <summary>
/// Medical bill for the generic chapter application recipe, pinned to the chapter gene chosen when the bill was made.
/// </summary>
public class Bill_ApplyChapter : Bill_Medical
{
    public GeneDef chapterGene;

    public Bill_ApplyChapter()
    {
    }

    public Bill_ApplyChapter(RecipeDef recipe, GeneDef chapterGene) : base(recipe, null)
    {
        this.chapterGene = chapterGene;
    }

    public override string Label => "BEWH.MankindsFinest.ApplyChapter.BillLabel".Translate(CustomChapterGeneUtility.ChapterLabelOf(chapterGene).CapitalizeFirst());

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref chapterGene, "chapterGene");
    }
}
