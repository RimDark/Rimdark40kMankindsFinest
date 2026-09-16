using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core40k;
using HarmonyLib;
using RimWorld;
using VEF.Genes;
using Verse;

namespace Genes40k;

/// <summary>
/// Builds and registers the runtime GeneDef behind a custom design: the chosen ChapterTraitDefs (or a Custodes design's
/// discipline levels) are merged into one def and their random gene/trait grants are handed to Gene_CustomChapter. The
/// generated def is the only GeneDef the system registers; registration happens when a design is saved and again while a
/// save is loading, before any pawn or vial references it.
/// </summary>
public static class CustomChapterGeneDefBuilder
{
    private static readonly FieldInfo CachedDescriptionField = AccessTools.Field(typeof(GeneDef), "cachedDescription");
    private static readonly FieldInfo CachedIconField = AccessTools.Field(typeof(GeneDef), "cachedIcon");
    private static readonly FieldInfo CachedGenesInOrderField = AccessTools.Field(typeof(GeneUtility), "cachedGeneDefsInOrder");
    private static readonly FieldInfo TakenHashesPerDefTypeField = AccessTools.Field(typeof(ShortHashGiver), "takenHashesPerDeftype");
    private static readonly MethodInfo GiveShortHashMethod = AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash");

    private static List<GeneDef> cachedDisableableGenes;

    private static List<GeneDef> DisableableGenes => cachedDisableableGenes ??= DefDatabase<GeneDef>.AllDefsListForReading
        .Where(geneDef => geneDef.GetModExtension<DefModExtension_GeneDisabledBy>()?.geneDisabledBy != null)
        .ToList();

    private static string DefNamePrefix(CustomChapterGeneTemplateDef template)
    {
        return template?.kind switch
        {
            CustomGeneKind.Primarch => "BEWH_CustomPrimarch_",
            CustomGeneKind.Custodes => "BEWH_CustomCustodes_",
            _ => "BEWH_CustomChapter_"
        };
    }

    public static string NewDefName(CustomChapterGeneTemplateDef template = null)
    {
        return DefNamePrefix(template) + Guid.NewGuid().ToString("N");
    }

    public static bool IsGeneratedChapterGene(GeneDef geneDef)
    {
        return geneDef != null && geneDef.HasModExtension<DefModExtension_CustomChapterGenerated>();
    }

    public static bool IsGeneratedOfKind(GeneDef geneDef, CustomGeneKind kind)
    {
        return geneDef?.GetModExtension<DefModExtension_CustomChapterGenerated>()?.kind == kind;
    }

    /// <summary>
    /// Returns the registered GeneDef for the design, creating and registering it on first use and
    /// repopulating it from the design's current traits otherwise.
    /// </summary>
    public static GeneDef EnsureRegistered(CustomChapterGene chapter)
    {
        if (chapter.defName.NullOrEmpty())
        {
            chapter.defName = NewDefName(chapter.Template);
        }

        var geneDef = DefDatabase<GeneDef>.GetNamedSilentFail(chapter.defName);
        var isNew = geneDef == null;

        geneDef ??= new GeneDef
        {
            defName = chapter.defName,
            modContentPack = chapter.Template.modContentPack
        };

        Populate(geneDef, chapter);

        if (isNew)
        {
            DefGenerator.AddImpliedDef(geneDef, true);
            geneDef.ResolveReferences();
            GiveShortHashTo(geneDef);
        }
        else
        {
            geneDef.ClearCachedData();
            CachedDescriptionField?.SetValue(geneDef, null);
            CachedIconField?.SetValue(geneDef, null);
        }

        CachedGenesInOrderField?.SetValue(null, null);
        StaticCollectionsClass.hidden_genes.Add(geneDef);
        Genes40kUtils.InvalidatePrimarchRelationCache();

        return geneDef;
    }

