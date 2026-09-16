using System;
using Core40k;
using RimWorld;
using Verse;

namespace Genes40k;

[DefOf]
public static class Genes40kDefOf
{
    //Thunder warrior genes
    public static GeneDef BEWH_ProtoOssmodula;
    public static GeneDef BEWH_Musculeator;
    public static GeneDef BEWH_Mentanifex;
    public static GeneDef BEWH_Vigoranis;
    public static GeneDef BEWH_Hyperanatomica;
    public static GeneDef BEWH_Furybound;

    //Custom chapter gene template and default flag icon
    public static CustomChapterGeneTemplateDef BEWH_CustomChapterGene;
    public static CustomChapterGeneTemplateDef BEWH_CustomPrimarchGene;
    public static CustomChapterGeneTemplateDef BEWH_CustomCustodesGene;
    public static FlagIconDef BEWH_FlagAquila;

    //Space marine genes
    public static GeneDef BEWH_SecondaryHeart;
    public static GeneDef BEWH_Ossmodula;
    public static GeneDef BEWH_Biscopea;
    public static GeneDef BEWH_Haemastamen;
    public static GeneDef BEWH_LarramansOrgan;
    public static GeneDef BEWH_CatalepseanNode;
    public static GeneDef BEWH_Preomnor;
    public static GeneDef BEWH_Omophagea;
    public static GeneDef BEWH_MultiLung;
    public static GeneDef BEWH_Occulobe;
    public static GeneDef BEWH_LymansEar;
    public static GeneDef BEWH_SusAnMembrane;
    public static GeneDef BEWH_Melanochrome;
    public static GeneDef BEWH_OoliticKidney;
    public static GeneDef BEWH_Neuroglottis;
    public static GeneDef BEWH_Mucranoid;
    public static GeneDef BEWH_BetchersGland;
    public static GeneDef BEWH_ProgenoidGlands;
    public static GeneDef BEWH_BlackCarapace;

    //Primaris genes
    public static GeneDef BEWH_SinewCoil;
    public static GeneDef BEWH_Magnificat;
    public static GeneDef BEWH_BelisarianFurnace;

    //Custodes genes
    public static GeneDef BEWH_ImmunisLeucocyte;
    public static GeneDef BEWH_AthanaticVitae;
    public static GeneDef BEWH_FulguriteNervePlexus;
    public static GeneDef BEWH_AtlasMorphogen;
    public static GeneDef BEWH_MnemosyneMindshield;
    public static GeneDef BEWH_FulgurVitaliumstrand;

    //Priamrch genes
    public static GeneDef BEWH_ImmortisGland;
    public static GeneDef BEWH_TempestusOcularium;
    public static GeneDef BEWH_ThalaxCortex;
    public static GeneDef BEWH_HelixomeArray;
    public static GeneDef BEWH_VermillionCache;
    public static GeneDef BEWH_CelerityNexus;
    public static GeneDef BEWH_HyperionMuscleStrands;

    public static GeneDef BEWH_PrimarchSpecificGeneXX;

    //Psyker genes
    public static GeneDef BEWH_IotaPsyker;
    public static GeneDef BEWH_EpsilonPsyker;
    public static GeneDef BEWH_DeltaPsyker;
    public static GeneDef BEWH_BetaPsyker;
    public static GeneDef BEWH_AlphaPsyker;
        
    //Pariah genes
    public static GeneDef BEWH_SigmaPariah;
    public static GeneDef BEWH_UpsilonPariah;
    public static GeneDef BEWH_OmegaPariah;
        
    //Perpetual genes
    public static GeneDef BEWH_PerpetualGamma;
    public static GeneDef BEWH_PerpetualBeta;
    public static GeneDef BEWH_PerpetualAlpha;

    //Living saint genes
    public static GeneDef BEWH_LivingSaintBeingOfFaith;
    public static GeneDef BEWH_LivingSaintDivineGrace;
    public static GeneDef BEWH_LivingSaintDivineFlight;
    public static GeneDef BEWH_LivingSaintSacredRegeneration;
    public static GeneDef BEWH_LivingSaintFuryOfTheEmperor;
    public static GeneDef BEWH_LivingSaintMartyrsEndurance;
    public static GeneDef BEWH_LivingSaintHolyRadiance;

    //Xenotype
    public static XenotypeDef BEWH_LivingSaint;
    public static XenotypeDef BEWH_PrimarisSpaceMarine;
    public static XenotypeDef BEWH_Primarch;
        
    //XenotypeIcon
    public static XenotypeIconDef BEWH_AstartesIcon;
    public static XenotypeIconDef BEWH_PrimarisIcon;

    //Researchprojects
    public static ResearchProjectDef BEWH_GeneseedExtractionFirstborn;
    public static ResearchProjectDef BEWH_GeneseedExtractionPrimaris;

