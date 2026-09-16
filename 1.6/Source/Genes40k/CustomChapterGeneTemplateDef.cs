using System.Collections.Generic;
using Verse;

namespace Genes40k;

/// <summary>
/// Tuning for one line of player-designed gene-seeds (chapter, primarch or Custodes) and the template fields every
/// generated GeneDef of that line copies. One def per line; the kind decides which budget and editor apply.
/// </summary>
public class CustomChapterGeneTemplateDef : Def
{
    public CustomGeneKind kind = CustomGeneKind.Chapter;

    public string geneLabel = "Gene-seed Material";
    public string geneDescription;
    [NoTranslate]
    public string iconPath;
    public GeneCategoryDef displayCategory;
    public float displayOrderInCategory;
    public List<string> exclusionTags;
    public int biostatCpx = 1;
    public float marketValueFactor = 1f;
    public float minAgeActive;

    public int failChancePerStabilityPoint = 5;
    public int failChanceCapPerStabilityPoint = 5;
    public float rubiconPerStabilityPoint = 0.1f;
    public IntRange stabilityRange = new(-5, 5);
    public int maxTraits = 4;
    public List<ResearchProjectDef> extraTraitSlotResearches;
    public int maxCustomChapters = 20;
    [NoTranslate]
    public string vialTexturePath;

    public int gestationTicksPerPoint = 60000;
    public int minGestationTicks = 150000;

    public int disciplinePoints = 20;
    public int extraPointsPerResearch = 3;
    public List<ResearchProjectDef> extraPointResearches;
    public int softCapLevel = 7;
    public int softCapCostPerLevel = 2;
}
