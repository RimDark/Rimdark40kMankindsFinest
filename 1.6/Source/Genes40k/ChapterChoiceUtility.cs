using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Genes40k;

/// <summary>
/// One row of the chapter application picker: a chapter gene coming either from a legion material or from a
/// player-made design, and whether its material is still locked.
/// </summary>
public class ChapterChoice
{
    public GeneDef chapterGene;
    public ThingDef chapterMaterial;
    public CustomChapterGene customChapter;
    public bool missingMaterial;

    public bool Available => !missingMaterial;

    public string Label => ChapterChoiceUtility.ChoiceLabel(this);
}

public static class ChapterChoiceUtility
{
    private static List<ThingDef> chapterMaterials;

    /// <summary>
    /// Every legion material that carries a chapter gene, in legion order.
    /// </summary>
    public static List<ThingDef> ChapterMaterials => chapterMaterials ??= DefDatabase<ThingDef>.AllDefsListForReading
        .Where(def => def.HasModExtension<DefModExtension_ChapterMaterial>() && def.GetModExtension<DefModExtension_GeneFromMaterial>()?.addedGene != null)
        .OrderBy(def => def.GetModExtension<DefModExtension_ChapterMaterial>().orderInt)
        .ToList();

    private static bool? genericChapterRecipeExists;

    public static bool GenericChapterRecipeExists => genericChapterRecipeExists ??= DefDatabase<RecipeDef>.AllDefsListForReading
        .Any(recipeDef => recipeDef.GetModExtension<DefModExtension_LegionMaterialCreation>()?.selectsChapter == true);

    public static string MaterialLabel(ThingDef material)
    {
        var shownName = material?.GetModExtension<DefModExtension_BaseMaterial>()?.shownMaterialName;
        return shownName.NullOrEmpty() ? material?.label : shownName;
    }

    public static string ChoiceLabel(ChapterChoice choice)
    {
        if (choice?.customChapter != null)
        {
            return choice.customChapter.name;
        }

        var chapterLabel = CustomChapterGeneUtility.ChapterLabelOf(choice?.chapterGene);
        return chapterLabel.NullOrEmpty() ? MaterialLabel(choice?.chapterMaterial) : chapterLabel.CapitalizeFirst();
    }

    /// <summary>
    /// Every chapter the picker offers: one per legion material, marked as locked while the material is not unlocked,
    /// followed by every player-made chapter design.
    /// </summary>
    public static List<ChapterChoice> Choices()
    {
        var choices = new List<ChapterChoice>();
        var unlockedMaterials = GameComponent_UnlockedMaterials.Instance;

        foreach (var material in ChapterMaterials)
        {
            choices.Add(new ChapterChoice
            {
                chapterGene = material.GetModExtension<DefModExtension_GeneFromMaterial>().addedGene,
                chapterMaterial = material,
                missingMaterial = unlockedMaterials == null || !unlockedMaterials.HasMaterial(material)
            });
        }

        var customChapters = GameComponent_CustomChapterGenes.Instance?.DesignsOf(CustomGeneKind.Chapter) ?? new List<CustomChapterGene>();

        foreach (var chapter in customChapters)
        {
            choices.Add(new ChapterChoice
            {
                chapterGene = chapter.GeneDef,
                customChapter = chapter
            });
        }

        return choices;
    }

    public static bool AnyChoice()
    {
        return ChapterMaterials.Any() || (GameComponent_CustomChapterGenes.Instance?.DesignsOf(CustomGeneKind.Chapter).Any() ?? false);
    }

    /// <summary>
    /// Creates the chapter-pinned bill on the patient, with the same courtesy messages vanilla's CreateSurgeryBill sends.
    /// </summary>
    public static Bill_ApplyChapter CreateBill(Pawn medPawn, RecipeDef recipe, BodyPartRecord part, ChapterChoice choice)
    {
        var bill = new Bill_ApplyChapter(recipe, choice.chapterGene);
        medPawn.BillStack.AddBill(bill);
        bill.Part = part;

        var map = medPawn.MapHeld;

        if (map == null)
        {
            return bill;
        }

        var colonists = map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(pawn => pawn.IsFreeColonist || pawn.IsColonyMechPlayerControlled).ToList();

        if (!colonists.Any(colonist => recipe.PawnSatisfiesSkillRequirements(colonist)))
        {
            Bill.CreateNoPawnsWithSkillDialog(recipe);
        }

        if (!medPawn.InBed() && medPawn.RaceProps.IsFlesh && medPawn.RaceProps.Humanlike && !map.listerBuildings.allBuildingsColonist.Any(building => building is Building_Bed bed && RestUtility.CanUseBedEver(medPawn, bed.def) && bed.Medical))
        {
            Messages.Message("MessageNoMedicalBeds".Translate(), medPawn, MessageTypeDefOf.CautionInput, false);
        }

        if (medPawn.Faction != null && !medPawn.Faction.Hidden && !medPawn.Faction.HostileTo(Faction.OfPlayer) && recipe.Worker.IsViolationOnPawn(medPawn, part, Faction.OfPlayer))
        {
            Messages.Message("MessageMedicalOperationWillAngerFaction".Translate(medPawn.HomeFaction), medPawn, MessageTypeDefOf.CautionInput, false);
        }

        return bill;
    }
}
