using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Genes40k;

/// <summary>
/// One row of the gene-seed implantation picker: a vial def + chapter gene, the vials of that kind on the map,
/// and what is still missing to gestate it when there are none.
/// </summary>
public class GeneseedVialKind
{
    public ThingDef vialDef;
    public GeneDef chapterGene;
    public ThingDef matrixDef;
    public ThingDef chapterMaterial;
    public CustomChapterGene customChapter;
    public List<GeneseedVial> vials = [];
    public List<ResearchProjectDef> missingResearch = [];
    public bool missingMaterial;

    public bool Available => vials.Count > 0;
    public bool Locked => !Available && (missingResearch.Any() || missingMaterial);
    public bool Gestatable => !Available && !Locked;

    public string Label => GeneseedVialKindUtility.KindLabel(vialDef, chapterGene);
}

public static class GeneseedVialKindUtility
{
    private static List<ThingDef> pickerVialDefs;
    public static List<ThingDef> PickerVialDefs => pickerVialDefs ??= DefDatabase<ThingDef>.AllDefsListForReading
        .Where(def => typeof(GeneseedVial).IsAssignableFrom(def.thingClass) && def.GetModExtension<DefModExtension_GeneseedVial>() is { usesImplantPicker: true, xenotype: not null })
        .ToList();