    private static void GiveShortHashTo(GeneDef geneDef)
    {
        if (geneDef.shortHash != 0)
        {
            return;
        }

        if (GiveShortHashMethod == null || TakenHashesPerDefTypeField?.GetValue(null) is not Dictionary<Type, HashSet<ushort>> takenHashes)
        {
            return;
        }

        if (!takenHashes.TryGetValue(typeof(GeneDef), out var taken))
        {
            taken = [];
            takenHashes.Add(typeof(GeneDef), taken);
        }

        GiveShortHashMethod.Invoke(null, [geneDef, typeof(GeneDef), taken]);
        DefDatabase<GeneDef>.InitializeShortHashDictionary();
    }

    private static void Populate(GeneDef geneDef, CustomChapterGene chapter)
    {
        var template = chapter.Template;
        var kind = template.kind;
        var traits = chapter.EffectiveTraits;

        geneDef.label = template.geneLabel + ": " + chapter.name;
        geneDef.description = template.geneDescription;

        if (kind == CustomGeneKind.Custodes)
        {
            var levels = chapter.disciplines.Where(entry => entry?.def != null && entry.level > 0).Select(entry => entry.def.label + " " + entry.level).ToList();

            if (levels.Any())
            {
                geneDef.description += "\n\n" + "BEWH.MankindsFinest.CustomCustodes.DisciplinesLine".Translate(levels.ToCommaList().CapitalizeFirst());
            }
        }
        else if (traits.Any())
        {
            var key = kind == CustomGeneKind.Primarch ? "BEWH.MankindsFinest.CustomPrimarch.TraitsLine" : "BEWH.MankindsFinest.CustomChapter.TraitsLine";
            geneDef.description += "\n\n" + key.Translate(traits.Select(trait => trait.label).ToCommaList().CapitalizeFirst());
        }

        geneDef.iconPath = chapter.flagIconDef?.iconPath ?? template.iconPath;
        geneDef.skinColorOverride = traits.Select(trait => trait.skinColorOverride).FirstOrDefault(colour => colour.HasValue);
        geneDef.hairColorOverride = traits.Select(trait => trait.hairColorOverride).FirstOrDefault(colour => colour.HasValue);
        geneDef.geneClass = typeof(Gene_CustomChapter);
        geneDef.displayCategory = template.displayCategory;
        geneDef.displayOrderInCategory = template.displayOrderInCategory;
        geneDef.exclusionTags = MergeExclusionTags(template, traits);
        geneDef.biostatCpx = template.biostatCpx;
        geneDef.biostatMet = 0;
        geneDef.biostatArc = 0;
        geneDef.marketValueFactor = template.marketValueFactor;
        geneDef.canGenerateInGeneSet = false;
        geneDef.passOnDirectly = false;
        geneDef.selectionWeight = 0f;
        geneDef.minAgeActive = template.minAgeActive;

        geneDef.statOffsets = MergeOffsets(traits);
        geneDef.statFactors = MergeFactors(traits);
        geneDef.capMods = Concat(traits, trait => trait.capMods);
        geneDef.forcedTraits = Concat(traits, trait => trait.forcedTraits);
        geneDef.aptitudes = Concat(traits, trait => trait.aptitudes);
        geneDef.conditionalStatAffecters = Concat(traits, trait => trait.conditionalStatAffecters);
        geneDef.abilities = Concat(traits, trait => trait.abilities)?.Distinct().ToList();
        geneDef.renderNodeProperties = Concat(traits, trait => trait.renderNodeProperties);
        geneDef.makeImmuneTo = Concat(traits, trait => trait.makeImmuneTo)?.Distinct().ToList();
        geneDef.hediffGiversCannotGive = Concat(traits, trait => trait.hediffGiversCannotGive)?.Distinct().ToList();
        geneDef.preventPermanentWounds = traits.Any(trait => trait.preventPermanentWounds);
        geneDef.disabledWorkTags = traits.Aggregate(WorkTags.None, (tags, trait) => tags | trait.disabledWorkTags);
        geneDef.painOffset = traits.Sum(trait => trait.painOffset);
        geneDef.painFactor = traits.Aggregate(1f, (factor, trait) => factor * trait.painFactor);
        geneDef.socialFightChanceFactor = traits.Aggregate(1f, (factor, trait) => factor * trait.socialFightChanceFactor);
        geneDef.aggroMentalBreakSelectionChanceFactor = traits.Aggregate(1f, (factor, trait) => factor * trait.aggroMentalBreakSelectionChanceFactor);

        var effectLines = new List<string>();

        foreach (var trait in traits.Where(trait => !trait.customEffectDescriptions.NullOrEmpty()))
        {
            effectLines.AddRange(trait.customEffectDescriptions);
        }

        effectLines = effectLines.Distinct().ToList();

        switch (kind)
        {
            case CustomGeneKind.Chapter:
                effectLines.Add("BEWH.MankindsFinest.CustomChapter.StabilityEffectLine".Translate(chapter.Stability.ToStringWithSign(), chapter.FailChanceOffset.ToStringWithSign()));
                break;
            case CustomGeneKind.Primarch:
                effectLines.Add("BEWH.MankindsFinest.CustomPrimarch.ComplexityEffectLine".Translate(chapter.Budget.ToStringWithSign(), (chapter.GestationTicksOffset / (float)GenDate.TicksPerDay).ToStringWithSign("0.#")));
                break;
        }

        geneDef.customEffectDescriptions = effectLines.Any() ? effectLines : null;
        geneDef.descriptionHyperlinks = null;

        var vefExtension = new GeneExtension { hideGene = true, disableGeneExtraction = true };

        foreach (var trait in traits)
        {
            if (!trait.hediffsToBodyParts.NullOrEmpty())
            {
                vefExtension.hediffsToBodyParts ??= [];
                vefExtension.hediffsToBodyParts.AddRange(trait.hediffsToBodyParts);
            }

            vefExtension.hediffToWholeBody ??= trait.hediffToWholeBody;
        }

        var twinLinked = traits.Any(trait => trait.twinLinked);

        var generated = new DefModExtension_CustomChapterGenerated
        {
            chapterDefName = chapter.defName,
            kind = kind,
            traits = traits,
            geneGrants = traits.Select(trait => trait.geneGrant).Where(grant => grant != null).ToList(),
            traitGrants = traits.Select(trait => trait.traitGrant).Where(grant => grant != null).ToList(),
            vefAbilityGrants = traits.Select(trait => trait.vefAbilityGrant).Where(grant => grant != null).ToList(),
            workerTraits = traits.Where(trait => trait.worker != null).ToList(),
            twinLinked = twinLinked
        };

        geneDef.modExtensions = [generated, vefExtension];

        switch (kind)
        {
            case CustomGeneKind.Chapter:
                geneDef.modExtensions.Add(new DefModExtension_ChapterGene { chapterName = chapter.name, relatedPrimarchGene = chapter.RelatedPrimarchGene });
                geneDef.modExtensions.Add(new DefModExtension_GeneseedPurity
                {
                    additionalChanceOffset = chapter.FailChanceOffset,
                    additionalChanceCapOffset = chapter.FailChanceCapOffset,
                    rubiconAdditionalChanceOffset = chapter.RubiconOffset
                });
                break;
            case CustomGeneKind.Primarch:
                geneDef.modExtensions.Add(new DefModExtension_PrimarchMaterial { shownMaterialName = chapter.name });
                geneDef.modExtensions.Add(new DefModExtension_PrimarchGestation { gestationTicksOffset = chapter.GestationTicksOffset });
                break;
        }

        if (twinLinked)
        {
            geneDef.modExtensions.Add(new DefModExtension_PrimarchVatExtras { childAmount = 2 });
        }

        var inducedFear = MergeInducedFear(geneDef, traits);

        if (inducedFear != null)
        {
            geneDef.modExtensions.Add(inducedFear);
        }

        SyncDisabledGenes(geneDef, traits);
    }

