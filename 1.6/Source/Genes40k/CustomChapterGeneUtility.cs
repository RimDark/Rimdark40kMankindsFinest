using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Genes40k;

public static class CustomChapterGeneUtility
{
    public static CustomChapterGeneTemplateDef Tuning => Genes40kDefOf.BEWH_CustomChapterGene;

    private static List<CustomChapterGeneTemplateDef> templatesInOrder;
    public static List<CustomChapterGeneTemplateDef> TemplatesInOrder => templatesInOrder ??= DefDatabase<CustomChapterGeneTemplateDef>.AllDefsListForReading
        .Where(template => template.kind != CustomGeneKind.Any)
        .OrderBy(template => (int)template.kind)
        .ToList();

    public static CustomChapterGeneTemplateDef TemplateFor(CustomGeneKind kind)
    {
        return TemplatesInOrder.FirstOrDefault(template => template.kind == kind) ?? Tuning;
    }

    private static List<ChapterTraitDef> allTraits;
    public static List<ChapterTraitDef> AllTraits => allTraits ??= DefDatabase<ChapterTraitDef>.AllDefsListForReading
        .OrderBy(trait => trait.displayOrder)
        .ThenBy(trait => trait.label)
        .ToList();

    private static readonly Dictionary<CustomGeneKind, List<ChapterTraitDef>> traitsByKind = new();

    public static List<ChapterTraitDef> TraitsFor(CustomGeneKind kind)
    {
        if (!traitsByKind.TryGetValue(kind, out var traits))
        {
            traits = AllTraits.Where(trait => trait.AppliesTo(kind)).ToList();
            traitsByKind[kind] = traits;
        }

        return traits;
    }

    private static List<CustodesDisciplineDef> allDisciplines;
    public static List<CustodesDisciplineDef> AllDisciplines => allDisciplines ??= DefDatabase<CustodesDisciplineDef>.AllDefsListForReading
        .OrderBy(discipline => discipline.displayOrder)
        .ThenBy(discipline => discipline.label)
        .ToList();

    private static List<GeneDef> primarchGenes;
    /// <summary>
    /// The shipped primarch-specific genes, in legion order, for the related-primarch picker.
    /// </summary>
    public static List<GeneDef> ShippedPrimarchGenes => primarchGenes ??= DefDatabase<GeneDef>.AllDefsListForReading
        .Where(geneDef => geneDef.HasModExtension<DefModExtension_PrimarchMaterial>() && !CustomChapterGeneDefBuilder.IsGeneratedChapterGene(geneDef))
        .OrderBy(geneDef => geneDef.displayOrderInCategory)
        .ToList();

    private static string MaterialLabel(ThingDef material)
    {
        var shownName = material.GetModExtension<DefModExtension_BaseMaterial>()?.shownMaterialName;
        return shownName.NullOrEmpty() ? material.label : shownName;
    }

    public static int StabilityOf(IEnumerable<ChapterTraitDef> traits)
    {
        return traits.Sum(trait => trait?.stabilityOffset ?? 0);
    }

    public static int MaxTraits(CustomChapterGeneTemplateDef template = null)
    {
        template ??= Tuning;
        var max = template.maxTraits;

        if (template.extraTraitSlotResearches.NullOrEmpty())
        {
            return max;
        }

        return max + template.extraTraitSlotResearches.Count(research => research != null && research.IsFinished);
    }

    public static int DisciplinePoints(CustomChapterGeneTemplateDef template)
    {
        var points = template.disciplinePoints;

        if (template.extraPointResearches.NullOrEmpty())
        {
            return points;
        }

        return points + template.extraPointsPerResearch * template.extraPointResearches.Count(research => research != null && research.IsFinished);
    }

    /// <summary>
    /// Points a discipline costs at a level: costPerLevel per level up to the soft cap, softCapCostPerLevel above it.
    /// </summary>
    public static int PointCost(CustodesDisciplineDef discipline, int level, CustomChapterGeneTemplateDef template)
    {
        if (discipline == null || level <= 0)
        {
            return 0;
        }

        var belowCap = Mathf.Min(level, template.softCapLevel);
        var aboveCap = Mathf.Max(0, level - template.softCapLevel);
        return belowCap * discipline.costPerLevel + aboveCap * Mathf.Max(discipline.costPerLevel, template.softCapCostPerLevel);
    }

    public static int PointsSpent(IEnumerable<CustodesDisciplineLevel> levels, CustomChapterGeneTemplateDef template)
    {
        return levels?.Sum(entry => PointCost(entry?.def, entry?.level ?? 0, template)) ?? 0;
    }

