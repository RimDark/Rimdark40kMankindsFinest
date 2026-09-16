using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Genes40k;

[StaticConstructorOnStartup]
public class GeneseedVial : ThingWithComps
{
    protected GeneSet geneSet;

    public string xenotypeName;

    public XenotypeDef xenotype;

    public XenotypeIconDef iconDef;

    public GeneDef extraGeneFromMaterial = null;

    public CustomChapterGene CustomChapter => GameComponent_CustomChapterGenes.Instance?.GetByGeneDef(extraGeneFromMaterial);

    public string newGeneseedVialTexture = null;

    [Unsaved]
    private Graphic cachednewGeneseedVialTexture;
    private Graphic NewGeneseedVialTexture => cachednewGeneseedVialTexture ??= GraphicDatabase.Get<Graphic_Single>(newGeneseedVialTexture, def.graphicData.shaderType.Shader, def.graphicData.drawSize, Color.white);

    public override Graphic Graphic => newGeneseedVialTexture != null ? NewGeneseedVialTexture : DefaultGraphic;

    public override void Print(SectionLayer layer)
    {
        if (invisible)
        {
            return;
        }

        base.Print(layer);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (invisible)
        {
            return;
        }

        base.DrawAt(drawLoc, flip);
    }
    
    private bool invisible = false;
        
    private static readonly CachedTexture GeneticInfoTex = new("UI/Gizmos/ViewGenes");

    private const int MaxGeneLabels = 5;

    private List<string> tmpGeneLabelsDesc = new();

    private List<string> tmpGeneLabels = new();

    public GeneSet GeneSet => geneSet;

    public override string DescriptionDetailed
    {
        get
        {
            tmpGeneLabelsDesc.Clear();
            var text = base.DescriptionDetailed;
            if (geneSet == null || !geneSet.GenesListForReading.Any())
            {
                return text;
            }
            if (!text.NullOrEmpty())
            {
                text += "\n\n";
            }
            foreach (var t in geneSet.GenesListForReading)
            {
                tmpGeneLabelsDesc.Add(t.label);
            }
            return text + ("Genes".Translate().CapitalizeFirst() + ":\n" + tmpGeneLabelsDesc.ToLineList("  - ", capitalizeItems: true));
        }
    }

    [Unsaved]
    private string cachedLabelNoCount;
    [Unsaved]
    private string cachedLabelXenotypeName;
    [Unsaved]
    private LoadedLanguage cachedLabelLanguage;

    public override string LabelNoCount
    {
        get
        {
            if (xenotypeName.NullOrEmpty())
            {
                return base.LabelNoCount;
            }

            if (cachedLabelNoCount == null || cachedLabelXenotypeName != xenotypeName || cachedLabelLanguage != LanguageDatabase.activeLanguage)
            {
                cachedLabelXenotypeName = xenotypeName;
                cachedLabelLanguage = LanguageDatabase.activeLanguage;
                cachedLabelNoCount = "BEWH.MankindsFinest.GeneseedVial.NamedGeneseedVial".Translate(xenotypeName.Named("NAME"));
            }

            return cachedLabelNoCount;
        }
    }

    public override void PostMake()
    {
        base.PostMake();
        geneSet = new GeneSet();
        Initialize();
    }

    public void ChangeVisibility(bool newValue)
    {
        invisible = newValue;
    }

    public void Initialize()
    {
        var defMod = def.GetModExtension<DefModExtension_GeneseedVial>();

        if (!defMod.xenotype.genes.NullOrEmpty())
        {
            foreach (var gene in defMod.xenotype.genes)
            {
                geneSet.AddGene(gene);
            }
        }

        xenotype = defMod.xenotype; 
        xenotypeName = defMod.xenotype.label;
        iconDef = defMod.xenotypeIcon;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }
        if (geneSet != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "InspectGenes".Translate() + "...",
                defaultDesc = "InspectGenesEmbryoDesc".Translate(),
                icon = GeneticInfoTex.Texture,
                action = delegate
                {
                    Genes40kUtils.InspectGeneseedVialGenes(this);
                }
            };
        }
    }

    public override string GetInspectString()
    {
        var text = base.GetInspectString();
        tmpGeneLabels.Clear();

        var chapterLabel = CustomChapterGeneUtility.ChapterLabelOf(this);
        if (!chapterLabel.NullOrEmpty())
        {
            if (!text.NullOrEmpty())
            {
                text += "\n";
            }
            text += "BEWH.MankindsFinest.GeneseedVial.ChapterMaterial".Translate(chapterLabel.CapitalizeFirst());
        }

        if (geneSet == null || !geneSet.GenesListForReading.Any()) return text;
            
        if (!text.NullOrEmpty())
        {
            text += "\n";
        }
        var genesListForReading = geneSet.GenesListForReading;
        var num = Mathf.Min(MaxGeneLabels, genesListForReading.Count);
            
        for (var i = 0; i < num; i++)
        {
            var text2 = genesListForReading[i].label;
            if (geneSet.IsOverridden(genesListForReading[i]))
            {
                text2 += " (" + "Overridden".Translate() + ")";
            }
            tmpGeneLabels.Add(text2);
        }
        if (genesListForReading.Count > num)
        {
            tmpGeneLabels.Add("Etc".Translate() + "...");
        }
        text += "Genes".Translate().CapitalizeFirst() + ":\n" + tmpGeneLabels.ToLineList("  - ", capitalizeItems: true);
        return text;
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (var item in base.SpecialDisplayStats())
        {
            yield return item;
        }
        if (geneSet == null)
        {
            yield break;
        }
        Dialog_InfoCard.Hyperlink? inspectGenesHyperlink = null;
        if (ThingSelectionUtility.SelectableByMapClick(this))
        {
            inspectGenesHyperlink = new Dialog_InfoCard.Hyperlink(this, -1, thingIsGeneOwner: true);
        }
        foreach (var item2 in geneSet.SpecialDisplayStats(inspectGenesHyperlink))
        {
            yield return item2;
        }
    }

    /// <summary>
    /// Only vials carrying the same chapter gene and texture may merge; stack mods raise stackLimit above 1.
    /// </summary>
    public override bool CanStackWith(Thing other)
    {
        if (!base.CanStackWith(other) || other is not GeneseedVial vial)
        {
            return false;
        }

        return extraGeneFromMaterial == vial.extraGeneFromMaterial
               && newGeneseedVialTexture == vial.newGeneseedVialTexture;
    }

    public override Thing SplitOff(int count)
    {
        var piece = base.SplitOff(count);
        if (piece != this && piece is GeneseedVial vial)
        {
            vial.extraGeneFromMaterial = extraGeneFromMaterial;
            vial.newGeneseedVialTexture = newGeneseedVialTexture;
        }

        return piece;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref xenotypeName, "xenotypeName");
        Scribe_Defs.Look(ref xenotype, "xenotype");
        Scribe_Defs.Look(ref iconDef, "iconDef");
        Scribe_Defs.Look(ref extraGeneFromMaterial, "extraGeneFromMaterial");
        Scribe_Values.Look(ref newGeneseedVialTexture, "newGeneseedVialTexture");
        Scribe_Deep.Look(ref geneSet, "geneSet");
        Scribe_Values.Look(ref invisible, "invisible");
            
        if (iconDef == null && Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            iconDef = XenotypeIconDefOf.Basic;
        }
    }
}