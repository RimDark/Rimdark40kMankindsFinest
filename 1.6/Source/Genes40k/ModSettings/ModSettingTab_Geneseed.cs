using Core40k;
using UnityEngine;
using Verse;

namespace Genes40k;

public class ModSettingTab_Geneseed : ModSettingTab
{
    public override void DrawTab(Rect inRect, ModSettings settings)
    {
        if (settings is not Genes40kModSettings genes40KModSettings)
        {
            Log.Error("Settings not correct type");
            return;
        }
        
        var viewRect = new Rect(inRect.x, inRect.y, inRect.width - 16f, scrollViewHeight);
        scrollViewHeight = 0f;
            
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(viewRect);
        listingStandard.Gap(36);
        scrollViewHeight += ListingHeightIncreaseGap;
        scrollViewHeight += ListingHeightIncrease;
        
        //Default Chapter Colour
        listingStandard.Label("BEWH.MankindsFinest.ModSettings.DefaultChapterColoursDesc".Translate());
        scrollViewHeight += ListingHeightIncrease;
        listingStandard.Indent(inRect.width * 0.25f);
        if (listingStandard.ButtonText("BEWH.MankindsFinest.ModSettings.DefaultChapterColours".Translate(genes40KModSettings.CurrentlySelectedPreset.label), widthPct: 0.5f))
        {
            Find.WindowStack.Add(new Dialog_ChangeDefaultChapterColour(genes40KModSettings));
        }
        scrollViewHeight += ListingHeightIncreaseGap;
        listingStandard.Outdent(inRect.width * 0.25f);
        
        //Geneseed Implantation Offset
        listingStandard.GapLine(36);
        scrollViewHeight += ListingHeightIncreaseGap;
        listingStandard.Label("BEWH.MankindsFinest.ModSettings.ImplantationDesc".Translate());
        scrollViewHeight += ListingHeightIncrease;
        //Geneseed Implantation Success Offset
        genes40KModSettings.implantationSuccessOffset = (int)listingStandard.SliderLabeled("BEWH.MankindsFinest.ModSettings.ImplantationSuccessOffset".Translate(genes40KModSettings.implantationSuccessOffset),genes40KModSettings.implantationSuccessOffset, -200, 200, tooltip: "BEWH.MankindsFinest.ModSettings.ImplantationSuccessOffsetDesc".Translate());
        scrollViewHeight += ListingHeightIncrease;
        //Geneseed Implantation Cap Offset
        genes40KModSettings.implantationCapOffset = (int)listingStandard.SliderLabeled("BEWH.MankindsFinest.ModSettings.ImplantationCapOffset".Translate(genes40KModSettings.implantationCapOffset), genes40KModSettings.implantationCapOffset, -100, 100, tooltip: "BEWH.MankindsFinest.ModSettings.ImplantationCapOffsetDesc".Translate());
        scrollViewHeight += ListingHeightIncrease;
        
        //Matrix gestation time factor
        listingStandard.GapLine(36);
        scrollViewHeight += ListingHeightIncreaseGap;
        genes40KModSettings.matrixGestationTimeFactor = (int)listingStandard.SliderLabeled("BEWH.MankindsFinest.ModSettings.MatrixGestationFactor".Translate(genes40KModSettings.matrixGestationTimeFactor), genes40KModSettings.matrixGestationTimeFactor, 1, 200, tooltip: "BEWH.MankindsFinest.ModSettings.MatrixGestationFactorDesc".Translate());
        scrollViewHeight += ListingHeightIncrease;
        
        //Custom chapter design limits
        listingStandard.GapLine(36);
        scrollViewHeight += ListingHeightIncreaseGap;
        listingStandard.Label("BEWH.MankindsFinest.ModSettings.CustomDesignDesc".Translate());
        scrollViewHeight += ListingHeightIncrease;
        DrawDesignLimits(listingStandard, ref genes40KModSettings.chapterStabilityRange, ref genes40KModSettings.chapterBaseTraitPicks, "BEWH.MankindsFinest.ModSettings.ChapterStabilityRange", "BEWH.MankindsFinest.ModSettings.ChapterBaseTraitPicks", 1001);
        DrawDesignLimits(listingStandard, ref genes40KModSettings.primarchComplexityRange, ref genes40KModSettings.primarchBaseTraitPicks, "BEWH.MankindsFinest.ModSettings.PrimarchComplexityRange", "BEWH.MankindsFinest.ModSettings.PrimarchBaseTraitPicks", 1002);
        listingStandard.Gap(6);
        if (listingStandard.ButtonText("BEWH.MankindsFinest.ModSettings.ResetDesignLimits".Translate(), widthPct: 0.3f))
        {
            genes40KModSettings.chapterStabilityRange = Genes40kModSettings.DefaultChapterStabilityRange;
            genes40KModSettings.chapterBaseTraitPicks = Genes40kModSettings.DefaultChapterBaseTraitPicks;
            genes40KModSettings.primarchComplexityRange = Genes40kModSettings.DefaultPrimarchComplexityRange;
            genes40KModSettings.primarchBaseTraitPicks = Genes40kModSettings.DefaultPrimarchBaseTraitPicks;
        }
        scrollViewHeight += ListingHeightIncreaseGap;
        
        scrollViewHeight += ListingHeightIncrease;
        
        listingStandard.End();
        Widgets.EndScrollView();
    }

    private void DrawDesignLimits(Listing_Standard listingStandard, ref IntRange range, ref int basePicks, string rangeKey, string picksKey, int id)
    {
        listingStandard.Gap(6);
        scrollViewHeight += 6f;
        listingStandard.Label(rangeKey.Translate(range.min.ToStringWithSign(), range.max.ToStringWithSign()), tooltip: (rangeKey + "Desc").Translate());
        scrollViewHeight += ListingHeightIncrease;
        Widgets.IntRange(listingStandard.GetRect(32f), id, ref range, MinStabilityBound, MaxStabilityBound);
        scrollViewHeight += 32f;
        basePicks = (int)listingStandard.SliderLabeled(picksKey.Translate(basePicks), basePicks, 1, MaxBaseTraitPicks, tooltip: (picksKey + "Desc").Translate());
        scrollViewHeight += ListingHeightIncrease;
    }

    private const int MinStabilityBound = -20;
    private const int MaxStabilityBound = 20;
    private const int MaxBaseTraitPicks = 12;
}
