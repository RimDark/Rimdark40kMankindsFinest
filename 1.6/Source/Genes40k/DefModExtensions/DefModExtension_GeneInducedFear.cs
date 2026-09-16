using System.Collections.Generic;
using Core40k;
using Verse;

namespace Genes40k;

public class DefModExtension_GeneInducedFear : DefModExtension
{
    public int tickInterval = 625;

    public float effectRadius = 7.9f;

    public int chanceToFear = 50;

    public List<GeneDef> genesCausesImmunityToFear = new();

    public List<TraitData> traitCausesImmunityToFear = new();
}