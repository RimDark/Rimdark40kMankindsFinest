using Verse;

namespace Genes40k;

public class DefModExtension_GeneseedVialRecipe : DefModExtension
{   
    public ThingDef geneseedVial = null;
    public GeneDef geneFromMaterial = null;
    public bool matchesCustomChapterGenes = false;
    public bool selectsVialKind = false;

    /// <summary>
    /// Whether this recipe accepts the vial: an exact chapter gene match, or additionally any runtime-generated design gene when flagged.
    /// </summary>
    public bool MatchesVial(GeneseedVial geneseedVial)
    {
        if (geneseedVial == null)
        {
            return false;
        }

        if (matchesCustomChapterGenes && CustomChapterGeneDefBuilder.IsGeneratedChapterGene(geneseedVial.extraGeneFromMaterial))
        {
            return true;
        }

        return geneFromMaterial == geneseedVial.extraGeneFromMaterial;
    }
}