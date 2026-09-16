using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Genes40k;

/// <summary>
/// A player-designed gene-seed of one line (chapter, primarch or Custodes): a name, a flag icon and either a set of
/// ChapterTraitDefs or a set of Custodes discipline levels, backed by a GeneDef generated at runtime
/// (see CustomChapterGeneDefBuilder). Stored in GameComponent_CustomChapterGenes.
/// </summary>
public class CustomChapterGene : IExposable
{
    public int id = -1;
    public string defName;
    public string name = string.Empty;
    public CustomChapterGeneTemplateDef template;
    public FlagIconDef flagIconDef;
    public List<ChapterTraitDef> traits = [];
    public List<CustodesDisciplineLevel> disciplines = [];
    public bool locked = false;
    public bool adHoc = false;
    private string relatedPrimarchGeneDefName;

    [Unsaved]
    private GeneDef geneDef;

    public GeneDef GeneDef => geneDef ??= CustomChapterGeneDefBuilder.EnsureRegistered(this);

    public CustomChapterGeneTemplateDef Template => template ??= Genes40kDefOf.BEWH_CustomChapterGene;

    public CustomGeneKind Kind => Template.kind;

    /// <summary>
    /// The primarch gene a chapter design is tied to, resolved by name so a generated primarch design registered later in
    /// the same load still resolves.
    /// </summary>
    public GeneDef RelatedPrimarchGene
    {
        get => relatedPrimarchGeneDefName.NullOrEmpty() ? null : DefDatabase<GeneDef>.GetNamedSilentFail(relatedPrimarchGeneDefName);
        set => relatedPrimarchGeneDefName = value?.defName;
    }

    public int Budget => Kind == CustomGeneKind.Custodes
        ? CustomChapterGeneUtility.PointsSpent(disciplines, Template)
        : CustomChapterGeneUtility.StabilityOf(traits);

    public int Stability => Budget;

    public int FailChanceOffset => Kind == CustomGeneKind.Chapter ? -Stability * Template.failChancePerStabilityPoint : 0;

    public int FailChanceCapOffset => Kind == CustomGeneKind.Chapter ? -Stability * Template.failChanceCapPerStabilityPoint : 0;

    public float RubiconOffset => Kind == CustomGeneKind.Chapter ? Stability * Template.rubiconPerStabilityPoint : 0f;

    public int GestationTicksOffset => Kind == CustomGeneKind.Primarch ? -Budget * Template.gestationTicksPerPoint : 0;

    /// <summary>
    /// The traits the generated gene merges: the chosen trait defs, or for a Custodes design the transient trait of each
    /// discipline at its level.
    /// </summary>
    public List<ChapterTraitDef> EffectiveTraits => Kind == CustomGeneKind.Custodes
        ? disciplines.Where(entry => entry?.def != null && entry.level > 0).Select(entry => entry.def.ToTraitDef(entry.level)).ToList()
        : traits.Where(trait => trait != null).ToList();

    public int LevelOf(CustodesDisciplineDef discipline)
    {
        return disciplines.FirstOrDefault(entry => entry?.def == discipline)?.level ?? 0;
    }

    public bool SameDisciplinesAs(List<CustodesDisciplineLevel> other)
    {
        var mine = disciplines.Where(entry => entry?.def != null && entry.level > 0).ToDictionary(entry => entry.def, entry => entry.level);
        var theirs = (other ?? new List<CustodesDisciplineLevel>()).Where(entry => entry?.def != null && entry.level > 0).ToDictionary(entry => entry.def, entry => entry.level);
        return mine.Count == theirs.Count && mine.All(pair => theirs.TryGetValue(pair.Key, out var level) && level == pair.Value);
    }

    /// <summary>
    /// Rebuilds the generated GeneDef after the design's name, icon, traits or disciplines changed.
    /// </summary>
    public void RefreshGeneDef()
    {
        geneDef = CustomChapterGeneDefBuilder.EnsureRegistered(this);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref id, "id", -1);
        Scribe_Values.Look(ref defName, "defName");
        Scribe_Values.Look(ref name, "name");
        Scribe_Defs.Look(ref template, "template");
        Scribe_Defs.Look(ref flagIconDef, "flagIconDef");
        Scribe_Collections.Look(ref traits, "traits", LookMode.Def);
        Scribe_Collections.Look(ref disciplines, "disciplines", LookMode.Deep);
        Scribe_Values.Look(ref locked, "locked", false);
        Scribe_Values.Look(ref adHoc, "adHoc", false);
        Scribe_Values.Look(ref relatedPrimarchGeneDefName, "relatedPrimarchGene");

        if (Scribe.mode != LoadSaveMode.LoadingVars)
        {
            return;
        }

        template ??= Genes40kDefOf.BEWH_CustomChapterGene;
        traits ??= [];
        traits.RemoveAll(trait => trait == null);
        disciplines ??= [];
        disciplines.RemoveAll(entry => entry?.def == null);
        name ??= string.Empty;
        flagIconDef ??= Genes40kDefOf.BEWH_FlagAquila;
        geneDef = CustomChapterGeneDefBuilder.EnsureRegistered(this);
    }
}
