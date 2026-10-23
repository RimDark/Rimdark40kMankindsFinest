using HarmonyLib;
using Verse;

namespace Genes40k;

[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.RedressPawn))]
public class SpaceMarineChapterRedress
{
    public static void Postfix(Pawn pawn)
    {
        if (pawn?.genes == null || pawn.Faction == null || pawn.Faction.IsPlayer || pawn.Faction.def == Genes40kDefOf.BEWH_OffworldMarinesFaction)
        {
            return;
        }

        var chapterGene = pawn.genes.GenesListForReading.FirstOrFallback(gene => gene.def.HasModExtension<DefModExtension_ChapterGene>(), null);
        if (chapterGene == null)
        {
            return;
        }

        Genes40kUtils.ApplyChapterColours(pawn, Genes40kUtils.ChapterColourFor(chapterGene.def));
    }
}