    private static List<ThingDef> matrixDefs;
    private static List<ThingDef> MatrixDefs => matrixDefs ??= DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.HasModExtension<DefModExtension_GeneMatrix>()).ToList();

    private static List<ThingDef> ChapterMaterials => ChapterChoiceUtility.ChapterMaterials;

    public static string KindLabel(ThingDef vialDef, GeneDef chapterGene)
    {
        var xenotype = vialDef?.GetModExtension<DefModExtension_GeneseedVial>()?.xenotype;
        string xenotypeLabel = string.Empty;

        if (xenotype != null)
        {
            xenotypeLabel = xenotype.LabelCap;
        }
        else if (vialDef != null)
        {
            xenotypeLabel = vialDef.LabelCap;
        }

        var chapterLabel = CustomChapterGeneUtility.ChapterLabelOf(chapterGene);

        return chapterLabel.NullOrEmpty()
            ? "BEWH.MankindsFinest.ImplantGeneseed.KindChapterless".Translate(xenotypeLabel)
            : "BEWH.MankindsFinest.ImplantGeneseed.KindWithChapter".Translate(xenotypeLabel, chapterLabel.CapitalizeFirst());
    }

    public static int RequiredMedicineSkill(ThingDef vialDef)
    {
        return vialDef?.GetModExtension<DefModExtension_GeneseedVial>()?.implantMedicineSkill ?? 0;
    }

    public static bool PawnMeetsSkill(Pawn pawn, ThingDef vialDef)
    {
        var required = RequiredMedicineSkill(vialDef);
        return required <= 0 || (pawn?.skills != null && pawn.skills.GetSkill(SkillDefOf.Medicine).Level >= required);
    }

    /// <summary>
    /// Every vial kind the picker offers: the kinds present on the map plus, for each picker vial type,
    /// the chapterless kind, one kind per legion material and one per custom chapter, marked as gestatable or locked.
    /// </summary>
    public static List<GeneseedVialKind> KindsFor(Map map)
    {
        var kinds = new List<GeneseedVialKind>();
        var unlockedMaterials = GameComponent_UnlockedMaterials.Instance;
        var customChapters = GameComponent_CustomChapterGenes.Instance?.DesignsOf(CustomGeneKind.Chapter) ?? new List<CustomChapterGene>();
        var custodesDesigns = GameComponent_CustomChapterGenes.Instance?.DesignsOf(CustomGeneKind.Custodes) ?? new List<CustomChapterGene>();

        foreach (var vialDef in PickerVialDefs)
        {
            var matrixDef = MatrixDefs.FirstOrDefault(def => def.GetModExtension<DefModExtension_GeneMatrix>().makesGeneVial == vialDef);
            var matrixDefMod = matrixDef?.GetModExtension<DefModExtension_GeneMatrix>();
            var research = new List<ResearchProjectDef>();

            if (matrixDefMod?.researchNeeded is { IsFinished: false })
            {
                research.Add(matrixDefMod.researchNeeded);
            }

            kinds.Add(new GeneseedVialKind { vialDef = vialDef, matrixDef = matrixDef, missingResearch = research.ToList() });

            if (matrixDefMod != null && matrixDefMod.usesDisciplineCustomization)
            {
                foreach (var design in custodesDesigns)
                {
                    kinds.Add(new GeneseedVialKind
                    {
                        vialDef = vialDef,
                        chapterGene = design.GeneDef,
                        matrixDef = matrixDef,
                        customChapter = design,
                        missingResearch = research.ToList()
                    });
                }
            }

            if (matrixDefMod == null || !matrixDefMod.canUseChapterMaterial)
            {
                continue;
            }

            foreach (var material in ChapterMaterials)
            {
                kinds.Add(new GeneseedVialKind
                {
                    vialDef = vialDef,
                    chapterGene = material.GetModExtension<DefModExtension_GeneFromMaterial>().addedGene,
                    matrixDef = matrixDef,
                    chapterMaterial = material,
                    missingResearch = research.ToList(),
                    missingMaterial = unlockedMaterials == null || !unlockedMaterials.HasMaterial(material)
                });
            }

            foreach (var chapter in customChapters)
            {
                kinds.Add(new GeneseedVialKind
                {
                    vialDef = vialDef,
                    chapterGene = chapter.GeneDef,
                    matrixDef = matrixDef,
                    customChapter = chapter,
                    missingResearch = research.ToList()
                });
            }
        }

        if (map == null)
        {
            return kinds;
        }

        foreach (var vialDef in PickerVialDefs)
        {
            foreach (var thing in map.listerThings.ThingsOfDef(vialDef))
            {
                if (thing is not GeneseedVial vial || vial.IsForbidden(Faction.OfPlayer) || vial.Position.Fogged(map))
                {
                    continue;
                }

                var kind = kinds.FirstOrDefault(candidate => candidate.vialDef == vial.def && candidate.chapterGene == vial.extraGeneFromMaterial);

                if (kind == null)
                {
                    kind = new GeneseedVialKind { vialDef = vial.def, chapterGene = vial.extraGeneFromMaterial };
                    kinds.Add(kind);
                }

                kind.vials.Add(vial);
            }
        }

        return kinds;
    }

    public static bool UsesPicker(ThingDef vialDef)
    {
        return vialDef != null && PickerVialDefs.Contains(vialDef);
    }

    public static bool AnyImplantableVialOn(Map map)
    {
        return map != null && PickerVialDefs.Any(vialDef => map.listerThings.ThingsOfDef(vialDef).Any(thing => thing is GeneseedVial vial && !vial.IsForbidden(Faction.OfPlayer) && !vial.Position.Fogged(map)));
    }

    /// <summary>
    /// Creates the kind-pinned implantation bill on the patient, with the same courtesy messages vanilla's CreateSurgeryBill sends.
    /// </summary>
    public static Bill_GeneseedImplant CreateBill(Pawn medPawn, RecipeDef recipe, BodyPartRecord part, GeneseedVialKind kind)
    {
        var bill = new Bill_GeneseedImplant(recipe, kind.vialDef, kind.chapterGene);
        medPawn.BillStack.AddBill(bill);
        bill.Part = part;

        var map = medPawn.MapHeld;

        if (map == null)
        {
            return bill;
        }

        var colonists = map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(pawn => pawn.IsFreeColonist || pawn.IsColonyMechPlayerControlled).ToList();

        if (!colonists.Any(colonist => recipe.PawnSatisfiesSkillRequirements(colonist) && PawnMeetsSkill(colonist, kind.vialDef)))
        {
            Messages.Message("BEWH.MankindsFinest.ImplantGeneseed.NoPawnWithSkill".Translate(RequiredMedicineSkill(kind.vialDef)), medPawn, MessageTypeDefOf.CautionInput, false);
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