    private static List<string> MergeExclusionTags(CustomChapterGeneTemplateDef template, List<ChapterTraitDef> traits)
    {
        var tags = new List<string>();

        if (!template.exclusionTags.NullOrEmpty())
        {
            tags.AddRange(template.exclusionTags);
        }

        foreach (var trait in traits.Where(trait => !trait.geneExclusionTags.NullOrEmpty()))
        {
            tags.AddRange(trait.geneExclusionTags);
        }

        tags = tags.Distinct().ToList();
        return tags.Any() ? tags : null;
    }

    /// <summary>
    /// One induced-fear extension from every trait that carries one, with the generated def itself immune so a
    /// design's own carriers do not panic each other.
    /// </summary>
    private static DefModExtension_GeneInducedFear MergeInducedFear(GeneDef geneDef, List<ChapterTraitDef> traits)
    {
        var sources = traits.Where(trait => trait.inducedFear != null).Select(trait => trait.inducedFear).ToList();

        if (!sources.Any())
        {
            return null;
        }

        var merged = new DefModExtension_GeneInducedFear
        {
            tickInterval = sources.Min(source => source.tickInterval),
            effectRadius = sources.Max(source => source.effectRadius),
            chanceToFear = sources.Max(source => source.chanceToFear),
            genesCausesImmunityToFear = sources.Where(source => !source.genesCausesImmunityToFear.NullOrEmpty())
                .SelectMany(source => source.genesCausesImmunityToFear).Where(gene => gene != null).Distinct().ToList(),
            traitCausesImmunityToFear = sources.Where(source => !source.traitCausesImmunityToFear.NullOrEmpty())
                .SelectMany(source => source.traitCausesImmunityToFear).Where(data => data?.traitDef != null).Distinct().ToList()
        };

        merged.genesCausesImmunityToFear.Add(geneDef);
        return merged;
    }

