using RimWorld;
using Verse;

namespace Genes40k;

/// <summary>
/// Production bill for the primarch embryo recipe, pinned to the exact ascendance vial and human embryo chosen in the
/// crafting dialog so an interrupted craft can never be resumed with different ingredients.
/// </summary>
public class Bill_PrimarchEmbryo : Bill_ProductionWithUft
{
    public GeneseedVial pinnedVial;
    public HumanEmbryo pinnedEmbryo;

    private bool PinnedIngredientsLost => pinnedVial is not { Destroyed: false } || pinnedEmbryo is not { Destroyed: false };

    public Bill_PrimarchEmbryo()
    {
    }

    public Bill_PrimarchEmbryo(RecipeDef recipe, GeneseedVial pinnedVial, HumanEmbryo pinnedEmbryo) : base(recipe)
    {
        this.pinnedVial = pinnedVial;
        this.pinnedEmbryo = pinnedEmbryo;
    }

    public override string Label
    {
        get
        {
            var lineLabel = Genes40kUtils.PrimarchLineLabel(pinnedVial);

            if (lineLabel.NullOrEmpty())
            {
                return base.Label;
            }

            return "BEWH.MankindsFinest.GeneManupulationTable.BillLabel".Translate(lineLabel);
        }
    }

    public override bool CompletableEver => !PinnedIngredientsLost && base.CompletableEver;

    protected override bool CanCopy => false;

    public override bool IsFixedOrAllowedIngredient(Thing thing)
    {
        return thing == pinnedVial || thing == pinnedEmbryo;
    }

    public override bool ShouldDoNow()
    {
        return !PinnedIngredientsLost && base.ShouldDoNow();
    }

    public override Bill Clone()
    {
        var clone = (Bill_PrimarchEmbryo)base.Clone();
        clone.pinnedVial = pinnedVial;
        clone.pinnedEmbryo = pinnedEmbryo;
        return clone;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref pinnedVial, "pinnedVial");
        Scribe_References.Look(ref pinnedEmbryo, "pinnedEmbryo");
    }
}
