using Verse;

namespace Genes40k;

public class DefModExtension_GeneMatrix : DefModExtension
{
    public int ticksToGestate;
    public ThingDef makesGeneVial;
    public ResearchProjectDef researchNeeded;

    public bool canUseChapterMaterial = false;
    public bool canUsePrimarchMaterial = false;
    public bool usesDisciplineCustomization = false;
}