    //Heddifs
    public static HediffDef BEWH_PsychicComa;
    public static HediffDef BEWH_PsychicConnectionSevered;
    public static HediffDef BEWH_PsychicCrafting;
    public static HediffDef BEWH_DeniedWitch;

    public static HediffDef BEWH_PariahEffecter;
    public static HediffDef BEWH_PariahEffecterEnemies;
        
    public static HediffDef BEWH_FirstbornPhaseOne;
    public static HediffDef BEWH_FirstbornPhaseTwo;
    public static HediffDef BEWH_FirstbornPhaseThree;
        
    public static HediffDef BEWH_PrimarisPhaseOne;
    public static HediffDef BEWH_PrimarisPhaseTwo;
    public static HediffDef BEWH_PrimarisPhaseThree;
        
    public static HediffDef BEWH_DivineGraceFading;
    public static HediffDef BEWH_LivingSaintHolyAscension;
        
    public static HediffDef BEWH_SerfBuff;

    //Letters
    public static LetterDef BEWH_NaturalBornX;
    public static LetterDef BEWH_GoldenPositive;

    //DamageDefs
    public static DamageDef BEWH_WarpEnergy;

    //WeatherDefs
    public static WeatherDef BEWH_BloodRain;

    //ThingDefs
    public static ThingDef BEWH_GeneseedGestator;
    public static ThingDef BEWH_SangprimusPortum;
    public static ThingDef BEWH_PrimarchGrowthVat;
    
    public static ThingDef BEWH_GeneseedVialFirstborn;
    public static ThingDef BEWH_GeneseedVialPrimaris;
    public static ThingDef BEWH_GeneseedVialPrimarch;
    public static ThingDef BEWH_GeneseedVialCustodes;

    public static ThingDef BEWH_PrimarchEmbryo;

    public static ThingDef BEWH_RaisedWall;
    public static ThingDef BEWH_RaisedBarricade;
    public static ThingDef BEWH_RaisedTurret;
        
    public static ThingDef BEWH_RawGestationalSlurry;
        
    public static ThingDef BEWH_LSaintSword;
    public static ThingDef BEWH_LSaintBoltPistol;
    public static ThingDef BEWH_LivingSaintArmor;

    //JobDefs
    public static JobDef BEWH_CarryMatrixToGeneGestator;
    public static JobDef BEWH_CarryPrimarchEmbryoToVat;
    public static JobDef BEWH_CarryMaterialToSangprimus;
    public static JobDef BEWH_InducedFearJob;
    public static JobDef BEWH_WaitLegionHeist;

    //ThoughtDefs
    public static ThoughtDef BEWH_LivingSaintHolyRadianceThought;
    public static ThoughtDef BEWH_PrimarchSpecificXIIMood;
    public static ThoughtDef BEWH_ChaplainInspired;

    //RecipeDefs
    public static RecipeDef BEWH_RubiconSurgery;
    public static RecipeDef BEWH_MakePrimarchEmbryo;
        
    //MentalStateDefs
    public static MentalStateDef BEWH_InducedFear;
        
    //TraitDefs
    public static TraitDef PsychicSensitivity;
    public static TraitDef BEWH_Serf;
        
    //ResearchDefs
    public static ResearchProjectDef BEWH_StasisResurrection;
        
    //RankCategoryDefs
    public static RankCategoryDef BEWH_AstartesRankCategory;
        
    //ShoulderIconDefs
    public static ShoulderIconDef BEWH_ShoulderNone;
    public static ShoulderIconDef BEWH_FollowRankUp;
        
    //PawnKindDefs
    public static PawnKindDef BEWH_FirstbornPawn;
    public static PawnKindDef BEWH_FirstbornPawnLegionMaterialHeist;
        
    //FactionDefs
    public static FactionDef BEWH_OffworldMarinesFaction;
        
    //AbilityCategoryDef
    [MayRequireRoyalty]
    public static AbilityCategoryDef Psychic;

    //ChapterColourDef
    public static ChapterColourDef BEWH_ChapterColourXIII;
        
    //BodyPartDef
    public static BodyPartDef Kidney;
    
    //StatDefs
    public static StatDef BEWH_JoyFromArtFactor;
    
    //AbilityDefs
    [Obsolete]
    public static AbilityDef BEWH_WarpShield;
    
    //Altered Carbon Stuff
    [MayRequireAlteredCarbon]
    public static HediffDef AC_NeuralStack;
    [MayRequireAlteredCarbon]
    public static HediffDef AC_RemoteStack;
    [MayRequireAlteredCarbon]
    public static HediffDef AC_ArchotechStack;
    
    static Genes40kDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(Genes40kDefOf));
    }
}