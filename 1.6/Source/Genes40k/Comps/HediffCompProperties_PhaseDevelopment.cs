using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Genes40k;

public class HediffCompProperties_PhaseDevelopment : HediffCompProperties
{
    public HediffDef nextPhase = null;

    public List<GeneDef> addsGenes = new();

    public string letterLabel = null;

    public string letterText = null;

    public LetterDef letterDef = null;

    public HediffCompProperties_PhaseDevelopment()
    {
        compClass = typeof(HediffComp_PhaseDevelopment);
    }
}
