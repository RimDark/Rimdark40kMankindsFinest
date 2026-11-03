using Core40k;
using Verse;

namespace Genes40k;

public class ChapterRankDef : RankDef
{
    public ShoulderIconDef unlocksRankIcon = null;

    public override void UnlockRank(CompRankInfo rankComp)
    {
        base.UnlockRank(rankComp);
        SyncShoulderIcons(rankComp.ParentPawn);
    }

    public override void RemoveRank(CompRankInfo rankComp)
    {
        base.RemoveRank(rankComp);
        SyncShoulderIcons(rankComp.ParentPawn);
    }

    private static void SyncShoulderIcons(Pawn pawn)
    {
        if (pawn?.apparel == null)
        {
            return;
        }

        foreach (var apparel in pawn.apparel.WornApparel)
        {
            apparel.GetComp<CompChapterColorWithShoulderDecoration>()?.SyncRankIcon(pawn);
        }
    }

    public override void DescribeEffects(EffectReport report)
    {
        base.DescribeEffects(report);
        if (unlocksRankIcon != null)
        {
            report.Header("BEWH.MankindsFinest.RankSystem.ShoulderIconUnlock".Translate()).Line(unlocksRankIcon.LabelCap);
        }
    }
}