    /// <summary>
    /// Registers or unregisters the generated def in the geneDisabledBy list of every gene a trait switches off.
    /// Nothing is scribed; the list is read live by Core40k.Gene_DisabledBy, so only pawns carrying this gene
    /// lose the organ.
    /// </summary>
    private static void SyncDisabledGenes(GeneDef geneDef, List<ChapterTraitDef> traits)
    {
        var disabled = traits.Where(trait => !trait.disablesGenes.NullOrEmpty()).SelectMany(trait => trait.disablesGenes).Where(gene => gene != null).ToList();

        foreach (var candidate in DisableableGenes)
        {
            var disabledBy = candidate.GetModExtension<DefModExtension_GeneDisabledBy>()?.geneDisabledBy;

            if (disabledBy == null)
            {
                continue;
            }

            var wanted = disabled.Contains(candidate);
            var present = disabledBy.Contains(geneDef);

            if (wanted && !present)
            {
                disabledBy.Add(geneDef);
            }
            else if (!wanted && present)
            {
                disabledBy.Remove(geneDef);
            }
        }
    }

    private static List<StatModifier> MergeOffsets(List<ChapterTraitDef> traits)
    {
        var totals = new Dictionary<StatDef, float>();

        foreach (var modifier in traits.Where(trait => trait.statOffsets != null).SelectMany(trait => trait.statOffsets))
        {
            totals[modifier.stat] = totals.TryGetValue(modifier.stat, out var current) ? current + modifier.value : modifier.value;
        }

        return totals.Any() ? totals.Select(pair => new StatModifier { stat = pair.Key, value = pair.Value }).ToList() : null;
    }

    private static List<StatModifier> MergeFactors(List<ChapterTraitDef> traits)
    {
        var totals = new Dictionary<StatDef, float>();

        foreach (var modifier in traits.Where(trait => trait.statFactors != null).SelectMany(trait => trait.statFactors))
        {
            totals[modifier.stat] = totals.TryGetValue(modifier.stat, out var current) ? current * modifier.value : modifier.value;
        }

        return totals.Any() ? totals.Select(pair => new StatModifier { stat = pair.Key, value = pair.Value }).ToList() : null;
    }

    private static List<T> Concat<T>(List<ChapterTraitDef> traits, Func<ChapterTraitDef, List<T>> selector)
    {
        var result = traits.Select(selector).Where(list => list != null).SelectMany(list => list).ToList();
        return result.Any() ? result : null;
    }
}
