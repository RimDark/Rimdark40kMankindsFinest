using Verse;

namespace Genes40k;

public class CustodesDisciplineLevel : IExposable
{
    public CustodesDisciplineDef def;
    public int level;

    public CustodesDisciplineLevel()
    {
    }

    public CustodesDisciplineLevel(CustodesDisciplineDef def, int level)
    {
        this.def = def;
        this.level = level;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref level, "level", 0);
    }
}
