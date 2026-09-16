using RimWorld;
using Verse;

namespace Genes40k;

public class ThoughtWorker_XXTwinDead : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (p.genes == null)
        {
            return false;
        }
            
        var twinGene = p.TwinGene();
        if (twinGene?.Twin == null)
        {
            return false;
        }
        return twinGene.Twin.Dead;
    }
}