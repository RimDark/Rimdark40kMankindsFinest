using System.Collections.Generic;
using System.Linq;
using Core40k;
using RimWorld;
using UnityEngine;
using VEF.Genes;
using Verse;

namespace Genes40k;

/// <summary>
/// A trait option for player-designed chapter gene-seeds. Not a gene: the selected traits are merged into one runtime
/// GeneDef per chapter by CustomChapterGeneDefBuilder, and their grants are executed by Gene_CustomChapter.
/// </summary>
public class ChapterTraitDef : Def
{
    [NoTranslate]
    public string iconPath;
    public float displayOrder;
    public List<string> customEffectDescriptions;
    public CustomGeneKind kind = CustomGeneKind.Chapter;

    public int stabilityOffset;
    public List<ThingDef> requiredAnyMaterial;
    public ResearchProjectDef requiredResearch;
    public List<string> exclusionTags;

    public List<StatModifier> statOffsets;
    public List<StatModifier> statFactors;
    public List<PawnCapacityModifier> capMods;
    public List<GeneticTraitData> forcedTraits;
    public List<Aptitude> aptitudes;
    public List<ConditionalStatAffecter> conditionalStatAffecters;
    public List<AbilityDef> abilities;
    public WorkTags disabledWorkTags;
    public float painOffset;
    public float painFactor = 1f;
    public float socialFightChanceFactor = 1f;
    public float aggroMentalBreakSelectionChanceFactor = 1f;

    public DefModExtension_AddRandomGeneByWeight geneGrant;
    public DefModExtension_AddRandomTraitByWeight traitGrant;
    public DefModExtension_GivesVEFAbility vefAbilityGrant;
    public List<PawnRenderNodeProperties> renderNodeProperties;
    public List<string> geneExclusionTags;
    public List<HediffDef> makeImmuneTo;
    public List<HediffDef> hediffGiversCannotGive;
    public bool preventPermanentWounds;
    public bool twinLinked;

    public List<GeneDef> disablesGenes;
    public DefModExtension_GeneInducedFear inducedFear;

    public Color? skinColorOverride;
    public Color? hairColorOverride;

    public List<HediffToBodyparts> hediffsToBodyParts;
    public HediffDef hediffToWholeBody;

    public ChapterTraitWorker worker;

    [Unsaved(false)]
    private Texture2D icon;

    [Unsaved(false)]
    private GeneDef transientGeneDef;

    public Texture2D Icon
    {
        get
        {
            if (icon != null)
            {
                return icon;
            }

            icon = iconPath.NullOrEmpty() ? BaseContent.BadTex : ContentFinder<Texture2D>.Get(iconPath) ?? BaseContent.BadTex;
            return icon;
        }
    }

    public bool AppliesTo(CustomGeneKind targetKind)
    {
        return kind == CustomGeneKind.Any || kind == targetKind;
    }

    public bool ConflictsWith(ChapterTraitDef other)
    {
        if (other == null)
        {
            return false;
        }

        if (other == this)
        {
            return true;
        }

        return exclusionTags != null && other.exclusionTags != null && exclusionTags.Any(tag => other.exclusionTags.Contains(tag));
    }

    /// <summary>
    /// An unregistered GeneDef carrying this trait's effects, used for merging and for vanilla's gene card/tooltip drawing.
    /// It is never added to the DefDatabase, so other mods enumerating genes do not see it.
    /// </summary>
    public GeneDef AsGeneDef()
    {
        if (transientGeneDef != null)
        {
            return transientGeneDef;
        }

        var template = Genes40kDefOf.BEWH_CustomChapterGene;

        transientGeneDef = new GeneDef
        {
            defName = defName + "_Transient",
            label = label,
            description = description,
            iconPath = iconPath,
            displayCategory = template?.displayCategory,
            displayOrderInCategory = displayOrder,
            biostatCpx = 0,
            biostatMet = 0,
            minAgeActive = 0f,
            canGenerateInGeneSet = false,
            passOnDirectly = false,
            selectionWeight = 0f,
            customEffectDescriptions = customEffectDescriptions,
            exclusionTags = exclusionTags,
            statOffsets = statOffsets,
            statFactors = statFactors,
            capMods = capMods,
            forcedTraits = forcedTraits,
            aptitudes = aptitudes,
            conditionalStatAffecters = conditionalStatAffecters,
            abilities = abilities,
            disabledWorkTags = disabledWorkTags,
            painOffset = painOffset,
            painFactor = painFactor,
            socialFightChanceFactor = socialFightChanceFactor,
            aggroMentalBreakSelectionChanceFactor = aggroMentalBreakSelectionChanceFactor,
            makeImmuneTo = makeImmuneTo,
            hediffGiversCannotGive = hediffGiversCannotGive,
            preventPermanentWounds = preventPermanentWounds,
            modContentPack = modContentPack
        };

        return transientGeneDef;
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        if (worker != null)
        {
            worker.def = this;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }

        if (iconPath.NullOrEmpty())
        {
            yield return "no iconPath";
        }

        if (vefAbilityGrant != null && vefAbilityGrant.abilityDefs.NullOrEmpty())
        {
            yield return "vefAbilityGrant lists no abilityDefs";
        }

        if (worker != null)
        {
            foreach (var error in worker.ConfigErrors())
            {
                yield return error;
            }
        }

        if (disablesGenes.NullOrEmpty())
        {
            yield break;
        }

        foreach (var geneDef in disablesGenes.Where(geneDef => geneDef != null && (geneDef.geneClass == null || !typeof(Gene_DisabledBy).IsAssignableFrom(geneDef.geneClass))))
        {
            yield return "disablesGenes lists " + geneDef.defName + ", whose geneClass is not Core40k.Gene_DisabledBy, so it will not be disabled";
        }
    }
}
