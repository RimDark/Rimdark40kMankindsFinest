using System.Collections.Generic;
using System.Linq;
using Core40k;
using RimWorld;
using UnityEngine;
using VEF.Genes;
using Verse;

namespace Genes40k;

/// <summary>
/// One slider of the Custodes customization window. Per-level modifiers scale linearly with the chosen level; tiers add
/// the effects that should not scale, once the level reaches their minimum. A discipline at a given level is converted to a
/// transient ChapterTraitDef so the merged gene goes through the same builder as chapter and primarch designs.
/// </summary>
public class CustodesDisciplineDef : Def
{
    [NoTranslate]
    public string iconPath;
    public float displayOrder;
    public int maxLevel = 10;
    public int costPerLevel = 1;

    public List<StatModifier> statOffsetsPerLevel;
    public List<StatModifier> statFactorsPerLevel;
    public List<PawnCapacityModifier> capModsPerLevel;
    public List<CustodesDisciplineTier> tiers;

    [Unsaved(false)]
    private Texture2D icon;

    [Unsaved(false)]
    private ChapterTraitDef[] traitDefsByLevel;

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

    public IEnumerable<CustodesDisciplineTier> TiersAt(int level)
    {
        return tiers.NullOrEmpty() ? Enumerable.Empty<CustodesDisciplineTier>() : tiers.Where(tier => tier != null && tier.minLevel > 0 && tier.minLevel <= level);
    }

    public CustodesDisciplineTier NextTierAfter(int level)
    {
        return tiers.NullOrEmpty() ? null : tiers.Where(tier => tier != null && tier.minLevel > level).OrderBy(tier => tier.minLevel).FirstOrDefault();
    }

    /// <summary>
    /// An unregistered ChapterTraitDef carrying this discipline's effects at the given level, cached per level.
    /// </summary>
    public ChapterTraitDef ToTraitDef(int level)
    {
        level = Mathf.Clamp(level, 0, maxLevel);
        traitDefsByLevel ??= new ChapterTraitDef[maxLevel + 1];

        if (traitDefsByLevel[level] != null)
        {
            return traitDefsByLevel[level];
        }

        var trait = new ChapterTraitDef
        {
            defName = defName + "_L" + level,
            label = label + " " + level,
            description = description,
            iconPath = iconPath,
            displayOrder = displayOrder,
            kind = CustomGeneKind.Custodes,
            modContentPack = modContentPack,
            statOffsets = ScaleOffsets(statOffsetsPerLevel, level),
            statFactors = ScaleFactors(statFactorsPerLevel, level),
            capMods = ScaleCapMods(capModsPerLevel, level)
        };

        foreach (var tier in TiersAt(level))
        {
            Append(ref trait.abilities, tier.abilities);
            Append(ref trait.forcedTraits, tier.forcedTraits);
            Append(ref trait.aptitudes, tier.aptitudes);
            Append(ref trait.hediffsToBodyParts, tier.hediffsToBodyParts);
            Append(ref trait.makeImmuneTo, tier.makeImmuneTo);
            Append(ref trait.hediffGiversCannotGive, tier.hediffGiversCannotGive);
            Append(ref trait.customEffectDescriptions, tier.customEffectDescriptions);
            trait.preventPermanentWounds |= tier.preventPermanentWounds;
            trait.geneGrant ??= tier.geneGrant;
            trait.vefAbilityGrant ??= tier.vefAbilityGrant;
            trait.hediffToWholeBody ??= tier.hediffToWholeBody;
        }

        traitDefsByLevel[level] = trait;
        return trait;
    }

    private static void Append<T>(ref List<T> target, List<T> source)
    {
        if (source.NullOrEmpty())
        {
            return;
        }

        target ??= [];
        target.AddRange(source);
    }

    private static List<StatModifier> ScaleOffsets(List<StatModifier> perLevel, int level)
    {
        if (perLevel.NullOrEmpty() || level <= 0)
        {
            return null;
        }

        return perLevel.Select(modifier => new StatModifier { stat = modifier.stat, value = modifier.value * level }).ToList();
    }

    private static List<StatModifier> ScaleFactors(List<StatModifier> perLevel, int level)
    {
        if (perLevel.NullOrEmpty() || level <= 0)
        {
            return null;
        }

        return perLevel.Select(modifier => new StatModifier { stat = modifier.stat, value = 1f + modifier.value * level }).ToList();
    }

    private static List<PawnCapacityModifier> ScaleCapMods(List<PawnCapacityModifier> perLevel, int level)
    {
        if (perLevel.NullOrEmpty() || level <= 0)
        {
            return null;
        }

        return perLevel.Select(modifier => new PawnCapacityModifier
        {
            capacity = modifier.capacity,
            offset = modifier.offset * level,
            postFactor = 1f + (modifier.postFactor - 1f) * level,
            setMax = modifier.setMax
        }).ToList();
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

        if (maxLevel < 1)
        {
            yield return "maxLevel must be at least 1";
        }
    }
}

public class CustodesDisciplineTier
{
    public int minLevel = 1;
    public List<AbilityDef> abilities;
    public List<GeneticTraitData> forcedTraits;
    public List<Aptitude> aptitudes;
    public List<HediffToBodyparts> hediffsToBodyParts;
    public HediffDef hediffToWholeBody;
    public List<HediffDef> makeImmuneTo;
    public List<HediffDef> hediffGiversCannotGive;
    public bool preventPermanentWounds;
    public List<string> customEffectDescriptions;
    public DefModExtension_AddRandomGeneByWeight geneGrant;
    public DefModExtension_GivesVEFAbility vefAbilityGrant;
}