    public static string PatternCode(IEnumerable<CustodesDisciplineLevel> levels)
    {
        var byDef = (levels ?? Enumerable.Empty<CustodesDisciplineLevel>()).Where(entry => entry?.def != null).ToDictionary(entry => entry.def, entry => entry.level);
        return string.Join("-", AllDisciplines.Select(discipline => byDef.TryGetValue(discipline, out var level) ? level : 0));
    }

    /// <summary>
    /// Whether the trait can currently be picked in the editor, with the unmet requirement as text when it cannot.
    /// </summary>
    public static bool TraitUnlocked(ChapterTraitDef trait, out string lockedReason)
    {
        lockedReason = null;

        if (trait == null)
        {
            return true;
        }

        if (!trait.requiredAnyMaterial.NullOrEmpty())
        {
            var gameComp = GameComponent_UnlockedMaterials.Instance;

            if (gameComp == null || !trait.requiredAnyMaterial.Any(gameComp.HasMaterial))
            {
                var names = trait.requiredAnyMaterial.Select(MaterialLabel).ToCommaListOr();
                lockedReason = "BEWH.MankindsFinest.CustomChapter.RequiresMaterial".Translate(names);
                return false;
            }
        }

        if (trait.requiredResearch != null && !trait.requiredResearch.IsFinished)
        {
            lockedReason = "BEWH.MankindsFinest.CustomChapter.RequiresResearch".Translate(trait.requiredResearch.LabelCap);
            return false;
        }

        return true;
    }

    public static string RequirementDescription(ChapterTraitDef trait)
    {
        if (trait == null)
        {
            return null;
        }

        var parts = new List<string>();

        if (!trait.requiredAnyMaterial.NullOrEmpty())
        {
            var names = trait.requiredAnyMaterial.Select(MaterialLabel).ToCommaListOr();
            parts.Add("BEWH.MankindsFinest.CustomChapter.RequiresMaterial".Translate(names));
        }

        if (trait.requiredResearch != null)
        {
            parts.Add("BEWH.MankindsFinest.CustomChapter.RequiresResearch".Translate(trait.requiredResearch.LabelCap));
        }

        return parts.Any() ? parts.ToLineList() : null;
    }

    public static string StabilityEffectDescription(int stability)
    {
        var failOffset = -stability * Tuning.failChancePerStabilityPoint;
        var capOffset = -stability * Tuning.failChanceCapPerStabilityPoint;
        var rubicon = stability * Tuning.rubiconPerStabilityPoint;

        return "BEWH.MankindsFinest.CustomChapter.StabilityEffects".Translate(failOffset.ToStringWithSign(), capOffset.ToStringWithSign(), rubicon.ToStringWithSign("0.#"));
    }

    public static string ComplexityEffectDescription(CustomChapterGeneTemplateDef template, int complexity)
    {
        var offsetTicks = -complexity * template.gestationTicksPerPoint;
        var days = offsetTicks / (float)GenDate.TicksPerDay;
        return "BEWH.MankindsFinest.CustomPrimarch.ComplexityEffects".Translate(days.ToStringWithSign("0.#"));
    }

    /// <summary>
    /// The budget line shown under a design: stability effects for chapters, gestation offset for primarchs, points for Custodes.
    /// </summary>
    public static string BudgetEffectDescription(CustomChapterGeneTemplateDef template, int budget)
    {
        return template.kind switch
        {
            CustomGeneKind.Primarch => ComplexityEffectDescription(template, budget),
            CustomGeneKind.Custodes => "BEWH.MankindsFinest.CustomCustodes.PointsSpent".Translate(budget, DisciplinePoints(template)),
            _ => StabilityEffectDescription(budget)
        };
    }

    public static string BudgetLabel(CustomChapterGeneTemplateDef template)
    {
        return template.kind switch
        {
            CustomGeneKind.Primarch => "BEWH.MankindsFinest.CustomPrimarch.Complexity".Translate(),
            CustomGeneKind.Custodes => "BEWH.MankindsFinest.CustomCustodes.Points".Translate(),
            _ => "BEWH.MankindsFinest.CustomChapter.Stability".Translate()
        };
    }

    public static string BudgetDescription(CustomChapterGeneTemplateDef template)
    {
        return template.kind switch
        {
            CustomGeneKind.Primarch => "BEWH.MankindsFinest.CustomPrimarch.ComplexityDesc".Translate(),
            CustomGeneKind.Custodes => "BEWH.MankindsFinest.CustomCustodes.PointsDesc".Translate(),
            _ => "BEWH.MankindsFinest.CustomChapter.StabilityDesc".Translate()
        };
    }

