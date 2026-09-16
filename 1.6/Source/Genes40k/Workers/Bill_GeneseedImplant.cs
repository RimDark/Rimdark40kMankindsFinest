using RimWorld;
using Verse;

namespace Genes40k;

/// <summary>
/// Medical bill for the generic gene-seed implantation recipe, pinned to one vial kind (vial def + chapter gene).
/// Supplies the kind's work amount and medicine requirement so a single RecipeDef covers every vial type.
/// </summary>
public class Bill_GeneseedImplant : Bill_Medical
{
    public ThingDef vialDef;
    public GeneDef chapterGene;

    private DefModExtension_GeneseedVial VialDefMod => vialDef?.GetModExtension<DefModExtension_GeneseedVial>();

    public Bill_GeneseedImplant()
    {
    }

    public Bill_GeneseedImplant(RecipeDef recipe, ThingDef vialDef, GeneDef chapterGene) : base(recipe, null)
    {
        this.vialDef = vialDef;
        this.chapterGene = chapterGene;
    }

    public bool Matches(GeneseedVial geneseedVial)
    {
        return geneseedVial != null && geneseedVial.def == vialDef && geneseedVial.extraGeneFromMaterial == chapterGene;
    }

    public override string Label => "BEWH.MankindsFinest.ImplantGeneseed.BillLabel".Translate(GeneseedVialKindUtility.KindLabel(vialDef, chapterGene));

    public override float GetWorkAmount(Thing thing = null)
    {
        var workAmount = VialDefMod?.implantWorkAmount ?? 0;
        return workAmount > 0 ? workAmount : base.GetWorkAmount(thing);
    }

    public override bool PawnAllowedToStartAnew(Pawn p)
    {
        return base.PawnAllowedToStartAnew(p) && GeneseedVialKindUtility.PawnMeetsSkill(p, vialDef);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref vialDef, "vialDef");
        Scribe_Defs.Look(ref chapterGene, "chapterGene");
    }
}
