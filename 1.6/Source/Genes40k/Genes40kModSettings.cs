using UnityEngine;
using Verse;

namespace Genes40k;

public class Genes40kModSettings : ModSettings
{
    public bool psychicPhenomena = true;
    public bool psykerPariahBirth = true;
    public int psykerPariahBirthChance = 10;
        
    public bool perpetualBirth = true;
    public int perpetualBirthChance = 3;

    public bool livingSaintSystem = true;
    public bool livingSaintOnlyBaselinerAscension = true;
    public bool livingSaintMale = false;
    public bool livingSaintAddedChanceFromViolenceSkills = false;
    public int livingSaintLimit = 1;
    public float livingSaintBaseChance = 1;
    public int livingSaintBigThreat = 65;
    public int livingSaintSmallThreat = 35;
        
    public int implantationSuccessOffset = 0;
    public int implantationCapOffset = 0;
    
    public int matrixGestationTimeFactor = 100;

    public IntRange chapterStabilityRange = DefaultChapterStabilityRange;
    public int chapterBaseTraitPicks = DefaultChapterBaseTraitPicks;
    public IntRange primarchComplexityRange = DefaultPrimarchComplexityRange;
    public int primarchBaseTraitPicks = DefaultPrimarchBaseTraitPicks;

    public static readonly IntRange DefaultChapterStabilityRange = new(-5, 5);
    public const int DefaultChapterBaseTraitPicks = 4;
    public static readonly IntRange DefaultPrimarchComplexityRange = new(-5, 5);
    public const int DefaultPrimarchBaseTraitPicks = 3;

    public bool psychicCrafting = true;

    public bool allowFemalePrimarchBirths = false;
        
    private ChapterColourDef currentlySelectedPreset = Genes40kDefOf.BEWH_ChapterColourXIII;
        
    public ChapterColourDef CurrentlySelectedPreset
    {
        get => currentlySelectedPreset ??= CustomPreset;
        set => currentlySelectedPreset = value;
    }

    private ChapterColourDef customPreset = null;
    public ChapterColourDef CustomPreset =>
        customPreset ??= new ChapterColourDef
        {
            defName = "BEWH_CustomChapterDef",
            label = "Custom",
            primaryColour = chapterColorOne,
            secondaryColour = chapterColorTwo,
            tertiaryColour = chapterColorThree,
            relatedChapterIcon = chapterShoulderIcon,
            chapterIconColour = chapterShoulderIconColor ?? Color.white,
            colorAmount = 3,
        };

    public Color chapterColorOne = Genes40kDefOf.BEWH_ChapterColourXIII.primaryColour;
    public Color chapterColorTwo = Genes40kDefOf.BEWH_ChapterColourXIII.secondaryColour;
    public Color chapterColorThree = Genes40kDefOf.BEWH_ChapterColourXIII.secondaryColour;
    
    public ShoulderIconDef chapterShoulderIcon = Genes40kDefOf.BEWH_ShoulderNone;
    public Color? chapterShoulderIconColor = null;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref psychicPhenomena, "psychicPhenomena", true);
        Scribe_Values.Look(ref psykerPariahBirth, "psykerPariahBirth", true);
        Scribe_Values.Look(ref psykerPariahBirthChance, "psykerPariahBirthChance", 10);
            
        Scribe_Values.Look(ref perpetualBirth, "perpetualBirth", true);
        Scribe_Values.Look(ref perpetualBirthChance, "perpetualBirthChance", 3);
            
        Scribe_Values.Look(ref livingSaintSystem, "livingSaintSystem", true);
        Scribe_Values.Look(ref livingSaintOnlyBaselinerAscension, "livingSaintOnlyBaselinerAscension", true);
        Scribe_Values.Look(ref livingSaintMale, "livingSaintMale", false);
        Scribe_Values.Look(ref livingSaintAddedChanceFromViolenceSkills, "livingSaintAddedChanceFromViolenceSkills", false);
        Scribe_Values.Look(ref livingSaintLimit, "livingSaintLimit", 1);
        Scribe_Values.Look(ref livingSaintBaseChance, "livingSaintBaseChance", 1f);
        Scribe_Values.Look(ref livingSaintBigThreat, "livingSaintBigThreat", 65);
        Scribe_Values.Look(ref livingSaintSmallThreat, "livingSaintSmallThreat", 35);
            
        Scribe_Values.Look(ref implantationSuccessOffset, "implantationSuccessOffset", 0);
        Scribe_Values.Look(ref implantationCapOffset, "implantationCapOffset", 0);
        
        Scribe_Values.Look(ref matrixGestationTimeFactor, "matrixGestationTimeFactor", 100);

        Scribe_Values.Look(ref chapterStabilityRange, "chapterStabilityRange", DefaultChapterStabilityRange);
        Scribe_Values.Look(ref chapterBaseTraitPicks, "chapterBaseTraitPicks", DefaultChapterBaseTraitPicks);
        Scribe_Values.Look(ref primarchComplexityRange, "primarchComplexityRange", DefaultPrimarchComplexityRange);
        Scribe_Values.Look(ref primarchBaseTraitPicks, "primarchBaseTraitPicks", DefaultPrimarchBaseTraitPicks);
        
        Scribe_Values.Look(ref allowFemalePrimarchBirths, "allowFemalePrimarchBirths", false);
        
        Scribe_Values.Look(ref psychicCrafting, "psychicCrafting", true);
            
        Scribe_Values.Look(ref chapterColorOne, "chapterColorOne", Genes40kDefOf.BEWH_ChapterColourXIII.primaryColour);
        Scribe_Values.Look(ref chapterColorTwo, "chapterColorTwo", Genes40kDefOf.BEWH_ChapterColourXIII.secondaryColour);
        Scribe_Values.Look(ref chapterColorThree, "chapterColorThree", Genes40kDefOf.BEWH_ChapterColourXIII.secondaryColour);
        Scribe_Defs.Look(ref chapterShoulderIcon, "chapterShoulderIcon");
        Scribe_Values.Look(ref chapterShoulderIconColor, "chapterShoulderIconColor");
        
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            if (currentlySelectedPreset == CustomPreset)
            {
                currentlySelectedPreset = null;
            }
        }
            
        Scribe_Defs.Look(ref currentlySelectedPreset, "currentlySelectedPreset");
            
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (currentlySelectedPreset != null)
            {
                return;
            }
            currentlySelectedPreset = CustomPreset;
        }
    }
}