    public static string KindLabel(CustomChapterGeneTemplateDef template)
    {
        return template.kind switch
        {
            CustomGeneKind.Primarch => "BEWH.MankindsFinest.CustomPrimarch.KindLabel".Translate(),
            CustomGeneKind.Custodes => "BEWH.MankindsFinest.CustomCustodes.KindLabel".Translate(),
            _ => "BEWH.MankindsFinest.CustomChapter.KindLabel".Translate()
        };
    }

    /// <summary>
    /// Adds the chapter gene carried by a vial to the pawn and names the pawn's xenotype after the chapter.
    /// </summary>
    public static void AddChapterGeneFromVial(Pawn pawn, GeneseedVial geneseedVial, bool lockChapter = true)
    {
        AddChapterGene(pawn, geneseedVial?.extraGeneFromMaterial, true, lockChapter);
    }

    /// <summary>
    /// Adds a chapter gene to the pawn, optionally naming the pawn's xenotype after the chapter. For a custom design the
    /// generated gene brings its grants along and the design is locked.
    /// </summary>
    public static void AddChapterGene(Pawn pawn, GeneDef chapterGene, bool renameXenotype = true, bool lockChapter = true)
    {
        if (pawn?.genes == null || chapterGene == null)
        {
            return;
        }

        if (!pawn.genes.HasActiveGene(chapterGene))
        {
            pawn.genes.AddGene(chapterGene, true);
        }

        if (renameXenotype)
        {
            var chapterName = chapterGene.GetModExtension<DefModExtension_ChapterGene>()?.chapterName;

            if (!chapterName.NullOrEmpty())
            {
                pawn.genes.xenotypeName = chapterName;
            }
        }

        if (!lockChapter)
        {
            return;
        }

        var customChapter = GameComponent_CustomChapterGenes.Instance?.GetByGeneDef(chapterGene);

        if (customChapter != null)
        {
            customChapter.locked = true;
        }
    }

    /// <summary>
    /// The implantation failure offsets a chapter gene contributes; generated chapter genes carry a purity extension computed from their stability.
    /// </summary>
    public static bool TryGetChapterGenePurityOffsets(GeneDef chapterGene, out int failChanceOffset, out int failChanceCapOffset, out string sourceLabel)
    {
        failChanceOffset = 0;
        failChanceCapOffset = 0;
        sourceLabel = null;

        var geneDefMod = chapterGene?.GetModExtension<DefModExtension_GeneseedPurity>();

        if (geneDefMod == null)
        {
            return false;
        }

        failChanceOffset = geneDefMod.additionalChanceOffset;
        failChanceCapOffset = geneDefMod.additionalChanceCapOffset;
        sourceLabel = chapterGene.label;
        return true;
    }

    public static bool TryGetVialPurityOffsets(GeneseedVial geneseedVial, out int failChanceOffset, out int failChanceCapOffset, out string sourceLabel)
    {
        return TryGetChapterGenePurityOffsets(geneseedVial?.extraGeneFromMaterial, out failChanceOffset, out failChanceCapOffset, out sourceLabel);
    }

    public static string ChapterLabelOf(GeneseedVial geneseedVial)
    {
        return ChapterLabelOf(geneseedVial?.extraGeneFromMaterial);
    }

    public static string ChapterLabelOf(GeneDef chapterGene)
    {
        if (chapterGene == null)
        {
            return null;
        }

        var chapterName = chapterGene.GetModExtension<DefModExtension_ChapterGene>()?.chapterName;

        if (chapterName.NullOrEmpty())
        {
            chapterName = chapterGene.GetModExtension<DefModExtension_PrimarchMaterial>()?.shownMaterialName;
        }

        if (chapterName.NullOrEmpty() && CustomChapterGeneDefBuilder.IsGeneratedChapterGene(chapterGene))
        {
            chapterName = GameComponent_CustomChapterGenes.Instance?.GetByGeneDef(chapterGene)?.name;
        }

        return chapterName.NullOrEmpty() ? chapterGene.label : chapterName;
    }

    /// <summary>
    /// The total growth-vat gestation for an embryo carrying these genes: the base time plus every gene's gestation offset, floored.
    /// </summary>
    public static int GestationTicksFor(IEnumerable<GeneDef> genes, int baseTicks)
    {
        var template = TemplateFor(CustomGeneKind.Primarch);
        var offset = genes?.Sum(gene => gene?.GetModExtension<DefModExtension_PrimarchGestation>()?.gestationTicksOffset ?? 0) ?? 0;
        return Mathf.Max(template.minGestationTicks, baseTicks + offset);
    }
}
