using Verse;

namespace Genes40k;

public class Gene_TwinConnected : Gene, ITwinGene
{
    private Pawn twin = null;
    public Pawn Twin => twin;

    public bool TwinCapable => true;
        
    public void SetTwin(Pawn twinPawn)
    {
        twin = twinPawn;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref twin, "twin");
    }
}