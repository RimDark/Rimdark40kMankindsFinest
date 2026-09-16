using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Core40k;
using UnityEngine;
using Verse;

namespace Genes40k;

[StaticConstructorOnStartup]
public static class Genes40kUtils
{
    private static List<ChapterColourDef> chapterColourDefs = null;

    public static List<ChapterColourDef> ChapterColourDefs => chapterColourDefs ??= DefDatabase<ChapterColourDef>.AllDefsListForReading.ToList();
    
    
    private static Genes40kModSettings modSettings = null;
    public static Genes40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Genes40kMod>().GetSettings<Genes40kModSettings>();

    private static List<ShoulderIconDef> leftShoulderIconDef = null;
    public static List<ShoulderIconDef> LeftShoulderIconDef => leftShoulderIconDef ??= DefDatabase<ShoulderIconDef>.AllDefsListForReading.Where(leftShoulderDef => leftShoulderDef.leftShoulder).ToList();

    private static List<ThingDef> geneMaterialDefs = null;
    public static List<ThingDef> GeneMaterialDefs => geneMaterialDefs ??= DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.thingClass != null && typeof(GeneMaterialExtra).IsAssignableFrom(def.thingClass)).ToList();
    
    public static readonly Texture2D MindShieldIcon = ContentFinder<Texture2D>.Get("UI/Abilities/BEWH_MindShield");
    public static readonly Texture2D PermaDeathPerpetualIcon = ContentFinder<Texture2D>.Get("UI/Abilities/BEWH_PerpetrualPermaDeath");
    
    public static readonly CachedTexture PaintableIcon = new ("UI/Misc/PaintableIcon");
        
    private static List<GeneDef> thunderWarriorGenes = null;
    public static List<GeneDef> ThunderWarriorGenes => thunderWarriorGenes ??= new List<GeneDef>
    {
        Genes40kDefOf.BEWH_ProtoOssmodula,
        Genes40kDefOf.BEWH_Musculeator,
        Genes40kDefOf.BEWH_Mentanifex,
        Genes40kDefOf.BEWH_Vigoranis,
        Genes40kDefOf.BEWH_Hyperanatomica,
        Genes40kDefOf.BEWH_Furybound,
    };

    private static List<GeneDef> spaceMarineGenes = null;
    public static List<GeneDef> SpaceMarineGenes => spaceMarineGenes ??= new List<GeneDef>
    {
        Genes40kDefOf.BEWH_SecondaryHeart,
        Genes40kDefOf.BEWH_Ossmodula,
        Genes40kDefOf.BEWH_Biscopea,
        Genes40kDefOf.BEWH_Haemastamen,
        Genes40kDefOf.BEWH_LarramansOrgan,
        Genes40kDefOf.BEWH_CatalepseanNode,
        Genes40kDefOf.BEWH_Preomnor,
        Genes40kDefOf.BEWH_Omophagea,
        Genes40kDefOf.BEWH_MultiLung,
        Genes40kDefOf.BEWH_Occulobe,
        Genes40kDefOf.BEWH_LymansEar,
        Genes40kDefOf.BEWH_SusAnMembrane,
        Genes40kDefOf.BEWH_Melanochrome,
        Genes40kDefOf.BEWH_OoliticKidney,
        Genes40kDefOf.BEWH_Neuroglottis,
        Genes40kDefOf.BEWH_Mucranoid,
        Genes40kDefOf.BEWH_BetchersGland,
        Genes40kDefOf.BEWH_ProgenoidGlands,
        Genes40kDefOf.BEWH_BlackCarapace
    };

    private static List<GeneDef> primarisGenes = null;
    public static List<GeneDef> PrimarisGenes => primarisGenes ??= new List<GeneDef>
    {
        Genes40kDefOf.BEWH_SinewCoil,
        Genes40kDefOf.BEWH_Magnificat,
        Genes40kDefOf.BEWH_BelisarianFurnace
    };

    private static List<GeneDef> custodesGenes = null;
    public static List<GeneDef> CustodesGenes => custodesGenes ??= new List<GeneDef>
    {
        Genes40kDefOf.BEWH_ImmunisLeucocyte,
        Genes40kDefOf.BEWH_AthanaticVitae,
        Genes40kDefOf.BEWH_FulguriteNervePlexus,
        Genes40kDefOf.BEWH_AtlasMorphogen,
        Genes40kDefOf.BEWH_MnemosyneMindshield,
        Genes40kDefOf.BEWH_FulgurVitaliumstrand
    };

    private static List<GeneDef> primarchGenes = null;
    public static List<GeneDef> PrimarchGenes => primarchGenes ??= new List<GeneDef>
    {
        Genes40kDefOf.BEWH_ImmortisGland,
        Genes40kDefOf.BEWH_TempestusOcularium,
        Genes40kDefOf.BEWH_ThalaxCortex,
        Genes40kDefOf.BEWH_HelixomeArray,
        Genes40kDefOf.BEWH_VermillionCache,
        Genes40kDefOf.BEWH_CelerityNexus,
        Genes40kDefOf.BEWH_HyperionMuscleStrands
    };

    private static List<GeneDef> psykerGenes = null;
    public static List<GeneDef> PsykerGenes
    {
        get
        {
            psykerGenes ??= DefDatabase<GeneDef>.AllDefs.Where(def => def.HasModExtension<DefModExtension_Psyker>()).ToList();
            return psykerGenes;
        }
    }
    
    private static List<GeneDef> perpetualGenes = null;
    public static List<GeneDef> PerpetualGenes
    {
        get
        {
            perpetualGenes ??= DefDatabase<GeneDef>.AllDefs.Where(def => def.HasModExtension<DefModExtension_PerpetualGene>()).ToList();
            return perpetualGenes;
        }
    }
        
    private static HashSet<HediffDef> pariahHediffDefs = null;
    public static HashSet<HediffDef> PariahHediffDefs => pariahHediffDefs ??= new HashSet<HediffDef>(DefDatabase<HediffDef>.AllDefsListForReading.Where(def => def.HasModExtension<DefModExtension_Pariah>()));

    private static List<GeneDef> pariahGenes = null;
    public static List<GeneDef> PariahGenes{
        get
        {
            pariahGenes ??= DefDatabase<GeneDef>.AllDefs.Where(def => def.HasModExtension<DefModExtension_Pariah>()).ToList();
            return pariahGenes;
        }
    }
        
    private static List<GeneDef> livingSaintGenes = null;
    public static List<GeneDef> LivingSaintGenes => livingSaintGenes ??= new List<GeneDef>
    {
        Genes40kDefOf.BEWH_LivingSaintBeingOfFaith,
        Genes40kDefOf.BEWH_LivingSaintDivineGrace,
        Genes40kDefOf.BEWH_LivingSaintDivineFlight,
        Genes40kDefOf.BEWH_LivingSaintSacredRegeneration,
        Genes40kDefOf.BEWH_LivingSaintFuryOfTheEmperor,
        Genes40kDefOf.BEWH_LivingSaintMartyrsEndurance,
        Genes40kDefOf.BEWH_LivingSaintHolyRadiance,
    };

    private static List<HediffDef> developmentPhases = null;
    public static List<HediffDef> DevelopmentPhases => developmentPhases ??= new List<HediffDef>
    {
        Genes40kDefOf.BEWH_FirstbornPhaseOne,
        Genes40kDefOf.BEWH_FirstbornPhaseTwo,
        Genes40kDefOf.BEWH_FirstbornPhaseThree,

        Genes40kDefOf.BEWH_PrimarisPhaseOne,
        Genes40kDefOf.BEWH_PrimarisPhaseTwo,
        Genes40kDefOf.BEWH_PrimarisPhaseThree,
    };

    private static HashSet<HediffDef> developmentPhaseSet = null;
    private static HashSet<HediffDef> DevelopmentPhaseSet => developmentPhaseSet ??= new HashSet<HediffDef>(DevelopmentPhases);

    private const int MaxCachedGeneSets = 32;

    private static readonly Dictionary<List<GeneDef>, HashSet<GeneDef>> geneSetCache = new();

    private static readonly HashSet<GeneDef> tmpMatchedGenes = new();

    /// <summary>
    /// The gene lists above are cached statics, so a set keyed on the list instance is built once per list.
    /// </summary>
    private static HashSet<GeneDef> SetFor(List<GeneDef> geneDefs)
    {
        if (!geneSetCache.TryGetValue(geneDefs, out var set))
        {
            if (geneSetCache.Count >= MaxCachedGeneSets)
            {
                geneSetCache.Clear();
            }

            set = new HashSet<GeneDef>(geneDefs);
            geneSetCache.Add(geneDefs, set);
        }

        return set;
    }
        
    private static Dictionary<GeneDef, GeneDef> chapterGeneToPrimarchGene = null;
    private static Dictionary<GeneDef, GeneDef> ChapterGeneToPrimarchGene
    {
        get
        {
            if (chapterGeneToPrimarchGene != null)
            {
                return chapterGeneToPrimarchGene;
            }

            chapterGeneToPrimarchGene = new Dictionary<GeneDef, GeneDef>();

            foreach (var geneDef in DefDatabase<GeneDef>.AllDefsListForReading)
            {
                var relatedPrimarchGene = geneDef.GetModExtension<DefModExtension_ChapterGene>()?.relatedPrimarchGene;

                if (relatedPrimarchGene != null)
                {
                    chapterGeneToPrimarchGene[geneDef] = relatedPrimarchGene;
                }
            }

            return chapterGeneToPrimarchGene;
        }
    }

    private static HashSet<GeneDef> relatedPrimarchGenes = null;
    public static HashSet<GeneDef> RelatedPrimarchGenes => relatedPrimarchGenes ??= new HashSet<GeneDef>(ChapterGeneToPrimarchGene.Values);

    /// <summary>
    /// Drops the chapter-to-primarch caches so a generated chapter gene registered after startup is picked up on next use.
    /// </summary>
    public static void InvalidatePrimarchRelationCache()
    {
        chapterGeneToPrimarchGene = null;
        relatedPrimarchGenes = null;
    }

    /// <summary>
    /// The pawn's active twin-capable gene (Alpharius/Omegon, shipped or custom), or null.
    /// </summary>
    public static ITwinGene TwinGene(this Pawn pawn)
    {
        if (pawn?.genes == null)
        {
            return null;
        }

        foreach (var gene in pawn.genes.GenesListForReading)
        {
            if (gene is ITwinGene { TwinCapable: true } twinGene && gene.Active)
            {
                return twinGene;
            }
        }

        return null;
    }

    /// <summary>
    /// The primarch gene tied to whichever chapter gene this pawn carries, or null if they carry none.
    /// </summary>
    public static GeneDef RelatedPrimarchGeneFor(this Pawn pawn)
    {
        if (pawn?.genes == null)
        {
            return null;
        }

        foreach (var gene in pawn.genes.GenesListForReading)
        {
            if (ChapterGeneToPrimarchGene.TryGetValue(gene.def, out var relatedPrimarchGene))
            {
                return relatedPrimarchGene;
            }
        }

        return null;
    }

    /// <summary>
    /// The progenoid gland readout for this pawn, or null when they have no active gland gene.
    /// Shared by the inspect-string patch and the RimHUD integration; the caller supplies its own
    /// separator, since only the inspect string needs one.
    /// </summary>
    public static string ProgenoidProgressLine(Pawn pawn)
    {
        if (pawn?.genes?.GetGene(Genes40kDefOf.BEWH_ProgenoidGlands) is not Gene_ProgenoidGlands { Active: true } progenoidGlands)
        {
            return null;
        }

        if (progenoidGlands.FirstProgenoidGlandHarvested)
        {
            return "BEWH.MankindsFinest.SpaceMarine.FirstGeneseedsHarvested".Translate();
        }

        var secondProgenoid = !progenoidGlands.SecondProgenoidGlandHarvested
            ? " " + (string)"BEWH.MankindsFinest.SpaceMarine.SecondGeneseedsHarvestableUponDeath".Translate()
            : string.Empty;

        float ticksLeft = progenoidGlands.TicksUntilHarvestable;

        return ticksLeft > 0
            ? "BEWH.MankindsFinest.SpaceMarine.FirstGeneseedsHarvestableIn".Translate((ticksLeft / 60000).ToString("0.00"), secondProgenoid)
            : "BEWH.MankindsFinest.SpaceMarine.FirstGeneseedsHarvestable".Translate();
    }

    public static bool HasGene(this Pawn_GeneTracker geneTracker, GeneDef geneDef)
    {
        if (geneDef == null)
        {
            return false;
        }
        var genesListForReading = geneTracker.GenesListForReading;
            
        foreach (var gene in genesListForReading)
        {
            if (gene.def == geneDef)
            {
                return true;
            }
        }
            
        return false;
    }
        
    public static bool HasGenes(this Pawn_GeneTracker geneTracker, List<GeneDef> geneDefs)
    {
        if (geneTracker == null || geneDefs.NullOrEmpty())
        {
            return false;
        }

        var required = SetFor(geneDefs);
        tmpMatchedGenes.Clear();

        foreach (var gene in geneTracker.GenesListForReading)
        {
            if (required.Contains(gene.def))
            {
                tmpMatchedGenes.Add(gene.def);
            }
        }

        var result = tmpMatchedGenes.Count == required.Count;
        tmpMatchedGenes.Clear();
        return result;
    }

    /// <summary>
    /// True if the pawn has an active gene whose def is in the list. One pass over the pawn's genes
    /// instead of one HasActiveGene scan per listed def.
    /// </summary>
    public static bool HasAnyActiveGeneOf(this Pawn_GeneTracker geneTracker, List<GeneDef> geneDefs)
    {
        if (geneTracker == null || geneDefs.NullOrEmpty())
        {
            return false;
        }

        var set = SetFor(geneDefs);

        foreach (var gene in geneTracker.GenesListForReading)
        {
            if (gene.Active && set.Contains(gene.def))
            {
                return true;
            }
        }

        return false;
    }
        
    public static bool IsThunderWarrior(this Pawn pawn)
    {
        return pawn.genes.HasGenes(ThunderWarriorGenes);
    }

    public static bool IsFirstborn(this Pawn pawn)
    {
        return pawn.genes.HasGenes(SpaceMarineGenes);
    }

    public static bool IsPrimaris(this Pawn pawn)
    {
        return pawn.genes.HasGenes(PrimarisGenes) && pawn.IsFirstborn();
    }

    public static bool IsCustodes(this Pawn pawn)
    {
        return pawn.genes.HasGenes(CustodesGenes);
    }

    public static bool IsPrimarch(this Pawn pawn)
    {
        return pawn.genes.HasGenes(PrimarchGenes);
    }
        
    public static bool IsSuperHuman(this Pawn pawn)
    {
        //Primaris is not checked, as if they are primaris, then they are by extension also firstborn
        return pawn.IsThunderWarrior() || pawn.IsFirstborn() || pawn.IsCustodes() || pawn.IsPrimarch();
    }
        
    public static bool IsPsyker(this Pawn pawn)
    {
        return pawn.genes.HasAnyActiveGeneOf(PsykerGenes);
    }

    public static bool IsPariah(this Pawn pawn)
    {
        return pawn.genes.HasAnyActiveGeneOf(PariahGenes);
    }

    public static bool IsLivingSaint(this Pawn pawn)
    {
        return pawn.genes.HasAnyActiveGeneOf(LivingSaintGenes);
    }

    public static bool UndergoingPhaseDevelopment(this Pawn pawn)
    {
        var hediffs = pawn.health?.hediffSet?.hediffs;

        if (hediffs == null)
        {
            return false;
        }

        var phases = DevelopmentPhaseSet;

        foreach (var hediff in hediffs)
        {
            if (phases.Contains(hediff.def))
            {
                return true;
            }
        }

        return false;
    }
        
    public static void OffsetDivineGrace(Pawn pawn, float offset)
    {
        var geneDivineGrace = pawn.genes?.GetFirstGeneOfType<Gene_DivineGrace>();
        geneDivineGrace?.ChangeDivineGraceAmount(offset);
    }
        
    public static ChapterColourDef SetupChapterForPawn(Pawn pawn, bool randomChapter)
    {
        if (pawn.genes == null || !pawn.IsFirstborn())
        {
            return null;
        }

        if (Enumerable.Any(pawn.genes.GenesListForReading, gene => gene.def.HasModExtension<DefModExtension_ChapterGene>()))
        {
            return null;
        }
        var xenotypeName = string.Empty;

        ChapterColourDef chapter;

        if (randomChapter)
        {
            var defMod = pawn.kindDef.GetModExtension<DefModExtension_SpawnAsChapter>();
        
            if (defMod != null)
            {
                if (defMod.specificChapters != null)
                {
                    chapter = defMod.specificChapters.RandomElement();
                }
                else
                {
                    var chapList = ChapterColourDefs.Where(chapterCol => chapterCol.loyalist == defMod.loyalist).ToList();
                    if (!chapList.NullOrEmpty())
                    {
                        chapter = chapList.RandomElement();
                    }
                    else
                    {
                        chapter = Current.Game.GetComponent<GameComponent_MankindFinestUtils>().CurrentChapterColour;
                    }
                }
            }
            else
            {
                chapter = Current.Game.GetComponent<GameComponent_MankindFinestUtils>().CurrentChapterColour;
            }
        }
        else
        {
            chapter = ModSettings.CurrentlySelectedPreset;
        }

        if (chapter == null)
        {
            return null;
        }
        
        var chapterColourPrimary = chapter.primaryColour;
        var chapterColourSecondary = chapter.secondaryColour;
        var chapterColourTertiary = chapter.tertiaryColour ?? chapter.secondaryColour;
        var shoulderIconDef = chapter.relatedChapterIcon;
        GeneDef chapterGene = null;

        if (chapter.relatedChapterGene != null)
        {
            chapterGene = chapter.relatedChapterGene;
        }
        else if (shoulderIconDef != null)
        {
            chapterGene = shoulderIconDef.relatedChapterGene;
        }
        if (chapterGene != null)
        {
            if (chapterGene.HasModExtension<DefModExtension_ChapterGene>())
            {
                xenotypeName = chapterGene.GetModExtension<DefModExtension_ChapterGene>().chapterName;
            }
            if (!pawn.genes.HasActiveGene(chapterGene))
            {
                pawn.genes.AddGene(chapterGene, true);
                if (xenotypeName != string.Empty)
                {
                    pawn.genes.xenotypeName = xenotypeName;
                    pawn.genes.iconDef = Genes40kDefOf.BEWH_AstartesIcon;
                }
            }
        }

        if (pawn.apparel?.WornApparel != null)
        {
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.GetComp<CompChapterColor>();
                if (comp == null)
                {
                    continue;
                }
            
                comp.SetColors(chapterColourPrimary, chapterColourSecondary, chapterColourTertiary);
                comp.SetOriginals();

                if (comp is CompChapterColorWithShoulderDecoration compExtended)
                {
                    compExtended.SetChapterIcon(shoulderIconDef);
                    compExtended.DecorativeComp?.SetOriginalDecorations();
                }
            }
        }
        
        var equipment = pawn.equipment?.PrimaryEq?.parent;
        if (equipment != null && equipment.HasComp<CompMultiColor>())
        {
            equipment.GetComp<CompMultiColor>().SetColors(chapter);
            equipment.GetComp<CompMultiColor>().SetOriginals();
        }
        return chapter;
    }

    public static void MakeGeneseedVial(Pawn pawn, bool isPrimaris)
    {
        GeneseedVial geneseedVial;
            
        if (isPrimaris)
        {
            geneseedVial = (GeneseedVial)ThingMaker.MakeThing(Genes40kDefOf.BEWH_GeneseedVialPrimaris);
        }
        else
        {
            geneseedVial = (GeneseedVial)ThingMaker.MakeThing(Genes40kDefOf.BEWH_GeneseedVialFirstborn);
        }

        if (pawn.genes?.GenesListForReading != null)
        {
            var gene = pawn.genes.GenesListForReading.FirstOrFallback(gene => gene.Active && gene.def.HasModExtension<DefModExtension_ChapterGene>(), null);
            if (gene != null)
            {
                geneseedVial.extraGeneFromMaterial = gene.def;
                if (CustomChapterGeneDefBuilder.IsGeneratedChapterGene(gene.def))
                {
                    geneseedVial.newGeneseedVialTexture = CustomChapterGeneUtility.Tuning.vialTexturePath;
                }
            }
        }

        if (GenPlace.TryPlaceThing(geneseedVial, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near))
        {
            return;
        }
        Log.Error("Could not drop item near " + pawn.PositionHeld);
    }

    public static void InspectPrimarchEmbryoGenes(PrimarchEmbryo embryo)
    {
        if (embryo == null)
        {
            return;
        }
            
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist);
        pawn.ageTracker.AgeBiologicalTicks = 3600000 * 25;
            
        foreach (var gene in pawn.genes.GenesListForReading)
        {
            pawn.genes.RemoveGene(gene);
        }
            
        foreach (var gene in embryo.birthGenes.GenesListForReading)
        {
            pawn.genes.AddGene(gene, false);
        }
            
        foreach (var gene in embryo.PrimarchGenes.GenesListForReading)
        {
            pawn.genes.AddGene(gene, true);
        }

        pawn.genes.SetXenotypeDirect(Genes40kDefOf.BEWH_Primarch);
            
        Find.WindowStack.Add(new Dialog_ViewGenes(pawn));
            
        pawn.Destroy();
        Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
    }
        
    public static void InspectGeneseedVialGenes(GeneseedVial geneseedVial)
    {
        if (geneseedVial == null)
        {
            return;
        }
            
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist);
        pawn.ageTracker.AgeBiologicalTicks = 3600000 * 25;
            
        foreach (var gene in pawn.genes.GenesListForReading)
        {
            pawn.genes.RemoveGene(gene);
        }
            
        foreach (var gene in geneseedVial.GeneSet.GenesListForReading)
        {
            pawn.genes.AddGene(gene, true);
        }

        var xenotypeDef = XenotypeDefOf.Baseliner;

        if (geneseedVial.xenotype != null)
        {
            xenotypeDef = geneseedVial.xenotype;
        }

        pawn.genes.SetXenotypeDirect(xenotypeDef);

        CustomChapterGeneUtility.AddChapterGeneFromVial(pawn, geneseedVial, false);
            
        Find.WindowStack.Add(new Dialog_ViewGenes(pawn));
            
        pawn.Destroy();
        Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
    }

    /// <summary>
    /// The primarch line a vial represents: its custom design name, the gene its material granted, or its xenotype.
    /// </summary>
    public static string PrimarchLineLabel(GeneseedVial geneseedVial)
    {
        if (geneseedVial == null)
        {
            return null;
        }

        var customChapter = geneseedVial.CustomChapter;

        if (customChapter != null && !customChapter.name.NullOrEmpty())
        {
            return customChapter.name;
        }

        if (geneseedVial.extraGeneFromMaterial != null)
        {
            return geneseedVial.extraGeneFromMaterial.LabelCap;
        }

        if (geneseedVial.xenotype != null)
        {
            return geneseedVial.xenotype.LabelCap;
        }

        return geneseedVial.LabelCap;
    }

    /// <summary>
    /// Shows the gene card of the primarch embryo the given vial and human embryo would produce, without making one.
    /// </summary>
    public static void InspectProspectivePrimarchGenes(GeneseedVial geneseedVial, HumanEmbryo humanEmbryo)
    {
        if (geneseedVial == null)
        {
            return;
        }

        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist);
        pawn.ageTracker.AgeBiologicalTicks = 3600000 * 25;

        foreach (var gene in pawn.genes.GenesListForReading.ToList())
        {
            pawn.genes.RemoveGene(gene);
        }

        if (humanEmbryo?.GeneSet != null)
        {
            foreach (var gene in humanEmbryo.GeneSet.GenesListForReading)
            {
                pawn.genes.AddGene(gene, false);
            }
        }

        if (geneseedVial.GeneSet != null)
        {
            foreach (var gene in geneseedVial.GeneSet.GenesListForReading)
            {
                pawn.genes.AddGene(gene, true);
            }
        }

        if (geneseedVial.extraGeneFromMaterial != null)
        {
            pawn.genes.AddGene(geneseedVial.extraGeneFromMaterial, true);
        }

        pawn.genes.SetXenotypeDirect(geneseedVial.xenotype ?? Genes40kDefOf.BEWH_Primarch);

        Find.WindowStack.Add(new Dialog_ViewGenes(pawn));

        pawn.Destroy();
        Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
    }

    public static int GetGeneseedImplantationSuccessChance(Pawn pawn, GeneseedVial geneseedVial)
    {
        return GetGeneseedImplantationFailChance(pawn, geneseedVial.def, geneseedVial.extraGeneFromMaterial);
    }

    /// <summary>
    /// Failure chance in percent for implanting a vial of the given def and chapter gene into the pawn, capped like the vial defines.
    /// </summary>
    public static int GetGeneseedImplantationFailChance(Pawn pawn, ThingDef vialDef, GeneDef chapterGene)
    {
        var defMod = vialDef.GetModExtension<DefModExtension_GeneseedVial>();

        var failChanceAgeOffset = 0;
        if (pawn.ageTracker.AgeBiologicalYears < defMod.minAgeImplant)
        {
            failChanceAgeOffset = defMod.minAgeImplant - pawn.ageTracker.AgeBiologicalYears;
        }
        else if (pawn.ageTracker.AgeBiologicalYears > defMod.maxAgeImplant)
        {
            failChanceAgeOffset = pawn.ageTracker.AgeBiologicalYears - defMod.maxAgeImplant;
        }
        failChanceAgeOffset *= defMod.failureChancePerAgePast;
            
        CustomChapterGeneUtility.TryGetChapterGenePurityOffsets(chapterGene, out var failChanceGeneOffset, out var failChanceCapGeneOffset, out _);
            
        var failChance = defMod.baseFailureChance;
        failChance += failChanceAgeOffset + failChanceGeneOffset;

        var failCapChance = defMod.failChanceCap;
        failCapChance += failChanceCapGeneOffset;
        
        if (ModSettings.implantationSuccessOffset != 0)
        {
            failChance += ModSettings.implantationSuccessOffset;
        }
            
        if (ModSettings.implantationCapOffset != 0)
        {
            failCapChance += ModSettings.implantationCapOffset;
        }
            
        if (failCapChance > 100)
        {
            failCapChance = 100;
        }

        if (failChance > failCapChance)
        {
            failChance = failCapChance;
        }

        return failChance;
    }

    public static string GetGeneseedImplantationSuccessChanceDesc(Pawn pawn, GeneseedVial geneseedVial)
    {
        if (geneseedVial == null)
        {
            return string.Empty;
        }

        return GetGeneseedImplantationFailChanceDesc(pawn, geneseedVial.def, geneseedVial.extraGeneFromMaterial, true, true, true);
    }

    /// <summary>
    /// The implantation failure breakdown for a vial kind. The intro sentence, chapter line and confirmation prompt are
    /// optional so the same text serves the legacy confirmation box and the vial picker's tooltip.
    /// </summary>
    public static string GetGeneseedImplantationFailChanceDesc(Pawn pawn, ThingDef vialDef, GeneDef chapterGene, bool includeIntro, bool includeChapterLine, bool includeContinuePrompt)
    {
        var defMod = vialDef.GetModExtension<DefModExtension_GeneseedVial>();
        var parts = new List<string>();
        var failChanceCausedBy = new List<string>();

        if (includeIntro)
        {
            parts.Add("BEWH.MankindsFinest.GeneseedVial.ImplantGeneseedDesc".Translate(pawn, defMod.xenotype.label));
        }

        var chapterLabel = CustomChapterGeneUtility.ChapterLabelOf(chapterGene);
        if (includeChapterLine && !chapterLabel.NullOrEmpty())
        {
            parts.Add("BEWH.MankindsFinest.GeneseedVial.ChapterMaterial".Translate(chapterLabel.CapitalizeFirst()));
        }
            
        failChanceCausedBy.Add("\t* " + "BEWH.MankindsFinest.GeneseedVial.FailureChanceCause".Translate(defMod.baseFailureChance, "BEWH.MankindsFinest.GeneseedVial.BaseFailureChance".Translate()));
            
        var failChanceAgeOffset = 0;
        if (pawn.ageTracker.AgeBiologicalYears < defMod.minAgeImplant)
        {
            failChanceAgeOffset = defMod.minAgeImplant - pawn.ageTracker.AgeBiologicalYears;
        }
        else if (pawn.ageTracker.AgeBiologicalYears > defMod.maxAgeImplant)
        {
            failChanceAgeOffset = pawn.ageTracker.AgeBiologicalYears - defMod.maxAgeImplant;
        }

        if (failChanceAgeOffset != 0)
        {
            failChanceAgeOffset *= defMod.failureChancePerAgePast;
            failChanceCausedBy.Add("\t* " + "BEWH.MankindsFinest.GeneseedVial.FailureChanceCause".Translate(failChanceAgeOffset, "BEWH.MankindsFinest.GeneseedVial.OutsideOptimalAgeRange".Translate(pawn, defMod.minAgeImplant, defMod.maxAgeImplant)));
        }

        if (CustomChapterGeneUtility.TryGetChapterGenePurityOffsets(chapterGene, out var failChanceGeneOffset, out var failChanceCapGeneOffset, out var puritySourceLabel) && failChanceGeneOffset != 0)
        {
            failChanceCausedBy.Add("\t* " + "BEWH.MankindsFinest.GeneseedVial.FailureChanceCause".Translate(failChanceGeneOffset, puritySourceLabel));
        }

        var failChance = defMod.baseFailureChance;
        failChance += failChanceGeneOffset + failChanceAgeOffset;
            
        var failCapChance = defMod.failChanceCap;
        failCapChance += failChanceCapGeneOffset;
            
        if (ModSettings.implantationSuccessOffset != 0)
        {
            failChance += ModSettings.implantationSuccessOffset;
            failChanceCausedBy.Add("\t* " + "BEWH.MankindsFinest.GeneseedVial.FailureChanceCause".Translate(ModSettings.implantationSuccessOffset, "BEWH.Framework.CommonKeywords.ModSettings".Translate()));
        }
            
        if (ModSettings.implantationCapOffset != 0)
        {
            failCapChance += ModSettings.implantationCapOffset;
        }
            
        if (failCapChance > 100)
        {
            failCapChance = 100;
        }

        var wasCapped = false;

        if (failChance > failCapChance)
        {
            failChance = failCapChance;
            wasCapped = true;
        }

        if (failChance > 0)
        {
            parts.Add("BEWH.MankindsFinest.GeneseedVial.CurrentFailureChance".Translate(failChance));
            parts.Add("BEWH.MankindsFinest.GeneseedVial.FailureChanceCausedBy".Translate() + "\n" + failChanceCausedBy.ToLineList());

            if (wasCapped)
            {
                parts.Add("BEWH.MankindsFinest.GeneseedVial.FailureChanceCapped".Translate(failCapChance));
            }
        }
        else if (!includeIntro)
        {
            parts.Add("BEWH.MankindsFinest.GeneseedVial.NoFailureChance".Translate());
        }

        if (includeContinuePrompt)
        {
            parts.Add("WouldYouLikeToContinue".Translate());
        }

        return string.Join("\n\n", parts);
    }

    private static bool? alteredCarbonActive;

    public static bool PawnHasAlteredCarbonStack(this Pawn pawn)
    {
        alteredCarbonActive ??= ModLister.GetActiveModWithIdentifier("hlx.UltratechAlteredCarbon") != null;

        if (!alteredCarbonActive.Value)
        {
            return false;
        }
        
        if (pawn.health.hediffSet.HasHediff(Genes40kDefOf.AC_NeuralStack))
        {
            return true;
        }
        
        if (pawn.health.hediffSet.HasHediff(Genes40kDefOf.AC_RemoteStack))
        {
            return true;
        }

        if (pawn.health.hediffSet.HasHediff(Genes40kDefOf.AC_ArchotechStack))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gizmo that pans the camera to the map's Sangprimus Portum, disabled when there is none.
    /// </summary>
    public static Gizmo ViewSangprimusPortumGizmo(Map map)
    {
        var sangprimusPortum = map?.listerBuildings.AllBuildingsColonistOfDef(Genes40kDefOf.BEWH_SangprimusPortum).FirstOrDefault();

        var command = new Command_Action
        {
            defaultLabel = "BEWH.MankindsFinest.Containers.ViewSangprimusPortum".Translate(),
            defaultDesc = "BEWH.MankindsFinest.Containers.ViewSangprimusPortumDesc".Translate(),
            icon = Genes40kDefOf.BEWH_SangprimusPortum.uiIcon,
            action = delegate
            {
                CameraJumper.TryJumpAndSelect(sangprimusPortum);
            }
        };

        if (sangprimusPortum == null)
        {
            command.Disable("BEWH.MankindsFinest.GeneGestator.NoSangprimus".Translate());
        }

        return command;
    }
}
