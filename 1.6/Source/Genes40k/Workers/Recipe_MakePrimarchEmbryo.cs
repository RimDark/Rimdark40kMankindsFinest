using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Genes40k;

public class Recipe_MakePrimarchEmbryo : RecipeWorker
{
    public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
    {
        var embryo = (PrimarchEmbryo)ThingMaker.MakeThing(Genes40kDefOf.BEWH_PrimarchEmbryo);

        if (ingredients.FirstOrDefault(x => x is HumanEmbryo) is not HumanEmbryo hEmbryo ||
            ingredients.FirstOrDefault(x => x is GeneseedVial) is not GeneseedVial geneseedVial)
        {
            Log.Error("[Mankind's Finest] Primarch embryo recipe ran without both a human embryo and a geneseed vial.");
            return;
        }

        //A copy, so the embryo and the vial do not end up sharing one GeneSet that both deep-scribe.
        var primarchGenes = new GeneSet();

        if (geneseedVial.GeneSet != null)
        {
            foreach (var gene in geneseedVial.GeneSet.GenesListForReading)
            {
                primarchGenes.AddGene(gene);
            }
        }

        if (geneseedVial.extraGeneFromMaterial != null)
        {
            primarchGenes.AddGene(geneseedVial.extraGeneFromMaterial);
        }
            
        embryo.Initialize(hEmbryo.Mother, null, primarchGenes, hEmbryo.GeneSet, geneseedVial.iconDef, geneseedVial.xenotype);

        if (billDoer != null)
        {
            GenPlace.TryPlaceThing(embryo, billDoer.Position, billDoer.Map, ThingPlaceMode.Direct);
        }
    }
}